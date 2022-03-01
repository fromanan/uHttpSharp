using System.Threading.Tasks;

namespace uhttpsharp.RequestProviders
{
    public class HttpRequestProviderMethodOverrideDecorator : IHttpRequestProvider
    {
        private readonly IHttpRequestProvider child;

        public HttpRequestProviderMethodOverrideDecorator(IHttpRequestProvider child)
        {
            this.child = child;
        }

        public async Task<IHttpRequest> Provide(IStreamReader streamReader)
        {
            IHttpRequest childValue = await child.Provide(streamReader).ConfigureAwait(false);

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