using System;
using System.Collections.Generic;
using System.Text;
using System.Data.OleDb;
using System.Data;
using System.IO;
namespace CYQ.Data
{
    internal class OleDbDal : DbBase
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
        protected override bool IsExistsDbName(string dbName)
        {
            return File.Exists(Con.DataSource.Replace(DataBase, dbName));
        }
    }
}
