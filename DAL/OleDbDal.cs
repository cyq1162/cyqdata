using System;
using System.Collections.Generic;
using System.Text;
using System.Data.OleDb;
using System.Data;
using System.IO;
using System.Data.Common;
namespace CYQ.Data
{
    internal partial class OleDbDal : DalBase
    {
        public OleDbDal(ConnObject co)
            : base(co)
        {

        }
        public override bool AddParameters(string parameterName, object value, DbType dbType, int size, ParameterDirection direction)
        {
            parameterName = parameterName.Substring(0, 1) == "@" ? parameterName : "@" + parameterName;
            if (Com.Parameters.Contains(parameterName))
            {
                return false;
            }
            OleDbParameter para = new OleDbParameter();
            para.ParameterName = parameterName;
            para.Value = value;
            if (dbType == DbType.DateTime)
            {
                para.OleDbType = OleDbType.DBTimeStamp;
                if (value != null && value != DBNull.Value)
                {
                    para.Value = value.ToString();
                }
                else
                {
                    para.Value = DBNull.Value;
                }
            }
            else
            {
                if (dbType == DbType.Time)
                {
                    para.DbType = DbType.String;
                }
                else
                {
                    para.DbType = dbType;
                }
                if (value == null)
                {
                    para.Value = DBNull.Value;
                }
                else
                {
                    para.Value = value;
                }
            }
            Com.Parameters.Add(para);
            return true;
        }
        protected override DbProviderFactory GetFactory()
        {
            return DbProviderFactories.GetFactory("System.Data.OleDb");
        }
        protected override bool IsExistsDbName(string dbName)
        {
            return File.Exists(Con.DataSource.Replace(DataBaseName, dbName));
        }
    }

    internal partial class OleDbDal
    {
        public override Dictionary<string, string> GetTables()
        {
            return GetSchemaDic("U");
        }
        public override Dictionary<string, string> GetViews()
        {
            switch (ConnObj.Master.ConnDataBaseType)
            {
                case Data.DataBaseType.Excel:
                case Data.DataBaseType.FoxPro:
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            return GetSchemaDic("V");
        }
        private Dictionary<string, string> GetSchemaDic(string type)
        {
            #region 用ADO.NET属性拿数据
            DataTable dt = null;
            Dictionary<string, string> tables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Con.Open();
            if (type == "U")
            {
                dt = Con.GetSchema("Tables", new string[] { null, null, null, "Table" });
            }
            else
            {
                dt = Con.GetSchema("Views");
            }
            Con.Close();
            if (dt != null && dt.Rows.Count > 0)
            {
                string tableName = string.Empty;
                foreach (DataRow row in dt.Rows)
                {
                    tableName = Convert.ToString(row["TABLE_NAME"]);
                    if (!tables.ContainsKey(tableName))
                    {
                        tables.Add(tableName, string.Empty);
                    }
                }
                dt = null;
            }
            return tables;
            #endregion
        }
    }
}
