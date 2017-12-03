using Microsoft.AspNetCore.Http;

namespace System.Web
{
    public class HttpSessionState
    {
        private Microsoft.AspNetCore.Http.HttpContext context;

        public HttpSessionState(Microsoft.AspNetCore.Http.HttpContext context)
        {
            this.context = context;
        }

        public string SessionID { get { return context.Session.Id; } }
    }
}