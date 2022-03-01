using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace uhttpsharp.Handlers.Compression
{
    /// <summary>
    /// An <see cref="IHttpRequestHandler"/>
    /// 
    /// That lets the following <see cref="IHttpRequestHandler"/>s in the chain to run
    /// and afterwards tries to compress the returned response by the "Accept-Encoding" header that
    /// given from the client.
    /// 
    /// The compressors given in the constructor are prefered by the order that they are given.
    /// </summary>
    public class CompressionHandler : IHttpRequestHandler
    {
        private readonly IEnumerable<ICompressor> compressors;
        private static readonly char[] Separator = { ',' };

        /// <summary>
        /// Creates an instance of <see cref="CompressionHandler"/>
        /// </summary>
        /// <param name="compressors">The compressors to use, Ordered by preference</param>
        public CompressionHandler(params ICompressor[] compressors)
        {
            this.compressors = compressors;
        }

        public async Task Handle(IHttpContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);

            if (context.Response == null)
            {
                return;
            }

            if (!context.Request.Headers.TryGetByName("Accept-Encoding", out string encodingNames))
            {
                return;
            }

            string[] encodings = encodingNames.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

            ICompressor compressor =
                compressors.FirstOrDefault(c => encodings.Contains(c.Name, StringComparer.InvariantCultureIgnoreCase));

            if (compressor == null)
            {
                return;
            }

            context.Response = await compressor.Compress(context.Response).ConfigureAwait(false);
        }
    }
}