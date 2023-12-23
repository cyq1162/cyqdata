using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data.SqlClient;
using CYQ.Data.Tool;

namespace CYQ.Data
{
    /// <summary>
    /// 数据库对象基础实例
    /// </summary>
    internal partial class ConnBean
    {
        private ConnBean()
        {

        }
        /// <summary>
        /// 对应的ConnectionString的Name
        /// 或者最原始传进来的链接
        /// </summary>
        public string ConnName = string.Empty;
        /// <summary>
        /// 链接的状态是否正常。
        /// </summary>
        public bool IsOK = true;
        /// <summary>
        /// 是否从库
        /// </summary>
        public bool IsSlave = false;
        /// <summary>
        /// 是否备用库
        /// </summary>
        public bool IsBackup = false;
        /// <summary>
        /// 链接错误时的异常消息。
        /// </summary>
        internal string ErrorMsg = string.Empty;
        /// <summary>
        /// 经过格式化后的数据库链接字符串
        /// </summary>
        public string ConnString = string.Empty;
        /// <summary>
        /// 数据库类型
        /// </summary>
        public DataBaseType ConnDataBaseType;
        /// <summary>
        /// 数据库版本信息
        /// </summary>
        public string Version;
        public ConnBean Clone()
        {
            ConnBean cb = new ConnBean();
            cb.ConnName = this.ConnName;
            cb.ConnString = this.ConnString;
            cb.ConnDataBaseType = this.ConnDataBaseType;
            cb.IsOK = this.IsOK;
            return cb;
        }
        public bool TryTestConn()
        {
            //err = string.Empty;
            if (!string.IsNullOrEmpty(ConnName) && AppConfig.GetAppBool("Conn.Result", true))
            {
                DalBase helper = DalCreate.CreateDal(ConnName);
                try
                {

                    helper.Con.Open();
                    Version = helper.Con.ServerVersion;
                    if (string.IsNullOrEmpty(Version))
                    {
                        Version = helper.DataBaseType.ToString();
                    }
                    helper.Con.Close();
                    IsOK = true;
                    ErrorMsg = string.Empty;
                }
                catch (Exception er)
                {
                    ErrorMsg = er.Message;
                    IsOK = false;
                }
                finally
                {
                    helper.Dispose();
                }
            }
            else
            {
                IsOK = false;
            }
            return IsOK;
        }
        public string GetHashKey()
        {
            return StaticTool.GetHashKey(ConnString.Replace(" ", "").ToLower());
        }
    }
    internal partial class ConnBean
    {
        /// <summary>
        /// 所有链接的对象集合
        /// </summary>
        private static MDictionary<int, ConnBean> connBeanDicCache = new MDictionary<int, ConnBean>();
        public static void Clear()
        {
            connBeanDicCache.Clear();
        }
        public static void Remove(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                int hash = key.GetHashCode();

                ConnBean cb = connBeanDicCache[hash];
                if (cb != null)
                {
                    connBeanDicCache.Remove(hash);
                    connBeanDicCache.Remove(cb.ConnName.GetHashCode());
                    connBeanDicCache.Remove(cb.ConnString.GetHashCode());
                }
                else
                {
                    connBeanDicCache.Remove(hash);
                    string connString = string.Format(AppConfig.GetConn(key), AppConfig.WebRootPath);
                    connBeanDicCache.Remove(connString.GetHashCode());
                }
            }
        }
        public static string GetHashKey(string conn)
        {
            ConnBean co = Create(conn);
            if (co == null)
            {
                return StaticTool.GetHashKey(conn);
            }
            return co.GetHashKey();
        }
        static readonly object o = new object();
        /// <summary>
        /// 创建一个实例。
        /// </summary>
        /// <returns></returns>
        public static ConnBean Create(string connNameOrString)
        {
            string connString = string.Format(AppConfig.GetConn(connNameOrString), AppConfig.WebRootPath);
            if (string.IsNullOrEmpty(connString))
            {
                return null;
            }
            //检测缓存中有木有
            int hash = connString.GetHashCode();
            if (connBeanDicCache.ContainsKey(hash))
            {
                ConnBean cbCache = connBeanDicCache[hash];
                if (cbCache != null)
                {
                    return cbCache;
                }
            }
            ConnBean cb = new ConnBean();
            cb.ConnName = connNameOrString;
            cb.ConnDataBaseType = GetDataBaseType(connString);
            cb.ConnString = RemoveConnProvider(cb.ConnDataBaseType, connString);
            lock (o)
            {
                if (!connBeanDicCache.ContainsKey(hash))
                {
                    connBeanDicCache.Set(hash, cb);//设置缓存
                }
                int hash2 = cb.ConnString.GetHashCode();
                if (hash != hash2 && !connBeanDicCache.ContainsKey(hash2))
                {
                    connBeanDicCache.Set(hash2, cb);//存两份，以connName,connString为key
                }
            }
            return cb;
        }
        /// <summary>
        /// 去掉 链接中的 provider=xxxx;
        /// </summary>
        public static string RemoveConnProvider(DataBaseType dal, string connString)
        {
            switch (dal)
            {
                case DataBaseType.Access:
                case DataBaseType.Excel:
                case DataBaseType.FoxPro:
                    return connString;
            }

            string conn = connString.ToLower();
            int index = conn.IndexOf("provider");
            if (index > -1 && index < connString.Length - 5 && (connString[index + 8] == '=' || connString[index + 9] == '='))
            {
                int end = conn.IndexOf(';', index);
                if (end == -1)
                {
                    connString = connString.Remove(index);
                }
                else
                {
                    connString = connString.Remove(index, end - index + 1);
                }
            }

            return connString;
        }
        public static DataBaseType GetDataBaseType(string connString)
        {
            connString = connString.ToLower().Replace(" ", "");//去掉空格

            #region provider识别
            if (connString.Contains("provider"))
            {
                if (connString.Contains("provider=mssql"))
                {
                    return DataBaseType.MsSql;
                }
                if (connString.Contains("provider=ase") || connString.Contains("provider=sybase"))
                {
                    //data source=127.0.0.1;port=5000;database=cyqdata;uid=sa;pwd=123456
                    return DataBaseType.Sybase;
                }
                if (connString.Contains("provider=pg") || connString.Contains("provider=postgre") || connString.Contains("provider=postgresql") || connString.Contains("provider=npgsql"))
                {
                    ////server=.;port=5432;database=xx;uid=xx;pwd=xx;
                    return DataBaseType.PostgreSQL;
                }
                if (connString.Contains("provider=mysql"))
                {
                    //host=localhost;port=3306;database=mysql;uid=root;pwd=123456;Convert Zero Datetime=True;
                    return DataBaseType.MySql;
                }
                if (connString.Contains("provider=db2"))
                {
                    return DataBaseType.DB2;
                }
                if (connString.Contains("provider=oracle") || connString.Contains("provider=msdaora") || connString.Contains("provider=oraoledb.oracle"))
                {
                    //Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT = 1521)))(CONNECT_DATA =(Sid = orcl)));User id=sa;password=123456
                    return DataBaseType.Oracle;
                }
                if (connString.Contains("provider=firebird"))
                {
                    //User=SYSDBA;Password=masterkey;Database=<database file path>;DataSource=<hostname or IP address>;Port=<port number>;Dialect=3;Charset=UTF8;ServerType=0;
                    //user id = SYSDBA; password = 123456; database = d:\\test.fdb; server type = Default; data source = 127.0.0.1; port number = 3050
                    return DataBaseType.FireBird;
                }
                if (connString.Contains("provider=dm") || connString.Contains("provider=dameng"))
                {
                    //user id = SYSDBA; password = 123456789; data source = 127.0.0.1; database = dmhr;
                    return DataBaseType.DaMeng;
                }
            }
            #endregion

            #region 默认端口识别
            if (connString.Contains("port"))
            {
                if (connString.Contains("=1433"))
                {
                    return DataBaseType.MsSql;
                }
                if (connString.Contains("=5000"))
                {
                    //data source=127.0.0.1;port=5000;database=cyqdata;uid=sa;pwd=123456
                    return DataBaseType.Sybase;
                }
                if (connString.Contains("=5432"))
                {
                    ////server=.;port=5432;database=xx;uid=xx;pwd=xx;
                    return DataBaseType.PostgreSQL;
                }
                if (connString.Contains("=3306"))
                {
                    //host=localhost;port=3306;database=mysql;uid=root;pwd=123456;Convert Zero Datetime=True;
                    return DataBaseType.MySql;
                }
                if (connString.Contains("=50000"))
                {
                    return DataBaseType.DB2;
                }
                if (connString.Contains("=1521"))
                {
                    //Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT = 1521)))(CONNECT_DATA =(Sid = orcl)));User id=sa;password=123456
                    return DataBaseType.Oracle;
                }
                if (connString.Contains("=3050"))
                {
                    //User=SYSDBA;Password=masterkey;Database=<database file path>;DataSource=<hostname or IP address>;Port=<port number>;Dialect=3;Charset=UTF8;ServerType=0;
                    //user id = SYSDBA; password = 123456; database = d:\\test.fdb; server type = Default; data source = 127.0.0.1; port number = 3050
                    return DataBaseType.FireBird;
                }
                if (connString.Contains("=5236"))
                {
                    //user id = SYSDBA; password = 123456789; data source = 127.0.0.1; database = dmhr;
                    return DataBaseType.DaMeng;
                }
            }
            #endregion

            #region 文件后缀识别
            if (connString.Contains(".xls") || connString.Contains(".xlsx"))
            {
                //"Provider=Microsoft.ACE.OLEDB.12.0; Data Source=D:\\xxx.xls;Extended Properties='Excel 12.0;HDR=Yes;'";
                return DataBaseType.Excel;
            }
            if (connString.Contains(".mdb") || connString.Contains(".accdb") || connString.Contains(".oledb."))
            {
                //Provider=Microsoft.Jet.OLEDB.4.0; Data Source={0}App_Data/demo.mdb
                //Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0}App_Data/demo.accdb
                return DataBaseType.Access;
            }
            if (connString.Contains("vfpoledb.1") || connString.Contains(".dbf"))
            {
                //"Provider=VFPOLEDB.1;Data Source=F:\\10443.dbf";
                return DataBaseType.FoxPro;
            }
            if (connString.Contains(".db;") || connString.Contains(".db3;") || connString.Contains("failifmissing"))
            {
                //Data Source={0}App_Data/demo.db;failifmissing=false
                return DataBaseType.SQLite;
            }
            if (connString.Contains(".gdb;") || connString.Contains(".fdb;"))
            {
                return DataBaseType.FireBird;
            }
            #endregion


            #region 关键字识别
            if (connString.Contains("txtpath="))
            {
                // txt path={0}
                return DataBaseType.Txt;
            }
            if (connString.Contains("xmlpath="))
            {
                // xml path={0}
                return DataBaseType.Xml;
            }
            if (connString.Contains("description=") || connString.Contains("fororacle"))
            {
                //Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT = 1521)))(CONNECT_DATA =(Sid = orcl)));User id=sa;password=123456
                return DataBaseType.Oracle;
            }
            if (connString.Contains("hostname=") || connString.Contains("ibmdadb2"))
            {
                //Provider=IBMDADB2.IBMDBCL1;Data Source=数据库名;Persist Security Info=True;User ID=用户名;pwd=密码;Location=服务器IP地址
                return DataBaseType.DB2;
            }
            if (connString.Contains("server=") && !connString.Contains("port="))
            {
                //server=.;database=xx;uid=xx;pwd=xx;
                return DataBaseType.MsSql;
            }
            #endregion

            #region dll文件识别

            if (connString.Contains("datasource") && (Exists("Sybase.AdoNet2.AseClient.dll") || Exists("Sybase.AdoNet4.AseClient.dll")))
            {
                return DataBaseType.Sybase;
            }
            if (connString.TrimEnd(';').Split(';').Length == 3 && (Exists("IBM.Data.DB2.dll") || Exists("IBM.Data.DB2.Core.dll")))
            {
                //database=xxx;uid=xxx;pwd=xxx;
                return DataBaseType.DB2;
            }
            if (connString.Contains("host=") && Exists("MySql.Data.dll"))
            {
                return DataBaseType.MySql;
            }
            //postgre和mssql的链接语句一样，为postgre
            if (Exists("Npgsql.dll"))
            {
                return DataBaseType.PostgreSQL;
            }
            if (Exists("DmProvider.dll"))
            {
                return DataBaseType.DaMeng;
            }
            #endregion

            return DataBaseType.MsSql;

        }
        private static bool Exists(string dll)
        {
            return File.Exists(AppConfig.AssemblyPath + dll);
        }
    }
}
