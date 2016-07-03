using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data.Common;
using CYQ.Data.Cache;
using System.IO;

namespace CYQ.Data
{
    internal class SybaseDal : DbBase
    {
        public SybaseDal(ConnObject co)
            : base(co)
        {

        }
        internal static Assembly GetAssembly()
        {
            object ass = CacheManage.LocalInstance.Get("Sybase_Assembly");
            if (ass == null)
            {
                try
                {
                    string name = string.Empty;
                    if (File.Exists(AppConst.RunFolderPath + "Sybase.AdoNet2.AseClient.dll"))
                    {
                        name = "Sybase.AdoNet2.AseClient";
                    }
                    else if (File.Exists(AppConst.RunFolderPath + "Sybase.AdoNet4.AseClient.dll"))
                    {
                        name = "Sybase.AdoNet4.AseClient";
                    }
                    else
                    {
                        name = "Can't find the Sybase.AdoNet2.AseClient.dll";
                        Error.Throw(name);
                    }
                    ass = Assembly.Load(name);
                    CacheManage.LocalInstance.Add("Sybase_Assembly", ass, null, 10080, System.Web.Caching.CacheItemPriority.High);
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
            object factory = CacheManage.LocalInstance.Get("Sybase_Factory");
            if (factory == null)
            {
                Assembly ass = GetAssembly();
                factory = ass.CreateInstance("Sybase.Data.AseClient.AseClientFactory");
                if (factory == null)
                {
                    throw new System.Exception("Can't Create  AseClientFactory in Sybase.AdoNet2.AseClient");
                }
                else
                {
                    CacheManage.LocalInstance.Add("Sybase_Factory", factory, null, 10080, System.Web.Caching.CacheItemPriority.High);
                }

            }
            return factory as DbProviderFactory;

        }

        protected override bool IsExistsDbName(string dbName)
        {
            try
            {
                IsAllowRecordSql = false;
                bool result = ExeScalar("select 1 from master..sysdatabases where [name]='" + dbName + "'", false) != null;
                IsAllowRecordSql = true;
                return result;
            }
            catch
            {
                return true;
            }
        }

        public override void AddReturnPara()
        {

        }
    }
}
