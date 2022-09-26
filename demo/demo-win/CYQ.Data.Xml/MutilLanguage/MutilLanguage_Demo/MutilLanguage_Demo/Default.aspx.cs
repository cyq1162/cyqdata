using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using CYQ.Data.Xml;
namespace MutilLanguage_Demo
{
    public partial class Default : System.Web.UI.Page
    {
        protected MutilLanguage lang = null;
        protected void Page_Load(object sender, EventArgs e)
        {
            if (lang == null)
            {
                lang = new MutilLanguage(Server.MapPath("Lang.xml"), false);
            }
            if (!IsPostBack)
            {
                this.Title = lang.Get("title");
                labUrl.Text = lang.Get("url");
            }

        }
        protected void btnChina_Click(object sender, EventArgs e)
        {
            lang.SetToCookie(LanguageKey.Chinese);
            Response.Redirect(Request.RawUrl);
        }
        protected void btnEnglish_Click(object sender, EventArgs e)
        {
            lang.SetToCookie(LanguageKey.English);
            Response.Redirect(Request.RawUrl);
        }
        protected void btnCustom_Click(object sender, EventArgs e)
        {
            lang.SetToCookie(LanguageKey.Custom);
            Response.Redirect(Request.RawUrl);
        }
    }
}