using System;
using System.Collections.Generic;
using System.Text;

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
                if (!Context.TraceIdentifier.StartsWith("IsEnd:"))
                {
                    PostMapRequestHandler?.Invoke(this, null);
                    if (!Context.TraceIdentifier.StartsWith("IsEnd:"))
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
