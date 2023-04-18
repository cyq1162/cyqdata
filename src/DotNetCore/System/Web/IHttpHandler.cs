namespace System.Web
{
    public interface IHttpHandler
    {
        bool IsReusable { get; }
        void ProcessRequest(HttpContext context);
    }

    internal class DefaultHttpHandler : IHttpHandler
    {
        public static readonly DefaultHttpHandler Instance = new DefaultHttpHandler();
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        public void ProcessRequest(HttpContext context)
        {

        }
    }
}
