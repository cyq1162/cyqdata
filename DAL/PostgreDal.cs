using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data.Common;
using CYQ.Data.Cache;
using System.IO;

namespace CYQ.Data
{
    internal class PostgreDal : DbBase
    {
        public PostgreDal(ConnObject co)
            : base(co)
        {

        }
        internal static Assembly GetAssembly()
        {
            object ass = CacheManage.LocalInstance.Get("Postgre_Assembly");
            if (ass == null)
            {
                try
                {
                    string name = string.Empty;
                    if (File.Exists(AppConst.RunFolderPath + "Npgsql.dll"))
                    {
                        name = "Npgsql";
                    }
                    else
                    {
                        name = "Can't find the Npgsql.dll";
                        Error.Throw(name);
                    }
                    ass = Assembly.Load(name);
                    CacheManage.LocalInstance.Set("Postgre_Assembly", ass, 10080);
                }
                catch (Exception err)
                {
                    string errMsg = err.Message;
                    Error.Throw(errMsg);
                }
            }
            return ass as Assembly;
        }
        protected override DbProviderFactory GetFactory(string providerName)
        {
            object factory = CacheManage.LocalInstance.Get("Postgre_Factory");
            if (factory == null)
            {
                Assembly ass = GetAssembly();
                factory = ass.GetType("Npgsql.NpgsqlFactory").GetField("Instance").GetValue(null);
               // factory = ass.CreateInstance("Npgsql.NpgsqlFactory.Instance");
                if (factory == null)
                {
                    throw new System.Exception("Can't Create  NpgsqlFactory in Npgsql.dll");
                }
                else
                {
                    CacheManage.LocalInstance.Set("Postgre_Factory", factory, 10080);
                }

            }
            return factory as DbProviderFactory;

        }

        protected override bool IsExistsDbName(string dbName)
        {
            try
            {
                IsAllowRecordSql = false;
                bool result = ExeScalar("select 1 from pg_catalog.pg_database where datname='" + dbName + "'", false) != null;
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
                return ':';
            }
        }
        public override void AddReturnPara()
        {

        }
    }
}
