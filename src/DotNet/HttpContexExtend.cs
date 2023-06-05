using System;
using System.Collections.Generic;
using System.Text;


namespace System.Web
{
    public static class HttpContexExtend
    {
        //public static WebSocketManager WebSockets(this HttpContext context)
        //{
        //    return new WebSocketManager();
        //}
        /// <summary>
        /// 获得分布式追踪ID
        /// </summary>
        public static string GetTraceID(this HttpContext context)
        {
            string tid = context.Request.Headers["X-Request-ID"];
            if (!string.IsNullOrEmpty(tid))
            {
                return tid;
            }
           
            if (!context.Items.Contains("TraceIdentifier"))
            {
                context.Items.Add("TraceIdentifier", Guid.NewGuid().ToString());
            }
            return context.Items["TraceIdentifier"].ToString();
            
        }
        
    }
}
