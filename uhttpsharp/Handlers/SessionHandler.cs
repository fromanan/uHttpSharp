using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uhttpsharp.Handlers
{
    public class SessionHandler<TSession> : IHttpRequestHandler
    {
        private readonly Func<TSession> sessionFactory;
        private readonly TimeSpan expiration;

        private static readonly Random RandomGenerator = new();

        private class SessionHolder
        {
            private readonly TSession session;

            public TSession Session
            {
                get
                {
                    LastAccessTime = DateTime.Now;
                    return session;
                }
            }

            public DateTime LastAccessTime { get; private set; } = DateTime.Now;

            public SessionHolder(TSession session)
            {
                this.session = session;
            }
        }

        private readonly ConcurrentDictionary<string, SessionHolder> sessions = new();

        public SessionHandler(Func<TSession> sessionFactory, TimeSpan expiration)
        {
            this.sessionFactory = sessionFactory;
            this.expiration = expiration;
        }

        private const string SessionID = "SESSID";

        public Task Handle(IHttpContext context, Func<Task> next)
        {
            if (!context.Cookies.TryGetByName(SessionID, out string sessId))
            {
                sessId = RandomGenerator.Next().ToString(CultureInfo.InvariantCulture);
                context.Cookies.Upsert(SessionID, sessId);
            }

            SessionHolder sessionHolder = sessions.GetOrAdd(sessId, CreateSession);

            if (DateTime.Now - sessionHolder.LastAccessTime > expiration)
            {
                sessionHolder = CreateSession(sessId);
                sessions.AddOrUpdate(sessId, sessionHolder, (sessionId, oldSession) => sessionHolder);
            }

            context.State.Session = sessionHolder.Session;

            return next();
        }

        private SessionHolder CreateSession(string sessionId)
        {
            return new SessionHolder(sessionFactory());
        }
    }
}