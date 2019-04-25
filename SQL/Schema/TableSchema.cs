using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;

using System.Data.Common;
using System.Data;
using System.IO;
using System.Data.OleDb;
using CYQ.Data.Cache;
using System.Reflection;
using CYQ.Data.Tool;
using CYQ.Data.Orm;
using System.Configuration;


namespace CYQ.Data.SQL
{
    /// <summary>
    /// 表结构类
    /// </summary>
    internal partial class TableSchema
    {
        static readonly object ot = new object();

        internal static Dictionary<string, string> GetSchemas(string conn, string type)
        {
            ConnBean connBean = ConnBean.Create(conn);
            int hash = connBean.GetHashCode();
            Dictionary<int, Dictionary<string, string>> schemaDic = null;
            switch (type)
            {
                case "U":
                    schemaDic = _TableCache;
                    break;
                case "V":
                    schemaDic = _ViewCache;
                    break;
                case "P":
                    schemaDic = _ProcCache;
                    break;
            }
            if (schemaDic.ContainsKey(hash))
            {
                return schemaDic[hash];
            }
            Dictionary<string, string> tables = null;
            #region 查看有没有表架构缓存
            if (!string.IsNullOrEmpty(AppConfig.DB.SchemaMapPath))
            {
                string fullPath = AppConfig.RunPath + AppConfig.DB.SchemaMapPath + hash + ".ts";
                if (System.IO.File.Exists(fullPath))
                {
                    tables = JsonHelper.Split(IOHelper.ReadAllText(fullPath));
                }
            }
            #endregion
            if (tables == null)
            {
                lock (ot)
                {
                    if (!schemaDic.ContainsKey(hash))
                    {
                        #region 从数据库读
                        using (DalBase helper = DalCreate.CreateDal(connBean.ConnName))
                        {
                            helper.IsRecordDebugInfo = false;
                            switch (type)
                            {
                                case "U":
                                    tables = helper.GetTables();
                                    break;
                                case "V":
                                    tables = helper.GetViews();
                                    break;
                                case "P":
                                    tables = helper.GetProcs();
                                    break;
                            }
                        }
                    }
                    else
                    {
                        return schemaDic[hash];
                    }
                }
                        #endregion
            }
            #region 写入表结构映射
            if (!string.IsNullOrEmpty(AppConfig.DB.SchemaMapPath))
            {
                string fullPath = AppConfig.RunPath + AppConfig.DB.SchemaMapPath + hash + ".ts";
                IOHelper.Save(fullPath, JsonHelper.ToJson(tables), false, true);
            }
            #endregion

            #region 写入缓存
            if (!schemaDic.ContainsKey(hash) && tables != null && tables.Count > 0)//读不到表不缓存。
            {
                schemaDic.Add(hash, tables);
            }
            #endregion
            return tables;
        }
        public static Dictionary<string, string> GetTables(string conn)
        {
            return GetSchemas(conn, "U");
        }
        public static Dictionary<string, string> GetViews(string conn)
        {
            return GetSchemas(conn, "V");
        }
        public static Dictionary<string, string> GetProcs(string conn)
        {
            return GetSchemas(conn, "P");
        }
        /// <summary>
        /// 全局表名缓存（只缓存表名和表名的描述）
        /// </summary>
        private static Dictionary<int, Dictionary<string, string>> _TableCache = new Dictionary<int, Dictionary<string, string>>();
        /// <summary>
        /// 全局表名缓存（只缓存视图名和表名的描述）
        /// </summary>
        private static Dictionary<int, Dictionary<string, string>> _ViewCache = new Dictionary<int, Dictionary<string, string>>();
        /// <summary>
        /// 全局表名缓存（只缓存存储过程名和表名的描述）
        /// </summary>
        private static Dictionary<int, Dictionary<string, string>> _ProcCache = new Dictionary<int, Dictionary<string, string>>();

        /// <summary>
        /// 是否存在表或视图
        /// </summary>
        /// <param name="type">"U"或"V"或"P"</param>
        /// <param name="name">表名或视图名，允许database.tableName</param>
        public static bool Exists(string name, string type, string conn)
        {
            return CrossDB.Exists(name, type, conn);
        }
        /// <summary>
        /// 移除表的缓存。
        /// </summary>
        public static bool Remove(string name, string type, string conn)
        {
            return CrossDB.Remove(name, type, conn);
        }
        /// <summary>
        /// 把表添加到缓存中。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static bool Add(string name, string type, string conn)
        {
            return CrossDB.Add(name, type, conn);
        }
        /// <summary>
        /// 获得表的描述
        /// </summary>
        internal static string GetTableDescription(string conn, string tableName)
        {
            TableInfo info = CrossDB.GetTableInfoByName(tableName, conn);
            if (info != null)
            {
                return info.Description;
            }
            return "";
        }

        internal static int GetTableHash(string tableName)
        {
            return Math.Abs(tableName.Replace("-", "").Replace("_", "").Replace(" ", "").ToLower().GetHashCode());
        }
        public static void Clear()
        {
            _TableCache.Clear();
            _ViewCache.Clear();
            _ProcCache.Clear();
        }
    }
    internal partial class TableSchema
    {
        //internal const string ExistMsSql = "SELECT count(*) FROM sysobjects where id = OBJECT_id(N'{0}') AND xtype in (N'{1}')";
        ////internal const string Exist2005 = "SELECT count(*) FROM sys.objects where object_id = OBJECT_id(N'{0}') AND type in (N'{1}')";
        //internal const string ExistOracle = "Select count(*)  From user_objects where  object_name=upper('{0}') and object_type='{1}'";
        //internal const string ExistMySql = "SELECT count(*)  FROM  `information_schema`.`COLUMNS`  where TABLE_NAME='{0}' and TABLE_SCHEMA='{1}'";
        //internal const string ExistSybase = "SELECT count(*) FROM sysobjects where id = OBJECT_id(N'{0}') AND type in (N'{1}')";
        //internal const string ExistSqlite = "SELECT count(*) FROM sqlite_master where name='{0}' and type='{1}'";
        //internal const string ExistPostgre = "SELECT count(*) FROM information_schema.tables where table_schema = 'public' and table_name='{0}' and table_type='{1}'";
        internal const string ExistOracleSequence = "SELECT count(*) FROM All_Sequences where Sequence_name='{0}'";
        internal const string CreateOracleSequence = "create sequence {0} start with {1} increment by 1";
        internal const string GetOracleMaxID = "select max({0}) from {1}";


    }

}
