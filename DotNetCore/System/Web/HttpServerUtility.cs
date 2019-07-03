using CYQ.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace System.Web
{
    public class HttpServerUtility
    {
        internal HttpServerUtility()
        {

        }
        public string HtmlDecode(string s)
        {
            return System.Net.WebUtility.HtmlDecode(s);
        }
        public string HtmlEncode(string s)
        {
            return System.Net.WebUtility.HtmlDecode(s);
        }
        public string UrlEncode(string s)
        {
            return System.Net.WebUtility.UrlEncode(s);
        }
        public string UrlDecode(string s)
        {
            return System.Net.WebUtility.UrlDecode(s);
        }
        public string MapPath(string path)
        {
            return AppConfig.WebRootPath + path.TrimStart('~', '/');//.Replace("/", "\\");
        }
    }
}
