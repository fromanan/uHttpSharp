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
using System.Diagnostics;
using uhttpsharp.Headers;

namespace uhttpsharp
{
    [DebuggerDisplay("{Method} {OriginalUri,nq}")]
    internal class HttpRequest : IHttpRequest
    {
        public HttpRequest(IHttpHeaders headers, HttpMethods method, string protocol, Uri uri, string[] requestParameters,
            IHttpHeaders queryString, IHttpPost post)
        {
            Headers = headers;
            Method = method;
            Protocol = protocol;
            Uri = uri;
            RequestParameters = requestParameters;
            QueryString = queryString;
            Post = post;
        }

        public IHttpHeaders Headers { get; }

        public HttpMethods Method { get; }

        public string Protocol { get; }

        public Uri Uri { get; }

        public string[] RequestParameters { get; }

        public IHttpPost Post { get; }

        public IHttpHeaders QueryString { get; }

        internal string OriginalUri =>
            QueryString == null ? Uri.OriginalString : $"{Uri.OriginalString}?{QueryString.ToUriData()}";
    }

    public interface IHttpRequest
    {
        IHttpHeaders Headers { get; }

        HttpMethods Method { get; }

        string Protocol { get; }

        Uri Uri { get; }

        string[] RequestParameters { get; }

        IHttpPost Post { get; }

        IHttpHeaders QueryString { get; }
    }

    public interface IHttpPost
    {
        byte[] Raw { get; }

        IHttpHeaders Parsed { get; }
    }

    public sealed class HttpRequestParameters
    {
        private readonly string[] _params;

        private static readonly char[] Separators = { '/' };

        public HttpRequestParameters(Uri uri)
        {
            string url = uri.OriginalString;
            _params = url.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        }

        public IList<string> Params => _params;
    }
}