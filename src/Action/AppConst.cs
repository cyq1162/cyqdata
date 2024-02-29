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
    /// ֻ������
    /// </summary>
    public static partial class AppConst
    {
        #region ����ʹ�ó���

        #region ������� IsDebugMode
        private static object lockObj = new object();
        private static bool? _IsDebugMode;
        /// <summary>
        /// Get �Ƿ��� Debug ģʽ�����С�
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
                                        //ȥ��ϵͳdll
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

        #region Web��� IsWeb��WebRootPath��IsNetCore


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
        /// Get ���и�Ŀ¼����"/"��"\"��β��
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
        /// Get ��ǰ���л����Ƿ� Web Ӧ�á�
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
                if (HttpContext.Current != null)// && HttpContext.Current.Handler != null //AppConfig.XHtml.Domain ������Ҫ��Begin�¼��л�ȡ�����ֻ��ð��һȡ��
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

        #region �������� RunPath��AssemblyPath��Version


        /// <summary>
        /// Get ��ܵ�����·��(������Ŀ¼������"\\" ��"/"��β
        /// Win�� .NetCore ��Ŀ����dll��exe���ڵ�Ŀ¼��
        /// ASPNET ��Ŀ����ָ��Ŀ¼��
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
        ///// ��ܳ�������
        ///// </summary>
        //private static string _DLLFullName = string.Empty;
        private static string _AssemblyPath;

        /// <summary>
        /// Get ����(dll) ���ڵ�����·������"\\" ��"/"��β
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
        /// ��ȡ CYQ.Data.dll �İ汾��
        /// </summary>
        public static string Version
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }


        #endregion

        #region ϵͳ ProcessID��HostIP

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
        private static string _HostIP;
        /// <summary>
        /// ��������IP�����ޣ�127.0.0.1
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
                    { // �������������
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
                                if (ipAddr.EndsWith(".1") || ipAddr.Contains(":")) // ����·�ɺ�������ַ��
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
        #region �ڲ�ʹ�ó���


        #region �����ַ����
        internal static readonly string Github = "https://github.com/cyq1162/cyqdata";
        internal static readonly string Host = "cyqdata.com";
        internal static readonly string Host_Aries = "aries.cyqdata.com";
        internal static readonly string Host_Taurus = "taurus.cyqdata.com";
        internal static readonly string Host_Lic = "lic.cyqdata.com";
        #endregion

        #region License ����

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

        #region ȫ��
        internal const string FilePre = "file:";
        internal const string Result = ".Result";
        internal const string Global_NotImplemented = "The method or operation is not implemented.";
        internal const string ACKey = "4pMxvlk1OlOv0K6z96T+mNDBdEkX6mPa7Yq27cWP/u0#=2";
        internal const string ALKey = "YH/xArdNhygAvQ7NwJiq2HreAmphvcTP7Yq27cWP/u0#=2";

        #endregion

        #region ��̬����
        /// <summary>
        /// ��Ч���ļ�·���ַ�
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

        #region ���Գ���

        internal static Type JsonIgnoreType = typeof(JsonIgnoreAttribute);
        //internal static Type JsonFormatType = typeof(JsonFormatAttribute);
        internal static Type JsonEnumToStringType = typeof(JsonEnumToStringAttribute);
        //internal static Type JsonEnumToDescriptionType = typeof(JsonEnumToDescriptionAttribute);
        #endregion

        #endregion
    }
}
