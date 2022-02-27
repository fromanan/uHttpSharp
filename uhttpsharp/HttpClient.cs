/*
 * Copyright (C) 2011 uhttpsharp project - http://github.com/raistlinthewiz/uhttpsharp
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.

 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.

 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System.Text;
using System.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using uhttpsharp.Clients;
using uhttpsharp.Headers;
using uhttpsharp.RequestProviders;

namespace uhttpsharp
{
    internal sealed class HttpClientHandler
    {
        private const string CrLf = "\r\n";
        private static readonly byte[] CrLfBuffer = Encoding.UTF8.GetBytes(CrLf);

        private readonly Func<IHttpContext, Task> _requestHandler;
        private readonly IHttpRequestProvider _requestProvider;
        private readonly EndPoint _remoteEndPoint;
        private Stream _stream;

        public HttpClientHandler(IClient client, Func<IHttpContext, Task> requestHandler, IHttpRequestProvider requestProvider)
        {
            _remoteEndPoint = client.RemoteEndPoint;
            Client = client;
            _requestHandler = requestHandler;
            _requestProvider = requestProvider;

            // TODO:
            // Logger.InfoFormat("Got Client {0}", _remoteEndPoint);
            Console.WriteLine($"Got Client {_remoteEndPoint}");

            Task.Factory.StartNew(Process);

            UpdateLastOperationTime();
        }

        private async Task InitializeStream()
        {
            if (Client is ClientSslDecorator decorator)
            {
                await decorator.AuthenticateAsServer().ConfigureAwait(false);
            }

            _stream = new BufferedStream(Client.Stream, 8096);
        }

        private async void Process()
        {
            try
            {
                await InitializeStream();

                while (Client.Connected)
                {
                    // TODO : Configuration.
                    NotFlushingStream limitedStream = new NotFlushingStream(new LimitedStream(_stream));

                    IHttpRequest request = await _requestProvider.Provide(new MyStreamReader(limitedStream)).ConfigureAwait(false);

                    if (request != null)
                    {
                        UpdateLastOperationTime();

                        HttpContext context = new HttpContext(request, Client.RemoteEndPoint);

                        // TODO:
                        // Logger.InfoFormat("Got Client {0}", _remoteEndPoint);
                        Console.WriteLine($"{Client.RemoteEndPoint} : Got request {request.Uri}");

                        await _requestHandler(context).ConfigureAwait(false);

                        if (context.Response != null)
                        {
                            StreamWriter streamWriter = new StreamWriter(limitedStream) { AutoFlush = false };
                            streamWriter.NewLine = "\r\n";
                            await WriteResponse(context, streamWriter).ConfigureAwait(false);
                            await limitedStream.ExplicitFlushAsync().ConfigureAwait(false);

                            if (!request.Headers.KeepAliveConnection() || context.Response.CloseConnection)
                            {
                                Client.Close();
                            }
                        }

                        UpdateLastOperationTime();
                    }
                    else
                    {
                        Client.Close();
                    }
                }
            }
            catch (Exception e)
            {
                // Hate people who make bad calls.
                // TODO:
                //Logger.WarnException($"Error while serving : {_remoteEndPoint}", e);
                Console.WriteLine($"Error while serving : {_remoteEndPoint}");
                Console.WriteLine(e.Message);
                Client.Close();
            }

            // TODO:
            // Logger.InfoFormat("Lost Client {0}", _remoteEndPoint);
            Console.WriteLine($"Lost Client {_remoteEndPoint}");
        }
        private static async Task WriteResponse(IHttpContext context, StreamWriter writer)
        {
            IHttpResponse response = context.Response;
            IHttpRequest request = context.Request;

            // Headers
            await writer.WriteLineAsync($"HTTP/1.1 {(int)response.ResponseCode} {response.ResponseCode}")
                .ConfigureAwait(false);

            foreach (KeyValuePair<string, string> header in response.Headers)
            {
                await writer.WriteLineAsync($"{header.Key}: {header.Value}").ConfigureAwait(false);
            }

            // Cookies
            if (context.Cookies.Touched)
            {
                await writer.WriteAsync(context.Cookies.ToCookieData()).ConfigureAwait(false);
            }

            // Empty Line
            await writer.WriteLineAsync().ConfigureAwait(false);
            await writer.FlushAsync();

            // Body
            await response.WriteBody(writer).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }

        public IClient Client { get; }

        public void ForceClose()
        {
            Client.Close();
        }

        public DateTime LastOperationTime { get; }

        private void UpdateLastOperationTime()
        {
            // _lastOperationTime = DateTime.Now;
        }
    }

    internal class NotFlushingStream : Stream
    {
        private readonly Stream _child;
        public NotFlushingStream(Stream child)
        {
            _child = child;
        }

        public void ExplicitFlush()
        {
            _child.Flush();
        }

        public Task ExplicitFlushAsync()
        {
            return _child.FlushAsync();
        }

        public override void Flush()
        {
            // _child.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _child.Seek(offset, origin);
        }
        public override void SetLength(long value)
        {
            _child.SetLength(value);
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _child.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            return _child.ReadByte();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            _child.Write(buffer, offset, count);
        }
        public override void WriteByte(byte value)
        {
            _child.WriteByte(value);
        }
        public override bool CanRead => _child.CanRead;
        public override bool CanSeek => _child.CanSeek;

        public override bool CanWrite => _child.CanWrite;
        public override long Length => _child.Length;
        public override long Position
        {
            get => _child.Position;
            set => _child.Position = value;
        }
        public override int ReadTimeout
        {
            get => _child.ReadTimeout;
            set => _child.ReadTimeout = value;
        }
        public override int WriteTimeout
        {
            get => _child.WriteTimeout;
            set => _child.WriteTimeout = value;
        }
    }

    public static class RequestHandlersAggregateExtensions
    {
        public static Func<IHttpContext, Task> Aggregate(this IList<IHttpRequestHandler> handlers)
        {
            return handlers.Aggregate(0);
        }

        private static Func<IHttpContext, Task> Aggregate(this IList<IHttpRequestHandler> handlers, int index)
        {
            if (index == handlers.Count)
            {
                return null;
            }

            IHttpRequestHandler currentHandler = handlers[index];
            Func<IHttpContext, Task> nextHandler = handlers.Aggregate(index + 1);

            return context => currentHandler.Handle(context, () => nextHandler(context));
        }
    }
}