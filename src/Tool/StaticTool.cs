using System;
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
            return HashCreator.CreateKey(sourceString);
        }
        /// <summary>
        /// ���ڱ�ʶ�����û�Ϊ��λ���� ���� ��Ψһ��ʶ
        /// </summary>
        /// <returns></returns>
        public static string GetMasterSlaveKey()
        {
            return "MasterSlave_" + GetMasterSlaveID();
        }
        /// <summary>
        /// ���ڱ�ʶ�����߳�Ϊ��λ���� ȫ������ ��Ψһ��ʶ
        /// </summary>
        public static string GetTransationKey(string conn)
        {
            //��Task Э�� �첽�У����ܻ��в�ͬ���߳�ִ��������������������ٿ�����
            string hash = ConnBean.GetHashKey(conn);
            return "Transation_" + Thread.CurrentThread.ManagedThreadId + hash;
        }

        private static string GetMasterSlaveID()
        {
            string id = string.Empty;
            //�ܿ��쳣�������ڴ��������в����ã�Global.asax.cs��Application_Start ������
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
                    id = request.UserHostAddress;//��ȡIP��ַ��
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
