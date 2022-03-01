using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using uhttpsharp.Headers;

namespace uhttpsharp.ModelBinders
{
    public class JsonModelBinder : IModelBinder
    {
        private readonly JsonSerializer serializer;

        public JsonModelBinder(JsonSerializer serializer)
        {
            this.serializer = serializer;
        }

        public JsonModelBinder() : this(JsonSerializer.CreateDefault()) { }
        
        public T Get<T>(byte[] raw, string prefix)
        {
            string rawDecoded = Encoding.UTF8.GetString(raw);

            if (raw.Length == 0)
            {
                return default;
            }

            if (prefix == null && typeof(T) == typeof(string))
            {
                return (T)(object)rawDecoded;
            }

            JToken jToken = JToken.Parse(rawDecoded);

            if (prefix != null)
            {
                jToken = jToken.SelectToken(prefix);
            }

            return jToken.ToObject<T>(serializer);
        }
        
        public T Get<T>(IHttpHeaders headers)
        {
            throw new NotSupportedException();
        }
        
        public T Get<T>(IHttpHeaders headers, string prefix)
        {
            throw new NotSupportedException();
        }
    }
}