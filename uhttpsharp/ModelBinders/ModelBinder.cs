using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using uhttpsharp.Headers;

namespace uhttpsharp.ModelBinders
{
    public class ModelBinder : IModelBinder
    {
        private readonly IObjectActivator activator;

        public ModelBinder(IObjectActivator activator)
        {
            this.activator = activator;
        }

        public T Get<T>(byte[] raw, string prefix)
        {
            throw new NotSupportedException();
        }

        public T Get<T>(IHttpHeaders headers)
        {
            T retVal = activator.Activate<T>(null);

            foreach (PropertyInfo prop in retVal.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string))
                {
                    if (!headers.TryGetByName(prop.Name, out string stringValue)) continue;
                    object value = Convert.ChangeType(stringValue, prop.PropertyType);
                    prop.SetValue(retVal, value);
                }
                else
                {
                    object value = Get(prop.PropertyType, headers, prop.Name);
                    prop.SetValue(retVal, value);
                }
            }

            return retVal;
        }

        private object Get(Type type, IHttpHeaders headers, string prefix)
        {
            if (type.IsPrimitive || type == typeof(string))
            {
                return headers.TryGetByName(prefix, out string value) ? Convert.ChangeType(value, type) : null;
            }

            object retVal = activator.Activate(type, null);

            List<PropertyInfo> setValues =
                retVal.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => headers.TryGetByName($"{prefix}[{p.Name}]", out string _)).ToList();

            if (setValues.Count == 0)
            {
                return null;
            }

            foreach (PropertyInfo prop in setValues)
            {
                if (!headers.TryGetByName($"{prefix}[{prop.Name}]", out string stringValue)) continue;
                object value = prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string)
                    ? Convert.ChangeType(stringValue, prop.PropertyType)
                    : Get(prop.PropertyType, headers, $"{prefix}[{prop.Name}]");

                prop.SetValue(retVal, value);
            }

            return retVal;
        }

        public T Get<T>(IHttpHeaders headers, string prefix)
        {
            return (T)Get(typeof(T), headers, prefix);
        }
    }

    public class ObjectActivator : IObjectActivator
    {
        public object Activate(Type type, Func<string, Type, object> argumentGetter)
        {
            return Activator.CreateInstance(type);
        }
    }

    public interface IObjectActivator
    {
        object Activate(Type type, Func<string, Type, object> argumentGetter);
    }

    public static class ObjectActivatorExtensions
    {
        public static T Activate<T>(this IObjectActivator activator, Func<string, Type, object> argumentGetter)
        {
            return (T)activator.Activate(typeof(T), argumentGetter);
        }
    }
}