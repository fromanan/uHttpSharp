using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace uhttpsharp.Headers
{
    internal class HttpHeadersDebuggerProxy
    {
        private readonly IHttpHeaders real;

        [DebuggerDisplay("{Value,nq}", Name = "{Key,nq}")]
        internal class HttpHeader
        {
            private readonly KeyValuePair<string, string> header;
            public HttpHeader(KeyValuePair<string, string> header)
            {
                this.header = header;
            }

            public string Value => header.Value;

            public string Key => header.Key;
        }

        public HttpHeadersDebuggerProxy(IHttpHeaders real)
        {
            this.real = real;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public HttpHeader[] Headers
        {
            get { return real.Select(kvp => new HttpHeader(kvp)).ToArray(); }
        }
    }
}