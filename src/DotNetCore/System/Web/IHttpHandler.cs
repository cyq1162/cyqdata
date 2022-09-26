
namespace System.Web
{
    public interface IHttpHandler
    {
        bool IsReusable { get; }
        void ProcessRequest(HttpContext context);
    }
}
