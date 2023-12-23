using System.Data;
using System.Data.Common;
using CYQ.Data.Cache;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.IO;
namespace CYQ.Data
{
    internal partial class DaMengDal : DalBase
    {
        private CacheManage _Cache = CacheManage.LocalInstance;//Cache操作
        public DaMengDal(ConnObject co)
            : base(co)
        {

        }
        internal static Assembly GetAssembly()
        {
            object ass = CacheManage.LocalInstance.Get("DaMengClient_Assembly");
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
                CacheManage.LocalInstance.Set("DaMengClient_Assembly", ass, 10080);

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
                bool result = ExeScalar("show databases like '" + dbName + "'", false) != null;
                IsRecordDebugInfo = true;
                return result;
            }
            catch
            {
                return true;
            }
        }
        string dbName = String.Empty;
        public override string DataBaseName
        {
            get
            {
                if (string.IsNullOrEmpty(dbName))
                {
                    string conn = UsingConnBean.ConnString;
                    int len = 7;
                    int index = conn.ToLower().IndexOf("schema=");
                    if (index == -1)
                    {
                        index = conn.ToLower().IndexOf("database=");
                        len = 9;
                    }
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
                _com.CommandText = "set schema " + DataBaseName;
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
        protected override string GetSchemaSql(string type)
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
                return string.Format("select table_name as TableName,comments as Description from all_tab_comments  where owner='{0}' and TABLE_TYPE='{1}' order by table_name", DataBaseName, type);
            }
        }
    }
}
