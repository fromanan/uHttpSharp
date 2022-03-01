using System;

namespace uhttpsharp.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class HttpMethodAttribute : Attribute
    {
        public HttpMethodAttribute(HttpMethods httpMethod)
        {
            HttpMethod = httpMethod;
        }

        public HttpMethods HttpMethod { get; }
    }
}