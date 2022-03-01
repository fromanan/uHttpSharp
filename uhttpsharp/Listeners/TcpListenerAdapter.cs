using System.Net.Sockets;
using System.Threading.Tasks;
using uhttpsharp.Clients;

namespace uhttpsharp.Listeners
{
    public class TcpListenerAdapter : IHttpListener
    {
        private readonly TcpListener listener;

        public TcpListenerAdapter(TcpListener listener)
        {
            this.listener = listener;
            this.listener.Start();
        }
        public async Task<IClient> GetClient()
        {
            return new TcpClientAdapter(await listener.AcceptTcpClientAsync().ConfigureAwait(false));
        }

        public void Dispose()
        {
            listener.Stop();
        }
    }
}