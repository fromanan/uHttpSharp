using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using uhttpsharp.Headers;

namespace uhttpsharp.Handlers.Compression
{
    public class CompressedResponse : IHttpResponse
    {
        private readonly MemoryStream _memoryStream;

        public CompressedResponse(IHttpResponse child, MemoryStream memoryStream, string encoding)
        {
            _memoryStream = memoryStream;

            ResponseCode = child.ResponseCode;
            CloseConnection = child.CloseConnection;
            Headers =
                new ListHttpHeaders(
                    child.Headers.Where(h => !h.Key.Equals("content-length", StringComparison.InvariantCultureIgnoreCase))
                        .Concat(new[]
                        {
                            new KeyValuePair<string, string>("content-length",
                                memoryStream.Length.ToString(CultureInfo.InvariantCulture)),
                            new KeyValuePair<string, string>("content-encoding", encoding),
                        })
                        .ToList());
        }

        public static async Task<IHttpResponse> Create(string name, IHttpResponse child, Func<Stream, Stream> streamFactory)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (Stream deflateStream = streamFactory(memoryStream))
            using (StreamWriter deflateWriter = new StreamWriter(deflateStream))
            {
                await child.WriteBody(deflateWriter).ConfigureAwait(false);
                await deflateWriter.FlushAsync().ConfigureAwait(false);
            }

            return new CompressedResponse(child, memoryStream, name);
        }

        public static Task<IHttpResponse> CreateDeflate(IHttpResponse child)
        {
            return Create("deflate", child, s => new DeflateStream(s, CompressionMode.Compress, true));
        }

        public static Task<IHttpResponse> CreateGZip(IHttpResponse child)
        {
            return Create("gzip", child, s => new GZipStream(s, CompressionMode.Compress, true));
        }

        public async Task WriteBody(StreamWriter writer)
        {
            _memoryStream.Position = 0;

            await writer.FlushAsync().ConfigureAwait(false);
            await _memoryStream.CopyToAsync(writer.BaseStream).ConfigureAwait(false);
        }
        
        public HttpResponseCode ResponseCode { get; }
        
        public IHttpHeaders Headers { get; }
        
        public bool CloseConnection { get; }
    }
}