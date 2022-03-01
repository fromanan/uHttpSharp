using System;
using System.Collections.Concurrent;

namespace uhttpsharp
{
    public class HttpMethodProviderCache : IHttpMethodProvider
    {
        private readonly ConcurrentDictionary<string, HttpMethods> cache = new();

        private readonly Func<string, HttpMethods> childProvide;
        public HttpMethodProviderCache(IHttpMethodProvider child)
        {
            childProvide = child.Provide;
        }
        public HttpMethods Provide(string name)
        {
            return cache.GetOrAdd(name, childProvide);
        }
    }
}