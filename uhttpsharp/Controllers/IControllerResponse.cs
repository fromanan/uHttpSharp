using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using uhttpsharp.Handlers;

namespace uhttpsharp.Controllers
{
    public interface IControllerResponse
    {
        Task<IHttpResponse> Respond(IHttpContext context, IView view);
    }

    public class CustomResponse : IControllerResponse
    {
        private readonly IHttpResponse httpResponse;
        public CustomResponse(IHttpResponse httpResponse)
        {
            this.httpResponse = httpResponse;
        }
        public Task<IHttpResponse> Respond(IHttpContext context, IView view)
        {
            return Task.FromResult(httpResponse);
        }
    }

    public class RenderResponse : IControllerResponse
    {
        public RenderResponse(HttpResponseCode code, object state)
        {
            Code = code;
            State = state;
        }

        public object State { get; }

        public HttpResponseCode Code { get; }

        public async Task<IHttpResponse> Respond(IHttpContext context, IView view)
        {
            IViewResponse output = await view.Render(context, State).ConfigureAwait(false);
            return StringHttpResponse.Create(output.Body, Code, output.ContentType);
        }
    }

    public class RedirectResponse : IControllerResponse
    {
        private readonly Uri newLocation;

        public RedirectResponse(Uri newLocation)
        {
            this.newLocation = newLocation;
        }

        public Task<IHttpResponse> Respond(IHttpContext context, IView view)
        {
            KeyValuePair<string, string>[] headers =
            {
                new("Location", newLocation.ToString())
            };
            return Task.FromResult<IHttpResponse>(
                new HttpResponse(HttpResponseCode.Found, string.Empty, headers, false));
        }
    }

    public static class Response
    {
        public static Task<IControllerResponse> Create(IControllerResponse response)
        {
            return Task.FromResult(response);
        }

        public static Task<IControllerResponse> Custom(IHttpResponse httpResponse)
        {
            return Create(new CustomResponse(httpResponse));
        }

        public static Task<IControllerResponse> Render(HttpResponseCode code, object state)
        {
            return Create(new RenderResponse(code, state));
        }

        public static Task<IControllerResponse> Render(HttpResponseCode code)
        {
            return Create(new RenderResponse(code, null));
        }

        public static Task<IControllerResponse> Redirect(Uri newLocation)
        {
            return Create(new RedirectResponse(newLocation));
        }
    }

    public static class Pipeline
    {
        private class EmptyPipeline : IPipeline
        {
            public Task<IControllerResponse> Go(Func<Task<IControllerResponse>> injectedTask, IHttpContext context)
            {
                return injectedTask();
            }
        }

        public static IPipeline Empty = new EmptyPipeline();
    }
}