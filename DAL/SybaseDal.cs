using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data.Common;
using CYQ.Data.Cache;
using System.IO;

namespace CYQ.Data
{
    internal partial class SybaseDal : DalBase
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
                string name = string.Empty;
                if (File.Exists(AppConst.AssemblyPath + "Sybase.AdoNet2.AseClient.dll"))
                {
                    name = "Sybase.AdoNet2.AseClient";
                }
                else if (File.Exists(AppConst.AssemblyPath + "Sybase.AdoNet4.AseClient.dll"))
                {
                    name = "Sybase.AdoNet4.AseClient";
                }
                else
                {
                    name = "Can't find the Sybase.AdoNet2.AseClient.dll";
                    Error.Throw(name);
                }
                ass = Assembly.Load(name);
                CacheManage.LocalInstance.Set("Sybase_Assembly", ass, 10080);
            }
            return ass as Assembly;
        }
        protected override DbProviderFactory GetFactory()
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
                    CacheManage.LocalInstance.Set("Sybase_Factory", factory, 10080);
                }

            }
            return factory as DbProviderFactory;

        }

        protected override bool IsExistsDbName(string dbName)
        {
            try
            {
                IsRecordDebugInfo = false || AppDebug.IsContainSysSql;
                bool result = ExeScalar("select 1 from master..sysdatabases where [name]='" + dbName + "'", false) != null;
                IsRecordDebugInfo = true;
                return result;
            }
            catch
            {
                return true;
            }
        }
    }

    internal partial class SybaseDal
    {

        protected override string GetSchemaSql(string type)
        {
            return "SELECT name as TableName,'' as Description FROM sysobjects where type='" + type + "'";
        }
    }
}
