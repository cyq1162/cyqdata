using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http
{
    public static class HttpContextExtensions
    {
        private static bool IsUseHttpContext = false;
        private static bool IsAddHttpContext = false;
        public static void AddHttpContext(this IServiceCollection services)
        {
            if (!IsAddHttpContext)
            {
                IsAddHttpContext = true;
                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            }
        }


        public static IApplicationBuilder UseHttpContext(this IApplicationBuilder app)
        {
            if (!IsUseHttpContext)
            {
                IsUseHttpContext = true;
                var httpContextAccessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
                System.Web.HttpContext.Configure(httpContextAccessor);
            }
            return app;
        }
    }
}