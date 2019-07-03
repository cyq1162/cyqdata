using Microsoft.AspNetCore.Http;

namespace System.Web.UI
{
    public class Page
    {
        Microsoft.AspNetCore.Http.HttpContext context
        {
            get
            {
                return HttpContext.contextAccessor.HttpContext;
            }
        }

        internal Page()
        {
           
        }

        internal object FindControl(string key)
        {
            throw new NotImplementedException();
        }
        protected virtual void OnPreInit(EventArgs e) { }
    }
}