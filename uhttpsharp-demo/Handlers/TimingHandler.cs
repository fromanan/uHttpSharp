using System;
using System.Diagnostics;
using System.Threading.Tasks;
using uhttpsharp;

namespace uhttpsharpdemo.Handlers
{
    public class TimingHandler : IHttpRequestHandler
    {
        public async Task Handle(IHttpContext context, Func<Task> next)
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            await next();

            // TODO:
            // Logger.InfoFormat("request {0} took {1}", context.Request.Uri, stopWatch.Elapsed);
            Console.WriteLine($"request {context.Request.Uri} took {stopWatch.Elapsed}");
        }
    }
}