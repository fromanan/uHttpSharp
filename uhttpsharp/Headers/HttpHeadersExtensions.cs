using System;
using System.Collections.Generic;
using System.Text;

namespace uhttpsharp.Headers
{
    public static class HttpHeadersExtensions
    {
        public static bool KeepAliveConnection(this IHttpHeaders headers)
        {
            return headers.TryGetByName("connection", out string value)
                   && value.Equals("Keep-Alive", StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool TryGetByName<T>(this IHttpHeaders headers, string name, out T value)
        {
            if (headers.TryGetByName(name, out string stringValue))
            {
                value = (T)Convert.ChangeType(stringValue, typeof(T));
                return true;
            }

            value = default;
            return false;
        }

        public static T GetByName<T>(this IHttpHeaders headers, string name)
        {
            headers.TryGetByName(name, out T value);
            return value;
        }

        public static T GetByNameOrDefault<T>(this IHttpHeaders headers, string name, T defaultValue)
        {
            return headers.TryGetByName(name, out T value) ? value : defaultValue;
        }

        public static string ToUriData(this IHttpHeaders headers)
        {
            StringBuilder builder = new();

            foreach ((string key, string value) in headers)
            {
                builder.AppendFormat("{0}={1}&", Uri.EscapeDataString(key), Uri.EscapeDataString(value));
            }

            return builder.ToString(0, builder.Length - 1);
        }
    }
}