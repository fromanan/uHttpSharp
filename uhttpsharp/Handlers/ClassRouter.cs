using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace uhttpsharp.Handlers
{
    public class ClassRouter : IHttpRequestHandler
    {
        private static readonly object SyncRoot = new();

        private static readonly HashSet<Type> LoadedRoutes = new();

        private static readonly IDictionary<Tuple<Type, string>, Func<IHttpRequestHandler, IHttpRequestHandler>>
            Routers = new Dictionary<Tuple<Type, string>, Func<IHttpRequestHandler, IHttpRequestHandler>>();

        private static readonly ConcurrentDictionary<Type,
                Func<IHttpContext, IHttpRequestHandler, string, Task<IHttpRequestHandler>>>
            IndexerRouters = new();

        private readonly IHttpRequestHandler root;

        public ClassRouter(IHttpRequestHandler root)
        {
            this.root = root;
            LoadRoute(this.root);
        }

        private void LoadRoute(IHttpRequestHandler root)
        {
            Type rootType = root.GetType();

            if (LoadedRoutes.Contains(rootType))
            {
                return;
            }

            lock (SyncRoot)
            {
                if (!LoadedRoutes.Add(rootType)) return;

                foreach (PropertyInfo route in GetRoutesOfHandler(rootType))
                {
                    Tuple<Type, string> tuple = Tuple.Create(rootType, route.Name);
                    Func<IHttpRequestHandler, IHttpRequestHandler> value = CreateRoute(tuple);
                    Routers.Add(tuple, value);
                }
            }
        }

        private static IEnumerable<PropertyInfo> GetRoutesOfHandler(Type type)
        {
            return type
                .GetProperties()
                .Where(p => typeof(IHttpRequestHandler).IsAssignableFrom(p.PropertyType));
        }

        public async Task Handle(IHttpContext context, Func<Task> next)
        {
            IHttpRequestHandler handler = root;

            foreach (string parameter in context.Request.RequestParameters)
            {
                LoadRoute(handler);

                if (Routers.TryGetValue(Tuple.Create(handler.GetType(), parameter),
                        out Func<IHttpRequestHandler, IHttpRequestHandler> getNextHandler))
                {
                    handler = getNextHandler(handler);
                }
                else
                {
                    Func<IHttpContext, IHttpRequestHandler, string, Task<IHttpRequestHandler>> getNextByIndex =
                        IndexerRouters.GetOrAdd(handler.GetType(), GetIndexerRouter);

                    // Indexer is not found
                    if (getNextByIndex == null)
                    {
                        await next().ConfigureAwait(false);
                        return;
                    }

                    Task<IHttpRequestHandler> returnedTask = getNextByIndex(context, handler, parameter);

                    // Indexer found, but returned null (for whatever reason)
                    if (returnedTask == null)
                    {
                        await next().ConfigureAwait(false);
                        return;
                    }

                    handler = await returnedTask.ConfigureAwait(false);
                }

                // Incase that one of the methods returned null (Indexer / Getter)
                if (handler != null) continue;
                await next().ConfigureAwait(false);
                return;
            }

            await handler.Handle(context, next).ConfigureAwait(false);
        }

        private Func<IHttpContext, IHttpRequestHandler, string, Task<IHttpRequestHandler>> GetIndexerRouter(Type arg)
        {
            MethodInfo indexer = GetIndexer(arg);
            return indexer == null ? null : CreateIndexerFunction<IHttpRequestHandler>(arg, indexer);
        }

        internal static Func<IHttpContext, T, string, Task<T>> CreateIndexerFunction<T>(Type arg, MethodInfo indexer)
        {
            AssertIndexerParameters<T>(indexer);

            Type parameterType = indexer.GetParameters()[1].ParameterType;

            ParameterExpression httpContext = Expression.Parameter(typeof(IHttpContext), "context");
            ParameterExpression inputHandler = Expression.Parameter(typeof(T), "instance");
            ParameterExpression inputObject = Expression.Parameter(typeof(string), "input");

            MethodInfo tryParseMethod =
                parameterType.GetMethod("TryParse", new[] { typeof(string), parameterType.MakeByRefType() });

            Expression body;

            if (tryParseMethod == null)
            {
                UnaryExpression handlerConverted = Expression.Convert(inputHandler, arg);
                UnaryExpression objectConverted =
                    Expression.Convert(
                        Expression.Call(
                            typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) }) ??
                            throw new InvalidOperationException(),
                            inputObject,
                            Expression.Constant(parameterType)), parameterType);

                MethodCallExpression indexerExpression =
                    Expression.Call(handlerConverted, indexer, httpContext, objectConverted);
                UnaryExpression returnValue = Expression.Convert(indexerExpression, typeof(Task<T>));

                body = returnValue;
            }
            else
            {
                ParameterExpression inputConvertedVar = Expression.Variable(parameterType, "inputObjectConverted");

                UnaryExpression handlerConverted = Expression.Convert(inputHandler, arg);

                MethodCallExpression indexerExpression =
                    Expression.Call(handlerConverted, indexer, httpContext, inputConvertedVar);
                UnaryExpression returnValue = Expression.Convert(indexerExpression, typeof(Task<T>));
                LabelTarget returnTarget = Expression.Label(typeof(Task<T>));
                LabelExpression returnLabel = Expression.Label(returnTarget,
                    Expression.Convert(Expression.Constant(null), typeof(Task<T>)));
                body =
                    Expression.Block(
                        new[] { inputConvertedVar },
                        Expression.IfThen(
                            Expression.Call(tryParseMethod, inputObject,
                                inputConvertedVar),
                            Expression.Return(returnTarget, returnValue)
                        ),
                        returnLabel);
            }

            return
                Expression.Lambda<Func<IHttpContext, T, string, Task<T>>>(body, httpContext,
                    inputHandler,
                    inputObject).Compile();
        }

        private static void AssertIndexerParameters<T>(MethodInfo methodInfo)
        {
            ParameterInfo[] parameters = methodInfo.GetParameters();

            if (parameters.Length != 2 || parameters[0].ParameterType != typeof(IHttpContext))
            {
                throw new ArgumentException(
                    string.Format(
                        "Indexer Method ({2}.{3}) should always receive two parameters, The first one should be of type {0} and the second should be of primitive type (string, int, long, etc'). Also, It must return {1}.",
                        typeof(IHttpContext).FullName, typeof(Task<T>).FullName, methodInfo.DeclaringType, methodInfo.Name));
            }

            if (methodInfo.ReturnType != typeof(Task<T>))
            {
                throw new ArgumentException($"Indexer method should always return {typeof(Task<T>).FullName}.");
            }
        }

        private static MethodInfo GetIndexer(Type arg)
        {
            MethodInfo indexer =
                arg.GetMethods().SingleOrDefault(m => Attribute.IsDefined(m, typeof(IndexerAttribute))
                                                      && m.GetParameters().Length == 2
                                                      && typeof(Task<IHttpRequestHandler>).IsAssignableFrom(m.ReturnType));

            return indexer;
        }

        private static Func<IHttpRequestHandler, IHttpRequestHandler> CreateRoute(Tuple<Type, string> arg)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(IHttpRequestHandler), "input");
            (Type item1, string item2) = arg;
            UnaryExpression converted = Expression.Convert(parameter, item1);

            PropertyInfo propertyInfo = item1.GetProperty(item2);

            if (propertyInfo == null)
            {
                return null;
            }

            MemberExpression property = Expression.Property(converted, propertyInfo);
            UnaryExpression propertyConverted = Expression.Convert(property, typeof(IHttpRequestHandler));

            return Expression.Lambda<Func<IHttpRequestHandler, IHttpRequestHandler>>(propertyConverted, parameter)
                .Compile();
        }
    }

    public class IndexerAttribute : Attribute
    {
        public IndexerAttribute(int precedence = 0)
        {
            Precedence = precedence;
        }

        public int Precedence { get; }
    }
}