using System;
using uhttpsharp.Headers;

namespace uhttpsharp.RequestProviders
{
    internal class HttpRequestMethodDecorator : IHttpRequest
    {
        private readonly IHttpRequest _child;

        public HttpRequestMethodDecorator(IHttpRequest child, HttpMethods method)
        {
            _child = child;
            Method = method;
        }

        public IHttpHeaders Headers => _child.Headers;

        public HttpMethods Method { get; }

        public string Protocol => _child.Protocol;

        public Uri Uri => _child.Uri;

        public string[] RequestParameters => _child.RequestParameters;

        public IHttpPost Post => _child.Post;

        public IHttpHeaders QueryString => _child.QueryString;
    }
}