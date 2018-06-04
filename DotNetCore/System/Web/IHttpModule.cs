using System;
using System.Collections.Generic;
using System.Text;

namespace System.Web
{
    /// <summary>
    /// 兼容MVC（Taurus、Aries）而存在，以下都是。
    /// </summary>
    public interface IHttpModule
    {
        void Dispose();
        void Init(HttpApplication context);
    }
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
        public void ExecuteEventHandler()
        {
            BeginRequest?.Invoke(this, null);
            PostMapRequestHandler?.Invoke(this, null);
            AcquireRequestState?.Invoke(this, null);
            //Error?.Invoke(this, null);这个只有异常才调用，所以不需要在这里调用
        }
        class LocalShell
        {
            internal static readonly HttpApplication instance = new HttpApplication();
        }
    }

    public interface IHttpHandler
    {
        bool IsReusable { get; }
        void ProcessRequest(HttpContext context);
    }
}
namespace System.Web.SessionState
{
    public interface IRequiresSessionState
    {
    }
}