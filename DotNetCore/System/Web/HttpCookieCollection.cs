using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using CYQ.Data.Tool;
using Microsoft.AspNetCore.Http;

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
                CookieOptions op = new CookieOptions();
                op.Domain = cookie.Domain;
                op.Expires = cookie.Expires;
                op.HttpOnly = cookie.HttpOnly;
                op.Path = cookie.Path;
                context.Response.Cookies.Append(cookie.Name, cookie.Value,op);
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
