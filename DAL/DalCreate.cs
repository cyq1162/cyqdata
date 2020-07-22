using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Tool;
using System.Threading;
using System.IO;
using CYQ.Data.Orm;


namespace CYQ.Data
{
    /// <summary>
    /// 数据库类型操作类
    /// </summary>
    internal static class DalCreate
    {
        //private const string SqlClient = "System.Data.SqlClient";
        //private const string OleDb = "System.Data.OleDb";
        //private const string OracleClient = "System.Data.OracleClient";
        //private const string SQLiteClient = "System.Data.SQLite";
        //private const string MySqlClient = "MySql.Data.MySqlClient";
        //private const string SybaseClient = "Sybase.Data.AseClient";
        //private const string PostgreClient = "System.Data.NpgSqlClient";
        //private const string TxtClient = "CYQ.Data.TxtClient";
        //private const string XmlClient = "CYQ.Data.XmlClient";
        //private const string XHtmlClient = "CYQ.Data.XHtmlClient";

        /// <summary>
        /// 全局存档，是为了用单例来实现全局事务。
        /// </summary>
        private static MDictionary<string, DalBase> dalBaseDic = new MDictionary<string, DalBase>();
        public static DalBase Get(string key)
        {
            if (dalBaseDic.ContainsKey(key))
            {
                return dalBaseDic[key];
            }
            return null;
        }
        public static bool Remove(string key)
        {
            return dalBaseDic.Remove(key);
        }
        /// <summary>
        /// 简单工厂（Factory Method）
        /// </summary>
        public static DalBase CreateDal(string connNameOrString)
        {
            string key = StaticTool.GetTransationKey(connNameOrString);
            //检测是否开启了全局事务;
            bool isTrans = DBFast.HasTransation(key);
            if (isTrans)
            {
                if (dalBaseDic.ContainsKey(key))
                {
                    return dalBaseDic[key];
                }

            }
            DalBase dal = CreateDalBase(connNameOrString);
            if (isTrans)
            {
                dal.TranLevel = DBFast.GetTransationLevel(key);
                dal.IsOpenTrans = true;
                dalBaseDic.Add(key, dal);
            }
            return dal;
        }
        private static DalBase CreateDalBase(string connNameOrString)
        {
            //ABCConn
            DalBase db = GetDalBaseBy(ConnObject.Create(connNameOrString));

            if (!string.IsNullOrEmpty(connNameOrString) && db.ConnObj.Master.ConnName.ToLower() != connNameOrString.ToLower() && connNameOrString.EndsWith("Conn"))//需要切换配置。
            {
                //Conn  A库
                //BConn  xxx 找不到时，找默认库。
                DBResetResult result = db.ChangeDatabase(connNameOrString.Substring(0, connNameOrString.Length - 4));
                if (result == DBResetResult.Yes) // 写入缓存
                {
                    db.ConnObj.SaveToCache(connNameOrString);
                }
            }
            return db;
        }
        private static DalBase GetDalBaseBy(ConnObject co)
        {
            DataBaseType dalType = co.Master.ConnDataBaseType;
            //License.Check(providerName);//框架模块授权检测。
            switch (dalType)
            {
                case DataBaseType.MsSql:
                    return new MsSqlDal(co);
                case DataBaseType.Access:
                    return new OleDbDal(co);
                case DataBaseType.Oracle:
                    return new OracleDal(co);
                case DataBaseType.SQLite:
                    return new SQLiteDal(co);
                case DataBaseType.MySql:
                    return new MySQLDal(co);
                case DataBaseType.Sybase:
                    return new SybaseDal(co);
                case DataBaseType.PostgreSQL:
                    return new PostgreDal(co);
                case DataBaseType.DB2:
                    return new DB2Dal(co);
                case DataBaseType.Txt:
                case DataBaseType.Xml:
                    return new NoSqlDal(co);
            }
            return (DalBase)Error.Throw(string.Format("GetHelper:{0} No Be Support Now!", dalType.ToString()));
        }

        public static DataBaseType GetDalTypeByReaderName(string typeName)
        {
            switch (typeName.Replace("DataReader", "").ToLower())
            {
                case "oracle":
                    return DataBaseType.Oracle;
                case "sql":
                    return DataBaseType.MsSql;
                case "sqlite":
                    return DataBaseType.SQLite;
                case "oledb":
                    return DataBaseType.Access;
                case "mysql":
                    return DataBaseType.MySql;
                case "odbc":
                case "ase":
                    return DataBaseType.Sybase;
                case "pgsql":
                case "npgsql":
                    return DataBaseType.PostgreSQL;
                case "db2":
                    return DataBaseType.DB2;
                default:
                    return DataBaseType.None;

            }
        }

    }


}