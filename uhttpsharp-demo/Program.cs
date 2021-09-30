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
using System.Collections.Concurrent;
using System.Dynamic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using log4net.Config;
using uhttpsharp;
using uhttpsharp.Handlers;
using uhttpsharp.Handlers.Compression;
using uhttpsharp.Listeners;
using uhttpsharp.ModelBinders;
using uhttpsharp.RequestProviders;
using uhttpsharpdemo.Controllers;
using uhttpsharpdemo.Handlers;

namespace uhttpsharpdemo
{
    internal static class Program
    {
        private static void Main()
        {
            XmlConfigurator.Configure();

            //var serverCertificate = X509Certificate.CreateFromCertFile(@"TempCert.cer");

            using (var httpServer = new HttpServer(new HttpRequestProvider()))
            {
                httpServer.Use(new TcpListenerAdapter(new TcpListener(IPAddress.Any, 82)));
                //httpServer.Use(new ListenerSslDecorator(new TcpListenerAdapter(new TcpListener(IPAddress.Loopback, 443)), serverCertificate));

                //httpServer.Use(new SessionHandler<DateTime>(() => DateTime.Now));

                httpServer.Use(new ExceptionHandler());
                httpServer.Use(new SessionHandler<dynamic>(() => new ExpandoObject(), TimeSpan.FromMinutes(20)));
                httpServer.Use(new BasicAuthenticationHandler("Hohoho", "username", "password5"));
                httpServer.Use(new ControllerHandler(new DerivedController(), new ModelBinder(new ObjectActivator()), new JsonView()));

                httpServer.Use(new CompressionHandler(DeflateCompressor.Default, GZipCompressor.Default));
                httpServer.Use(new ControllerHandler(new BaseController(), new JsonModelBinder(), new JsonView(), StringComparer.OrdinalIgnoreCase));
                httpServer.Use(new HttpRouter().With(string.Empty, new IndexHandler())
                    .With("about", new AboutHandler())
                    .With("Assets", new AboutHandler())
                    .With("strings", new RestHandler<string>(new StringsRestController(), JsonResponseProvider.Default)));
                
                httpServer.Use(new ClassRouter(new MySuperHandler()));
                httpServer.Use(new TimingHandler());

                httpServer.Use(new MyHandler());
                httpServer.Use(new FileHandler());
                httpServer.Use(new ErrorHandler());
                httpServer.Use((context, next) =>
                {
                    Console.WriteLine("Got Request!");
                    return next();
                });

                httpServer.Start();
                Console.ReadLine();
            }

        }
    }

    public class MySuperHandler : IHttpRequestHandler
    {
        private int _index;

        public MySuperHandler Child
        {
            get
            {
                _index++; return this; 
            }
        }
        public Task Handle(IHttpContext context, Func<Task> next)
        {
            context.Response = HttpResponse.CreateWithMessage(HttpResponseCode.Ok, "Hello!" + _index, true);
            return Task.Factory.GetCompleted();
        }


        [Indexer]
        public Task<IHttpRequestHandler> GetChild(IHttpContext context, int index)
        {
            _index += index;
            return Task.FromResult<IHttpRequestHandler>(this);
        }

    }

    class MyModel
    {
        public int MyProperty
        {
            get;
            set;
        }

        public MyModel Model
        {
            get;
            set;
        }
    }

    internal class MyHandler : IHttpRequestHandler
    {
        public Task Handle(IHttpContext context, Func<Task> next)
        {
            var model = new ModelBinder(new ObjectActivator()).Get<MyModel>(context.Request.QueryString);

            return Task.Factory.GetCompleted();
        }
    }

    
}