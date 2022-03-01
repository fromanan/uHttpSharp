using System;

namespace uhttpsharp
{
    public class HttpMethodProvider : IHttpMethodProvider
    {
        public static readonly IHttpMethodProvider Default = new HttpMethodProviderCache(new HttpMethodProvider());

        internal HttpMethodProvider() { }

        public HttpMethods Provide(string name)
        {
            string capitalName = name[..1].ToUpper() + name.Substring(1).ToLower();
            return (HttpMethods)Enum.Parse(typeof(HttpMethods), capitalName);
        }
    }
}