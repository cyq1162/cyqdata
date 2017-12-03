using System.Collections.Specialized;
using System.Text;

namespace System.Web
{
    internal class HttpRequest
    {
        Microsoft.AspNetCore.Http.HttpContext context;
        public HttpRequest(Microsoft.AspNetCore.Http.HttpContext context)
        {
            this.context = context;
        }

        public string this[string name]
        {
            get
            {
                string value = QueryString[name];
                if (string.IsNullOrEmpty(value))
                {
                    value = Form[name];
                }
                return value;
            }
        }
        public NameValueCollection Form
        {
            get
            {
                NameValueCollection nvc = new NameValueCollection();
                if (context.Request.Form != null && context.Request.Form.Keys.Count > 0)
                {
                    foreach (string key in context.Request.Form.Keys)
                    {
                        nvc.Add(key, context.Request.Form[key]);
                    }
                }
                return nvc;
            }
        }
        public NameValueCollection QueryString
        {
            get
            {
                NameValueCollection nvc = new NameValueCollection();
                if (context.Request.Query != null && context.Request.Query.Keys.Count > 0)
                {
                    foreach (string key in context.Request.Query.Keys)
                    {
                        nvc.Add(key, context.Request.Query[key]);
                    }
                }
                return nvc;
            }
        }
        public HttpCookieCollection Cookies
        {
            get
            {
                HttpCookieCollection nvc = new HttpCookieCollection(context);
                if (context.Request.Cookies != null && context.Request.Cookies.Keys.Count > 0)
                {
                    foreach (string key in context.Request.Cookies.Keys)
                    {
                        HttpCookie cookie = new HttpCookie(key, context.Request.Cookies[key]);
                        nvc.Add(cookie);
                    }
                }
                return nvc;
            }
        }
        public string[] UserLanguages
        {
            get
            {
                string lang = context.Request.Headers["Accept-Language"];
                if (!string.IsNullOrEmpty(lang))
                {
                    return lang.Split(';');
                }
                return null;
            }
        }
        public string RawUrl
        {
            get
            {
                Microsoft.AspNetCore.Http.HttpRequest request = context.Request;
                return new StringBuilder()
                .Append(request.PathBase)
                .Append(request.Path)
                .Append(request.QueryString).ToString();
            }
        }

        public Uri Url
        {
            get
            {
                Microsoft.AspNetCore.Http.HttpRequest request = context.Request;
                return new Uri(new StringBuilder()
                .Append(request.Scheme)
                .Append("://")
                .Append(request.Host)
                .Append(request.PathBase)
                .Append(request.Path)
                .Append(request.QueryString).ToString());

            }
        }
        public Uri UrlReferrer {
            get
            {
                Uri uri=null;
                string referer = context.Request.Headers["Referer"];
                if (!string.IsNullOrEmpty(referer))
                {
                    uri = new Uri(referer);
                }
                return uri;
            }
        }
    }
}