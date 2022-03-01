using System.Dynamic;
using System.Net;
using uhttpsharp.Headers;

namespace uhttpsharp
{
    internal class HttpContext : IHttpContext
    {
        private readonly ExpandoObject state = new();
        public HttpContext(IHttpRequest request, EndPoint remoteEndPoint)
        {
            Request = request;
            RemoteEndPoint = remoteEndPoint;
            Cookies = new CookiesStorage(Request.Headers.GetByNameOrDefault("cookie", string.Empty));
        }

        public IHttpRequest Request { get; }

        public IHttpResponse Response { get; set; }

        public ICookiesStorage Cookies { get; }

        public dynamic State => state;
        public EndPoint RemoteEndPoint { get; }
    }
}