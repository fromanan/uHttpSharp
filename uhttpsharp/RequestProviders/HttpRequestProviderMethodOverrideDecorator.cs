using System.Threading.Tasks;

namespace uhttpsharp.RequestProviders
{
    public class HttpRequestProviderMethodOverrideDecorator : IHttpRequestProvider
    {
        private readonly IHttpRequestProvider _child;

        public HttpRequestProviderMethodOverrideDecorator(IHttpRequestProvider child)
        {
            _child = child;
        }

        public async Task<IHttpRequest> Provide(IStreamReader streamReader)
        {
            IHttpRequest childValue = await _child.Provide(streamReader).ConfigureAwait(false);

            if (childValue == null)
            {
                return null;
            }

            return !childValue.Headers.TryGetByName("X-HTTP-Method-Override", out string methodName)
                ? childValue
                : new HttpRequestMethodDecorator(childValue, HttpMethodProvider.Default.Provide(methodName));
        }
    }
}