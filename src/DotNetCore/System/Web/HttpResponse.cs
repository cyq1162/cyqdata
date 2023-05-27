using System.IO;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Http;
using System.Text;
using CYQ.Data;

namespace System.Web
{
    /// <summary>
    /// 输出对象（单例存在，不该有自己的属性）
    /// </summary>
    public class HttpResponse
    {
        Microsoft.AspNetCore.Http.HttpContext context
        {
            get
            {
                return HttpContext.contextAccessor.HttpContext;
            }
        }
        Microsoft.AspNetCore.Http.HttpResponse response => context.Response;

        internal HttpResponse()
        {

        }

        #region 兼容Web
        public void End()
        {
            if (!context.Items.ContainsKey("IsRunToEnd"))
            {
                context.Items.Add("IsRunToEnd", true);
            }
        }
        private bool IsEnd
        {
            get
            {
                return context.Items.ContainsKey("IsRunToEnd");
            }
        }
        public string Charset
        {
            get
            {
                string ct = ContentType;
                int i = ct.IndexOf("charset=");
                if (i > -1)
                {
                    return ct.Substring(i + 8);
                }

                return "utf-8";
            }
            set
            {
                //Headers are read-only, response has already started
                if (HasStarted) { return; }
                string ct = ContentType;
                if (string.IsNullOrEmpty(ct))
                {
                    ContentType = "text/html; charset=" + value.Replace("charset=", "");
                }
                else if (!ct.Contains("charset="))
                {
                    ContentType = ct.TrimEnd(';') + "; charset=" + value;
                }
            }
        }
        public string ContentType
        {
            get => response.ContentType ?? "";
            set
            {
                try
                {
                    if (response.ContentType != value)
                    {
                        if (value.Contains("charset") || string.IsNullOrEmpty(response.ContentType))
                        {
                            response.ContentType = value;
                        }
                        else
                        {
                            string[] items = response.ContentType.Split(';');
                            if (items.Length == 2)//包含两个，只改前面的，不改编码
                            {
                                response.ContentType = value + ";" + items[1];
                            }
                            else if (response.ContentType.Contains("charset"))//只有charset
                            {
                                response.ContentType = value + "; " + items[0].Trim();
                            }
                            else//只有text/html
                            {
                                response.ContentType = value + "; charset=utf-8";
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    Log.Write(err, LogType.Error);
                }

            }
        }

        public Encoding ContentEncoding
        {
            get
            {
                return Encoding.GetEncoding(Charset);
            }
            set
            {
                Charset = value.WebName;
            }
        }

        public Stream Filter
        {
            get => Body;
            set
            {
                Body = value;
            }
        }

        public void AppendHeader(string key, string value)
        {
            if (!response.HasStarted)
            {
                response.Headers.Remove(key);
                response.Headers.Add(key, value);
            }
        }
        //public bool Buffer { get; set; }
        //public int Expires { get; set; }
        //public DateTime ExpiresAbsolute { get; set; }
        public string CacheControl
        {
            get => Headers["Cache-Control"];
            set => AppendHeader("Cache-Control", value);
        }

        public void Clear() { response.Clear(); }
        public void Flush() { response.Body.Flush(); }
        #endregion


        public HttpCookieCollection Cookies
        {
            get
            {
                if (context.Items.ContainsKey("ResponseCookies"))
                {
                    return context.Items["ResponseCookies"] as HttpCookieCollection;
                }
                else
                {
                    var cookies = new HttpCookieCollection(true);
                    context.Items.Add("ResponseCookies", cookies);
                    return cookies;
                }
            }
        }

        public int StatusCode
        {
            get => response.StatusCode; set
            {
                if (!response.HasStarted)
                {
                    response.StatusCode = value;
                }
            }
        }

        public NameValueCollection Headers
        {
            get
            {
                NameValueCollection nvc = new NameValueCollection();
                if (response.Headers != null && response.Headers.Keys.Count > 0)
                {
                    foreach (string key in response.Headers.Keys)
                    {
                        nvc.Add(key, response.Headers[key].ToString());
                    }
                }
                return nvc;
            }
        }

        public Stream Body
        {
            get => response.Body;
            set
            {
                response.Body = value;
            }
        }
        public long? ContentLength { get => response.ContentLength; set => response.ContentLength = value; }

        /// <summary>
        /// 是否已启用输出：（已输出到客户端 || 已调用Response.End() || 已调用Response.Write()）
        /// </summary>
        public bool HasStarted
        {
            get
            {
                return response.HasStarted || IsEnd || context.Items.TryGetValue("CallWrite", out _);
            }
        }



        public void OnCompleted(Func<object, Task> callback, object state)
        {
            response.OnCompleted(callback, state);
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            response.OnStarting(callback, state);
        }
        /// <summary>
        /// 302 跳转
        /// </summary>
        /// <param name="location"></param>
        public void Redirect(string location)
        {
            Redirect(location, false);
        }
        /// <summary>
        /// 跳转
        /// </summary>
        /// <param name="location"></param>
        /// <param name="permanent">true 301跳转，默认false 是302跳转</param>
        public void Redirect(string location, bool permanent)
        {
            if (string.IsNullOrEmpty(location)) { return; }
            if (!location.StartsWith("/") && !location.StartsWith("http://") && !location.StartsWith("https://"))
            {
                string path = context.Request.Path;// microservice/login
                string[] items = path.Trim('/').Split('/');
                items[items.Length - 1] = location;
                location = "/" + string.Join("/", items);
            }
            response.Redirect(location, permanent);
            End();
        }
        /// <summary>
        /// 内部：异步执行的（未等待）
        /// </summary>
        /// <param name="text"></param>
        public void Write(string text)
        {
            if (!IsEnd)
            {
                SetWriteFlag();
                byte[] data = ContentEncoding.GetBytes(text);
                response.Body.WriteAsync(data, 0, data.Length);
            }
        }
        /// <summary>
        /// 内部：异步执行的（并等待结束）
        /// </summary>
        /// <param name="fileName"></param>
        public void WriteFile(string fileName)
        {
            byte[] data = File.ReadAllBytes(fileName);
            if (!IsEnd && data != null)
            {
                SetWriteFlag();
                response.Body.WriteAsync(data, 0, data.Length).Wait();
                End();
            }
        }
        /// <summary>
        /// 内部：异步执行的（并等待结束）
        /// </summary>
        /// <param name="data"></param>
        public void BinaryWrite(byte[] data)
        {
            if (!IsEnd && data != null)
            {
                SetWriteFlag();
                response.Body.WriteAsync(data, 0, data.Length).Wait();
            }
        }
        private void SetWriteFlag()
        {
            if (!context.Items.ContainsKey("CallWrite"))
            {
                context.Items.Add("CallWrite", true);
            }

        }
    }
}
