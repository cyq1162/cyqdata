using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace CYQ.Data
{
    internal partial class MsSqlDal : DalBase
    {
        public MsSqlDal(ConnObject co)
            : base(co)
        { }
        protected override void AddReturnPara()
        {
            AddParameters("ReturnValue", null, DbType.Int32, 32, ParameterDirection.ReturnValue);
        }

        internal override void AddCustomePara(string paraName, ParaType paraType, object value, string typeName)
        {
            if (Com.Parameters.Contains(paraName))
            {
                return;
            }
            switch (paraType)
            {
                case ParaType.OutPut:
                case ParaType.ReturnValue:
                case ParaType.Structured:
                    SqlParameter para = new SqlParameter();
                    para.ParameterName = paraName;
                    if (paraType == ParaType.Structured)
                    {
                        para.SqlDbType = SqlDbType.Structured;
                        para.TypeName = typeName;
                        para.Value = value;
                    }
                    else if (paraType == ParaType.OutPut)
                    {
                        para.SqlDbType = SqlDbType.NVarChar;
                        para.Size = 2000;
                        para.Direction = ParameterDirection.Output;
                    }
                    else
                    {
                        para.SqlDbType = SqlDbType.Int;
                        para.Direction = ParameterDirection.ReturnValue;
                    }
                    Com.Parameters.Add(para);
                    break;
            }
        }

        protected override DbProviderFactory GetFactory()
        {
            return SqlClientFactory.Instance;
            //return DbProviderFactories.GetFactory("System.Data.SqlClient");
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
        //protected override string FormatConnString(string connString)
        //{
        //    SqlConnectionStringBuilder sb = new SqlConnectionStringBuilder(connString);
        //    if (sb.Pooling && sb.MaxPoolSize == 100)
        //    {
        //        sb.MaxPoolSize = 512;
        //    }
        //    return connString;
        //}
    }

    internal partial class MsSqlDal
    {
        protected override string GetSchemaSql(string type)
        {
            bool for2000 = Version.StartsWith("08");
            return @"Select o.name as TableName, p.value as Description from sysobjects o " + (for2000 ? "left join sysproperties p on p.id = o.id and smallid = 0" : "left join sys.extended_properties p on p.major_id = o.id and minor_id = 0")
               + " and p.name = 'MS_Description' where o.type = '" + type + "' AND o.name<>'dtproperties' AND o.name<>'sysdiagrams'" + (for2000 ? "" : " and category=0");
        }
    }
}
