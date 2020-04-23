using System.Data;
using System.Data.Common;
using CYQ.Data.Cache;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.IO;
namespace CYQ.Data
{
    internal partial class MySQLDal : DalBase
    {
        private CacheManage _Cache = CacheManage.LocalInstance;//Cache操作
        public MySQLDal(ConnObject co)
            : base(co)
        {

        }
        internal static Assembly GetAssembly()
        {
            object ass = CacheManage.LocalInstance.Get("MySqlClient_Assembly");
            if (ass == null)
            {
                try
                {
                    ass = Assembly.Load("MySql.Data");
                    CacheManage.LocalInstance.Set("MySqlClient_Assembly", ass, 10080);
                }
                catch (Exception err)
                {
                    string errMsg = err.Message;
                    if (!File.Exists(AppConst.AssemblyPath + "MySql.Data.dll"))
                    {
                        errMsg = "Can't find the MySql.Data.dll more info : " + errMsg;
                    }
                    Error.Throw(errMsg);
                }
            }
            return ass as Assembly;
        }
        protected override DbProviderFactory GetFactory()
        {
            object factory = _Cache.Get("MySqlClient_Factory");
            if (factory == null)
            {
                Assembly ass = GetAssembly();
                factory = ass.CreateInstance("MySql.Data.MySqlClient.MySqlClientFactory");
                if (factory == null)
                {
                    throw new System.Exception("Can't Create  MySqlClientFactory in MySql.Data.dll");
                }
                else
                {
                    _Cache.Set("MySqlClient_Factory", factory, 10080);
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
        public override char Pre
        {
            get
            {
                return '?';
            }
        }
    }
    internal partial class MySQLDal
    {
        protected override string GetSchemaSql(string type)
        {
            if (type == "P")
            {
                return "select ROUTINE_NAME as TableName,'P' as Description from information_schema.ROUTINES where ROUTINE_SCHEMA='" + DataBaseName + "'";
            }
            else
            {
                if (type == "U") { type = "BASE TABLE"; }
                else if (type == "V")
                {
                    type = "VIEW";
                }
                return string.Format("select TABLE_NAME as TableName,TABLE_COMMENT as Description from `information_schema`.`TABLES`  where TABLE_SCHEMA='{0}' and TABLE_TYPE='{1}'", DataBaseName, type);
            }
        }
    }
}
