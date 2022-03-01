using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uhttpsharp.RequestProviders;

namespace uhttpsharp.Headers
{
    internal class EmptyHttpPost : IHttpPost
    {
        private static readonly byte[] EmptyBytes = Array.Empty<byte>();

        public static readonly IHttpPost Empty = new EmptyHttpPost();

        private EmptyHttpPost() { }

        #region IHttpPost implementation
        public byte[] Raw => EmptyBytes;

        public IHttpHeaders Parsed => EmptyHttpHeaders.Empty;
        #endregion
    }

    internal class HttpPost : IHttpPost
    {
        public static async Task<IHttpPost> Create(IStreamReader reader, int postContentLength)
        {
            byte[] raw = await reader.ReadBytes(postContentLength).ConfigureAwait(false);
            return new HttpPost(raw, postContentLength);
        }

        private readonly int readBytes;

        private readonly Lazy<IHttpHeaders> parsed;

        public HttpPost(byte[] raw, int readBytes)
        {
            Raw = raw;
            this.readBytes = readBytes;
            parsed = new Lazy<IHttpHeaders>(Parse);
        }

        private IHttpHeaders Parse()
        {
            string body = Encoding.UTF8.GetString(Raw, 0, readBytes);
            QueryStringHttpHeaders parsed = new(body);

            return parsed;
        }

        #region IHttpPost implementation
        public byte[] Raw { get; }

        public IHttpHeaders Parsed => parsed.Value;
        #endregion
    }

    [DebuggerDisplay("{Count} Headers")]
    [DebuggerTypeProxy(typeof(HttpHeadersDebuggerProxy))]
    public class ListHttpHeaders : IHttpHeaders
    {
        private readonly IList<KeyValuePair<string, string>> values;
        
        public ListHttpHeaders(IList<KeyValuePair<string, string>> values)
        {
            this.values = values;
        }

        public string GetByName(string name)
        {
            return values.Where(kvp => kvp.Key.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                .Select(kvp => kvp.Value).First();
        }

        public bool TryGetByName(string name, out string value)
        {
            value = values.Where(kvp => kvp.Key.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                .Select(kvp => kvp.Value).FirstOrDefault();

            return value != default;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal int Count => values.Count;
    }

    [DebuggerDisplay("{Count} Headers")]
    [DebuggerTypeProxy(typeof(HttpHeadersDebuggerProxy))]
    public class HttpHeaders : IHttpHeaders
    {
        private readonly IDictionary<string, string> values;

        public HttpHeaders(IDictionary<string, string> values)
        {
            this.values = values;
        }

        public string GetByName(string name)
        {
            return values[name];
        }
        
        public bool TryGetByName(string name, out string value)
        {
            return values.TryGetValue(name, out value);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal int Count => values.Count;
    }
}