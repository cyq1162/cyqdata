using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Web.UI;

namespace System.Web
{
    internal class HttpContext//:
    {
        private static IHttpContextAccessor _contextAccessor;
        public static Microsoft.AspNetCore.Http.HttpContext context => _contextAccessor.HttpContext;
        internal static void Configure(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
           
        }
        private static HttpContext _Current;
        public static HttpContext Current
        {
            get
            {

                if (_Current == null)
                {
                    _Current = new HttpContext();
                }
                return _Current;
            }

        }
        public HttpRequest Request
        {
            get { return new HttpRequest(context); }
        }
        public HttpResponse Response { get { return new HttpResponse(context); } }
        public Page CurrentHandler { get { return new Page(context); } }
        public HttpSessionState Session { get { return new HttpSessionState(context); } }
        
    }

    public static class StaticHttpContextExtensions
    {
        public static void AddHttpContextAccessor(this IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }


        public static IApplicationBuilder UseStaticHttpContext(this IApplicationBuilder app)
        {
            var httpContextAccessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
            HttpContext.Configure(httpContextAccessor);
            return app;
        }
    }
}