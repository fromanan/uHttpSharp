using System;
using System.Collections.Generic;
using uhttpsharp.ModelBinders;

namespace uhttpsharp.Attributes
{
    internal interface IModelBinding
    {
        T Get<T>(IHttpContext context, IModelBinder binder);
    }

    public class FromStateAttribute : Attribute, IModelBinding
    {
        private readonly string _propertyName;
        public FromStateAttribute(string propertyName)
        {
            _propertyName = propertyName;
        }
        public T Get<T>(IHttpContext context, IModelBinder binder)
        {
            // Expando object
            if (context.State is IDictionary<string, object> state && state.TryGetValue(_propertyName, out object real) &&
                real is T value)
            {
                return value;
            }

            return default;
        }
    }

    public class FromBodyAttribute : PrefixAttribute
    {
        public FromBodyAttribute(string prefix = null) : base(prefix) { }

        public override T Get<T>(IHttpContext context, IModelBinder binder)
        {
            return binder.Get<T>(context.Request.Post.Raw, Prefix);
        }
    }

    public class FromPostAttribute : PrefixAttribute
    {
        public FromPostAttribute(string prefix = null) : base(prefix) { }
        
        public override T Get<T>(IHttpContext context, IModelBinder binder)
        {
            return binder.Get<T>(context.Request.Post.Parsed, Prefix);
        }
    }

    public class FromQueryAttribute : PrefixAttribute
    {
        public FromQueryAttribute(string prefix) : base(prefix) { }
        
        public override T Get<T>(IHttpContext context, IModelBinder binder)
        {
            return binder.Get<T>(context.Request.QueryString, Prefix);
        }
    }

    public class FromHeadersAttribute : PrefixAttribute
    {
        public FromHeadersAttribute(string prefix) : base(prefix) { }

        public override T Get<T>(IHttpContext context, IModelBinder binder)
        {
            return binder.Get<T>(context.Request.Headers, Prefix);
        }
    }

    public abstract class PrefixAttribute : Attribute, IModelBinding
    {
        public PrefixAttribute(string prefix)
        {
            Prefix = prefix;
        }

        public bool HasPrefix => !string.IsNullOrEmpty(Prefix);

        public string Prefix { get; }

        public abstract T Get<T>(IHttpContext context, IModelBinder binder);
    }
}