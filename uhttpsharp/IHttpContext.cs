using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using uhttpsharp.Headers;

namespace uhttpsharp
{
    public interface IHttpContext
    {
        IHttpRequest Request { get; }

        IHttpResponse Response { get; set; }

        ICookiesStorage Cookies { get; }

        dynamic State { get; }

        EndPoint RemoteEndPoint { get; }
    }

    public interface ICookiesStorage : IHttpHeaders
    {
        void Upsert(string key, string value);

        void Remove(string key);

        bool Touched { get; }

        string ToCookieData();
    }

    public class CookiesStorage : ICookiesStorage
    {
        private static readonly string[] CookieSeparators = { "; ", "=" };

        private readonly Dictionary<string, string> _values;

        public bool Touched { get; private set; }

        public string ToCookieData()
        {
            StringBuilder builder = new StringBuilder();

            foreach (KeyValuePair<string, string> kvp in _values)
            {
                builder.AppendFormat("Set-Cookie: {0}={1}{2}", kvp.Key, kvp.Value, Environment.NewLine);
            }

            return builder.ToString();
        }

        public CookiesStorage(string cookie)
        {
            string[] keyValues = cookie.Split(CookieSeparators, StringSplitOptions.RemoveEmptyEntries);
            _values = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            for (int i = 0; i < keyValues.Length; i += 2)
            {
                string key = keyValues[i];
                string value = keyValues[i + 1];

                _values[key] = value;
            }
        }

        public void Upsert(string key, string value)
        {
            _values[key] = value;

            Touched = true;
        }

        public void Remove(string key)
        {
            if (_values.Remove(key))
            {
                Touched = true;
            }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string GetByName(string name)
        {
            return _values[name];
        }

        public bool TryGetByName(string name, out string value)
        {
            return _values.TryGetValue(name, out value);
        }
    }
}