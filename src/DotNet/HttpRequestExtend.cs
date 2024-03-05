using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace System.Web
{
    public static class HttpRequestExtend
    {
        public static string GetHeader(this HttpRequest request, string name)
        {
            return request.Headers[name];
        }
        public static string GetForm(this HttpRequest request, string name)
        {
            return request.Form[name];
        }
        public static HttpPostedFile GetFile(this HttpRequest request, string name)
        {
            return request.Files[name];
        }
        public static string GetQueryString(this HttpRequest request, string name)
        {
            return request.QueryString[name];
        }
        public static HttpCookie GetCookie(this HttpRequest request, string name)
        {
            return request.Cookies[name];
        }
        public static bool GetIsFormContentType(this HttpRequest request)
        {
            return request.Form != null && request.Form.Count > 0;
        }
        /// <summary>
        /// 从 Stream 中读取数据
        /// </summary>
        /// <returns></returns>
        public static byte[] ReadBytes(this HttpRequest request, bool isReuse)
        {
            var stream = request.InputStream;
            if (stream == null || !stream.CanRead || request.ContentLength <= 0) { return null; }
            var len = request.ContentLength;
            Byte[] bytes = new Byte[len];
            stream.Position = 0;
            stream.Read(bytes, 0, bytes.Length);
            //if (stream.Position < len)
            //{
            //    //Linux CentOS-8 大文件下读不全，会延时，导致：Unexpected end of Stream, the content may have already been read by another component.
            //    int max = 0;
            //    int timeout = MsConfig.Server.GatewayTimeout * 1000;
            //    while (stream.Position < len)
            //    {
            //        max++;
            //        if (max > timeout)//60秒超时
            //        {
            //            break;
            //        }
            //        Thread.Sleep(1);
            //        stream.Read(bytes, (int)stream.Position, (int)(len - stream.Position));
            //    }
            //}
            if (isReuse)
            {
                stream.Position = 0;//重置，允许重复使用。
            }
            return bytes;

        }
    }
}
