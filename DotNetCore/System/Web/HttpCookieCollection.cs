using System.Collections.Specialized;
using CYQ.Data.Tool;
using Microsoft.AspNetCore.Http;
using CYQ.Data;

namespace System.Web
{
    public class HttpCookieCollection : NameObjectCollectionBase, IResponseCookies
    {
        MDictionary<string, HttpCookie> dic = new MDictionary<string, HttpCookie>();

        Microsoft.AspNetCore.Http.HttpContext context;

        public HttpCookieCollection(Microsoft.AspNetCore.Http.HttpContext context)
        {
            this.context = context;
        }

        public HttpCookie this[int index] { get { return dic[index]; } }

        public HttpCookie this[string name] { get { return dic[name]; } }

        public void Add(HttpCookie cookie)
        {
            Add(cookie, true);
        }
        public void Add(HttpCookie cookie, bool appendToReponse)
        {
            if (!appendToReponse)
            {
                dic.Add(cookie.Name, cookie);
                return;
            }
            if (context != null && cookie != null)
            {
                Append(cookie.Name, cookie.Value, cookie.ToCookieOptions());
            }
        }

        public void Append(string key, string value)
        {
            Append(key, value, new CookieOptions());
        }

        public void Append(string key, string value, CookieOptions options)
        {
            // context.Response.Headers["Set-Cookie"]
            //context.Response.Cookies.Delete(key);
            if (!context.Response.Headers.IsReadOnly)
            {
                context.Response.Cookies.Append(key, value, options);
                HttpCookie cookie = options;
                cookie.Name = key;
                cookie.Value = value;
                dic.Add(cookie.Name, cookie);
            }
            else
            {
                Log.Write("Response.Headers.IsReadOnly,Can't Set Cookie : " + key + "," + value,LogType.Error);
            }
        }

        public void Delete(string key)
        {
            Delete(key, null);
        }

        public void Delete(string key, CookieOptions options)
        {
            context.Response.Cookies.Delete(key, options);
            dic.Remove(key);
        }
    }
}
