using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace CYQ.Data
{
    internal class MsSqlDal : DbBase
    {
        public MsSqlDal(ConnObject co)
            : base(co)
        { }
        protected override void AddReturnPara()
        {
            AddParameters("ReturnValue", null, DbType.Int32, 32, ParameterDirection.ReturnValue);
        }

        internal override void AddCustomePara(string paraName, ParaType paraType, object value,string typeName)
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
            return DbProviderFactories.GetFactory("System.Data.SqlClient");
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
    }
}
