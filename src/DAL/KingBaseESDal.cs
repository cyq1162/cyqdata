using System.Data;
using System.Data.Common;
using CYQ.Data.Cache;
using System.Reflection;
using System;
using System.IO;
using System.Collections.Generic;
namespace CYQ.Data
{
    /// <summary>
    /// 多数据库 =》 多模式
    /// </summary>
    internal partial class KingBaseESDal : DalBase
    {
        private DistributedCache _Cache = DistributedCache.Local;//Cache操作
        public KingBaseESDal(ConnObject co)
            : base(co)
        {

        }
        protected override bool IsExistsDbName(string dbName)
        {
            try
            {
                IsRecordDebugInfo = false || AppDebug.IsContainSysSql;
                if (AppConfig.DB.IsKingBaseESLower) { dbName = dbName.ToLower(); }
                string sql = string.Format("select 1 from sys_database WHERE datname='{0}'", dbName);
                bool result = ExeScalar(sql, false) != null;
                IsRecordDebugInfo = true;
                return result;
            }
            catch
            {
                return true;
            }
        }

        private Assembly GetAssembly()
        {
            object ass = _Cache.Get("KingBaseESClient_Assembly");
            if (ass == null)
            {
                string name = string.Empty;
                if (File.Exists(AppConst.AssemblyPath + "Kdbndp.dll"))
                {
                    name = "Kdbndp";
                }
                else
                {
                    name = "Can't find the Kdbndp.dll";
                    Error.Throw(name);
                }
                ass = Assembly.Load(name);
                DistributedCache.Local.Set("KingBaseESClient_Assembly", ass, 10080);
            }
            return ass as Assembly;
        }
        protected override DbProviderFactory GetFactory()
        {
            object factory = _Cache.Get("KingBaseESClient_Factory");
            if (factory == null)
            {
                Assembly ass = GetAssembly();
                Type t = ass.GetType("Kdbndp.KdbndpFactory");
                if (t != null)
                {
                    FieldInfo fi = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                    if (fi != null)
                    {
                        factory = fi.GetValue(null);
                    }
                }
                if (factory == null)
                {
                    throw new System.Exception("Can't Create KdbndpFactory in Kdbndp.dll");
                }
                else
                {
                    _Cache.Set("KingBaseESClient_Factory", factory, 10080);
                }
            }
            return factory as DbProviderFactory;

        }
        string schemaName = string.Empty;
        /// <summary>
        /// 模式名称 （非等同数据库名称）
        /// </summary>
        public override string SchemaName
        {
            get
            {
                if (string.IsNullOrEmpty(schemaName))
                {
                    string conn = UsingConnBean.ConnStringOrg;
                    int len = 7;
                    int index = conn.ToLower().IndexOf("schema=");
                    if (index > 0)
                    {
                        int end = conn.IndexOf(';', index);
                        if (end > 0)
                        {
                            schemaName = conn.Substring(index + len, end - index - len).ToUpper();
                        }
                        else
                        {
                            schemaName = conn.Substring(index + len).ToUpper();
                        }
                    }
                    if (string.IsNullOrEmpty(schemaName))
                    {
                        schemaName = "public";//默认模式
                    }
                    if (AppConfig.DB.IsKingBaseESLower)
                    {
                        schemaName = schemaName.ToLower();
                    }
                }

                return schemaName;
            }
        }

        protected override void AfterOpen()
        {
            //非默认模式，即自定义模式
            if (SchemaName != "public" && _com.CommandType == CommandType.Text)
            {
                string sql = _com.CommandText;
                _com.CommandText = "set schema '" + SchemaName + "'";
                _com.ExecuteNonQuery();//有异常直接抛，无效的数据库名称。
                if (!string.IsNullOrEmpty(sql))
                {
                    _com.CommandText = sql;
                }
            }
        }
        public override char Pre
        {
            get
            {
                return ':';
            }
        }
    }

    internal partial class KingBaseESDal
    {
        protected override string GetUVPSql(string type)
        {
            if (type == "P")
            {
                return "";
            }
            else
            {
                if (type == "U") { type = "BASE TABLE"; }
                else if (type == "V")
                {
                    type = "VIEW";
                }
                /*       
                 * CASE WHEN c.relkind = 'p' THEN 'Y' ELSE 'N' END  is_partitioned , --   y 是分区表  n 不是分区表 

                     -- r = 普通表， i = 索引， S = 序列， t = TOAST表， v = 视图， m = 物化视图， c = 组合类型， f = 外部表， p = 分区表， I = 分区索引
                 */
                return string.Format(@"select a.table_name as TableName,d.description as Description from information_schema.TABLES a 
        INNER JOIN sys_namespace b ON b.nspname=a.table_schema
        LEFT JOIN sys_class c ON c.relnamespace=b.oid AND c.relkind='r'
        LEFT JOIN sys_description d ON d.objoid=c.oid AND d.objsubid=0  where table_schema='{0}' and table_type='{1}' order by a.table_name", SchemaName, type);
            }
        }
    }
}
