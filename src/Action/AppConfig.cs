﻿using System;
using System.Configuration;
using CYQ.Data.SQL;
using CYQ.Data.Tool;
using System.Web;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Reflection;

namespace CYQ.Data
{
    /// <summary>
    /// 常用配置文件项[Web(App).Config]的[appSettings|connectionStrings]项和属性名一致。
    /// 除了可以从配置文件配置，也可以直接赋值。
    /// </summary>
    public static partial class AppConfig
    {
        static AppConfig()
        {
            //这个比较常用，只好把这里当成应用程序入口最早的调用处
            DBSchema.InitDBSchemasOnStart();
        }
        #region 基方法
        private static MDictionary<string, string> appConfigs = new MDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static MDictionary<string, string> connConfigs = new MDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        //internal static void Clear()
        //{
        //    appConfigs.Clear();
        //    connConfigs.Clear();
        //}
        /// <summary>
        /// 设置Web.config或App.config的值 value为null时移除缓存。
        /// </summary>
        public static bool SetApp(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) { return false; }
            if (value == null)
            {
                return appConfigs.Remove(key);
            }
            if (appConfigs.ContainsKey(key))
            {
                appConfigs[key] = value;
            }
            else
            {
                appConfigs.Add(key, value);
            }

            return true;
        }
        /// <summary>
        /// 获取Web.config或App.config的值。
        /// </summary>
        public static string GetApp(string key)
        {
            return GetApp(key, string.Empty);
        }
        /// <summary>
        /// 获取Web.config或App.config的值（允许值不存在或为空时输出默认值）。
        /// </summary>
        public static string GetApp(string key, string defaultValue)
        {
            if (appConfigs.ContainsKey(key))
            {
                return appConfigs[key];
            }
            else
            {
                var appSettings = ConfigurationManager.AppSettings;
                string value = appSettings[key];
                if (string.IsNullOrEmpty(value) && key.IndexOf('.') > 0)
                {
                    value = appSettings[key.Replace(".", "")];
                    if (string.IsNullOrEmpty(value) && IsNetCore)
                    {
                        value = Convert.ToString(ConfigurationManager.GetSection(key));
                    }
                    if (string.IsNullOrEmpty(value))
                    {
                        value = appSettings[key.Substring(key.IndexOf('.') + 1)];
                    }
                }
                value = string.IsNullOrEmpty(value) ? defaultValue : value;
                // 注释以下代码：读取时，配置面不写入缓存字典【修改配置文件不重置通过代码设置的配置项。】
                //try
                //{
                //    if (!string.IsNullOrEmpty(value)) 
                //    {
                //        appConfigs.Add(key, value);
                //    }
                //}
                //catch
                //{

                //}

                return value;
            }
        }
        /// <summary>
        /// 获取Web.config或App.config的数字值（允许值不存在或为空时输出默认值）。
        /// </summary>
        public static int GetAppInt(string key, int defaultValue)
        {
            int result = 0;
            string value = GetApp(key);
            if (!int.TryParse(value, out result))
            {
                return defaultValue;
            }
            return result;
        }
        /// <summary>
        /// 获取Web.config或App.config的数字值（允许值不存在或为空时输出默认值）。
        /// </summary>
        public static bool GetAppBool(string key, bool defaultValue)
        {
            return ConvertTool.ChangeType<bool>(GetApp(key, defaultValue.ToString()));
        }

        /// <summary>
        /// 获取Web.config或App.config的connectionStrings节点的值。
        /// </summary>
        public static string GetConn(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = DB.DefaultConn;
            }
            if (name.Trim().Contains(" "))
            {
                return name;
            }
            if (connConfigs.ContainsKey(name))
            {
                return connConfigs[name];
            }
            ConnectionStringSettings conn = ConfigurationManager.ConnectionStrings[name];
            if (conn != null)
            {
                string connString = conn.ConnectionString;
                if (name == connString || string.IsNullOrEmpty(connString))//避免误写自己造成死循环。
                {
                    return name;
                }


                if (connString.EndsWith(".txt") || connString.EndsWith(".ini") || connString.EndsWith(".json"))
                {
                    return ConnConfigWatch.Start(name, connString);
                }
                //允许配置文件里加密。
                if (connString.Length > 32 && connString.Split(';', '=', ' ').Length == 1)
                {
                    connString = EncryptHelper.Decrypt(connString);
                }
                //启动高可用配置加载方式
                if (connString.Length < 32 && connString.Split(' ').Length == 1)
                {
                    return GetConn(connString);
                }
                // 注释以下代码：读取时，配置面不写入缓存字典【修改配置文件不重置通过代码设置的配置项。】
                //if (!connConfigs.ContainsKey(name))
                //{
                //    connConfigs.Add(name, connString);
                //}
                return connString;
            }
            if (name.Length > 32 && name.Split('=').Length > 3 && name.Contains(";")) //链接字符串很长，没空格的情况 txt path={0}
            {
                return name;
            }
            if (name == "Conn" && name != DB.DefaultConn)
            {
                return GetConn(DB.DefaultConn);
            }
            return "";
        }

        /// <summary>
        /// 添加自定义链接（内存有效，并未写入config文件） value为null时移除缓存
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="connectionString">链接字符串</param>
        public static bool SetConn(string name, string connectionString)
        {
            if (string.IsNullOrEmpty(name)) { return false; }
            if (connectionString == null)
            {
                return connConfigs.Remove(name);
            }
            if (!connConfigs.ContainsKey(name))
            {
                connConfigs.Add(name, connectionString);
            }
            else
            {
                connConfigs[name] = connectionString;
            }
            return true;
        }
        #endregion


        /// <summary>
        /// 是否使用表字段枚举转Int方式（默认为false）。
        /// 设置为true时，可以加快一点性能，但生成的表字段枚举必须和数据库一致。
        /// </summary>
        public static bool IsEnumToInt
        {
            get
            {
                return GetAppBool("IsEnumToInt", false);
            }
            set
            {
                SetApp("IsEnumToInt", value.ToString());
            }
        }
        /// <summary>
        /// 是否ToJson输出时自动转义特殊符号("\ \r \t等)
        /// 可配置项为（Default、Yes、No)
        /// </summary>
        public static string JsonEscape
        {
            get
            {
                return GetApp("JsonEscape", "Default");
            }
            set
            {
                SetApp("JsonEscape", value);
            }
        }
        /// <summary>
        /// Aop 插件配置项 示例配置：[ 完整类名,程序集(dll)名称]&lt;add key="Aop" value="Web.Aop.AopAction,Aop"/>
        /// </summary>
        public static string Aop
        {
            get
            {
                return GetApp("Aop");
            }
            set
            {
                SetApp("Aop", value);
            }
        }
        /// <summary>
        /// Tool.ThreadBreak 使用时，外置的文件配置相对路径（默认在环境变量Temp对应文件中）
        /// </summary>
        public static string ThreadBreakPath
        {
            get
            {
                return GetApp("ThreadBreakPath", Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.User));
            }
            set
            {
                SetApp("ThreadBreakPath", value);
            }
        }

        /// <summary>
        /// 获取当前Dll的版本号
        /// </summary>
        public static string Version
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }
        /// <summary>
        /// 框架的运行路径(最外层的目录），以"\\" 或"/"结尾
        /// Win项目：是dll和exe所在的目录；
        /// Asp.net项目：是指根目录；
        /// Asp.net core 项目：是指运行的路径（dll所在的路径，同Win项目）。
        /// </summary>
        public static string RunPath
        {
            get
            {
                if (AppConfig.IsWeb)
                {
                    return AppDomain.CurrentDomain.BaseDirectory;
                }
                return AppConst.AssemblyPath;
            }
        }
        /// <summary>
        /// 框架的程序集(dll)所在的运行路径，以"\\" 或"/"结尾
        /// </summary>
        public static string AssemblyPath
        {
            get
            {
                return AppConst.AssemblyPath;
            }
        }
        /// <summary>
        /// EncryptHelper加密的Key。
        /// </summary>
        public static string EncryptKey
        {
            get
            {
                return GetApp("EncryptKey", "");
            }
            set
            {
                SetApp("EncryptKey", value);
            }
        }

        /// <summary>
        /// Cache.CacheManage 默认缓存项的时间[分钟(默认60)]
        /// </summary>
        public static int DefaultCacheTime
        {
            get
            {
                return GetAppInt("DefaultCacheTime", 60);
            }
            set
            {
                SetApp("DefaultCacheTime", value.ToString());
            }
        }
    }
    public static partial class AppConfig
    {
        #region Web相关

        private static string _WebRootPath;
        //内部变量
        /// <summary>
        /// Web根目录，以"/"或"\"结尾。
        /// (ASP.NET Core 项目时，由于机制不同，指向的路径需要调整，所以该值可以修改)
        /// </summary>
        public static string WebRootPath
        {
            get
            {
                if (string.IsNullOrEmpty(_WebRootPath))
                {
                    if (IsNetCore)
                    {
                        string path = AppDomain.CurrentDomain.BaseDirectory + "wwwroot";
                        if (path.StartsWith("/"))
                        {
                            path = path + "/";
                        }
                        else
                        {
                            path = path + "\\";
                        }
                        if (Directory.Exists(path))
                        {
                            _WebRootPath = path;
                        }
                    }
                    if (string.IsNullOrEmpty(_WebRootPath))
                    {
                        _WebRootPath = AppDomain.CurrentDomain.BaseDirectory;
                    }

                }
                return _WebRootPath;
            }
            set
            {
                _WebRootPath = value;
                if (value.Contains("wwwroot"))
                {
                    webState = 1;
                }
            }
        }
        private static int webState = -1;
        /// <summary>
        /// 当前运行环境是否Web应用。
        /// </summary>
        public static bool IsWeb
        {
            get
            {
                if (webState == -1)
                {
                    if (HttpContext.Current != null || File.Exists(AppDomain.CurrentDomain.BaseDirectory + "web.config")
                       || (WebRootPath.Contains("wwwroot"))
                       || File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Taurus.Core.dll")
                       || File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Aries.Core.dll"))
                    {
                        webState = 1;
                    }
                }
                return webState == 1;
            }
            set
            {
                if (webState != 1)
                {
                    webState = value ? 1 : 0;
                }
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
        //读配置文件时会修改此值。
        private static int _NetCoreState = -1;
        /// <summary>
        /// 当前是否.NET Core环境。
        /// </summary>
        public static bool IsNetCore
        {
            get
            {
                if (_NetCoreState == -1)
                {
                    Assembly ass = Assembly.GetExecutingAssembly();
                    foreach (var item in ass.GetCustomAttributes(typeof(AssemblyTitleAttribute), false))
                    {
                        if (((AssemblyTitleAttribute)item).Title.Contains("Core"))
                        {
                            _NetCoreState = 1;
                            break;
                        }
                    }
                    if (_NetCoreState != 1)
                    {
                        _NetCoreState = 0;
                    }

                }
                return _NetCoreState == 1;
            }
        }
        #endregion
    }
    public static partial class AppConfig
    {
        #region Xml相关配置
        /// <summary>
        /// XHtml 相关的配置
        /// </summary>
        public static class XHtml
        {
            /// <summary>
            /// Xml.XHtmlHelper 中使用的 "&lt;![CDATA[" 项
            /// </summary>
            internal static string CDataLeft
            {
                get
                {
                    return GetApp("CDataLeft", "<![CDATA[");
                }
                set
                {
                    SetApp("CDataLeft", value);
                }
            }
            /// <summary>
            /// Xml.XHtmlHelper 中使用的 "]]&gt;" 项
            /// </summary>
            internal static string CDataRight
            {
                get
                {
                    return GetApp("CDataRight", "]]>");
                }
                set
                {
                    SetApp("CDataRight", value);
                }
            }

            /// <summary>
            /// Xml.XHtmlHelper 中操作Html需要配置的DTD解析文档相对路径
            /// </summary>
            public static string DtdUri
            {
                get
                {
                    string dtdUri = GetApp("XHtml.DtdUri", (IsWeb ? "/App_Data/dtd/" : "/Setting/DTD/") + "xhtml1-transitional.dtd");
                    if (dtdUri != null && dtdUri.IndexOf("http://") == -1)//相对路径
                    {
                        dtdUri = AppConfig.WebRootPath + dtdUri.TrimStart('/');//.Replace("/", "\\");
                        if (!dtdUri.StartsWith("/") && dtdUri.Contains(":\\"))
                        {
                            dtdUri = dtdUri.Replace("/", "\\");
                        }
                    }
                    return dtdUri;
                }
                set
                {
                    SetApp("XHtml.DtdUri", value);
                }
            }
            private static string _Domain;
            /// <summary>
            /// Xml.MutilLanguage 语言切换设置时Cookie所需要的网站主域名[不带www]
            /// </summary>
            public static string Domain
            {
                get
                {
                    if (string.IsNullOrEmpty(_Domain))
                    {
                        Uri uri = AppConfig.WebUri;
                        string domainList = GetApp("XHtml.Domain", "");
                        if (!string.IsNullOrEmpty(domainList))
                        {
                            string[] domains = domainList.Split(',');
                            if (domains != null && domains.Length > 0)
                            {

                                if (domains.Length > 1 && uri != null)
                                {
                                    foreach (string domain in domains)
                                    {
                                        if (uri.Authority.Contains(domain))
                                        {
                                            _Domain = domain;
                                            break;
                                        }
                                    }
                                }
                                if (string.IsNullOrEmpty(_Domain))
                                {
                                    _Domain = domains[0];
                                }
                            }
                        }
                        else
                        {
                            if (uri != null && uri.Host != "localhost" && uri.Host != "127.0.0.1")
                            {
                                _Domain = uri.Host.Replace("www.", string.Empty);
                            }
                        }
                    }
                    return _Domain;
                }
                set
                {
                    _Domain = string.Empty;
                    SetApp("XHtml.Domain", value);
                }
            }
            //private static int _UserFileLoadXml = -1;
            ///// <summary>
            ///// Xml.XHtmlHelper 操作Html时，配置此项使用File加载Xml[便可在IIS7以上非信任主机机制下使用]
            ///// </summary>
            //public static bool UseFileLoadXml
            //{
            //    get
            //    {
            //        if (_UserFileLoadXml == -1)
            //        {
            //            _UserFileLoadXml = GetApp("UseFileLoadXml") == "true" ? 1 : 0;
            //        }
            //        return _UserFileLoadXml == 1;
            //    }
            //    set
            //    {
            //        _UserFileLoadXml = value ? 1 : 0;
            //    }
            //}

            /// <summary>
            /// Xml.MutilLanguage 类的默认语言Key，默认值：Chinese
            /// </summary>
            public static string SysLangKey
            {
                get
                {
                    return GetApp("XHtml.SysLangKey", "Chinese");
                }
                set
                {
                    SetApp("XHtml.SysLangKey", value);
                }
            }
        }
        #endregion


        #region 数据库相关
        /// <summary>
        /// DataBase 相关的配置
        /// </summary>
        public static class DB
        {

            static string _DefaultConn = string.Empty;
            /// <summary>
            /// 默认数据库链接（可赋完整链接语句或Web.config配置项名称）
            /// 如果不在配置文件(Web.Config）上配置Conn链接，可对此属性赋值进行配置。
            /// </summary>
            public static string DefaultConn
            {
                get
                {
                    if (string.IsNullOrEmpty(_DefaultConn))
                    {
                        _DefaultConn = "Conn";
                        if (ConfigurationManager.ConnectionStrings != null && ConfigurationManager.ConnectionStrings[_DefaultConn] == null)
                        {
                            foreach (ConnectionStringSettings item in ConfigurationManager.ConnectionStrings)
                            {
                                if (item.Name.ToLower().EndsWith("conn"))
                                {
                                    _DefaultConn = item.Name;
                                    break;
                                }
                            }
                        }
                    }

                    return _DefaultConn;
                }
                set
                {
                    if (value == null)
                    {
                        value = string.Empty;
                    }
                    ConnObject.Remove("Conn");//移除原有配置文件的Conn项的链接缓存。
                    ConnBean.Remove("Conn");
                    _DefaultConn = value;
                    SetConn("Conn", value);

                }
            }
            /*
            static string _DefaultConnBak = string.Empty;
            /// <summary>
            /// 默认备用数据库链接（当主数据挂掉时，会自动切换到备用数据库链接）
            /// 如果不在配置文件(Web.Config）上配置Conn_Bak链接，可对此属性赋值进行配置。
            /// </summary>
            public static string DefaultConnBak
            {
                get
                {
                    if (_DefaultConnBak == string.Empty && _DefaultConn.Length < 32 && _DefaultConn.Split(' ').Length == 1)
                    {
                        return _DefaultConn + "_Bak";
                    }
                    return _DefaultConnBak;
                }
                set
                {
                    _DefaultConnBak = value;
                }
            }
            */
            private static string _DefaultDataBaseName;
            /// <summary>
            /// 默认数据库名称（只读）
            /// </summary>
            public static string DefaultDataBaseName
            {
                get
                {
                    if (string.IsNullOrEmpty(_DefaultDataBaseName))
                    {
                        SetDefault();

                    }
                    return _DefaultDataBaseName;
                }
            }
            private static DataBaseType _DefaultDataBaseType = DataBaseType.None;
            /// <summary>
            /// 默认数据库类型（只读）
            /// </summary>
            public static DataBaseType DefaultDataBaseType
            {
                get
                {
                    if (_DefaultDataBaseType == DataBaseType.None)
                    {
                        SetDefault();
                    }
                    return _DefaultDataBaseType;
                }
            }
            private static void SetDefault()
            {
                DalBase db = DalCreate.CreateDal(DefaultConn);
                if (db != null)
                {
                    _DefaultDataBaseName = db.DataBaseName;
                    _DefaultDataBaseType = db.DataBaseType;
                    db.Dispose();
                }
            }
            ///// <summary>
            ///// MSSQL是否启用分页存储过程SelectBase，默认false
            ///// </summary>
            //public static bool PagerBySelectBase
            //{
            //    get
            //    {
            //        return GetAppBool("PagerBySelectBase", false);
            //    }
            //    set
            //    {
            //        SetApp("PagerBySelectBase", value.ToString());
            //    }
            //}
            /// <summary>
            /// 配置此项时，会对：插入/更新/删除的操作进行Lock[请适当使用]
            /// </summary>
            //public static bool LockOnDbExe
            //{
            //    get
            //    {
            //        bool _LockOnDbExe;
            //        bool.TryParse(GetApp("LockOnDbExe"), out _LockOnDbExe);
            //        return _LockOnDbExe;
            //    }
            //    set
            //    {
            //        SetApp("LockOnDbExe", value.ToString());
            //    }
            //}

            /// <summary>
            /// 文本数据库是否只读（用于Demo演示，避免演示账号或数据被删除）
            /// 配置项：DB.IsTxtReadOnly ：false
            /// </summary>
            public static bool IsTxtReadOnly
            {
                get
                {
                    return GetAppBool("DB.IsTxtReadOnly", false);
                }
                set
                {
                    SetApp("DB.IsTxtReadOnly", value.ToString());
                }
            }
            /// <summary>
            /// Postgre 是否小写模式(默认false)。
            /// 配置项：DB.IsPostgreLower ：false
            /// </summary>
            public static bool IsPostgreLower
            {
                get
                {
                    return GetAppBool("DB.IsPostgreLower", false);
                }
                set
                {
                    SetApp("DB.IsPostgreLower", value.ToString());
                }
            }
            /// <summary>
            /// MAction所有操作中的where条件，默认有超强的过滤单词，来过滤Sql注入关键字，如果语句包含指定的过滤词，则会返回错误信息，并记录日志。
            /// 如果需要自定义关键字，可配置此项，如：“delete;from,truncate，其它单词”，分号表词组，需要同时包含两个词； 多个过滤词组以","逗号分隔
            /// 配置项：DB.FilterSqlInjection ：
            /// </summary>
            public static string FilterSqlInjection
            {
                get
                {
                    return GetApp("DB.FilterSqlInjection", SqlInjection.filterSqlInjection);
                }
                set
                {
                    SetApp("DB.ilterSqlInjection", value);
                    if (!string.IsNullOrEmpty(value))
                    {
                        List<string> list = new List<string>();
                        list.AddRange(value.TrimEnd(',').Split(','));
                        SqlInjection.FilterKeyList = list;
                    }
                }
            }
            /*
            /// <summary>
            /// MAction所有操作中的where条件，会被替换为空的字符，默认为“--“。
            /// 如果需要自定义，可配置此项，如：“--,;“ 多个替换符号以逗号分隔
            /// </summary>
            public static string ReplaceSqlInjection
            {
                get
                {
                    return GetApp("ReplaceSqlInjection", SqlInjection.replaceSqlInjection);
                }
                set
                {
                    SetApp("ReplaceSqlInjection", value);
                }
            }
             */
            /// <summary>
            /// MAction 操作 Oracle 时自增加int类型id所需要配置的序列id，Guid为id则不用。
            /// 如果需要为每个表都配置一个序列号，可以使用：SEQ_{0} 其中{0}会自动配对成表名，如果没有{0}，则为整个数据库共用一个序列。
            ///  配置项：DB.AutoID ：SEQ_{0}
            /// </summary>
            public static string AutoID
            {
                get
                {
                    return GetApp("DB.AutoID", "SEQ_{0}");
                }
                set
                {
                    SetApp("DB.AutoID", value);
                }
            }
            /// <summary>
            /// MAction 可将表架构映射到外部指定相对路径[外部存储,可避开数据库读取]
            /// 配置项：DB.SchemaMapPath ： /App_Data/schema
            /// </summary>
            public static string SchemaMapPath
            {
                get
                {
                    return GetApp("DB.SchemaMapPath", IsWeb? "/App_Data/schema":"");
                }
                set
                {
                    SetApp("DB.SchemaMapPath", value);
                }
            }
            /// <summary>
            /// 软删除字段名称（若表存在此设置的字段名称时，MActon的删除操作将变更变为更新操作）
            /// 配置项：DB.DeleteField ： IsDeleted
            /// </summary>
            public static string DeleteField
            {
                get
                {
                    return GetApp("DB.DeleteField", "IsDeleted");
                }
                set
                {
                    SetApp("DB.DeleteField", value);
                }
            }
            /// <summary>
            /// 更新时间字段名称（若表存在指定字段名称时，自动更新时间，多个用逗号分隔）
            /// 配置项：DB.EditTimeFields ： 
            /// </summary>
            public static string EditTimeFields
            {
                get
                {
                    return GetApp("DB.EditTimeFields", string.Empty);
                }
                set
                {
                    SetApp("DB.EditTimeFields", value);
                }
            }
            /// <summary>
            /// 系统全局要隐藏的字段名称（默认值为："cyqrownum,rowguid,deletefield"）
            /// 配置项：DB.HiddenFields ： cyqrownum,rowguid
            /// </summary>
            public static string HiddenFields
            {
                get
                {
                    string result = GetApp("DB.HiddenFields", "cyqrownum,rowguid");
                    //if (result == string.Empty)
                    //{
                    //    result = "cyqrownum,rowguid," + DeleteField;
                    //}
                    return result;
                }
                set
                {
                    if (!value.Contains("cyqrownum,rowguid"))
                    {
                        value = "cyqrownum,rowguid," + value;
                    }
                    SetApp("DB.HiddenFields", value);
                }
            }

            /// <summary>
            /// 全局的数据库命令默认超时设置，默认值120秒（单位：秒）
            /// 配置项：DB.CommandTimeout ： 120
            /// </summary>
            public static int CommandTimeout
            {
                get
                {
                    return GetAppInt("DB.CommandTimeout", 120);
                }
                set
                {
                    SetApp("DB.CommandTimeout", value.ToString());
                }
            }
            /// <summary>
            /// 读写分离时用户对主数据库操作持续时间，默认值10秒s（单位：秒s）
            /// 配置项：DB.MasterSlaveTime ： 10
            /// </summary>
            public static int MasterSlaveTime
            {
                get
                {
                    return GetAppInt("DB.MasterSlaveTime", 10);
                }
                set
                {
                    SetApp("DB.MasterSlaveTime", value.ToString());
                }
            }
            /// <summary>
            /// 毫秒数（记录数据库执行时时长(ms)的SQL语句写入日志，对应配置项Log.Path的配置路径）
            /// 配置项：DB.SqlFilter ： -1
            /// </summary>
            public static int SqlFilter
            {
                get
                {
                    return GetAppInt("DB.SqlFilter", -1);
                }
                set
                {
                    SetApp("DB.SqlFilter", value.ToString());
                }
            }
            /// <summary>
            /// 生成的实体类的后缀。
            /// 配置项：DB.EntitySuffix ：Bean
            /// </summary>
            public static string EntitySuffix
            {
                get
                {
                    return GetApp("DB.EntitySuffix", "Bean");
                }
                set
                {
                    SetApp("DB.EntitySuffix", value);
                }
            }
        }
        #endregion


        #region 分布式缓存

        /// <summary>
        /// Redis 配置
        /// </summary>
        public static class Redis
        {
            /// <summary>
            /// Redis分布式缓存的服务器配置，多个用逗号（,）分隔
            /// 格式：ip:port - password
            /// 配置项：Redis.Servers ：192.168.1.9:6379 - 888888
            /// </summary>
            public static string Servers
            {
                get
                {
                    return GetApp("Redis.Servers", string.Empty);
                }
                set
                {
                    SetApp("Redis.Servers", value);
                }
            }
            /// <summary>
            /// Redis 使用的DB数（默认1，使用db0）
            /// 配置项：Redis.UseDBCount ：1
            /// </summary>
            public static int UseDBCount
            {
                get
                {
                    return GetAppInt("Redis.UseDBCount", 1);
                }
                set
                {
                    SetApp("Redis.UseDBCount", value.ToString());
                }
            }
            /// <summary>
            /// Redis 使用的DB 索引（默认0，若配置，则会忽略RedisUseDBCount）
            /// 配置项：Redis.UseDBIndex ：0
            /// </summary>
            public static int UseDBIndex
            {
                get
                {
                    return GetAppInt("Redis.UseDBIndex", 0);
                }
                set
                {
                    SetApp("Redis.UseDBIndex", value.ToString());
                }
            }
            /// <summary>
            /// Redis  备份服务器（当主服务器挂了后，请求会转向备用机），多个用逗号（,）分隔
            /// 格式：ip:port - password
            /// 配置项：Redis.ServersBak ：192.168.1.9:6379 - 888888
            /// </summary>
            public static string ServersBak
            {
                get
                {
                    return GetApp("Redis.ServersBak", string.Empty);
                }
                set
                {
                    SetApp("Redis.ServersBak", value);
                }
            }
        }

        /// <summary>
        /// MemCache 配置
        /// </summary>
        public static class MemCache
        {
            /// <summary>
            /// MemCache分布式缓存的服务器配置，多个用逗号（,）分隔
            /// 格式：ip:port
            /// 配置项：MemCache.Servers ：192.168.1.9:12121
            /// </summary>
            public static string Servers
            {
                get
                {
                    return GetApp("MemCache.Servers", string.Empty);
                }
                set
                {
                    SetApp("MemCache.Servers", value);
                }
            }

            /// <summary>
            /// MemCache 备份服务器（当主服务器挂了后，请求会转向备用机）
            /// 格式：ip:port
            /// 配置项：MemCache.ServersBak ：192.168.1.9:12121
            /// </summary>
            public static string ServersBak
            {
                get
                {
                    return GetApp("MemCache.ServersBak", string.Empty);
                }
                set
                {
                    SetApp("MemCache.ServersBak", value);
                }
            }
        }

        #endregion

        #region 自动缓存相关配置

        /// <summary>
        /// 自动缓存相关的配置
        /// </summary>
        public static class AutoCache
        {
            /// <summary>
            /// 是否启用智能自动缓存数据（默认开启）
            /// 配置项：AutoCache.IsEnable ：true
            /// </summary>
            public static bool IsEnable
            {
                get
                {
                    string value = GetApp("AutoCache.IsEnable");
                    if (!string.IsNullOrEmpty(value))
                    {
                        bool result;
                        bool.TryParse(value, out result);
                        return result;
                    }
                    return GetAppBool("IsAutoCache", true);//兼容旧配置
                }
                set
                {
                    SetApp("AutoCache.IsEnable", value.ToString());
                }
            }
            /// <summary>
            /// AutoCache开启时，可以设置仅需要缓存的Table，多个用逗号分隔（此项配置时，NoCacheTables配置则被无忽略）
            /// 配置项：AutoCache.Tables ：users,user_vip
            /// </summary>
            public static string Tables
            {
                get
                {
                    string value = GetApp("AutoCache.Tables");
                    if (!string.IsNullOrEmpty(value))
                    {
                        return value;
                    }
                    return GetApp("CacheTables", "");//兼容旧配置
                }
                set
                {
                    SetApp("AutoCache.Tables", value);
                }
            }
            /// <summary>
            /// AutoCache开启时，可以设置不缓存的Table，多个用逗号分隔
            /// 配置项：AutoCache.IngoreTables ：logs,logs_temp
            /// </summary>
            public static string IngoreTables
            {
                get
                {
                    string value = GetApp("AutoCache.IngoreTables");
                    if (!string.IsNullOrEmpty(value))
                    {
                        return value;
                    }
                    return GetApp("NoCacheTables", "");//兼容旧配置
                }
                set
                {
                    SetApp("AutoCache.IngoreTables", value);
                }
            }

            /// <summary>
            /// AutoCache开启时，可以设置不受更新影响的列名，用Json格式。
            /// 配置项：AutoCache.IngoreColumns ：{user:'updatetime,createtime',tb2:'col1,2'}
            /// </summary>
            public static string IngoreColumns
            {
                get
                {
                    string value = GetApp("AutoCache.IngoreColumns");
                    if (!string.IsNullOrEmpty(value))
                    {
                        return value;
                    }
                    return GetApp("IngoreCacheColumns", "");
                }
                set
                {
                    SetApp("AutoCache.IngoreColumns", value);
                    CYQ.Data.Cache.AutoCache.IngoreCacheColumns = null;
                }
            }
            private static string _AutoCacheConn = null;
            /// <summary>
            /// CYQ.Data.Cache 自动缓存 - 数据库链接配置
            /// 在多个不同的应用项目里操作同一个数据库时（又不想使用分布式缓存MemCache或Redis），可以开启此项，达到缓存智能清除的效果。
            /// 配置项：AutoCacheConn：server=.;database=x;uid=s;pwd=p;
            /// </summary>
            public static string Conn
            {
                get
                {
                    if (_AutoCacheConn == null)
                    {
                        _AutoCacheConn = AppConfig.GetConn("AutoCacheConn");
                    }
                    return _AutoCacheConn;
                }
                set
                {
                    _AutoCacheConn = value;
                }
            }
            /// <summary>
            /// 当AutoCacheConn开启后，定时扫描数据库的任务时间（毫秒）,默认1000
            /// 配置项：AutoCache.TaskTime：1000
            /// </summary>
            public static int TaskTime
            {
                get
                {
                    return GetAppInt("AutoCache.TaskTime", 1000);
                }
                set
                {
                    SetApp("AutoCache.TaskTime", value.ToString());
                }

            }
            /*
            /// <summary>
            ///  Cache.CacheManage 内置线程-缓存的同步时间[(默认5)分钟同步一次]
            /// </summary>
            public static int CacheClearWorkTime
            {
                get
                {
                    return GetAppInt("CacheClearWorkTime", 5);
                }
                set
                {
                    SetApp("CacheClearWorkTime", value.ToString());
                }
            }
            
            /// <summary>
            ///  Cache.CacheManage 内置线程-调用次数：[N(默认4)分钟内调用次数少于指定值(默认2)，缓存即被清除]
            /// </summary>
            public static int CacheClearCallCount
            {
                get
                {
                    return GetAppInt("CacheClearCallCount", 2);
                }
                set
                {
                    SetApp("CacheClearCallCount", value.ToString());
                }
            }
            /// <summary>
            /// Cache.CacheManage 内置线程-时间设置：[N(默认4)分钟内调用次数少于指定值(默认2)，缓存即被清除]
            /// </summary>
            public static int CacheClearTime
            {
                get
                {
                    return GetAppInt("CacheClearTime", 4);
                }
                set
                {
                    SetApp("CacheClearTime", value.ToString());
                }
            }
            */
        }

        #endregion

        #region 日志相关配置

        /// <summary>
        /// 日志类Log 相关的配置
        /// </summary>
        public static class Log
        {
            /// <summary>
            /// 是否写数据库异常日志（默认true）:开启时：有异常不抛出，转写入数据库；不开启：有异常会抛出
            /// 配置项：Log.IsEnable ：true
            /// </summary>
            public static bool IsEnable
            {
                get
                {
                    string value = GetApp("Log.IsEnable");
                    if (!string.IsNullOrEmpty(value))
                    {
                        bool result;
                        bool.TryParse(value, out result);
                        return result;
                    }
                    return GetAppBool("IsWriteLog", true);//兼容旧配置
                }
                set
                {
                    SetApp("Log.IsEnable", value.ToString());
                }
            }
            /// <summary>
            /// CYQ.Data.Log 类记录数据库异常日志 - 数据库链接配置
            /// 配置项：LogConn：server=.;database=x;uid=s;pwd=p;
            /// </summary>
            public static string Conn
            {
                get
                {
                    return GetConn("LogConn");
                }
                set
                {
                    SetConn("LogConn", value);
                }
            }
            /// <summary>
            /// 文本日志的配置相对路径（Web 默认为：/App_Data/log"）
            /// 配置项：Log.Path ：/App_Data/log
            /// </summary>
            public static string Path
            {
                get
                {
                    return GetApp("Log.Path", IsWeb ? "/App_Data/log" : "/Logs");
                }
                set
                {
                    SetApp("Log.Path", value);
                }
            }



            /// <summary>
            /// 异常日志表名（默认为SysLogs，可配置）
            /// 配置项：Log.TableName ：SysLogs
            /// </summary>
            public static string TableName
            {
                get
                {
                    return GetApp("Log.TableName", "SysLogs");
                }
                set
                {
                    SetApp("Log.TableName", value);
                }
            }
        }
        #endregion

        #region 调试类相关的配置

        /// <summary>
        /// 调试类AppDebug 相关的配置
        /// </summary>
        public class Debug
        {
            #region 配置文件的其它属性

            /// <summary>
            /// 开启信息调试记录：开启后MAction.DebugInfo可输出执行日志。
            /// 同时AppDebug若要使用，也需要开启此项。
            /// 配置项：Debug.IsEnable ：false
            /// </summary>
            public static bool IsEnable
            {
                get
                {
                    string value = GetApp("Debug.IsEnable");
                    if (!string.IsNullOrEmpty(value))
                    {
                        bool result;
                        bool.TryParse(value, out result);
                        return result;
                    }
                    return GetAppBool("OpenDebugInfo", false);//兼容旧配置
                }
                set
                {
                    SetApp("Debug.IsEnable", value.ToString());
                }
            }
            #endregion
        }
        #endregion

        #region UI 相关的配置
        /// <summary>
        /// 有UI界面的Web、Winform、WPF。
        /// </summary>
        public static class UI
        {
            /// <summary>
            /// UI取值的默认前缀（ddl,chb,txt)，多个用逗号（,）分隔
            /// 配置项：UI.AutoPrefixs ：txt,chb,ddl
            /// </summary>
            public static string AutoPrefixs
            {
                get
                {
                    return GetApp("UI.AutoPrefixs", "txt,chb,ddl");
                }
                set
                {
                    SetApp("UI.AutoPrefixs", value);
                }
            }
        }
        #endregion

    }
}
