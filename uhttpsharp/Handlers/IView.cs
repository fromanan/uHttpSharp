using System.Threading.Tasks;
using Newtonsoft.Json;

namespace uhttpsharp.Handlers
{
    public interface IViewResponse
    {
        string Body { get; }

        string ContentType { get; }
    }

    public interface IView
    {
        Task<IViewResponse> Render(IHttpContext context, object state);
    }

    public class JsonView : IView
    {
        public Task<IViewResponse> Render(IHttpContext context, object state)
        {
            return Task.FromResult<IViewResponse>(new JsonViewResponse(JsonConvert.SerializeObject(state)));
        }

        private class JsonViewResponse : IViewResponse
        {
            public JsonViewResponse(string body)
            {
                Body = body;
            }
            
            public string Body { get; }

            public string ContentType => "application/json; charset=utf-8";
        }
    }
}