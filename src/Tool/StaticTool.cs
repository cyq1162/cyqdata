using System;
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
            return "MasterSlave_" + GetMasterSlaveID();
        }
        /// <summary>
        /// 用于标识（以线程为单位）的 全局事务 的唯一标识
        /// </summary>
        public static string GetTransationKey(string conn)
        {
            //在Task 协程 异步中，可能会有不同的线程执行任务，这特殊情况后续再考量。
            string hash = ConnBean.GetHashKey(conn);
            return "Transation_" + Thread.CurrentThread.ManagedThreadId + hash;
        }

        private static string GetMasterSlaveID()
        {
            string id = string.Empty;
            //避开异常：请求在此上下文中不可用（Global.asax.cs：Application_Start 方法）
            HttpContext context = HttpContext.Current;
            if (context != null && context.Handler != null)
            {
                HttpRequest request = context.Request;
                if (request["token"] != null)
                {
                    id = request["token"];
                }
                else if (request.Headers["token"] != null)
                {
                    id = request.Headers["token"];
                }
                else if (context.Session != null)
                {
                    id = context.Session.SessionID;
                }
                if (string.IsNullOrEmpty(id))
                {
                    id = request.UserHostAddress;//获取IP地址。
                }
            }
            if (string.IsNullOrEmpty(id))
            {
                id = LocalEnvironment.ProcessID.ToString();//winform
            }
            return id;
        }
    }
}
