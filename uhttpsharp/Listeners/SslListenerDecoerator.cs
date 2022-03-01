using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using uhttpsharp.Clients;

namespace uhttpsharp.Listeners
{
    public class ListenerSslDecorator : IHttpListener
    {
        private readonly IHttpListener child;
        private readonly X509Certificate certificate;

        public ListenerSslDecorator(IHttpListener child, X509Certificate certificate)
        {
            this.child = child;
            this.certificate = certificate;
        }

        public async Task<IClient> GetClient()
        {
            return new ClientSslDecorator(await child.GetClient().ConfigureAwait(false), certificate);
        }

        public void Dispose()
        {
            child.Dispose();
        }
    }
}