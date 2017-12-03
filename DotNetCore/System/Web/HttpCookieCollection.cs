using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using CYQ.Data.Tool;
namespace System.Web
{
    internal class HttpCookieCollection : NameObjectCollectionBase
    {
        MDictionary<string, HttpCookie> dic = new MDictionary<string, HttpCookie>();

        Microsoft.AspNetCore.Http.HttpContext context;
        public HttpCookieCollection()
        {

        }
        public HttpCookieCollection(Microsoft.AspNetCore.Http.HttpContext context)
        {
            this.context = context;
        }
        
        public HttpCookie this[int index] { get { return dic[index]; } }
      
        public HttpCookie this[string name] { get { return dic[name]; } }
        public void Add(HttpCookie cookie)
        {
            if (context != null && cookie!=null)
            {
                context.Response.Cookies.Append(cookie.Name, cookie.Value);//找不到其它Cookie的操作，暂时先这样。
                dic.Add(cookie.Name, cookie);
                //Microsoft.AspNetCore.Authentication.Cookies.
                //Microsoft.AspNetCore.Http.CookieBuilder cb = new Microsoft.AspNetCore.Http.CookieBuilder();
                //cb.Domain = cookie.Domain;
                //cb.Name = cookie.Name;
                //cb.Expiration = cookie.Expires-DateTime.Now;
                //cb.HttpOnly = cookie.HttpOnly;
                ////cb.
                //cb.Build(context);
            }
        }
    }
}
