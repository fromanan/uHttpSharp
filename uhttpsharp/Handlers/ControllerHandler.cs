using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using uhttpsharp.Attributes;
using uhttpsharp.Controllers;
using uhttpsharp.ModelBinders;

namespace uhttpsharp.Handlers
{
    /// <summary>
    /// Need some kind of way to prevent default behavior of controller that inherits a base controller...
    /// since we are not using virtual methods 
    /// </summary>
    public class ControllerHandler : IHttpRequestHandler
    {
        private sealed class ControllerMethod
        {
            public ControllerMethod(Type controllerType, HttpMethods method)
            {
                ControllerType = controllerType;
                Method = method;
            }

            public Type ControllerType { get; }

            public HttpMethods Method { get; }

            private bool Equals(ControllerMethod other)
            {
                return ControllerType == other.ControllerType && Method == other.Method;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is ControllerMethod method && Equals(method);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (ControllerType.GetHashCode() * 397) ^ (int)Method;
                }
            }
        }

        private sealed class ControllerRoute
        {
            private readonly Type controllerType;
            private readonly string propertyName;
            private readonly IEqualityComparer<string> propertyNameComparer;

            public ControllerRoute(Type controllerType, string propertyName,
                IEqualityComparer<string> propertyNameComparer)
            {
                this.controllerType = controllerType;
                this.propertyName = propertyName;
                this.propertyNameComparer = propertyNameComparer;
            }

            private bool Equals(ControllerRoute other)
            {
                return other != null
                       && controllerType == other.controllerType
                       && propertyNameComparer.Equals(propertyName, other.propertyName);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return ReferenceEquals(this, obj) || Equals(obj as ControllerRoute);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((controllerType != null ? controllerType.GetHashCode() : 0) * 397) ^
                           (propertyName != null ? propertyNameComparer.GetHashCode(propertyName) : 0);
                }
            }
        }

        private static readonly IDictionary<ControllerMethod, ControllerFunction> ControllerFunctions =
            new Dictionary<ControllerMethod, ControllerFunction>();

        private static readonly IDictionary<ControllerRoute, Func<IController, IController>> Routes =
            new Dictionary<ControllerRoute, Func<IController, IController>>();

        private static readonly IDictionary<Type, Func<IHttpContext, IController, string, Task<IController>>[]>
            IndexerRoutes =
                new Dictionary<Type, Func<IHttpContext, IController, string, Task<IController>>[]>();

        private static readonly ICollection<Type> LoadedControllerRoutes = new HashSet<Type>();

        private static readonly object SyncRoot = new();

        public delegate Task<IControllerResponse> ControllerFunction(IHttpContext context, IModelBinder binder,
            IController controller);

        private readonly IController controller;

        private readonly IView view;

        private readonly IEqualityComparer<string> propertyNameComparer;

        public ControllerHandler(IController controller, IModelBinder modelBinder, IView view)
            : this(controller, modelBinder, view, StringComparer.CurrentCulture)
        {
        }

        public ControllerHandler(IController controller, IModelBinder modelBinder, IView view,
            IEqualityComparer<string> propertyNameComparer)
        {
            this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
            ModelBinder = modelBinder ?? throw new ArgumentNullException(nameof(modelBinder));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.propertyNameComparer =
                propertyNameComparer ?? throw new ArgumentNullException(nameof(propertyNameComparer));
        }

        protected virtual IModelBinder ModelBinder { get; }

        private static Func<IController, IController> GenerateRouteFunction(MethodInfo getter)
        {
            if (getter.DeclaringType == null)
                throw new ArgumentException("Cannot generate route function for static method.");

            ParameterExpression instance = Expression.Parameter(typeof(IController), "instance");

            return Expression
                .Lambda<Func<IController, IController>>(
                    Expression.Call(Expression.Convert(instance, getter.DeclaringType), getter), instance).Compile();
        }

        private static void LoadRoutes(Type controllerType, IEqualityComparer<string> propertyNameComparer)
        {
            if (LoadedControllerRoutes.Contains(controllerType)) return;

            lock (SyncRoot)
            {
                if (LoadedControllerRoutes.Contains(controllerType)) return;

                foreach (PropertyInfo prop in controllerType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                             .Where(p => p.PropertyType == typeof(IController)))
                {
                    Routes.Add(new ControllerRoute(controllerType, prop.Name, propertyNameComparer),
                        GenerateRouteFunction(prop.GetMethod));
                }

                // Indexers
                List<MethodInfo> methods = controllerType.GetMethods()
                    .Where(m => Attribute.IsDefined(m, typeof(IndexerAttribute)))
                    .OrderBy(m => m.GetCustomAttribute<IndexerAttribute>()?.Precedence).ToList();

                if (methods.Select(m => m.GetCustomAttribute<IndexerAttribute>()?.Precedence)
                    .GroupBy(c => c)
                    .Any(c => c.Count() > 1))
                {
                    throw new ArgumentException($"Controller {controllerType}" +
                                                " Has more then two indexer functions with the same precedence, Please set precedence.");
                }

                if (methods.Count > 0)
                {
                    IndexerRoutes.Add(controllerType,
                        methods.Select(m => ClassRouter.CreateIndexerFunction<IController>(controllerType, m))
                            .ToArray());
                }

                LoadedControllerRoutes.Add(controllerType);
            }
        }

        public async Task Handle(IHttpContext context, Func<Task> next)
        {
            // I/O Bound?
            IController controller =
                await GetController(context.Request.RequestParameters, context).ConfigureAwait(false);

            if (controller == null)
            {
                await next().ConfigureAwait(false);
                return;
            }

            IControllerResponse response = await controller.Pipeline.Go(() => CallMethod(context, controller), context)
                .ConfigureAwait(false);
            context.Response = await response.Respond(context, view).ConfigureAwait(false);
        }

        private async Task<IController> GetController(IEnumerable<string> requestParameters, IHttpContext context)
        {
            IController current = controller;
            foreach (string parameter in requestParameters)
            {
                Type controllerType = current.GetType();

                LoadRoutes(controllerType, propertyNameComparer);

                ControllerRoute route = new(controllerType, parameter, propertyNameComparer);

                if (Routes.TryGetValue(route, out Func<IController, IController> routeFunction))
                {
                    current = routeFunction(current);
                    continue;
                }

                // Try find indexer.
                current = await TryGetIndexerValue(controllerType, context, current, parameter).ConfigureAwait(false);

                if (current != null)
                {
                    continue;
                }

                return null;
            }

            return current;
        }

        private static async Task<IController> TryGetIndexerValue(Type controllerType, IHttpContext context,
            IController current,
            string parameter)
        {
            if (!IndexerRoutes.TryGetValue(controllerType,
                    out Func<IHttpContext, IController, string, Task<IController>>[] indexerFunctions)) return null;

            foreach (Func<IHttpContext, IController, string, Task<IController>> indexerFunction in indexerFunctions)
            {
                Task<IController> returnedTask = indexerFunction(context, current, parameter);

                if (returnedTask != null) return await returnedTask.ConfigureAwait(false);

                // TODO: Logger.Info
                //Console.WriteLine("Returned task from indexer function was null. It may happen when we cannot convert from string to wanted type.");
            }

            return null;
        }

        private Task<IControllerResponse> CallMethod(IHttpContext context, IController controller)
        {
            ControllerMethod controllerMethod = new(controller.GetType(), context.Request.Method);

            if (ControllerFunctions.TryGetValue(controllerMethod, out ControllerFunction controllerFunction))
                return controllerFunction(context, ModelBinder, controller);

            lock (SyncRoot)
            {
                if (!ControllerFunctions.TryGetValue(controllerMethod, out controllerFunction))
                {
                    ControllerFunctions[controllerMethod] =
                        controllerFunction = CreateControllerFunction(controllerMethod);
                }
            }

            return controllerFunction(context, ModelBinder, controller);
            //context.Response = await controllerResponse.Respond(context, _view).ConfigureAwait(false);
        }

        private ControllerFunction CreateControllerFunction(ControllerMethod controllerMethod)
        {
            ParameterExpression httpContextArgument = Expression.Parameter(typeof(IHttpContext), "httpContext");
            ParameterExpression modelBinderArgument = Expression.Parameter(typeof(IModelBinder), "modelBinder");
            ParameterExpression controllerArgument = Expression.Parameter(typeof(object), "controller");

            ParameterExpression errorContainerVariable = Expression.Variable(typeof(IErrorContainer));

            MethodInfo foundMethod =
                (from method in controllerMethod.ControllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    let attributes = method.GetCustomAttributes<HttpMethodAttribute>()
                    where attributes.Any(a => a.HttpMethod == controllerMethod.Method)
                    select method).FirstOrDefault();

            if (foundMethod == null)
            {
                return MethodNotFoundControllerFunction;
            }

            if (foundMethod.ReturnType != typeof(Task<IControllerResponse>))
            {
                throw new ArgumentException(
                    $"Controller Methods should always return {typeof(Task<IControllerResponse>)}, The method {foundMethod.DeclaringType}.{foundMethod.Name} returns {foundMethod.ReturnType.FullName}");
            }

            ParameterInfo[] parameters = foundMethod.GetParameters();

            IList<ParameterExpression> variables = new List<ParameterExpression>(parameters.Length);

            IList<Expression> body = new List<Expression>(parameters.Length);

            MethodInfo modelBindingGetMethod = typeof(IModelBinding).GetMethods()[0];

            foreach (ParameterInfo parameter in parameters)
            {
                ParameterExpression variable = Expression.Variable(parameter.ParameterType, parameter.Name);
                variables.Add(variable);

                List<Attribute> attributes = parameter.GetCustomAttributes().ToList();

                IModelBinding modelBindingAttribute = attributes.OfType<IModelBinding>().Single();

                body.Add(
                    Expression.Assign(variable,
                        Expression.Call(Expression.Constant(modelBindingAttribute),
                            modelBindingGetMethod.MakeGenericMethod(parameter.ParameterType),
                            httpContextArgument, modelBinderArgument
                        )));

                if (!attributes.OfType<NullableAttribute>().Any())
                {
                    body.Add(Expression.IfThen(Expression.Equal(variable, Expression.Constant(null)),
                        Expression.Call(errorContainerVariable, "Log", Type.EmptyTypes,
                            Expression.Constant($"{parameter.Name} Is not found (null) and not marked as nullable."))));
                }

                if (parameter.ParameterType.GetInterfaces().Contains(typeof(IValidate)))
                {
                    body.Add(Expression.IfThen(Expression.NotEqual(variable, Expression.Constant(null)),
                        Expression.Call(variable, "Validate", Type.EmptyTypes, errorContainerVariable)));
                }
            }

            MethodCallExpression methodCallExp = Expression.Call(
                Expression.Convert(controllerArgument, controllerMethod.ControllerType),
                foundMethod, variables);

            LabelTarget labelTarget = Expression.Label(typeof(Task<IControllerResponse>));

            Expression parameterBindingExpression = body.Count > 0 ? Expression.Block(body) : Expression.Empty();

            BlockExpression methodBody = Expression.Block(
                variables.Concat(new[] { errorContainerVariable }),
                Expression.Assign(errorContainerVariable, Expression.New(typeof(ErrorContainer))),
                parameterBindingExpression,
                Expression.IfThen(Expression.Not(Expression.Property(errorContainerVariable, "Any")),
                    Expression.Return(labelTarget, methodCallExp)),
                Expression.Label(labelTarget, Expression.Call(errorContainerVariable, "GetResponse", Type.EmptyTypes))
            );

            ParameterExpression[] parameterExpressions =
                { httpContextArgument, modelBinderArgument, controllerArgument };
            Expression<ControllerFunction> lambda =
                Expression.Lambda<ControllerFunction>(methodBody, parameterExpressions);

            return lambda.Compile();
        }

        private static Task<IControllerResponse> MethodNotFoundControllerFunction(IHttpContext context,
            IModelBinder binder,
            object controller)
        {
            // TODO : MethodNotFound.
            return Task.FromResult<IControllerResponse>(new RenderResponse(HttpResponseCode.MethodNotAllowed,
                new { Message = "Not Allowed" }));
        }
    }
}