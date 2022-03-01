using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace uhttpsharp.Handlers
{
    public class RestHandler<T> : IHttpRequestHandler
    {
        private readonly struct RestCall
        {
            private readonly HttpMethods method;
            
            private readonly bool entryFull;

            public RestCall(HttpMethods method, bool entryFull)
            {
                this.method = method;
                this.entryFull = entryFull;
            }

            public static RestCall Create(HttpMethods method, bool entryFull)
            {
                return new RestCall(method, entryFull);
            }

            private bool Equals(RestCall other)
            {
                return method == other.method && entryFull.Equals(other.entryFull);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is RestCall call && Equals(call);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)method * 397) ^ entryFull.GetHashCode();
                }
            }
        }

        private static readonly IDictionary<RestCall, Func<IRestController<T>, IHttpRequest, Task<object>>> RestCallHandlers =
            new Dictionary<RestCall, Func<IRestController<T>, IHttpRequest, Task<object>>>();

        static RestHandler()
        {
            RestCallHandlers.Add(RestCall.Create(HttpMethods.Get, false), async (c, r) => await c.Get(r));
            RestCallHandlers.Add(RestCall.Create(HttpMethods.Get, true), async (c, r) => await c.GetItem(r));
            RestCallHandlers.Add(RestCall.Create(HttpMethods.Post, false), async (c, r) => await c.Create(r));
            RestCallHandlers.Add(RestCall.Create(HttpMethods.Put, true), async (c, r) => await c.Upsert(r));
            RestCallHandlers.Add(RestCall.Create(HttpMethods.Delete, true), async (c, r) => await c.Delete(r));
        }

        private readonly IRestController<T> controller;
        
        private readonly IResponseProvider responseProvider;
        
        public RestHandler(IRestController<T> controller, IResponseProvider responseProvider)
        {
            this.controller = controller;
            this.responseProvider = responseProvider;
        }

        public async Task Handle(IHttpContext httpContext, Func<Task> next)
        {
            IHttpRequest httpRequest = httpContext.Request;

            RestCall call = new(httpRequest.Method, httpRequest.RequestParameters.Length > 1);

            if (RestCallHandlers.TryGetValue(call, out Func<IRestController<T>, IHttpRequest, Task<object>> handler))
            {
                object value = await handler(controller, httpRequest).ConfigureAwait(false);
                httpContext.Response = await responseProvider.Provide(value);

                return;
            }

            await next().ConfigureAwait(false);
        }
    }
}