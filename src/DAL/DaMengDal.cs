using System.Data;
using System.Data.Common;
using CYQ.Data.Cache;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.IO;
namespace CYQ.Data
{
    /// <summary>
    /// 单数据库 =》 多模式
    /// </summary>
    internal partial class DaMengDal : DalBase
    {
        private DistributedCache _Cache = DistributedCache.Local;//Cache操作
        public DaMengDal(ConnObject co)
            : base(co)
        {

        }
        internal static Assembly GetAssembly()
        {
            object ass = DistributedCache.Local.Get("DaMengClient_Assembly");
            if (ass == null)
            {
                string name = string.Empty;
                if (File.Exists(AppConst.AssemblyPath + "DmProvider.dll"))
                {
                    name = "DmProvider";
                }
                else
                {
                    name = "Can't find the DmProvider.dll";
                    Error.Throw(name);
                }
                ass = ass = Assembly.LoadFrom("DmProvider.dll");// Assembly.Load(name);
                DistributedCache.Local.Set("DaMengClient_Assembly", ass, 10080);

            }
            return ass as Assembly;
        }
        protected override DbProviderFactory GetFactory()
        {
            object factory = _Cache.Get("DaMengClient_Factory");
            if (factory == null)
            {
                Assembly ass = GetAssembly();
                Type t = ass.GetType("Dm.DmClientFactory");
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
                    throw new System.Exception("Can't Create DmClientFactory in DmProvider.dll");
                }
                else
                {
                    _Cache.Set("DaMengClient_Factory", factory, 10080);
                }
            }
            return factory as DbProviderFactory;
        }
        protected override bool IsExistsDbName(string dbName)
        {
            try
            {
                IsRecordDebugInfo = false || AppDebug.IsContainSysSql;
                if(AppConfig.DB.IsDaMengUpper){dbName=dbName.ToUpper();}
                string sql = string.Format("select 1 from SYS.ALL_OBJECTS where OBJECT_NAME='{0}'", dbName);
                bool result = ExeScalar(sql, false) != null;
                IsRecordDebugInfo = true;
                return result;
            }
            catch
            {
                return true;
            }
        }
        string dbName = String.Empty;
        /// <summary>
        /// select * from v$database
        /// 达梦以（端口号对应服务）一个服务对应一个数据库名称，因此数据库名称可以忽略。
        /// </summary>
        public override string DataBaseName
        {
            get
            {
                if (string.IsNullOrEmpty(dbName))
                {
                    string conn = UsingConnBean.ConnString;
                    int len = 9;
                    int index = conn.ToLower().IndexOf("database=");
                    if (index > 0)
                    {
                        int end = conn.IndexOf(';', index);
                        if (end > 0)
                        {
                            dbName = conn.Substring(index + len, end - index - len).ToUpper();
                        }
                        else
                        {
                            dbName = conn.Substring(index + len).ToUpper();
                        }
                    }
                }
                return dbName;
            }
        }
        string schemaName = string.Empty;
        /// <summary>
        /// 模式 此数据库中提供约为 数据库名称等同
        /// </summary>
        public override string SchemaName
        {
            get
            {
                if (string.IsNullOrEmpty(schemaName))
                {
                    string conn = UsingConnBean.ConnString;
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
                        schemaName = DataBaseName;
                    }
                }

                return schemaName;
            }
        }
        public override char Pre
        {
            get
            {
                return ':';
            }
        }
        protected override void AfterOpen()
        {
            if (!string.IsNullOrEmpty(DataBaseName) && _com.CommandType == CommandType.Text)
            {
                string sql = _com.CommandText;
                _com.CommandText = "set schema " + SchemaName;
                _com.ExecuteNonQuery();//有异常直接抛，无效的数据库名称。
                if (!string.IsNullOrEmpty(sql))
                {
                    _com.CommandText = sql;
                }
            }
        }
    }
    internal partial class DaMengDal
    {
        protected override string GetUVPSql(string type)
        {
            if (type == "P")
            {
                return "";
            }
            else
            {
                if (type == "U")
                {
                    type = "TABLE";
                }
                else if (type == "V")
                {
                    type = "VIEW";
                }
                return string.Format("select table_name as TableName,comments as Description from all_tab_comments  where owner='{0}' and TABLE_TYPE='{1}' order by table_name", SchemaName, type);
            }
        }
    }
}
