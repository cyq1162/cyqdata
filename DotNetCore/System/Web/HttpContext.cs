using Microsoft.AspNetCore.Http;
using System.Web.UI;
using Microsoft.AspNetCore.Http.Features;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using CYQ.Data.Cache;
namespace System.Web
{
    public class HttpContext//: Microsoft.AspNetCore.Http.HttpContext
    {
        /// <summary>
        /// 存档全局唯一的上下文访问器
        /// </summary>
        internal static IHttpContextAccessor contextAccessor;
        /// <summary>
        /// 访问器的上下文访问器，会在不同的线程中返回不同的上下文
        /// </summary>
        public Microsoft.AspNetCore.Http.HttpContext NetCoreContext
        {
            get
            {
                return contextAccessor == null ? null : contextAccessor.HttpContext;
            }
        }
        internal static void Configure(IHttpContextAccessor contextAccessor)
        {
            HttpContext.contextAccessor = contextAccessor;

        }

        public void Abort()
        {
            NetCoreContext.Abort();
        }

        private static HttpContext _Current;
       // public static readonly object o = new object();
        public static HttpContext Current
        {
            get
            {

                if (contextAccessor == null || contextAccessor.HttpContext == null)
                {
                    return null;
                }
                if (_Current == null)
                {
                    _Current = new HttpContext();
                }
                return _Current;

                //HttpContext context = null;
                //string key = Thread.CurrentThread.ManagedThreadId + contextAccessor.HttpContext.TraceIdentifier+DateTime.Now.Second;
                ////if (CacheManage.LocalInstance.Contains(key))
                ////{
                ////    context=(HttpContext)CacheManage.LocalInstance.Get(key);
                ////}
                //if(context==null)
                //{
                //    context = new HttpContext();
                //   // CacheManage.LocalInstance.Set(key, context, 0.05);
                //}
                //return context;
                // return _Current;
                // return new HttpContext();
                //lock (o)
                //{
                //    if (_Current == null || _Current.useContext != NetCoreContext)
                //    {
                //        _Current = new HttpContext();
                //    }
                //}
                //return _Current;
            }

        }
        HttpResponse response;
        HttpRequest request;
        HttpSessionState session;
        HttpServerUtility server;
        Page page;
        /// <summary>
        /// 使用的上下文，可能是旧的。
        /// </summary>
        //private Microsoft.AspNetCore.Http.HttpContext useContext;
        private HttpContext()
        {
            //useContext = NetCoreContext;
            response = new HttpResponse();
            request = new HttpRequest();
            try
            {
                if (NetCoreContext.Session != null)
                {
                    session = new HttpSessionState();
                }
            }
            catch{
               // CYQ.Data.Log.WriteLogToTxt(err);
            }
            server = new HttpServerUtility();
            page = new Page();
        }

        public HttpRequest Request => request;
        public HttpResponse Response => response;
        internal Page CurrentHandler => page;
        public HttpSessionState Session => session;
        public HttpServerUtility Server => server;
        public IFeatureCollection Features => NetCoreContext.Features;
        public ConnectionInfo Connection => NetCoreContext.Connection;
        public Exception Error { get; set; }
        public IHttpHandler Handler { get; set; }
        public WebSocketManager WebSockets => NetCoreContext.WebSockets;

        // public AuthenticationManager Authentication => context.Authentication;

        public ClaimsPrincipal User { get => NetCoreContext.User; set => NetCoreContext.User = value; }
        public IDictionary<object, object> Items { get => NetCoreContext.Items; set => NetCoreContext.Items = value; }
        public IServiceProvider RequestServices { get => NetCoreContext.RequestServices; set => NetCoreContext.RequestServices = value; }
        public CancellationToken RequestAborted { get => NetCoreContext.RequestAborted; set => NetCoreContext.RequestAborted = value; }
        public string TraceIdentifier { get => NetCoreContext.TraceIdentifier; set => NetCoreContext.TraceIdentifier = value; }

        public void RewritePath(string path)
        {
            request.Path = '/' + path.TrimStart('/');
        }
    }


}