using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace uhttpsharp.Clients
{
    public class ClientSslDecorator : IClient
    {
        private readonly IClient child;
        private readonly X509Certificate certificate;
        private readonly SslStream sslStream;

        public ClientSslDecorator(IClient child, X509Certificate certificate)
        {
            this.child = child;
            this.certificate = certificate;
            sslStream = new SslStream(this.child.Stream);
        }

        public async Task AuthenticateAsServer()
        {
            Task timeout = Task.Delay(TimeSpan.FromSeconds(10));
            if (timeout == await Task
                    .WhenAny(sslStream.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls12, true), timeout)
                    .ConfigureAwait(false))
            {
                throw new TimeoutException("SSL Authentication Timeout");
            }
        }

        public Stream Stream => sslStream;

        public bool Connected => child.Connected;

        public void Close()
        {
            child.Close();
        }

        public EndPoint RemoteEndPoint => child.RemoteEndPoint;
    }
}