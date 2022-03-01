using System;
using uhttpsharp.Headers;

namespace uhttpsharp.RequestProviders
{
    internal class HttpRequestMethodDecorator : IHttpRequest
    {
        private readonly IHttpRequest child;

        public HttpRequestMethodDecorator(IHttpRequest child, HttpMethods method)
        {
            this.child = child;
            Method = method;
        }

        public IHttpHeaders Headers => child.Headers;

        public HttpMethods Method { get; }

        public string Protocol => child.Protocol;

        public Uri Uri => child.Uri;

        public string[] RequestParameters => child.RequestParameters;

        public IHttpPost Post => child.Post;

        public IHttpHeaders QueryString => child.QueryString;
    }
}