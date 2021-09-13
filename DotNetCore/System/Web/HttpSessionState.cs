using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace System.Web
{
    public class HttpSessionState : ISession
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

        public string SessionID
        {
            get
            {
                if (SessionIsNull())
                {
                    return null;
                }
                return context.Session.Id;
            }
        }

        public bool IsAvailable
        {
            get
            {
                if (SessionIsNull())
                {
                    return false;
                }
                return context.Session.IsAvailable;
            }
        }

        public string Id => SessionID;

        public IEnumerable<string> Keys
        {
            get
            {
                if (SessionIsNull())
                {
                    return null;
                }
                return context.Session.Keys;
            }
        }


        public void Clear()
        {
            if (SessionIsNull())
            {
                return;
            }
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
            if (SessionIsNull())
            {
                return;
            }
            context.Session.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            if (SessionIsNull())
            {
                return;
            }
            context.Session.Set(key, value);
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            if (SessionIsNull())
            {
                value = null;
                return false;
            }
            return context.Session.TryGetValue(key, out value);
        }
        private bool SessionIsNull()
        {
            if (context == null)
            {
                return true;
            }
            try
            {
                return context.Session == null;//可能抛异常
            }
            catch
            {
                return true;
            }
        }
    }
}