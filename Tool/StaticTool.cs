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
    /// ��̬����������
    /// </summary>
    internal static class StaticTool
    {
        /// <summary>
        /// ��GUIDת��16�ֽ��ַ���
        /// </summary>
        /// <returns></returns>
        internal static string ToGuidByteString(string guid)
        {
            return BitConverter.ToString(new Guid(guid).ToByteArray()).Replace("-", "");
        }

        /// <summary>
        /// �����ڷֲ�ʽ��
        /// </summary>
        /// <param name="sourceString"></param>
        /// <returns></returns>
        internal static string GetHashKey(string sourceString)
        {
            if (string.IsNullOrEmpty(sourceString))
            {
                return "K" + HashCreator.Create(sourceString);
            }
            return "K" + HashCreator.Create(sourceString) + sourceString.Length;

        }
        /// <summary>
        /// ���ڱ�ʶ�����û�Ϊ��λ���� ���� ��Ψһ��ʶ
        /// </summary>
        /// <returns></returns>
        public static string GetMasterSlaveKey()
        {
            string id = string.Empty;
            if (HttpContext.Current != null)
            {
                if (HttpContext.Current.Session != null)
                {
                    id = HttpContext.Current.Session.SessionID;
                }
                else if (HttpContext.Current.Request["Token"] != null)
                {
                    id = HttpContext.Current.Request["Token"];
                }
                else if (HttpContext.Current.Request.Headers["Token"] != null)
                {
                    id = HttpContext.Current.Request.Headers["Token"];
                }
                else if (HttpContext.Current.Request["MasterSlaveID"] != null)
                {
                    id = HttpContext.Current.Request["MasterSlaveID"];
                }
                if (string.IsNullOrEmpty(id))
                {
                    HttpCookie cookie = HttpContext.Current.Request.Cookies["MasterSlaveID"];
                    if (cookie != null)
                    {
                        id = cookie.Value;
                    }
                    else
                    {
                        id = Guid.NewGuid().ToString().Replace("-", "");
                        cookie = new HttpCookie("MasterSlaveID", id);
                        cookie.Expires = DateTime.Now.AddMonths(1);
                        HttpContext.Current.Response.Cookies.Add(cookie);
                    }
                }
            }
            if (string.IsNullOrEmpty(id))
            {
                id = DateTime.Now.Minute + Thread.CurrentThread.ManagedThreadId.ToString();
            }
            return "MasterSlave_" + id;
        }
        /// <summary>
        /// ���ڱ�ʶ�����û�Ϊ��λ���� ȫ������ ��Ψһ��ʶ
        /// </summary>
        public static string GetTransationKey(string conn)
        {
            string key = Thread.CurrentThread.ManagedThreadId.ToString();
            if (HttpContext.Current != null)
            {
                string id = string.Empty;
                if (HttpContext.Current.Session != null)
                {
                    id = HttpContext.Current.Session.SessionID;
                }
                else if (HttpContext.Current.Request["Token"] != null)
                {
                    id = HttpContext.Current.Request["Token"];
                }
                else if (HttpContext.Current.Request.Headers["Token"] != null)
                {
                    id = HttpContext.Current.Request.Headers["Token"];
                }
                key = id + key;
            }
            string hash = ConnBean.GetHashKey(conn);
            return "Transation_" + key + hash;
        }
    }
}
