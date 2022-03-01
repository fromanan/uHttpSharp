using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using uhttpsharp.Headers;

namespace uhttpsharp.RequestProviders
{
    public class HttpRequestProvider : IHttpRequestProvider
    {
        private static readonly char[] Separators = { '/' };

        public async Task<IHttpRequest> Provide(IStreamReader reader)
        {
            // parse the http request
            string request = await reader.ReadLine().ConfigureAwait(false);

            if (request == null)
                return null;

            int firstSpace = request.IndexOf(' ');
            int lastSpace = request.LastIndexOf(' ');

            string[] tokens =
            {
                request[..firstSpace],
                request.Substring(firstSpace + 1, lastSpace - firstSpace - 1),
                request[(lastSpace + 1)..]
            };

            if (tokens.Length != 3)
            {
                return null;
            }

            string httpProtocol = tokens[2];

            string url = tokens[1];
            IHttpHeaders queryString = GetQueryStringData(ref url);
            Uri uri = new(url, UriKind.Relative);

            List<KeyValuePair<string, string>> headersRaw = new();

            // get the headers
            string line;

            while (!string.IsNullOrEmpty((line = await reader.ReadLine().ConfigureAwait(false))))
            {
                KeyValuePair<string, string> headerKvp = SplitHeader(line);
                headersRaw.Add(headerKvp);
            }

            IHttpHeaders headers =
                new HttpHeaders(headersRaw.ToDictionary(k => k.Key, k => k.Value,
                    StringComparer.InvariantCultureIgnoreCase));
            IHttpPost post = await GetPostData(reader, headers).ConfigureAwait(false);

            if (!headers.TryGetByName("_method", out string verb))
            {
                verb = tokens[0];
            }

            HttpMethods httpMethod = HttpMethodProvider.Default.Provide(verb);
            return new HttpRequest(headers, httpMethod, httpProtocol, uri,
                uri.OriginalString.Split(Separators, StringSplitOptions.RemoveEmptyEntries), queryString, post);
        }

        private static IHttpHeaders GetQueryStringData(ref string url)
        {
            int queryStringIndex = url.IndexOf('?');
            IHttpHeaders queryString;
            if (queryStringIndex != -1)
            {
                queryString = new QueryStringHttpHeaders(url[(queryStringIndex + 1)..]);
                url = url[..queryStringIndex];
            }
            else
            {
                queryString = EmptyHttpHeaders.Empty;
            }

            return queryString;
        }

        private static async Task<IHttpPost> GetPostData(IStreamReader streamReader, IHttpHeaders headers)
        {
            IHttpPost post;
            if (headers.TryGetByName("content-length", out int postContentLength) && postContentLength > 0)
            {
                post = await HttpPost.Create(streamReader, postContentLength).ConfigureAwait(false);
            }
            else
            {
                post = EmptyHttpPost.Empty;
            }

            return post;
        }

        private static KeyValuePair<string, string> SplitHeader(string header)
        {
            int index = header.IndexOf(": ", StringComparison.InvariantCultureIgnoreCase);
            return new KeyValuePair<string, string>(header[..index], header[(index + 2)..]);
        }
    }
}