using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uhttpsharp.Headers;

namespace uhttpsharp.Handlers
{
    public class BasicAuthenticationHandler : IHttpRequestHandler
    {
        private const string BasicPrefix = "Basic ";
        private static readonly int BasicPrefixLength = BasicPrefix.Length;

        private readonly string username;
        private readonly string password;
        private readonly string authenticationKey;
        private readonly ListHttpHeaders headers;

        public BasicAuthenticationHandler(string realm, string username, string password)
        {
            this.username = username;
            this.password = password;
            authenticationKey = $"Authenticated.{realm}";
            headers = new ListHttpHeaders(new List<KeyValuePair<string, string>>
            {
                new("WWW-Authenticate", $@"Basic realm=""{realm}""")
            });
        }

        public Task Handle(IHttpContext context, Func<Task> next)
        {
            IDictionary<string, dynamic> session = context.State.Session;

            if (session.TryGetValue(authenticationKey, out dynamic ipAddress) && ipAddress == context.RemoteEndPoint)
                return next();
            
            if (TryAuthenticate(context, session))
            {
                return next();
            }

            context.Response = StringHttpResponse.Create("Not Authenticated", HttpResponseCode.Unauthorized, headers: headers);

            return Task.Factory.GetCompleted();

        }

        private bool TryAuthenticate(IHttpContext context, IDictionary<string, dynamic> session)
        {
            if (!context.Request.Headers.TryGetByName("Authorization", out string credentials)) return false;
            
            if (!TryAuthenticate(credentials)) return false;
            
            session[authenticationKey] = context.RemoteEndPoint;
            {
                return true;
            }
        }

        private bool TryAuthenticate(string credentials)
        {
            if (!credentials.StartsWith(BasicPrefix))
            {
                return false;
            }

            string basicCredentials = credentials[BasicPrefixLength..];

            string usernameAndPassword = Encoding.UTF8.GetString(Convert.FromBase64String(basicCredentials));
            int index = usernameAndPassword.IndexOf(':');
            if (index == -1) return false;
            string username = usernameAndPassword[..index];
            string password = usernameAndPassword[(index + 1)..];

            return username == this.username && password == this.password;
        }
    }
}