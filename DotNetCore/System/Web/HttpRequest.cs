using Microsoft.AspNetCore.Http;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Web
{
    public class HttpRequest
    {
        Microsoft.AspNetCore.Http.HttpContext context
        {
            get
            {
                return HttpContext.contextAccessor.HttpContext;
            }
        }
        Microsoft.AspNetCore.Http.HttpRequest request => context.Request;
        internal HttpRequest()
        {
            
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
                if (request.Method == "POST" && request.HasFormContentType && request.Form != null && request.Form.Keys.Count > 0)
                {
                    foreach (string key in request.Form.Keys)
                    {
                        nvc.Add(key, request.Form[key]);
                    }
                }
                return nvc;
            }
        }
        public HttpFileCollection Files
        {
            get
            {
                if (request.Method == "POST" && request.HasFormContentType && request.Form != null
                    && request.Form.Files != null && request.Form.Files.Count > 0)
                {
                    HttpFileCollection files = new HttpFileCollection();
                    foreach (IFormFile file in request.Form.Files)
                    {
                        files.Add(file.Name, new HttpPostedFile(file));
                    }
                    return files;
                }
                return null;
            }
        }

        public NameValueCollection QueryString
        {
            get
            {
                NameValueCollection nvc = new NameValueCollection();
                if (request.Query != null && request.Query.Keys.Count > 0)
                {
                    foreach (string key in request.Query.Keys)
                    {
                        nvc.Add(key, request.Query[key]);
                    }
                }
                return nvc;
            }
        }

        public HttpCookieCollection Cookies
        {
            get
            {
                HttpCookieCollection nvc = new HttpCookieCollection();
                if (request.Cookies != null && request.Cookies.Keys.Count > 0)
                {
                    foreach (string key in request.Cookies.Keys)
                    {
                        HttpCookie cookie = new HttpCookie(key, request.Cookies[key]);
                        nvc.Add(cookie, false);
                    }
                }
                return nvc;
            }
        }

        public string[] UserLanguages
        {
            get
            {
                string lang = request.Headers["Accept-Language"];
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
                return new StringBuilder()
                .Append(request.PathBase)
                .Append(request.Path.Value.Split('?')[0])
                .Append(request.QueryString).ToString();
            }
        }

        public Uri Url
        {
            get
            {
                return new Uri(new StringBuilder()
                .Append(request.Scheme)
                .Append("://")
                .Append(request.Host)
                .Append(request.PathBase)
                .Append(request.Path.Value.Split('?')[0])
                .Append(request.QueryString).ToString());

            }
        }
        public Uri UrlReferrer
        {
            get
            {
                Uri uri = null;
                string referer = request.Headers["Referer"];
                if (!string.IsNullOrEmpty(referer))
                {
                    uri = new Uri(referer);
                }
                return uri;
            }
        }
        public string UserHostAddress
        {
            get
            {
                return request.Headers["X-Original-For"];
            }
        }
        public string UserAgent
        {
            get
            {
                return request.Headers["User-Agent"];
            }
        }


        public string HttpMethod { get => request.Method; set => request.Method = value; }
        //public  string Method { get => request.Method; set => request.Method=value; }
        public string Scheme { get => request.Scheme; set => request.Scheme = value; }
        public bool IsHttps { get => request.IsHttps; set => request.IsHttps = value; }
        public HostString Host { get => request.Host; set => request.Host = value; }
        public PathString PathBase { get => request.PathBase; set => request.PathBase = value; }
        public PathString Path { get => request.Path; set => request.Path = value; }
        public IQueryCollection Query { get => request.Query; set => request.Query = value; }
        public string Protocol { get => request.Protocol; set => request.Protocol = value; }

        public NameValueCollection Headers
        {
            get
            {
                NameValueCollection nvc = new NameValueCollection();
                if (request.Headers != null && request.Headers.Keys.Count > 0)
                {
                    foreach (string key in request.Headers.Keys)
                    {
                        nvc.Add(key, request.Headers[key].ToString());
                    }
                }
                return nvc;
            }
        }

        public long? ContentLength { get => request.ContentLength; set => request.ContentLength = value; }
        public string ContentType { get => request.ContentType; set => request.ContentType = value; }
        public Stream InputStream => Body;
        public Stream Body { get => request.Body; set => request.Body = value; }

        public bool HasFormContentType => request.HasFormContentType;

        public Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return request.ReadFormAsync(cancellationToken = default(CancellationToken));
        }
        public string CurrentExecutionFilePathExtension
        {
            get
            {
                return IO.Path.GetExtension(request.Path);
            }
        }
    }
}