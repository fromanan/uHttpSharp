using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using uhttpsharp;

namespace uhttpsharpdemo
{
    internal class SomeRestController
    {
        private readonly IDictionary<int, string> _strings = new Dictionary<int, string> { { 1, "Hahaha" } };

        public Task<uhttpsharp.HttpResponse> Get(IHttpRequest request)
        {
            MemoryStream memoryStream = new MemoryStream();
            JsonWriter writer = new JsonTextWriter(new StreamWriter(memoryStream));

            JsonSerializer.Create().Serialize(writer, _strings);
            writer.Flush();
            return Task.FromResult(new HttpResponse(HttpResponseCode.Ok, "application/json; charset=utf-8", memoryStream,
                true));
        }

        public Task<HttpResponse> GetItem(IHttpRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponse> Create(IHttpRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponse> Upsert(IHttpRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponse> Delete(IHttpRequest request)
        {
            throw new NotImplementedException();
        }
    }
}