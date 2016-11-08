using System.Data;
using System.Data.Common;
using CYQ.Data.Cache;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.IO;
namespace CYQ.Data
{
    internal class MySQLDal : DbBase
    {
        private CacheManage _Cache = CacheManage.LocalInstance;//Cache操作
        public MySQLDal(ConnObject co)
            : base(co)
        {

        }

        public override void AddReturnPara()
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
                    CacheManage.LocalInstance.Set("MySqlClient_Assembly", 10080);
                }
                catch(Exception err)
                {
                    string errMsg = err.Message;
                    if (!File.Exists(AppConst.RunFolderPath + "MySql.Data.dll"))
                    {
                        errMsg = "Can't find the MySql.Data.dll more info : " + errMsg;
                    }
                    Error.Throw(errMsg);
                }
            }
            return ass as Assembly;
        }
        protected override DbProviderFactory GetFactory(string providerName)
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
                IsAllowRecordSql = false;
                bool result = ExeScalar("show databases like '" + dbName + "'", false) != null;
                IsAllowRecordSql = true;
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
}
