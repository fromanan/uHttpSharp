using System.Threading.Tasks;

namespace uhttpsharp.Handlers.Compression
{
    public class DeflateCompressor : ICompressor
    {
        public static readonly ICompressor Default = new DeflateCompressor();

        public string Name => "deflate";
        
        public Task<IHttpResponse> Compress(IHttpResponse response)
        {
            return CompressedResponse.CreateDeflate(response);
        }
    }
}