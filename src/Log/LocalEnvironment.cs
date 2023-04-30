using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CYQ.Data
{
    /// <summary>
    /// 用于收集本机的环境信息
    /// 相对固定的属性
    /// </summary>
    internal partial class LocalEnvironment
    {
        /// <summary>
        /// 电脑的名称
        /// </summary>
        public static string HostName
        {
            get
            {
                return Environment.MachineName;
            }
        }
        /// <summary>
        /// 程序运行的当前用户名。
        /// </summary>
        public static string UserName
        {
            get
            {
                return Environment.UserName;
            }
        }
        /// <summary>
        /// CPU的核数
        /// </summary>
        public static int ProcessorCount
        {
            get
            {
                return Environment.ProcessorCount;
            }
        }
        ///// <summary>
        ///// 当前系统的内存（M）
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
        /// 本机内网IP，若无，则返回主机名
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
                                if (ipAddr.EndsWith(".1") || ipAddr.Contains(":")) // 忽略路由和网卡地址。
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
    /// 变化的部分属性
    /// </summary>
    internal partial class LocalEnvironment
    {
        private static int _ProcessID;
        /// <summary>
        /// 当前进程ID
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
        /// 当前进程占用的内存（M）
        /// </summary>
        public int ProcessWorkingSet
        {
            get
            {
                return (int)((Environment.WorkingSet / 1024) / 1024);
                // System.Diagnostics.p
            }
        }
        //CPU占用率

    }
}
