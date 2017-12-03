using System;
using System.Collections.Generic;
using System.Text;

namespace System.Web
{
    internal class HttpResponse
    {
        Microsoft.AspNetCore.Http.HttpContext context;
        public HttpResponse(Microsoft.AspNetCore.Http.HttpContext context)
        {
            this.context = context;
        }
        public HttpCookieCollection Cookies
        {
            get
            {
                HttpCookieCollection nvc = new HttpCookieCollection();
                
                return nvc;
            }
        }
    }
}
