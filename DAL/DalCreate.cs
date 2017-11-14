using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Tool;
using System.Threading;


namespace CYQ.Data
{
    /// <summary>
    /// 数据库类型操作类
    /// </summary>
    internal class DalCreate
    {
        private const string SqlClient = "System.Data.SqlClient";
        private const string OleDb = "System.Data.OleDb";
        private const string OracleClient = "System.Data.OracleClient";
        private const string SQLiteClient = "System.Data.SQLite";
        private const string MySqlClient = "MySql.Data.MySqlClient";
        private const string SybaseClient = "Sybase.Data.AseClient";
        private const string TxtClient = "CYQ.Data.TxtClient";
        private const string XmlClient = "CYQ.Data.XmlClient";
        private const string XHtmlClient = "CYQ.Data.XHtmlClient";

        /// <summary>
        /// 简单工厂（Factory Method）
        /// </summary>
        /// <param name="dbConn"></param>
        /// <returns></returns>
        public static DbBase CreateDal(string dbConn)
        {
            return GetDbBaseBy(GetConnObject(dbConn));
            //ConnEntity cEntity = GetConnString(dbConn);
            //DbBase db = GetDbBaseBy(cEntity.Conn, cEntity.ProviderName);
            //db.connObject = cEntity;
            //return db;

        }

        public static DalType GetDalTypeByConn(string conn)
        {
            return GetConnBean(conn).ConnDalType;
        }
        public static DalType GetDalTypeByReaderName(string typeName)
        {
            switch (typeName.Replace("DataReader", "").ToLower())
            {
                case "oracle":
                    return DalType.Oracle;
                case "sql":
                    return DalType.MsSql;
                case "sqlite":
                    return DalType.SQLite;
                case "oledb":
                    return DalType.Access;
                case "mysql":
                    return DalType.MySql;
                case "odbc":
                case "ase":
                    return DalType.Sybase;
                default:
                    return DalType.None;

            }
        }
        public static string GetProvider(string connString)
        {
            connString = connString.ToLower().Replace(" ", "");//去掉空格
            if (connString.Contains("initial catalog="))
            {
                return SqlClient;
            }
            else if (connString.Contains("microsoft.jet.oledb.4.0") || connString.Contains("microsoft.ace.oledb") || connString.Contains(".mdb"))
            {
                return OleDb;
            }
            else if (connString.Contains("provider=msdaora") || connString.Contains("provider=oraoledb.oracle")
                || connString.Contains("description=") || connString.Contains("fororacle"))
            {
                return OracleClient;
            }
            else if (connString.Contains("failifmissing=") || (connString.StartsWith("datasource=") && connString.EndsWith(".db")))
            {
                return SQLiteClient;
            }
            else if (connString.Contains("convert zero datetime") || (connString.Contains("host=") && connString.Contains("port=") && connString.Contains("database=")))
            {
                return MySqlClient;
            }
            else if (connString.Contains("provider=ase") || (connString.Contains("datasource=") && connString.Contains("port=") && connString.Contains("database=")))
            {
                return SybaseClient;
            }
            else if (connString.Contains("txtpath="))
            {
                return TxtClient;
            }
            else if (connString.Contains("xmlpath="))
            {
                return XmlClient;
            }
            else
            {
                return SqlClient;
            }
        }
        public static DalType GetDalType(string providerName)
        {
            switch (providerName)
            {
                case SqlClient:
                    return DalType.MsSql;
                case OleDb:
                    return DalType.Access;
                case OracleClient:
                    return DalType.Oracle;
                case SQLiteClient:
                    return DalType.SQLite;
                case MySqlClient:
                    return DalType.MySql;
                case SybaseClient:
                    return DalType.Sybase;
                case TxtClient:
                    return DalType.Txt;
                case XmlClient:
                    return DalType.Xml;
            }
            return (DalType)Error.Throw(string.Format("GetDalType:{0} No Be Support Now!", providerName));
        }

        private static DbBase GetDbBaseBy(ConnObject co)
        {
            string providerName = co.Master.ProviderName;
            //License.Check(providerName);//框架模块授权检测。
            switch (providerName)
            {
                case SqlClient:
                    return new MsSqlDal(co);
                case OleDb:
                    return new OleDbDal(co);
                case OracleClient:
                    return new OracleDal(co);
                case SQLiteClient:
                    return new SQLiteDal(co);
                case MySqlClient:
                    return new MySQLDal(co);
                case SybaseClient:
                    return new SybaseDal(co);
                case TxtClient:
                case XmlClient:
                    return new NoSqlDal(co);
            }
            return (DbBase)Error.Throw(string.Format("GetHelper:{0} No Be Support Now!", providerName));
        }

        #region 注释的代码
        /*
          private static MDictionary<string, ConnEntity> connCache = new MDictionary<string, ConnEntity>();


        internal static ConnEntity GetConnString(string dbConn) // 思考备份链接的问题。=》下一步思考读写分离的问题。
        {
            if (connCache.ContainsKey(dbConn))
            {
                return connCache[dbConn];
            }

            ConnEntity cEntity = new ConnEntity();

            cEntity.Conn = string.IsNullOrEmpty(dbConn) ? AppConfig.DB.DefaultConn : dbConn;

            if (cEntity.Conn.Length < 32 && cEntity.Conn.Split(' ').Length == 1)//配置项
            {
                if (ConfigurationManager.ConnectionStrings[cEntity.Conn] == null && cEntity.Conn != AppConfig.DB.DefaultConn)
                {
                    cEntity.Conn = AppConfig.DB.DefaultConn;//转取默认配置；
                    if (cEntity.Conn.Length >= 32 || cEntity.Conn.Trim().Contains(" "))
                    {
                        goto er;
                    }
                }
                if (ConfigurationManager.ConnectionStrings[cEntity.Conn] != null)
                {
                    string p = string.Empty, c = string.Empty;
                    #region 获取备份链接
                    string bakKey = cEntity.Conn + "_Bak";
                    if (ConfigurationManager.ConnectionStrings[cEntity.Conn + "_Bak"] == null && cEntity.Conn == AppConfig.DB.DefaultConn && AppConfig.DB.DefaultConnBak != cEntity.Conn + "_Bak")
                    {
                        bakKey = AppConfig.DB.DefaultConnBak;
                    }
                    string connBak = AppConfig.GetConn(bakKey, out p);
                    if (connBak.Length >= 32 || connBak.Trim().Contains(" "))//如果有完整的数据库链接
                    {
                        cEntity.ConnBak = AppConfig.GetConn(bakKey, out p);
                        cEntity.ProviderNameBak = p;
                    }
                    #endregion

                    cEntity.Conn = AppConfig.GetConn(cEntity.Conn, out p);
                    cEntity.ProviderName = p;
                }
                else
                {
                    Error.Throw(string.Format("Can't find the connection key '{0}' from web.config!", cEntity.Conn));
                }
            }
            else if (cEntity.Conn == AppConfig.DB.DefaultConn && string.IsNullOrEmpty(cEntity.ConnBak))
            {
                string connBak = AppConfig.GetConn(AppConfig.DB.DefaultConnBak);//先赋默认备份值。
                if (connBak.Length >= 32 || connBak.Trim().Contains(" "))//如果有完整的数据库链接
                {
                    cEntity.ConnBak = connBak;
                }
            }
        er:
            if (string.IsNullOrEmpty(cEntity.ProviderName))
            {
                cEntity.ProviderName = GetProvider(cEntity.Conn);
                cEntity.ConnDalType = GetDalType(cEntity.ProviderName);
            }

            if (string.IsNullOrEmpty(cEntity.ProviderNameBak) && !string.IsNullOrEmpty(cEntity.ConnBak))
            {
                cEntity.ProviderNameBak = DalCreate.GetProvider(cEntity.ConnBak);
                cEntity.ConnBakDalType = GetDalType(cEntity.ProviderNameBak);
            }
            cEntity.Conn = string.Format(cEntity.Conn, AppConfig.WebRootPath);
            cEntity.ConnBak = string.Format(cEntity.ConnBak ?? string.Empty, AppConfig.WebRootPath);
            if (!connCache.ContainsKey(dbConn))
            {
                connCache.Set(dbConn, cEntity);
            }
            return cEntity;
        }
         */
        #endregion

        internal static string FormatConn(DalType dal, string connString)
        {
            if (dal != DalType.Access)
            {
                string conn = connString.ToLower();
                int index = conn.IndexOf("provider");
                if (index > -1 && index < connString.Length - 5 && (connString[index + 8] == '=' || connString[index + 9] == '='))
                {
                    int end = conn.IndexOf(';', index);
                    if (end > index)
                    {
                        connString = connString.Remove(index, end - index + 1);
                    }
                }
            }
            return connString;
        }

        /// <summary>
        /// 所有链接的对象集合
        /// </summary>
        private static MDictionary<string, ConnObject> connDicCache = new MDictionary<string, ConnObject>(StringComparer.OrdinalIgnoreCase);
        internal static ConnObject GetConnObject(string dbConn)
        {
            dbConn = string.IsNullOrEmpty(dbConn) ? AppConfig.DB.DefaultConn : dbConn;
            if (dbConn.EndsWith("_Bak")) { dbConn = dbConn.Replace("_Bak", ""); }
            if (connDicCache.ContainsKey(dbConn))
            {
                return connDicCache[dbConn];
            }
            ConnBean cbMaster = GetConnBean(dbConn);
            if (cbMaster == null)
            {
                string errMsg = string.Format("Can't find the connection key '{0}' from web.config or app.config!", dbConn);
                if (dbConn == AppConfig.DB.DefaultConn)
                {
                    Error.Throw(errMsg);
                }
                else
                {
                    ConnBean cb = GetConnBean(AppConfig.DB.DefaultConn);
                    if (cb != null)
                    {
                        cbMaster = cb.Clone();//获取默认的值。
                    }
                    else
                    {
                        Error.Throw(errMsg);
                    }
                }
            }
            ConnObject co = new ConnObject();
            co.Master = cbMaster;
            if (dbConn != null && dbConn.Length < 32 && !dbConn.Trim().Contains(" ")) // 为configKey
            {
                ConnBean coBak = GetConnBean(dbConn + "_Bak");
                if (coBak != null && coBak.ProviderName == cbMaster.ProviderName)
                {
                    co.BackUp = coBak;
                }
                for (int i = 1; i < 1000; i++)
                {
                    ConnBean cbSlave = GetConnBean(dbConn + "_Slave" + i);
                    if (cbSlave == null)
                    {
                        break;
                    }
                    co.Slave.Add(cbSlave);
                }
            }
            if (!connDicCache.ContainsKey(dbConn))
            {
                connDicCache.Set(dbConn, co);
            }
            return co;
        }

        private static ConnBean GetConnBean(string dbConn)
        {
            string provider;
            string conn = string.Format(AppConfig.GetConn(dbConn, out provider), AppConfig.WebRootPath);
            if (string.IsNullOrEmpty(conn))
            {
                return null;
            }
            ConnBean cb = new ConnBean();
            cb.ConfigName = dbConn;
            cb.Conn = conn;
            if (string.IsNullOrEmpty(provider))
            {
                provider = GetProvider(cb.Conn);
            }
            cb.ProviderName = provider;
            cb.ConnDalType = GetDalType(provider);
            return cb;
        }

        public static void CheckConnIsOk(object threadID)
        {
            while (true)
            {
                Thread.Sleep(3000);
                if (connDicCache.Count > 0)
                {
                    string[] items = new string[connDicCache.Count];
                    connDicCache.Keys.CopyTo(items, 0);
                    foreach (string key in items)
                    {
                        ConnObject obj = connDicCache[key];
                        if (obj != null)
                        {
                            if (!obj.Master.IsOK) { obj.Master.TryTestConn(); }
                            if (obj.BackUp != null && !obj.BackUp.IsOK) { obj.BackUp.TryTestConn(); }
                            if (obj.Slave != null && obj.Slave.Count > 0)
                            {
                                for (int i = 0; i < obj.Slave.Count; i++)
                                {
                                    if (!obj.Slave[i].IsOK)
                                    {
                                        obj.Slave[i].TryTestConn();
                                    }
                                }
                            }
                        }
                    }
                    items = null;
                }
            }
        }
    }


}