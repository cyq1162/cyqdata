using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;


namespace System.Web
{
    public static class HttpContexExtend
    {
        public static WebSocketManager WebSockets(this HttpContext context)
        {
            return new WebSocketManager();
        }
    }
}
