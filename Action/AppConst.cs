using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using CYQ.Data.Tool;
namespace CYQ.Data
{
    /// <summary>
    /// 内部常量类
    /// </summary>
    internal static class AppConst
    {
        #region 相关网址常量
        internal const string Github = "https://github.com/cyq1162/cyqdata";
        internal const string Host = "cyqdata.com";
        internal const string Host_Aries = "aries.cyqdata.com";
        internal const string Host_Taurus = "taurus.cyqdata.com";
        internal const string Host_Lic = "lic.cyqdata.com";
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
                    _HR = AppConfig.IsWeb ? "<hr>" : NewLine + "<---END--->" + NewLine;
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
                    _BR = AppConfig.IsWeb ? "<br />" : NewLine;
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
        /// <summary>
        /// 框架程序集名称
        /// </summary>
        internal static string _DLLFullName = string.Empty;
        static string _RunfolderPath;
        internal static string HNKey = LocalEnvironment.HostName;
        /// <summary>
        /// 框架的程序集所在的运行路径
        /// </summary>
        internal static string AssemblyPath
        {
            get
            {
                if (string.IsNullOrEmpty(_RunfolderPath))
                {
                    Assembly ass = System.Reflection.Assembly.GetExecutingAssembly();
                    _DLLFullName = ass.FullName;
                    _RunfolderPath = ass.CodeBase;
                    _RunfolderPath = System.IO.Path.GetDirectoryName(_RunfolderPath);
                    _RunfolderPath = _RunfolderPath.Replace(AppConst.FilePre, string.Empty).TrimStart('\\');
                    if (_RunfolderPath.Contains("\\"))
                    {
                        _RunfolderPath += "\\";
                    }
                    else
                    {
                        _RunfolderPath += "/";
                    }
                    ass = null;
                }
                return _RunfolderPath;
            }
        }
        #endregion

        #region 属性常量

        public static Type JsonIgnoreType = typeof(JsonIgnoreAttribute);
        public static Type JsonEnumToStringType = typeof(JsonEnumToStringAttribute);
        public static Type JsonEnumToDescriptionType = typeof(JsonEnumToDescriptionAttribute);
        #endregion
    }
}
