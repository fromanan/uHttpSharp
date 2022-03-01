using System.IO;
using System.Net;
using System.Net.Sockets;

namespace uhttpsharp.Clients
{
    public class TcpClientAdapter : IClient
    {
        private readonly TcpClient client;
        private readonly NetworkStream stream;

        public TcpClientAdapter(TcpClient client)
        {
            this.client = client;
            stream = this.client.GetStream();

            // The next lines are commented out because they caused exceptions, 
            // They have been added because .net doesn't allow me to wait for data (ReadAsyncBlock).
            // Instead, I've added Task.Delay in MyStreamReader.ReadBuffer when
            // Read returns without data.

            // See https://github.com/Code-Sharp/uHttpSharp/issues/14

            // Read Timeout of one second.
            // _stream.ReadTimeout = 1000;
        }

        public Stream Stream => stream;

        public bool Connected => client.Connected;

        public void Close()
        {
            client.Close();
        }

        public EndPoint RemoteEndPoint => client.Client.RemoteEndPoint;
    }
}