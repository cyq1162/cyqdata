using System.Collections.Generic;
using System.Net;
using CYQ.Data;
using CYQ.Data.Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace System.Web
{
    /// <summary>
    /// 分布式Session
    /// </summary>
    public class HttpSessionState
    {
        public static readonly HttpSessionState Instance = new HttpSessionState();

        protected Microsoft.AspNetCore.Http.HttpContext context
        {
            get
            {
                return HttpContext.contextAccessor.HttpContext;
            }
        }

        internal HttpSessionState()
        {
            Timeout = 20;
        }
        /// <summary>
        /// 超时时间，默认20分钟
        /// </summary>
        public int Timeout { get; set; }
        List<string> keys = new List<string>();
        CacheManage cache = CacheManage.LocalInstance;
        public string SessionID
        {
            get
            {
                if (context.Items.ContainsKey("HttpSessionID"))
                {
                    return context.Items["HttpSessionID"] as String;
                }
                string sessionID = context.Request.Cookies["CYQ.SessionID"];
                if (string.IsNullOrEmpty(sessionID))
                {
                    if (context.Request.Cookies.Count == 0)
                    {
                        string referer = context.Request.Headers["Referer"];
                        if (string.IsNullOrEmpty(referer))
                        {
                            return string.Empty;//API请求，不产生Cookie。
                        }
                    }
                    sessionID = DateTime.Now.ToString("HHmmss") + Guid.NewGuid().GetHashCode();
                    if(context.Request.IsHttps)
                    {
                        context.Response.Cookies.Append("CYQ.SessionID", sessionID, new CookieOptions() { SameSite = SameSiteMode.None, Secure = true });
                    }
                    else
                    {
                        context.Response.Cookies.Append("CYQ.SessionID", sessionID);
                    }
                    
                }
                context.Items.Add("HttpSessionID", sessionID);
                return sessionID;
            }
        }

        private string GetName(string name)
        {
            return name + "_" + SessionID;
        }

        public object this[int index]
        {
            get
            {
                if (index < keys.Count)
                {
                    return this[keys[index]];
                }
                return null;
            }
            set
            {
                if (index < keys.Count)
                {
                    this[keys[index]] = value;
                }
            }
        }

        public object this[string name]
        {
            get
            {
                string key = GetName(name);
                object obj = cache.Get(key);
                if (obj != null && DateTime.Now.Second % 9 == 0)
                {
                    cache.Set(key, obj, Timeout);//用随机概率延长时间
                }
                return obj;
            }
            set
            {
                Add(name, value);
            }
        }
        public void Add(string name, object value)
        {
            cache.Set(GetName(name), value, Timeout);
            if (keys.Contains(name))
            {
                keys.Add(name);
            }
        }
        public void Remove(string key)
        {
            cache.Remove(GetName(key));
        }
        public bool IsAvailable => true;

        public IEnumerable<string> Keys
        {
            get
            {
                return keys;
            }
        }

        /// <summary>
        /// 清空当前会话的所有数据。
        /// </summary>
        public void Clear()
        {
            foreach (string key in keys)
            {
                Remove(key);
            }
        }
    }
}