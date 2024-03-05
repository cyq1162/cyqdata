using CYQ.Data;
using Microsoft.AspNetCore.Http;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Web
{
    public partial class HttpRequest
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

        /// <summary>
        /// 返回 QueryString=》Form =》Cookies。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string this[string name]
        {
            get
            {
                string text = request.Query[name];
                if (text != null)
                {
                    return text;
                }
                if (request.HasFormContentType)
                {
                    text = request.Form[name];
                    if (text != null)
                    {
                        return text;
                    }
                }
                text = request.Cookies[name];
                if (text != null)
                {
                    return text;
                }
                return null;

                //string value = QueryString[name];
                //if (string.IsNullOrEmpty(value))
                //{
                //    value = Form[name];
                //}
                //return value;
            }
        }
        public NameValueCollection Form
        {
            get
            {
                if (context.Items.ContainsKey("RequestForm"))
                {
                    return context.Items["RequestForm"] as NameValueCollection;
                }
                else
                {
                    NameValueCollection nvc = new NameValueCollection();
                    if (request.HasFormContentType && request.Form.Keys.Count > 0)
                    {
                        foreach (string key in request.Form.Keys)
                        {
                            nvc.Add(key, request.Form[key]);
                        }
                    }
                    context.Items.Add("RequestForm", nvc);
                    return nvc;
                }


            }
        }

        public HttpFileCollection Files
        {
            get
            {
                if (context.Items.ContainsKey("RequestFiles"))
                {
                    return context.Items["RequestFiles"] as HttpFileCollection;
                }
                else
                {
                    HttpFileCollection files = new HttpFileCollection();
                    if (request.HasFormContentType && request.Form.Files.Count > 0)
                    {
                        foreach (IFormFile file in request.Form.Files)
                        {
                            files.Add(new HttpPostedFile(file), file.Name);
                        }
                    }
                    context.Items.Add("RequestFiles", files);
                    return files;
                }
            }
        }

        public NameValueCollection QueryString
        {
            get
            {
                if (context.Items.ContainsKey("RequestQueryString"))
                {
                    return context.Items["RequestQueryString"] as NameValueCollection;
                }
                else
                {
                    NameValueCollection nvc = new NameValueCollection();
                    if (request.Query != null && request.Query.Keys.Count > 0)
                    {
                        foreach (string key in request.Query.Keys)
                        {
                            nvc.Add(key, request.Query[key]);
                        }
                    }
                    context.Items.Add("RequestQueryString", nvc);
                    return nvc;
                }

            }
        }

        public HttpCookieCollection Cookies
        {
            get
            {
                if (context.Items.ContainsKey("RequestCookies"))
                {
                    return context.Items["RequestCookies"] as HttpCookieCollection;
                }
                else
                {
                    HttpCookieCollection nvc = new HttpCookieCollection(false);
                    if (request.Cookies != null && request.Cookies.Keys.Count > 0)
                    {
                        foreach (string key in request.Cookies.Keys)
                        {
                            HttpCookie cookie = new HttpCookie(key, request.Cookies[key]);
                            nvc.Add(cookie);
                        }
                    }
                    context.Items.Add("RequestCookies", nvc);
                    return nvc;
                }
            }
        }

        public string[] UserLanguages
        {
            get
            {
                string lang = request.Headers["Accept-Language"];
                if (!string.IsNullOrEmpty(lang))
                {
                    return lang.Split(',');
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
                //涉及Url重写时，不能缓存。
                string url = new StringBuilder()
            .Append(request.Scheme)
            .Append("://")
            .Append(request.Host)
            .Append(request.PathBase)
            .Append(request.Path.Value.Split('?')[0])
            .Append(request.QueryString).ToString();
                return new Uri(url);
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
                string[] items = context.Connection.RemoteIpAddress.ToString().Split(':');//::1 ip6 localhost,127.0.0.1 ip4 localhost
                return items[items.Length - 1] == "1" ? "127.0.0.1" : items[items.Length - 1];
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
                if (context.Items.ContainsKey("RequestHeaders"))
                {
                    return context.Items["RequestHeaders"] as NameValueCollection;
                }
                else
                {
                    NameValueCollection nvc = new NameValueCollection();
                    if (request.Headers != null && request.Headers.Keys.Count > 0)
                    {
                        foreach (string key in request.Headers.Keys)
                        {
                            nvc.Add(key, request.Headers[key].ToString());
                        }
                    }
                    context.Items.Add("RequestHeaders", nvc);
                    return nvc;
                }


            }
        }

        /// <summary>
        /// 兼容 Net、NetCore的写法，同时保持性能。
        /// </summary>
        public string GetHeader(string name)
        {
            return request.Headers[name];
        }
        public NameValueCollection ServerVariables
        {
            get
            {
                NameValueCollection nvc = new NameValueCollection();
                nvc.Add("REMOTE_ADDR", UserHostAddress);
                nvc.Add("REMOTE_PORT", context.Connection.RemotePort.ToString());
                return nvc;
            }
        }

        public long? ContentLength { get => request.ContentLength ?? 0; set => request.ContentLength = value; }
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

    public partial class HttpRequest
    {
        /// <summary>
        /// 兼容 Net、NetCore的写法，同时保持性能。
        /// </summary>
        public string GetForm(string name)
        {
            if (request.HasFormContentType)
            {
                return request.Form[name];
            }
            return null;
        }

        /// <summary>
        /// 兼容 Net、NetCore的写法，同时保持性能。
        /// </summary>
        public HttpPostedFile GetFile(string name)
        {
            if (request.HasFormContentType && request.Form.Files.Count > 0)
            {
                var file = request.Form.Files[name];
                if (file != null)
                {
                    return new HttpPostedFile(file);
                }
            }
            return null;
        }

        /// <summary>
        /// 兼容 Net、NetCore的写法，同时保持性能。
        /// </summary>
        public string GetQueryString(string name)
        {
            return request.Query[name];
        }

        /// <summary>
        /// 兼容 Net、NetCore的写法，同时保持性能。
        /// </summary>
        public HttpCookie GetCookie(string name)
        {
            var value = request.Cookies[name];
            if (value == null) { return null; }
            return new HttpCookie(name, value);
        }

        /// <summary>
        /// 兼容NET、NET Core 写法。
        /// </summary>
        /// <returns></returns>
        public bool GetIsFormContentType()
        {
            return request.HasFormContentType;
        }

        /// <summary>
        /// 从 Stream 中读取数据
        /// </summary>
        /// <returns></returns>
        public byte[] ReadBytes(bool isAllowReuse)
        {
            return ReadBytesAsync(isAllowReuse).GetAwaiter().GetResult();
        }
        private async Task<byte[]> ReadBytesAsync(bool isAllowReuse)
        {
            if (request.Body == null) { return null; }
            using (MemoryStream memoryStream = new MemoryStream())
            {
                if (isAllowReuse)
                {
                    request.EnableBuffering();
                }
                await request.Body.CopyToAsync(memoryStream);
                if (isAllowReuse)
                {
                    request.Body.Position = 0;
                }
                byte[] bytes = memoryStream.ToArray();
                return bytes;
            }
        }

    }
}