using System.Collections.Specialized;
using CYQ.Data.Tool;
using CYQ.Data;

namespace System.Web
{
    public class HttpCookieCollection : NameObjectCollectionBase
    {
        MDictionary<string, HttpCookie> dic = new MDictionary<string, HttpCookie>();

        Microsoft.AspNetCore.Http.HttpContext context
        {
            get
            {
                return HttpContext.contextAccessor.HttpContext;
            }
        }
        /// <summary>
        /// 是否来源Response.Cookie，反之Request.Cookie
        /// </summary>
        bool IsForResponse;
        internal HttpCookieCollection(bool isForResponse)
        {
            IsForResponse = isForResponse;
        }
        public HttpCookie this[int index]
        {
            get
            {
                return dic[index];
            }
        }

        public HttpCookie this[string name]
        {
            get
            {
                return dic[name];
            }
        }

        public void Add(HttpCookie cookie)
        {
            if (cookie != null)
            {
                dic.Add(cookie.Name, cookie);
                if (IsForResponse && context != null)
                {
                    if (!context.Response.Headers.IsReadOnly)
                    {
                        context.Response.Cookies.Append(cookie.Name, cookie.Value, cookie.ToCookieOptions());
                    }
                    else
                    {
                        Log.Write("Response.Headers.IsReadOnly,Can't Set Cookie : " + cookie.Name + "," + cookie.Value, LogType.Error);
                    }
                }
            }
        }
        public void Set(HttpCookie cookie)
        {
            Remove(cookie.Name);
            Add(cookie);
        }
        public void Remove(string name)
        {
            dic.Remove(name);
            if (IsForResponse)
            {
                context.Response.Cookies.Delete(name);
            }
        }
        public void Clear()
        {
            if (IsForResponse)
            {
                foreach (string name in dic.Keys)
                {
                    context.Response.Cookies.Delete(name);
                }
            }
            dic.Clear();
        }
        public override int Count
        {
            get
            {
                return dic.Count;
            }
        }
    }
}
