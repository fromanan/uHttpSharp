using System;
using System.Threading.Tasks;

namespace uhttpsharp
{
    public static class HttpServerExtensions
    {
        public static void Use(this HttpServer server, Func<IHttpContext, Func<Task>, Task> method)
        {
            server.Use(new AnonymousHttpRequestHandler(method));
        }
    }

    public class AnonymousHttpRequestHandler : IHttpRequestHandler
    {
        private readonly Func<IHttpContext, Func<Task>, Task> method;

        public AnonymousHttpRequestHandler(Func<IHttpContext, Func<Task>, Task> method)
        {
            this.method = method;
        }

        public Task Handle(IHttpContext context, Func<Task> next)
        {
            return method(context, next);
        }
    }
}