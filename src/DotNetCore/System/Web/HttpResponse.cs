using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

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
        HttpCookieCollection cookieCollection;
        internal HttpResponse()
        {
            cookieCollection = new HttpCookieCollection();
        }

        #region 兼容Web
        public void End()
        {
            if (string.IsNullOrEmpty(context.TraceIdentifier) || !context.TraceIdentifier.StartsWith("IsEnd:"))
            {
                context.TraceIdentifier = "IsEnd:" + context.TraceIdentifier;
            }
        }
        private bool IsEnd
        {
            get
            {
                return context.TraceIdentifier.StartsWith("IsEnd:");
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

                }

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
        public void BinaryWrite(byte[] data)
        {
            // response.Body = new MemoryStream(data);
            if (!IsEnd && data != null)
            {
                SetWriteFlag();
                response.Body.WriteAsync(data, 0, data.Length);
            }
            //response.Body.Flush();
            //response.SendFileAsync()
        }
        public void Clear() { response.Clear(); }
        public void Flush() { response.Body.Flush(); }
        #endregion


        public HttpCookieCollection Cookies => cookieCollection;

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
            response.Redirect(location, permanent);
            response.WriteAsync("");
        }
        public void Write(string text)
        {
            if (!IsEnd)
            {
                SetWriteFlag();
                response.WriteAsync(text);
            }
        }
        public void WriteFile(string fileName)
        {
            if (!IsEnd)
            {
                BinaryWrite(File.ReadAllBytes(fileName));
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
