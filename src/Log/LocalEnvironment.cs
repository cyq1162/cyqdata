using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

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
                    var nets = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (var item in nets)
                    {
                        var ips = item.GetIPProperties().UnicastAddresses;
                        foreach (var ip in ips)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip.Address))
                            {
                                string ipAddr = ip.Address.ToString();
                                if (ipAddr.EndsWith(".1") || ipAddr.Contains(":")) // ����·�ɺ�������ַ��
                                {
                                    continue;
                                }
                                _HostIP = ipAddr;
                                break;
                            }
                        }
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
        private static int _ProcessID;
        /// <summary>
        /// ��ǰ����ID
        /// </summary>
        public static int ProcessID
        {
            get
            {
                if (_ProcessID == 0)
                {
                    _ProcessID = Process.GetCurrentProcess().Id;
                }
                return _ProcessID;
            }
        }
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
