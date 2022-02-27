using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uhttpsharp.RequestProviders;
using uhttpsharp.Logging;

namespace uhttpsharp.Headers
{
    internal class EmptyHttpPost : IHttpPost
    {
        private static readonly byte[] _emptyBytes = Array.Empty<byte>();

        public static readonly IHttpPost Empty = new EmptyHttpPost();

        private EmptyHttpPost() { }

        #region IHttpPost implementation
        public byte[] Raw => _emptyBytes;

        public IHttpHeaders Parsed => EmptyHttpHeaders.Empty;
        #endregion
    }

    internal class HttpPost : IHttpPost
    {
        public static async Task<IHttpPost> Create(IStreamReader reader, int postContentLength, ILog logger)
        {
            byte[] raw = await reader.ReadBytes(postContentLength).ConfigureAwait(false);
            return new HttpPost(raw, postContentLength);
        }

        private readonly int _readBytes;

        private readonly Lazy<IHttpHeaders> _parsed;

        public HttpPost(byte[] raw, int readBytes)
        {
            Raw = raw;
            _readBytes = readBytes;
            _parsed = new Lazy<IHttpHeaders>(Parse);
        }

        private IHttpHeaders Parse()
        {
            string body = Encoding.UTF8.GetString(Raw, 0, _readBytes);
            QueryStringHttpHeaders parsed = new QueryStringHttpHeaders(body);

            return parsed;
        }

        #region IHttpPost implementation
        public byte[] Raw { get; }

        public IHttpHeaders Parsed => _parsed.Value;
        #endregion
    }

    [DebuggerDisplay("{Count} Headers")]
    [DebuggerTypeProxy(typeof(HttpHeadersDebuggerProxy))]
    public class ListHttpHeaders : IHttpHeaders
    {
        private readonly IList<KeyValuePair<string, string>> _values;
        
        public ListHttpHeaders(IList<KeyValuePair<string, string>> values)
        {
            _values = values;
        }

        public string GetByName(string name)
        {
            return _values.Where(kvp => kvp.Key.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                .Select(kvp => kvp.Value).First();
        }

        public bool TryGetByName(string name, out string value)
        {
            value = _values.Where(kvp => kvp.Key.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                .Select(kvp => kvp.Value).FirstOrDefault();

            return value != default;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal int Count => _values.Count;
    }

    [DebuggerDisplay("{Count} Headers")]
    [DebuggerTypeProxy(typeof(HttpHeadersDebuggerProxy))]
    public class HttpHeaders : IHttpHeaders
    {
        private readonly IDictionary<string, string> _values;

        public HttpHeaders(IDictionary<string, string> values)
        {
            _values = values;
        }

        public string GetByName(string name)
        {
            return _values[name];
        }
        
        public bool TryGetByName(string name, out string value)
        {
            return _values.TryGetValue(name, out value);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal int Count => _values.Count;
    }
}