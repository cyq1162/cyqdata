using CYQ.Data;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;

namespace System.Web
{
    /// <summary>
    /// 兼容MVC（Taurus、Aries）
    /// </summary>
    public class HttpApplication
    {
        /// <summary>
        /// 单例处理（不需要延时）
        /// </summary>
        public static HttpApplication Instance => LocalShell.instance;
        private HttpApplication()
        {
            if (!AppConfig.WebRootPath.Contains("wwwroot")) //NetCore项目不存在wwwroot文件夹
            {
                AppConfig.WebRootPath = AppConfig.WebRootPath + "wwwroot" + (AppConfig.WebRootPath[0]=='/' ? "/" : "\\");//设置根目录地址，ASPNETCore的根目录和其它应用不一样。
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
        public event EventHandler Error;
        public event EventHandler Disposed;
        public void ExecuteEventHandler()
        {
            try
            {
                BeginRequest?.Invoke(this, null);
                if (!Context.Items.Contains("IsRunToEnd"))
                {
                    PostMapRequestHandler?.Invoke(this, null);
                    if (!Context.Items.Contains("IsRunToEnd"))
                    {
                        AcquireRequestState?.Invoke(this, null);
                    }
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
