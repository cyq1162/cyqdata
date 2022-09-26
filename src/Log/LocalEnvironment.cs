using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace CYQ.Data
{
    /// <summary>
    /// �����ռ������Ļ�����Ϣ
    /// ��Թ̶�������
    /// </summary>
    internal partial class LocalEnvironment
    {
        /// <summary>
        /// ���Ե�����
        /// </summary>
        public static string HostName
        {
            get
            {
                return Environment.MachineName;
            }
        }
        /// <summary>
        /// �������еĵ�ǰ�û�����
        /// </summary>
        public static string UserName
        {
            get
            {
                return Environment.UserName;
            }
        }
        /// <summary>
        /// CPU�ĺ���
        /// </summary>
        public static int ProcessorCount
        {
            get
            {
                return Environment.ProcessorCount;
            }
        }
        ///// <summary>
        ///// ��ǰϵͳ���ڴ棨M��
        ///// </summary>
        //public static int HostWorkingSet
        //{
        //    get
        //    {

        //        return (int)((Environment.WorkingSet / 1024) / 1024);
        //    }
        //}

        private static string _HostIP;
        /// <summary>
        /// ��������IP�����ޣ��򷵻�������
        /// </summary>
        public static string HostIP
        {
            get
            {
                if (string.IsNullOrEmpty(_HostIP))
                {
                    IPAddress[] addressList = Dns.GetHostAddresses(HostName);
                    foreach (IPAddress address in addressList)
                    {
                        string ip = address.ToString();
                        if (ip.EndsWith(".1") || ip.Contains(":")) // ����·�ɺ�������ַ��
                        {
                            continue;
                        }
                        _HostIP = ip;
                        break;
                    }
                }
                return _HostIP ?? HostName;
            }
        }

    }
    /// <summary>
    /// �仯�Ĳ�������
    /// </summary>
    internal partial class LocalEnvironment
    {
        /// <summary>
        /// ��ǰ����ռ�õ��ڴ棨M��
        /// </summary>
        public int ProcessWorkingSet
        {
            get
            {
                return (int)((Environment.WorkingSet / 1024) / 1024);
                // System.Diagnostics.p
            }
        }
        //CPUռ����

    }
}
