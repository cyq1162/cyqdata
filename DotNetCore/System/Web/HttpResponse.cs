using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Http;
namespace System.Web
{
    public class HttpResponse
    {
        Microsoft.AspNetCore.Http.HttpContext context;
        Microsoft.AspNetCore.Http.HttpResponse response => context.Response;
        HttpCookieCollection cookieCollection;
        public HttpResponse(Microsoft.AspNetCore.Http.HttpContext context)
        {
            this.context = context;
            cookieCollection = new HttpCookieCollection(context);
        }
         
        #region 兼容Web
        public void End()
        {
            //context.Abort();
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
 
                return "";
            }
            set
            {
                //Headers are read-only, response has already started
                if (HasStarted) { return; }
                string ct = ContentType;
                if (string.IsNullOrEmpty(ct))
                {
                    ContentType =  "charset=" + value;
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
                if (value.Contains("charset") || string.IsNullOrEmpty(response.ContentType))
                {
                    response.ContentType = value;
                }
                else
                {
                    string[] items = response.ContentType.Split(';');
                    if (items.Length == 2)
                    {
                        response.ContentType = value + ";" + items[1];
                    }
                    else if (response.ContentType.Contains("charset"))//只有charset
                    {
                        response.ContentType = value + ";" + items[0];
                    }
                    else
                    {
                        response.ContentType = value;
                    }
                }


            }
        }
        public Stream Filter { get => response.Body; set => response.Body = value; }

        public void AppendHeader(string key, string value)
        {
            response.Headers.Add(key, value);
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
            response.Body = new MemoryStream(data);
        }
        public void Clear() { }
        public void Flush() { }
        #endregion


        public  HttpCookieCollection Cookies => cookieCollection;

        public  int StatusCode { get => response.StatusCode; set
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

        public  Stream Body { get => response.Body; set => response.Body=value; }
        public  long? ContentLength { get => response.ContentLength; set => response.ContentLength=value; }


        public  bool HasStarted => response.HasStarted;

        

        public  void OnCompleted(Func<object, Task> callback, object state)
        {
            response.OnCompleted(callback, state);
        }

        public  void OnStarting(Func<object, Task> callback, object state)
        {
            response.OnStarting(callback, state);
        }
        /// <summary>
        /// 302 跳转
        /// </summary>
        /// <param name="location"></param>
        public void Redirect(string location)
        {
            response.Redirect(location);
        }
        /// <summary>
        /// 跳转
        /// </summary>
        /// <param name="location"></param>
        /// <param name="permanent">true 301跳转，默认false 是302跳转</param>
        public  void Redirect(string location, bool permanent)
        {
            response.Redirect(location, permanent);
        }
        public void Write(string text)
        {
           response.WriteAsync(text);
        }
    }
}
