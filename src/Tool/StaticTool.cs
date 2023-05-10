using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data;
using CYQ.Data.Table;
using CYQ.Data.SQL;
using System.IO;
using System.Web;
using System.Threading;


namespace CYQ.Data.Tool
{
    /// <summary>
    /// 静态方法工具类
    /// </summary>
    internal static class StaticTool
    {
        /// <summary>
        /// 将GUID转成16字节字符串
        /// </summary>
        /// <returns></returns>
        internal static string ToGuidByteString(string guid)
        {
            return BitConverter.ToString(new Guid(guid).ToByteArray()).Replace("-", "");
        }

        /// <summary>
        /// 【用于分布式】
        /// </summary>
        /// <param name="sourceString"></param>
        /// <returns></returns>
        internal static string GetHashKey(string sourceString)
        {
            return HashCreator.CreateKey(sourceString);
        }
        /// <summary>
        /// 用于标识（以用户为单位）的 主从 的唯一标识
        /// </summary>
        /// <returns></returns>
        public static string GetMasterSlaveKey()
        {
            return "MasterSlave_" + GetID(true);
        }
        /// <summary>
        /// 用于标识（以用户为单位）的 全局事务 的唯一标识
        /// </summary>
        public static string GetTransationKey(string conn)
        {
            //在Task 协程 异步中，可能会有不同的线程执行任务，这特殊情况后续再考量。
            string key = GetID(false) + Thread.CurrentThread.ManagedThreadId.ToString();
            string hash = ConnBean.GetHashKey(conn);
            return "Transation_" + key + hash;
        }
        private static string GetID(bool isCreate)
        {
            string id = string.Empty;
            //避开异常：请求在此上下文中不可用（Global.asax.cs：Application_Start 方法）
            if (HttpContext.Current != null && HttpContext.Current.Handler != null)
            {
                if (HttpContext.Current.Request["token"] != null)
                {
                    id = HttpContext.Current.Request["token"];
                }
                else if (HttpContext.Current.Request.Headers["token"] != null)
                {
                    id = HttpContext.Current.Request.Headers["token"];
                }
                else if (HttpContext.Current.Session != null)
                {
                    id = HttpContext.Current.Session.SessionID;
                }
                if (isCreate && string.IsNullOrEmpty(id))
                {
                    HttpCookie cookie = HttpContext.Current.Request.Cookies["CYQ.SessionID"];
                    if (cookie != null)
                    {
                        id = cookie.Value;
                    }
                    else
                    {
                        id = Guid.NewGuid().ToString().Replace("-", "");
                        cookie = new HttpCookie("CYQ.SessionID", id);
                        HttpContext.Current.Response.Cookies.Add(cookie);
                    }
                }
            }
            if (isCreate && string.IsNullOrEmpty(id))
            {
                id = DateTime.Now.Minute + Thread.CurrentThread.ManagedThreadId.ToString();
            }
            return id;
        }
    }
}
