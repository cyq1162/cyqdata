using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Web;
using CYQ.Data.Json;
using CYQ.Data.Tool;
namespace CYQ.Data
{
    /// <summary>
    /// 只读属性
    /// </summary>
    public static partial class AppConst
    {
        #region 对外使用常量

        #region 程序调试 IsDebugMode
        private static object lockObj = new object();
        private static bool? _IsDebugMode;
        /// <summary>
        /// Get 是否在 Debug 模式下运行。
        /// </summary>
        public static bool IsDebugMode
        {
            get
            {
                if (_IsDebugMode == null)
                {
                    lock (lockObj)
                    {
                        if (_IsDebugMode == null)
                        {
                            var assembly = Assembly.GetEntryAssembly();
                            if (assembly == null)
                            {
                                Assembly[] assList = AppDomain.CurrentDomain.GetAssemblies();
                                foreach (Assembly ass in assList)
                                {
                                    if (ass.GlobalAssemblyCache || ass.GetName().GetPublicKeyToken().Length > 0)
                                    {
                                        //去掉系统dll
                                        continue;
                                    }
                                    object[] das = ass.GetCustomAttributes(typeof(DebuggableAttribute), true);
                                    if (das.Length > 0)
                                    {
                                        DebuggableAttribute da = das[0] as DebuggableAttribute;
                                        if (da.IsJITTrackingEnabled)
                                        {
                                            _IsDebugMode = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                object[] das = assembly.GetCustomAttributes(typeof(DebuggableAttribute), true);
                                if (das.Length > 0)
                                {
                                    DebuggableAttribute da = das[0] as DebuggableAttribute;
                                    _IsDebugMode = da.IsJITTrackingEnabled;
                                    // _IsDebugMode = (da.DebuggingFlags & DebuggableAttribute.DebuggingModes.EnableEditAndContinue) == DebuggableAttribute.DebuggingModes.EnableEditAndContinue;
                                }
                            }
                            if (_IsDebugMode == null) { _IsDebugMode = false; }
                        }
                    }

                }

                return _IsDebugMode.Value;
            }
        }

        #endregion

        #region Web相关 IsWeb、WebRootPath、IsNetCore


        //[Conditional("DEBUG")]
        private static void SetDebugRootPath(ref string path)
        {
            if (IsDebugMode)
            {
                bool isdebug = IsDebugMode;
                path = Environment.CurrentDirectory;
                path = path + (path[0] == '/' ? '/' : '\\');
            }
        }
        private static readonly object lockPathObj = new object();
        private static string _WebRootPath;

        /// <summary>
        /// Get 运行根目录，以"/"或"\"结尾。
        /// </summary>
        public static string WebRootPath
        {
            get
            {
                if (string.IsNullOrEmpty(_WebRootPath))
                {
                    if (IsNetCore)
                    {
                        lock (lockPathObj)
                        {
                            if (string.IsNullOrEmpty(_WebRootPath))
                            {
                                string path = AppDomain.CurrentDomain.BaseDirectory;
                                SetDebugRootPath(ref path);
                                path = path + "wwwroot";
                                if (path[0] == '/')
                                {
                                    path = path + "/";
                                }
                                else
                                {
                                    path = path + "\\";
                                }
                                if (Directory.Exists(path) || IsWeb)
                                {
                                    _WebRootPath = path;
                                }
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(_WebRootPath))
                    {
                        _WebRootPath = AppDomain.CurrentDomain.BaseDirectory;
                    }

                }
                return _WebRootPath;
            }
        }

        private static readonly object lockWebObj = new object();
        private static int webState = -1;
        /// <summary>
        /// Get 当前运行环境是否 Web 应用。
        /// </summary>
        public static bool IsWeb
        {
            get
            {
                if (webState == -1)
                {
                    lock (lockWebObj)
                    {
                        if (webState == -1)
                        {
                            string basePath = AppDomain.CurrentDomain.BaseDirectory;
                            bool isWeb = File.Exists(basePath + "web.config");
                            if (!isWeb && IsNetCore)
                            {
                                isWeb = File.Exists(basePath + "Taurus.Core.dll") || File.Exists(basePath + "Aries.Core.dll") || Directory.Exists("wwwroot");
                                if (!isWeb)
                                {
                                    Assembly[] assList = AppDomain.CurrentDomain.GetAssemblies();
                                    foreach (Assembly ass in assList)
                                    {
                                        if (isWeb) { break; }
                                        switch (ass.GetName().Name)
                                        {
                                            case "Microsoft.AspNetCore.Server.Kestrel":
                                            case "Microsoft.AspNetCore.Server.IIS":
                                                isWeb = true;
                                                break;
                                        }
                                    }
                                }
                            }
                            webState = isWeb ? 1 : 0;
                        }
                    }

                }
                return webState == 1;
            }
        }
        internal static Uri WebUri
        {
            get
            {
                if (HttpContext.Current != null)// && HttpContext.Current.Handler != null //AppConfig.XHtml.Domain 可能需要在Begin事件中获取，因此只能冒险一取。
                {
                    try
                    {
                        return HttpContext.Current.Request.Url;
                    }
                    catch
                    {
                        return null;
                    }
                }
                return null;
            }
        }

        #endregion

        #region 程序运行 RunPath、AssemblyPath、Version


        /// <summary>
        /// Get 框架的运行路径(最外层的目录），以"\\" 或"/"结尾
        /// Win、 .NetCore 项目：是dll和exe所在的目录；
        /// ASPNET 项目：是指根目录；
        /// </summary>
        public static string RunPath
        {
            get
            {
                if (IsWeb)
                {
                    string path = AppDomain.CurrentDomain.BaseDirectory;
                    if (IsNetCore)
                    {
                        SetDebugRootPath(ref path);
                    }
                    return path;
                }
                return AssemblyPath;
            }
        }
        ///// <summary>
        ///// 框架程序集名称
        ///// </summary>
        //private static string _DLLFullName = string.Empty;
        private static string _AssemblyPath;

        /// <summary>
        /// Get 程序集(dll) 所在的运行路径，以"\\" 或"/"结尾
        /// </summary>
        public static string AssemblyPath
        {
            get
            {
                if (string.IsNullOrEmpty(_AssemblyPath))
                {
                    Assembly ass = System.Reflection.Assembly.GetExecutingAssembly();
                    //_DLLFullName = ass.FullName;
                    _AssemblyPath = System.IO.Path.GetDirectoryName(ass.CodeBase).Replace(FilePre, string.Empty).TrimStart('\\');
                    if (_AssemblyPath.Contains("\\"))
                    {
                        _AssemblyPath += "\\";
                    }
                    else
                    {
                        _AssemblyPath += "/";
                    }
                    ass = null;
                }
                return _AssemblyPath;
            }
        }

        /// <summary>
        /// 获取 CYQ.Data.dll 的版本号
        /// </summary>
        public static string Version
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }


        #endregion

        #region 系统 ProcessID、HostIP

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
        private static string _HostIP;
        /// <summary>
        /// 本机内网IP，若无，127.0.0.1
        /// </summary>
        public static string HostIP
        {
            get
            {
                if (string.IsNullOrEmpty(_HostIP))
                {
                    bool isSupportDADS = true;
                    var nets = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (var item in nets)
                    { // 跳过虚拟机网卡
                        if (item.Description.StartsWith("VirtualBox ") || item.Description.StartsWith("Hyper-V") || item.Description.StartsWith("VMware ") || item.Description.StartsWith("Bluetooth "))
                        {
                            continue;
                        }
                        var ips = item.GetIPProperties().UnicastAddresses;
                        foreach (var ip in ips)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip.Address))
                            {
                                try
                                {
                                    if (isSupportDADS)
                                    {
                                        if (ip.DuplicateAddressDetectionState != DuplicateAddressDetectionState.Preferred)
                                        {
                                            continue;
                                        }

                                    }
                                }
                                catch (PlatformNotSupportedException err)
                                {
                                    isSupportDADS = false;
                                }

                                string ipAddr = ip.Address.ToString();
                                if (ipAddr.EndsWith(".1") || ipAddr.Contains(":")) // 忽略路由和网卡地址。
                                {
                                    continue;
                                }
                                _HostIP = ipAddr;
                                return _HostIP;
                            }
                        }
                    }
                }
                return _HostIP ?? "127.0.0.1";
            }
        }

        #endregion



        #endregion
    }

    public static partial class AppConst
    {
        #region 内部使用常量


        #region 相关网址常量
        internal static readonly string Github = "https://github.com/cyq1162/cyqdata";
        internal static readonly string Host = "cyqdata.com";
        internal static readonly string Host_Aries = "aries.cyqdata.com";
        internal static readonly string Host_Taurus = "taurus.cyqdata.com";
        internal static readonly string Host_Lic = "lic.cyqdata.com";
        #endregion

        #region License 常量

        //internal const string Lic_Error_Contact = "\r\nContact email:cyq1162@126.com;QQ:272657997\r\n site : http://www.cyqdata.com/cyqdata";
        ////internal const string Lic_Error_AtNight = "Sorry ! You need to get a license key when you run it at night!";
        //internal const string Lic_Error_NotBuyProvider = "Sorry ! Your license key not contains this provider function : ";
        //internal const string Lic_Error_InvalidVersion = "Sorry ! Your license key version invalid!";
        //internal const string Lic_Error_InvalidKey = "Sorry ! Your license key is invalid!";
        //internal const string Lic_PublicKey = "CYQ.Data.License";
        //internal const string Lic_UseKeyFileName = "cyq.data.keys";
        ////internal const string Lic_DevKeyFileName = "/cyq.data.dev.keys";
        //internal const string Lic_MacKeyType = "mac";
        //internal const string Lic_DllKeyType = "dll";
        //internal const string Lic_AriesCore = "Aries.Core";
        //internal const string Lic_AriesLogic = "Aries.Logic";
        #endregion

        #region 全局
        internal const string FilePre = "file:";
        internal const string Result = ".Result";
        internal const string Global_NotImplemented = "The method or operation is not implemented.";
        internal const string ACKey = "4pMxvlk1OlOv0K6z96T+mNDBdEkX6mPa7Yq27cWP/u0#=2";
        internal const string ALKey = "YH/xArdNhygAvQ7NwJiq2HreAmphvcTP7Yq27cWP/u0#=2";

        #endregion

        #region 静态常量
        /// <summary>
        /// 无效的文件路径字符
        /// </summary>
        internal static char[] InvalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
        private static string _HR;

        internal static string HR
        {
            get
            {
                if (string.IsNullOrEmpty(_HR))
                {
                    _HR = IsWeb ? "<hr />" : NewLine + "<---END--->" + NewLine;
                }
                return _HR;
            }
            set
            {
                _HR = value;
            }
        }
        private static string _BR;

        internal static string BR
        {
            get
            {
                if (string.IsNullOrEmpty(_BR))
                {
                    _BR = IsWeb ? "<br />" : NewLine;
                }
                return _BR;
            }
            set
            {
                _BR = value;
            }
        }
        internal static string NewLine
        {
            get
            {
                return Environment.NewLine;
            }
        }
        internal static string SplitChar = "$,$";
        internal static string HNKey = Environment.MachineName;

        #endregion

        #region 属性常量

        internal static Type JsonIgnoreType = typeof(JsonIgnoreAttribute);
        //internal static Type JsonFormatType = typeof(JsonFormatAttribute);
        internal static Type JsonEnumToStringType = typeof(JsonEnumToStringAttribute);
        //internal static Type JsonEnumToDescriptionType = typeof(JsonEnumToDescriptionAttribute);
        #endregion

        #endregion
    }
}
