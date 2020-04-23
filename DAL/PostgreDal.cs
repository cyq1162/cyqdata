using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data.Common;
using CYQ.Data.Cache;
using System.IO;

namespace CYQ.Data
{
    internal partial class PostgreDal : DalBase
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
                string name = string.Empty;
                if (File.Exists(AppConst.AssemblyPath + "Npgsql.dll"))
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
            return ass as Assembly;
        }
        protected override DbProviderFactory GetFactory()
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
                IsRecordDebugInfo = false || AppDebug.IsContainSysSql;
                bool result = ExeScalar("select 1 from pg_catalog.pg_database where datname='" + dbName + "'", false) != null;
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
                return ':';
            }
        }
    }

    internal partial class PostgreDal
    {
        protected override string GetSchemaSql(string type)
        {
            switch (type)
            {
                case "U":
                case "V":
                    if (type == "U") { type = "BASE TABLE"; }
                    else { type = "View"; }
                    return string.Format("select table_name as TableName,cast(obj_description(p.oid,'pg_class') as varchar) as Description from information_schema.tables t left join  pg_class p on t.table_name=p.relname  where table_schema='public' and table_type='{1}' and table_catalog='{0}'", DataBaseName, type);
                case "P":
                default:
                    return "select routine_name as TableName,'' as Description from information_schema.routines where specific_schema='public' and external_language='SQL'";
            }
        }

    }
}
