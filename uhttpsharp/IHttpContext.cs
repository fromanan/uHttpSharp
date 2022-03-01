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

        private readonly Dictionary<string, string> values;

        public bool Touched { get; private set; }

        public string ToCookieData()
        {
            StringBuilder builder = new StringBuilder();

            foreach ((string key, string value) in values)
            {
                builder.AppendFormat("Set-Cookie: {0}={1}{2}", key, value, Environment.NewLine);
            }

            return builder.ToString();
        }

        public CookiesStorage(string cookie)
        {
            string[] keyValues = cookie.Split(CookieSeparators, StringSplitOptions.RemoveEmptyEntries);
            values = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            for (int i = 0; i < keyValues.Length; i += 2)
            {
                string key = keyValues[i];
                string value = keyValues[i + 1];

                values[key] = value;
            }
        }

        public void Upsert(string key, string value)
        {
            values[key] = value;

            Touched = true;
        }

        public void Remove(string key)
        {
            if (values.Remove(key))
            {
                Touched = true;
            }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string GetByName(string name)
        {
            return values[name];
        }

        public bool TryGetByName(string name, out string value)
        {
            return values.TryGetValue(name, out value);
        }
    }
}