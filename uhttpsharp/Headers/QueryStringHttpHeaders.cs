using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace uhttpsharp.Headers
{
    [DebuggerDisplay("{Count} Query String Headers")]
    [DebuggerTypeProxy(typeof(HttpHeadersDebuggerProxy))]
    internal class QueryStringHttpHeaders : IHttpHeaders
    {
        private readonly HttpHeaders _child;
        private static readonly char[] Separators = { '&', '=' };

        public QueryStringHttpHeaders(string query)
        {
            string[] splitKeyValues = query.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> values = new Dictionary<string, string>(splitKeyValues.Length / 2,
                StringComparer.InvariantCultureIgnoreCase);

            for (int i = 0; i < splitKeyValues.Length; i += 2)
            {
                string key = Uri.UnescapeDataString(splitKeyValues[i]);
                string value = null;
                if (splitKeyValues.Length > i + 1)
                {
                    value = Uri.UnescapeDataString(splitKeyValues[i + 1]).Replace('+', ' ');
                }

                values[key] = value;
            }

            Count = values.Count;
            _child = new HttpHeaders(values);
        }

        public string GetByName(string name)
        {
            return _child.GetByName(name);
        }
        
        public bool TryGetByName(string name, out string value)
        {
            return _child.TryGetByName(name, out value);
        }
        
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _child.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal int Count { get; }
    }
}