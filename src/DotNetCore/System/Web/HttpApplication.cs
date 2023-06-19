using CYQ.Data;
using System;
using System.Collections.Generic;

namespace System.Web
{
    /// <summary>
    /// 兼容MVC（Taurus、Aries）
    /// </summary>
    public class HttpApplication
    {
        private static Dictionary<string, HttpApplication> keyValues= new Dictionary<string, HttpApplication>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// 根据指定 Key 获得唯一实例
        /// </summary>
        /// <param name="key">在不同的NetCore中间件被使用时，用Key区分</param>
        /// <returns></returns>
        public static HttpApplication GetInstance(string key)
        {
            if(keyValues.ContainsKey(key)) return keyValues[key];
            HttpApplication instance = new HttpApplication();
            keyValues.Add(key, instance);
            return instance;
        }
        /// <summary>
        /// 单例处理，（对于多个中间件，请使用GetInstance方法）
        /// </summary>
        public static HttpApplication Instance => LocalShell.instance;
        private HttpApplication()
        {
            if (!AppConfig.WebRootPath.Contains("wwwroot")) //NetCore项目不存在wwwroot文件夹
            {
                AppConfig.WebRootPath = AppConfig.WebRootPath + "wwwroot" + (AppConfig.WebRootPath[0] == '/' ? "/" : "\\");//设置根目录地址，ASPNETCore的根目录和其它应用不一样。
            }
        }
        public HttpContext Context
        {
            get
            {
                return HttpContext.Current;
            }
        }
        public event EventHandler BeginRequest;
        public event EventHandler PostMapRequestHandler;
        public event EventHandler AcquireRequestState;
        public event EventHandler PreSendRequestContent;
        public event EventHandler Error;
        public event EventHandler Disposed;
        public void ExecuteEventHandler()
        {
            try
            {
                BeginRequest?.Invoke(this, null);
                if (!Context.Items.Contains("IsRunToEnd"))
                {
                    AcquireRequestState?.Invoke(this, null);
                }
            }
            catch (Exception err)
            {
                Context.Error = err;
                Error?.Invoke(this, null);//这个只有异常才调用，所以不需要在这里调用
            }

        }
        class LocalShell
        {
            internal static readonly HttpApplication instance = new HttpApplication();
        }
    }
}
