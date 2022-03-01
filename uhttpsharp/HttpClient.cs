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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NovaCore.Common;
using uhttpsharp.Clients;
using uhttpsharp.Headers;
using uhttpsharp.RequestProviders;

namespace uhttpsharp
{
    internal sealed class HttpClientHandler
    {
        private const string CrLf = "\r\n";
        private static readonly byte[] CrLfBuffer = Encoding.UTF8.GetBytes("\r\n");
        private readonly Func<IHttpContext, Task> requestHandler;
        private readonly IHttpRequestProvider requestProvider;
        private readonly EndPoint remoteEndPoint;
        private Stream stream;
        private readonly Logger logger;

        public HttpClientHandler(IClient client, Func<IHttpContext, Task> requestHandler,
            IHttpRequestProvider requestProvider, Logger logger)
        {
            remoteEndPoint = client.RemoteEndPoint;
            Client = client;

            this.requestHandler = requestHandler;
            this.requestProvider = requestProvider;
            this.logger = logger;

            // Logger.InfoFormat("Got Client {0}", _remoteEndPoint);
            this.logger.Log($"Got Client {remoteEndPoint}");

            Task.Factory.StartNew(Process);

            UpdateLastOperationTime();
        }

        private async Task InitializeStream()
        {
            if (Client is ClientSslDecorator client)
            {
                await client.AuthenticateAsServer().ConfigureAwait(false);
            }

            stream = new BufferedStream(Client.Stream, 8096);
        }

        private async void Process()
        {
            try
            {
                await InitializeStream();

                while (Client.Connected)
                {
                    // TODO : Configuration.
                    NotFlushingStream limitedStream = new(new LimitedStream(stream));

                    IHttpRequest request = await requestProvider
                        .Provide(new MyStreamReader(limitedStream)).ConfigureAwait(false);

                    if (request != null)
                    {
                        UpdateLastOperationTime();

                        HttpContext context = new(request, Client.RemoteEndPoint);

                        // Logger.InfoFormat("Got Client {0}", _remoteEndPoint);
                        logger.Log($"{Client.RemoteEndPoint} : Got request {request.Uri}");

                        await requestHandler(context).ConfigureAwait(false);

                        if (context.Response != null)
                        {
                            StreamWriter streamWriter = new(limitedStream) { AutoFlush = false };
                            streamWriter.NewLine = CrLf;
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
            catch (Exception ex)
            {
                logger.LogError($"Error while serving : {remoteEndPoint}");
                logger.LogException(ex);
                Client.Close();
            }

            // Logger.InfoFormat("Lost Client {0}", _remoteEndPoint);
            logger.Log($"Lost Client {remoteEndPoint}");
        }

        private static async Task WriteResponse(IHttpContext context, StreamWriter writer)
        {
            IHttpResponse response = context.Response;
            IHttpRequest request = context.Request;

            // Headers
            await writer.WriteLineAsync($"HTTP/1.1 {(int)response.ResponseCode} {response.ResponseCode}")
                .ConfigureAwait(false);

            foreach ((string key, string value) in response.Headers)
            {
                await writer.WriteLineAsync($"{key}: {value}").ConfigureAwait(false);
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

        public DateTime LastOperationTime { get; private set; }

        private void UpdateLastOperationTime()
        {
            LastOperationTime = DateTime.Now;
        }
    }

    internal class NotFlushingStream : Stream
    {
        private readonly Stream child;

        public NotFlushingStream(Stream child)
        {
            this.child = child;
        }

        public void ExplicitFlush()
        {
            child.Flush();
        }

        public Task ExplicitFlushAsync()
        {
            return child.FlushAsync();
        }

        public override void Flush()
        {
            // _child.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return child.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            child.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return child.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            return child.ReadByte();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            child.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            child.WriteByte(value);
        }

        public override bool CanRead => child.CanRead;
        public override bool CanSeek => child.CanSeek;

        public override bool CanWrite => child.CanWrite;
        public override long Length => child.Length;

        public override long Position
        {
            get => child.Position;
            set => child.Position = value;
        }

        public override int ReadTimeout
        {
            get => child.ReadTimeout;
            set => child.ReadTimeout = value;
        }

        public override int WriteTimeout
        {
            get => child.WriteTimeout;
            set => child.WriteTimeout = value;
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