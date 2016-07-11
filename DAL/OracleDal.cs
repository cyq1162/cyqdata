using System.Data.OracleClient;
//using Oracle.DataAccess.Client;
using System.Data;
using System;
using System.Data.Common;
using System.Reflection;
using CYQ.Data.Tool;
using CYQ.Data.Cache;

namespace CYQ.Data
{
    internal class OracleDal : DbBase
    {
        /// <summary>
        /// 区分Oracle11和Oracle12的Dll名称。
        /// </summary>
        public static string ManagedName = "Managed";

        public OracleDal(ConnObject co)
            : base(co)
        {
        }
        public override void AddReturnPara()
        {
            if (!Com.Parameters.Contains("ResultCount"))
            {
                AddParameters("ResultCount", DBNull.Value, DbType.Int32, -1, ParameterDirection.Output);//记录总数在最后一位
            }
            if (!Com.Parameters.Contains("ResultCursor"))
            {
                AddCustomePara("ResultCursor", ParaType.Cursor, DBNull.Value);
            }

        }
        internal override void AddCustomePara(string paraName, ParaType paraType, object value)
        {
            if (IsUseOdpNet)
            {
                AddParaForOdpNet(paraName, paraType, value);
            }
            else
            {
                AddParaForOracleClient(paraName, paraType, value);
            }
        }

        private void AddParaForOracleClient(string paraName, ParaType paraType, object value)
        {
            if (Com.Parameters.Contains(paraName)) { return; }
            OracleParameter para = new OracleParameter();
            para.ParameterName = paraName;
            switch (paraType)
            {
                case ParaType.Cursor:
                case ParaType.OutPut:
                    if (paraType == ParaType.Cursor)
                    {
                        para.OracleType = OracleType.Cursor;
                    }
                    else
                    {
                        para.OracleType = OracleType.NVarChar;
                        para.Size = 4000;
                    }
                    para.Direction = ParameterDirection.Output;
                    break;
                case ParaType.ReturnValue:
                    para.OracleType = OracleType.Int32;
                    para.Direction = ParameterDirection.ReturnValue;
                    break;
                case ParaType.CLOB:
                case ParaType.NCLOB:
                    para.OracleType = paraType == ParaType.CLOB ? OracleType.Clob : OracleType.NClob;
                    para.Direction = ParameterDirection.Input;
                    if (value != null)
                    {
                        para.Value = value;
                    }
                    break;
            }
            Com.Parameters.Add(para);
        }
        private void AddParaForOdpNet(string paraName, ParaType paraType, object value)
        {
            Assembly ass = GetAssembly();
            DbParameter para = ass.CreateInstance("Oracle." + ManagedName + "DataAccess.Client.OracleParameter") as DbParameter;
            para.ParameterName = paraName;
            switch (paraType)
            {
                case ParaType.Cursor:
                case ParaType.OutPut:
                    if (paraType == ParaType.Cursor)
                    {
                        para.GetType().GetProperty("OracleDbType").SetValue(para, OracleDbType.RefCursor, null);
                    }
                    else
                    {
                        para.DbType = DbType.String;
                        para.Size = 4000;
                    }
                    para.Direction = ParameterDirection.Output;
                    value = DBNull.Value;
                    break;
                case ParaType.ReturnValue:
                    para.DbType = DbType.Int32;
                    para.Direction = ParameterDirection.ReturnValue;
                    value = DBNull.Value;
                    break;
                case ParaType.CLOB:
                case ParaType.NCLOB:
                    para.GetType().GetProperty("OracleDbType").SetValue(para, paraType == ParaType.CLOB ? OracleDbType.Clob : OracleDbType.NClob, null);
                    para.Direction = ParameterDirection.Input;
                    if (value != null)
                    {
                        para.Value = value;
                    }
                    break;
            }
            Com.Parameters.Add(para);
        }

        public override char Pre
        {
            get
            {
                return ':';
            }
        }
        internal static Assembly GetAssembly()
        {
            object ass = CacheManage.LocalInstance.Get("OracleClient_Assembly");
            if (ass == null)
            {
                //try
                //{
                ass = Assembly.Load("Oracle." + ManagedName + "DataAccess");
                CacheManage.LocalInstance.Add("OracleClient_Assembly", ass, null, 10080, System.Web.Caching.CacheItemPriority.High);
                //}
                //catch(Exception err)
                //{
                //    Error.Throw(errMsg);
                //}
            }
            return ass as Assembly;
        }
        protected override DbProviderFactory GetFactory(string providerName)
        {
            if (IsUseOdpNet)
            {
                object factory = CacheManage.LocalInstance.Get("OracleClient_Factory");
                if (factory == null)
                {

                    Assembly ass = GetAssembly();
                    factory = ass.CreateInstance("Oracle." + ManagedName + "DataAccess.Client.OracleClientFactory");
                    if (factory == null)
                    {
                        throw new System.Exception("Can't Create  OracleClientFactory in Oracle." + ManagedName + "DataAccess.dll");
                    }
                    else
                    {
                        CacheManage.LocalInstance.Add("OracleClient_Factory", factory, null, 10080, System.Web.Caching.CacheItemPriority.High);
                    }

                }
                return factory as DbProviderFactory;
            }
            else
            {
                return base.GetFactory(providerName);
            }
        }
        internal static int isUseOdpNet = -1;
        /// <summary>
        /// 是否使用Oracle的ODP.NET组件。
        /// </summary>
        private bool IsUseOdpNet
        {
            get
            {
                if (isUseOdpNet == -1)
                {
                    string path = string.Empty;
                    try
                    {
                        Assembly ass = System.Reflection.Assembly.GetExecutingAssembly();
                        path = System.IO.Path.GetDirectoryName(ass.CodeBase).Replace(AppConst.FilePre, string.Empty);
                        ass = null;
                    }
                    catch
                    {

                    }
                    if (System.IO.File.Exists(path + "\\Oracle.DataAccess.dll")) ////Oracle 11
                    {
                        ManagedName = "";
                        isUseOdpNet = 1;
                    }
                    else if (System.IO.File.Exists(path + "\\Oracle." + ManagedName + "DataAccess.dll"))//Oracle 12
                    {
                        if (AppConfig.GetConn(base.conn).IndexOf("host", StringComparison.OrdinalIgnoreCase) == -1)
                        {
                            Error.Throw("you need to use the connectionString like this : Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT = 1521)))(CONNECT_DATA =(SID = orcl)));User ID=sa;password=123456");
                        }
                        isUseOdpNet = 2;
                    }
                    else
                    {
                        isUseOdpNet = 0;
                    }

                }
                return isUseOdpNet > 0;
            }
        }
        protected override bool IsExistsDbName(string dbName)
        {
            return DBTool.TestConn(GetNewConn(dbName));
        }
    }

    internal enum OracleDbType
    {
        //BFile = 101,
        //Blob = 102,
        //Byte = 103,
        //Char = 104,
        Clob = 105,
        //Date = 106,
        //Decimal = 107,
        //Double = 108,
        //Long = 109,
        //LongRaw = 110,
        //Int16 = 111,
        //Int32 = 112,
        //Int64 = 113,
        //IntervalDS = 114,
        //IntervalYM = 115,
        NClob = 116,
        //NChar = 117,
        //NVarchar2 = 119,
        //Raw = 120,
        RefCursor = 121,
        //Single = 122,
        //TimeStamp = 123,
        //TimeStampLTZ = 124,
        //TimeStampTZ = 125,
        //Varchar2 = 126,
        //XmlType = 127,
        //Array = 128,
        //Object = 129,
        //Ref = 130,
        //BinaryDouble = 132,
        //BinaryFloat = 133,
    }
}