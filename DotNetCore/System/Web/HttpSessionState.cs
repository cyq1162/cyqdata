using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace System.Web
{
    public class HttpSessionState:ISession
    {
        Microsoft.AspNetCore.Http.HttpContext context
        {
            get
            {
                return HttpContext.contextAccessor.HttpContext;
            }
        }

        internal HttpSessionState()
        {
           
        }

        public string SessionID { get { return context.Session.Id; } }

        public bool IsAvailable => context.Session.IsAvailable;

        public string Id => context.Session.Id;

        public IEnumerable<string> Keys => context.Session.Keys;

        public void Clear()
        {
            context.Session.Clear();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
             return context.Session.CommitAsync(cancellationToken = default(CancellationToken));
        }

        public Task LoadAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return context.Session.LoadAsync(cancellationToken = default(CancellationToken));
        }

        public void Remove(string key)
        {
            context.Session.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            context.Session.Set(key, value);
        }

        public bool TryGetValue(string key, out byte[] value)
        {
           return context.Session.TryGetValue(key, out value);
        }
    }
}