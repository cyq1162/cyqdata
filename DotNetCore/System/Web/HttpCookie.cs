using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace System.Web
{
    public class HttpCookie
    {
        public HttpCookie() { }
        public HttpCookie(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }

        public string Name { get; internal set; }
        public DateTime Expires { get; internal set; }
        public string Path { get; internal set; }
        public bool HttpOnly { get; internal set; }
        public string Value { get; internal set; }
        public string Domain { get; internal set; }
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
            cookie.HttpOnly = op.HttpOnly;
            cookie.Path = op.Path;
            return cookie;
        }
        public CookieOptions ToCookieOptions()
        {
            CookieOptions op = new CookieOptions();
            op.Domain = this.Domain;
            op.Expires = ConverFromDateTime(this.Expires);
            op.HttpOnly = this.HttpOnly;
            op.Path = this.Path;
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
            return new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime));
        }
    }
}
