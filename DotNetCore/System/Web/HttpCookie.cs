using Microsoft.AspNetCore.Http;
using System.Collections.Specialized;

namespace System.Web
{
    public class HttpCookie
    {
        public HttpCookie() { }
        public HttpCookie(string name)
        {
            this.Name = name;
        }
        public HttpCookie(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }

        public string Name { get; set; }
        public DateTime Expires { get; set; }
        public string Path { get; set; }
        public bool HttpOnly { get; set; }
        public bool Secure { get; set; }
        public string Value { get; set; }
        public NameValueCollection Values
        {
            get
            {
                NameValueCollection nv = new NameValueCollection();
                if (!string.IsNullOrEmpty(Value))
                {
                    string[] values = Value.Split('&');
                    foreach (string value in values)
                    {
                        string[] items = value.Split('=');
                        if (items.Length == 1)
                        {
                            nv.Add("null", items[0]);
                        }
                        else if (items.Length > 1)
                        {
                            string name = items[0];
                            nv.Add(name, value.Substring(name.Length + 1));
                        }
                    }
                }
                return nv;
            }
        }
        public string Domain { get; set; }
        /// <summary>
        /// 转换后，还得补上name,value
        /// </summary>
        /// <param name="op"></param>
        public static implicit operator HttpCookie(CookieOptions op)
        {
            HttpCookie cookie = new HttpCookie();
            cookie.Domain = op.Domain;
            if (op.Expires.HasValue)
            {
                cookie.Expires = ConvertFromDateTimeOffset(op.Expires.Value);
            }
            cookie.Secure = op.Secure;
            cookie.HttpOnly = op.HttpOnly;
            cookie.Path = op.Path;
            return cookie;
        }
        public CookieOptions ToCookieOptions()
        {
            CookieOptions op = new CookieOptions();
            if (op.SameSite == SameSiteMode.None)//NETCore 2.1 默认是None，3.1 默认是：Unspecified
            {
                op.SameSite = SameSiteMode.Lax;
            }
            op.Secure = this.Secure;
            op.Domain = this.Domain;
            op.Expires = ConverFromDateTime(this.Expires);
            op.HttpOnly = this.HttpOnly;
            op.Path = string.IsNullOrEmpty(this.Path) ? "/" : this.Path;
            return op;
        }
        static DateTime ConvertFromDateTimeOffset(DateTimeOffset dateTime)
        {
            if (dateTime.Offset.Equals(TimeSpan.Zero))
                return dateTime.UtcDateTime;
            else if (dateTime.Offset.Equals(TimeZoneInfo.Local.GetUtcOffset(dateTime.DateTime)))
                return DateTime.SpecifyKind(dateTime.DateTime, DateTimeKind.Local);
            else
                return dateTime.DateTime;
        }
        static DateTimeOffset ConverFromDateTime(DateTime dateTime)
        {
            return dateTime.ToUniversalTime() <= DateTimeOffset.MinValue.UtcDateTime
                   ? DateTimeOffset.MinValue
                   : new DateTimeOffset(dateTime);

            // return new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime));
        }
    }
}
