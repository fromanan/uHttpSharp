using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using uhttpsharp;
using uhttpsharp.Logging;

namespace uhttpsharpdemo.Handlers
{
    public class TimingHandler : IHttpRequestHandler
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        public async Task Handle(IHttpContext context, Func<Task> next)
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            await next();

            Logger.InfoFormat("request {0} took {1}", context.Request.Uri, stopWatch.Elapsed);
        }
    }
}