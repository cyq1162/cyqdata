using System;
using System.Configuration;
using CYQ.Data.SQL;
using CYQ.Data.Tool;

namespace CYQ.Data
{
    /// <summary>
    /// 常用配置文件项[Web(App).Config]的[appSettings|connectionStrings]项和属性名一致。
    /// 除了可以从配置文件配置，也可以直接赋值。
    /// </summary>
    public static class AppConfig
    {
        #region 基方法
        private static MDictionary<string, string> configs = new MDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// 设置Web.config或App.config的值。
        /// </summary>
        public static void SetApp(string key, string value)
        {
            try
            {
                if (configs.ContainsKey(key))
                {
                    configs[key] = value;
                }
                else
                {
                    configs.Add(key, value);
                }
            }
            catch (Exception err)
            {
                CYQ.Data.Log.WriteLogToTxt(err);
            }
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
            if (configs.ContainsKey(key))
            {
                return configs[key];
            }
            else
            {
                string value = ConfigurationManager.AppSettings[key];
                value = string.IsNullOrEmpty(value) ? defaultValue : value;
                try
                {
                    configs.Add(key, value);
                }
                catch
                {

                }

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
        /// 获取Web.config或App.config的connectionStrings节点的值。
        /// </summary>
        public static string GetConn(string key, out string providerName)
        {
            providerName = string.Empty;
            if (string.IsNullOrEmpty(key))
            {
                key = "Conn";
            }
            if (key.Trim().Contains(" "))
            {
                return key;
            }
            ConnectionStringSettings conn = ConfigurationManager.ConnectionStrings[key];
            if (conn != null)
            {
                providerName = conn.ProviderName;
                if (key == conn.ConnectionString)//避免误写自己造成死循环。
                {
                    return key;
                }
                key = conn.ConnectionString;
                if (!string.IsNullOrEmpty(key) && key.Length < 32 && key.Split(' ').Length == 1)
                {
                    return GetConn(key);
                }
                return conn.ConnectionString;
            }
            if (key.Length > 32 && key.Split('=').Length > 3 && key.Contains(";")) //链接字符串很长，没空格的情况
            {
                return key;
            }
            return "";
        }

        /// <summary>
        /// 获取Web.config或App.config的connectionStrings节点的值。
        /// </summary>
        public static string GetConn(string key)
        {
            string p;
            return GetConn(key, out p);
        }

        #endregion

        #region Xml相关配置
        /// <summary>
        /// XHtml 相关的配置
        /// </summary>
        public static class XHtml
        {
            /// <summary>
            /// Xml.XHtmlHelper 中使用的 "&lt;![CDATA[" 项
            /// </summary>
            public static string CDataLeft
            {
                get
                {
                    return GetApp("CDataLeft", "<![CDATA[MMS::");
                }
                set
                {
                    SetApp("CDataLeft", value);
                }
            }
            /// <summary>
            /// Xml.XHtmlHelper 中使用的 "]]&gt;" 项
            /// </summary>
            public static string CDataRight
            {
                get
                {
                    return GetApp("CDataRight", "::MMS]]>");
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
                    string dtdUri = GetApp("DtdUri", "/Setting/DTD/xhtml1-transitional.dtd");
                    if (dtdUri != null && dtdUri.IndexOf("http://") == -1)//相对路径
                    {
                        dtdUri = AppDomain.CurrentDomain.BaseDirectory + dtdUri.TrimStart('/');
                    }
                    return dtdUri;
                }
                set
                {
                    SetApp("DtdUri", value);
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
                        string[] domains = GetApp("Domain", "").Split(',');
                        if (domains != null && domains.Length > 0)
                        {
                            if (domains.Length == 1)
                            {
                                _Domain = domains[0];
                            }
                            else if (System.Web.HttpContext.Current != null)
                            {
                                foreach (string domain in domains)
                                {
                                    if (System.Web.HttpContext.Current.Request.Url.Authority.Contains(domain))
                                    {
                                        _Domain = domain;
                                    }
                                }
                            }
                        }
                        else if (System.Web.HttpContext.Current != null)
                        {
                            _Domain = System.Web.HttpContext.Current.Request.Url.Authority.Replace("www.", string.Empty);
                        }
                    }
                    return _Domain;
                }
                set
                {
                    SetApp("Domain", value);
                }
            }
            private static int _UserFileLoadXml = -1;
            /// <summary>
            /// Xml.XHtmlHelper 操作Html时，配置此项使用File加载Xml[便可在IIS7以上非信任主机机制下使用]
            /// </summary>
            public static bool UseFileLoadXml
            {
                get
                {
                    if (_UserFileLoadXml == -1)
                    {
                        _UserFileLoadXml = GetApp("UseFileLoadXml") == "true" ? 1 : 0;
                    }
                    return _UserFileLoadXml == 1;
                }
                set
                {
                    _UserFileLoadXml = value ? 1 : 0;
                }
            }

            /// <summary>
            /// Xml.MutilLanguage 类的默认语言Key，默认值：Chinese
            /// </summary>
            public static string SysLangKey
            {
                get
                {
                    return GetApp("SysLangKey", "Chinese");
                }
                set
                {
                    SetApp("SysLangKey", value);
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
                    _DefaultConn = value;

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
            private static string _DefaultDataBase;
            /// <summary>
            /// 默认数据库名称（只读）
            /// </summary>
            internal static string DefaultDataBase
            {
                get
                {
                    if (string.IsNullOrEmpty(_DefaultDataBase))
                    {
                        SetDefault();

                    }
                    return _DefaultDataBase;
                }
            }
            private static DalType _DefaultDalType = DalType.None;
            /// <summary>
            /// 默认数据库类型（只读）
            /// </summary>
            internal static DalType DefaultDalType
            {
                get
                {
                    if (_DefaultDalType == DalType.None)
                    {
                        SetDefault();
                    }
                    return _DefaultDalType;
                }
            }
            private static void SetDefault()
            {
                DbBase db = DalCreate.CreateDal(DefaultConn);
                if (db != null)
                {
                    _DefaultDataBase = db.DataBase;
                    _DefaultDalType = db.dalType;
                    db.Dispose();
                }
            }
            /// <summary>
            /// MSSQL是否启用分页存储过程SelectBase，默认false
            /// </summary>
            public static bool PagerBySelectBase
            {
                get
                {
                    bool _PagerBySelectBase;
                    bool.TryParse(GetApp("PagerBySelectBase", "false"), out _PagerBySelectBase);
                    return _PagerBySelectBase;
                }
                set
                {
                    SetApp("PagerBySelectBase", value.ToString());
                }
            }
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
            /// MAction所有操作中的where条件，默认有超强的过滤单词，来过滤Sql注入关键字，如果语句包含指定的过滤词，则会返回错误信息，并记录日志。
            /// 如果需要自定义关键字，可配置此项，如：“delete;from,truncate，其它单词”，分号表词组，需要同时包含两个词； 多个过滤词组以","逗号分隔
            /// </summary>
            public static string FilterSqlInjection
            {
                get
                {
                    return GetApp("FilterSqlInjection", SqlInjection.filterSqlInjection);
                }
                set
                {
                    SetApp("FilterSqlInjection", value);
                    if (!string.IsNullOrEmpty(value))
                    {
                        SqlInjection.FilterKeyList = value.TrimEnd(',').Split(',');
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
            /// MAction 操作 Oracle 时自增加int类型ID所需要配置的序列ID，Guid为ID则不用。
            /// 如果需要为每个表都配置一个序列号，可以使用：SEQ_{0} 其中{0}会自动配对成表名，如果没有{0}，则为整个数据库共用一个序列。
            /// 默认参数值：SEQ_{0}
            /// </summary>
            public static string AutoID
            {
                get
                {
                    return GetApp("AutoID", "SEQ_{0}");
                }
                set
                {
                    SetApp("AutoID", value);
                }
            }
            /// <summary>
            /// MAction 可将表架构映射到外部指定相对路径[外部存储,可避开数据库读取]
            /// </summary>
            public static string SchemaMapPath
            {
                get
                {
                    string path = GetApp("SchemaMapPath", string.Empty);
                    if (!string.IsNullOrEmpty(path) && !path.EndsWith("\\"))
                    {
                        path = path.TrimEnd('/') + "\\";
                    }
                    return path;
                }
                set
                {
                    SetApp("SchemaMapPath", value);
                }
            }
            /// <summary>
            /// 删除字段名称（若表存在此设置的字段名称时，MActon的删除操作将变更变为更新操作）
            /// </summary>
            public static string DeleteField
            {
                get
                {
                    return GetApp("DeleteField", string.Empty);
                }
                set
                {
                    SetApp("DeleteField", value);
                }
            }
            /// <summary>
            /// 更新时间字段名称（若表存在指定字段名称时，自动更新时间，多个用逗号分隔）
            /// </summary>
            public static string EditTimeFields
            {
                get
                {
                    return GetApp("EditTimeFields", string.Empty);
                }
                set
                {
                    SetApp("EditTimeFields", value);
                }
            }
            /// <summary>
            ///系统全局要隐藏的字段名称（默认值为："cyqrownum,rowguid,deletefield"）
            /// </summary>
            public static string HiddenFields
            {
                get
                {
                    string result = GetApp("HiddenFields", string.Empty);
                    if (result == string.Empty)
                    {
                        result = "cyqrownum,rowguid," + DeleteField;
                    }
                    return result;
                }
                set
                {
                    SetApp("HiddenFields", value);
                }
            }

            /// <summary>
            /// 全局的数据库命令默认超时设置，默认值120秒（单位：秒）
            /// </summary>
            public static int CommandTimeout
            {
                get
                {
                    return GetAppInt("CommandTimeout", 120);
                }
                set
                {
                    SetApp("CommandTimeout", value.ToString());
                }
            }
            /// <summary>
            /// 读写分离时用户对主数据库操作持续时间，默认值10秒（单位：秒）
            /// </summary>
            public static int MasterSlaveTime
            {
                get
                {
                    return GetAppInt("MasterSlaveTime", 10);
                }
                set
                {
                    SetApp("MasterSlaveTime", value.ToString());
                }
            }
        }
        #endregion


        #region 缓存相关配置
        /// <summary>
        /// 缓存相关的配置
        /// </summary>
        public static class Cache
        {
            /// <summary>
            /// 分布式缓存的服务器配置，多个用逗号（,）分隔
            /// </summary>
            public static string MemCacheServers
            {
                get
                {
                    return GetApp("MemCacheServers", string.Empty);
                }
                set
                {
                    SetApp("MemCacheServers", value);
                }
            }
            /// <summary>
            /// Cache.CacheManage 调用GC.Collect()方法的间隔时间[(默认180)分钟]
            /// </summary>
            //public static int GCCollectTime
            //{
            //    get
            //    {
            //        return GetAppInt("GCCollectTime", 180);
            //    }
            //    set
            //    {
            //        SetApp("GCCollectTime", value.ToString());
            //    }
            //}

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

            private static int _IsAutoCache = -1;
            /// <summary>
            /// 是否智能缓存数据（默认开启）
            /// </summary>
            public static bool IsAutoCache
            {
                get
                {
                    if (_IsAutoCache == -1)
                    {
                        _IsAutoCache = AppConfig.GetApp("IsAutoCache").ToLower() == "false" ? 0 : 1;//默认开启

                    }
                    return _IsAutoCache == 1;
                }
                set
                {
                    _IsAutoCache = value ? 1 : 0;
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
            private static string _LogConn = null;
            /// <summary>
            /// CYQ.Data.Log 类记录数据库异常日志 - 数据库链接配置
            /// </summary>
            public static string LogConn
            {
                get
                {
                    if (_LogConn == null)
                    {
                        _LogConn = AppConfig.GetConn("LogConn");
                    }
                    return _LogConn;
                }
                set
                {
                    _LogConn = value;
                }
            }
            private static string _LogPath;
            /// <summary>
            /// 文本日志的配置相对路径（默认为：Logs\\"）
            /// </summary>
            public static string LogPath
            {
                get
                {
                    if (string.IsNullOrEmpty(_LogPath))
                    {
                        _LogPath = AppConfig.GetApp("LogPath", "Logs\\");
                        if (!_LogPath.EndsWith("\\"))
                        {
                            _LogPath = _LogPath.TrimEnd('/') + "\\";
                        }
                    }
                    return _LogPath;
                }
                set
                {
                    _LogPath = value;
                    if (!_LogPath.EndsWith("\\"))
                    {
                        _LogPath = _LogPath.TrimEnd('/') + "\\";
                    }
                }
            }
            private static int _IsWriteLog = -1;
            /// <summary>
            /// 是否写数据库异常日志:开启时：有异常不抛出，转写入数据库；不开启：有异常会抛出
            /// </summary>
            public static bool IsWriteLog
            {
                get
                {
                    if (_IsWriteLog == -1)
                    {
                        _IsWriteLog = AppConfig.GetApp("IsWriteLog").ToLower() == "true" ? 1 : 0;

                    }
                    return _IsWriteLog == 1;
                }
                set
                {
                    _IsWriteLog = value ? 1 : 0;
                }
            }
            private static string _LogTableName;
            /// <summary>
            /// 异常日志表名（默认为SysLogs，可配置）
            /// </summary>
            public static string LogTableName
            {
                get
                {
                    if (string.IsNullOrEmpty(_LogTableName))
                    {
                        _LogTableName = AppConfig.GetApp("LogTableName", "SysLogs");
                    }
                    return _LogTableName;
                }
                set
                {
                    _LogTableName = value;
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
            private static int _SqlFilter = -2;
            /// <summary>
            ///毫秒数（这个是在对所有SQL语句的：将所有长时间(ms)的SQL语句写入日志，对应配置项LogPath的路径）
            /// </summary>
            public static int SqlFilter
            {
                get
                {
                    if (_SqlFilter == -2)
                    {
                        _SqlFilter = AppConfig.GetAppInt("SqlFilter", -1);
                    }
                    return _SqlFilter;
                }
                set
                {
                    _SqlFilter = value;
                }
            }
            private static int _InfoFilter = -1;
            /// <summary>
            /// 毫秒数（这个是在AppDebug开启后的：可通过此项设置条件过滤出时间(ms)较长的SQL语句）
            /// </summary>
            public static int InfoFilter
            {
                get
                {
                    if (_InfoFilter == -1)
                    {
                        _InfoFilter = AppConfig.GetAppInt("InfoFilter", 0);
                    }
                    return _InfoFilter;
                }
                set
                {
                    _InfoFilter = value;
                }
            }
            private static int _OpenDebugInfo = -1;
            /// <summary>
            /// 开启信息调试记录：开启后MAction.DebugInfo可输出执行日志。
            /// 同时AppDebug若要使用，也需要开启此项。
            /// </summary>
            public static bool OpenDebugInfo
            {
                get
                {
                    if (_OpenDebugInfo == -1)
                    {
                        _OpenDebugInfo = AppConfig.GetApp("OpenDebugInfo").ToLower() == "true" ? 1 : 0;
                    }
                    return _OpenDebugInfo == 1;
                }
                set
                {
                    _OpenDebugInfo = value ? 1 : 0;
                }
            }
            #endregion
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
                bool _IsEnumToInt;
                bool.TryParse(GetApp("IsEnumToInt", "false"), out _IsEnumToInt);
                return _IsEnumToInt;
            }
            set
            {
                SetApp("IsEnumToInt", value.ToString());
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
        /// 生成的实体类的后缀。
        /// </summary>
        public static string EntitySuffix
        {
            get
            {
                return GetApp("EntitySuffix", "Bean");
            }
            set
            {
                SetApp("EntitySuffix", value);
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
    }
}
