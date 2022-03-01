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
using System.Linq;
using System.Threading.Tasks;
using NovaCore.Common;
using uhttpsharp.Clients;
using uhttpsharp.Listeners;
using uhttpsharp.RequestProviders;

namespace uhttpsharp
{
    public sealed class HttpServer : IDisposable
    {
        private bool isActive;

        private readonly IList<IHttpRequestHandler> handlers = new List<IHttpRequestHandler>();
        private readonly IList<IHttpListener> listeners = new List<IHttpListener>();
        private readonly IHttpRequestProvider requestProvider;
        private readonly IList<HttpClientHandler> clientHandlers = new List<HttpClientHandler>();

        public readonly Logger Logger;

        public HttpServer(IHttpRequestProvider requestProvider, Logger logger = null)
        {
            this.requestProvider = requestProvider;
            Logger = logger ?? new Logger();
        }

        public void Use(IHttpRequestHandler handler)
        {
            handlers.Add(handler);
        }

        public void Use(IHttpListener listener)
        {
            listeners.Add(listener);
        }

        public void Start()
        {
            isActive = true;
            foreach (IHttpListener listener in listeners)
            {
                IHttpListener tempListener = listener;
                Task.Factory.StartNew(() => Listen(tempListener));
            }

            // Logger.InfoFormat("Embedded uhttpserver started.");
            Logger.LogInfo("Embedded uhttpserver started.");
        }

        private async void Listen(IHttpListener listener)
        {
            Func<IHttpContext, Task> aggregatedHandler = handlers.Aggregate();
            while (isActive)
            {
                try
                {
                    IClient client = await listener.GetClient().ConfigureAwait(false);
                    clientHandlers.Add(new HttpClientHandler(client, aggregatedHandler, requestProvider, Logger));
                }
                catch (Exception ex)
                {
                    // Logger.WarnException("Error while getting client", e);
                    Logger.LogError("Error while getting client");
                    Logger.LogException(ex);
                }
            }

            CloseAllConnections();

            // Logger.InfoFormat("Embedded uhttpserver stopped.");
            Logger.LogInfo("Embedded uhttpserver stopped.");
        }

        public void Dispose()
        {
            CloseServer();
        }

        public void CloseServer()
        {
            isActive = false;
        }

        public bool Serving => !clientHandlers.Any(c => c.Client.Connected);

        public void CloseAllConnections()
        {
            foreach (HttpClientHandler clientHandler in clientHandlers)
            {
                clientHandler?.ForceClose();
            }
        }
    }
}