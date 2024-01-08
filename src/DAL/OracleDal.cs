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
    internal partial class OracleDal : DalBase
    {
        /// <summary>
        /// 区分Oracle11和Oracle12的Dll名称。
        /// </summary>
        public static string ManagedName = "Managed";

        public OracleDal(ConnObject co)
            : base(co)
        {
        }
        protected override void AddReturnPara()
        {
            if (!Com.Parameters.Contains("ResultCount"))
            {
                AddParameters("ResultCount", DBNull.Value, DbType.Int32, -1, ParameterDirection.Output);//记录总数在最后一位
            }
            if (!Com.Parameters.Contains("ResultCursor"))
            {
                AddCustomePara("ResultCursor", ParaType.Cursor, DBNull.Value, null);
            }

        }
        internal override void AddCustomePara(string paraName, ParaType paraType, object value, string typeName)
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
            //工厂方法理解点
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

        public override string DataBaseName
        {
            get
            {
                // (DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT = 1521)))(CONNECT_DATA =(Sid = Aries)))
                int i = _con.DataSource.LastIndexOf('=') + 1;
                return _con.DataSource.Substring(i).Trim(' ', ')');
            }
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
            object ass = DistributedCache.Local.Get("OracleClient_Assembly");
            if (ass == null)
            {
                //try
                //{
                ass = Assembly.Load("Oracle." + ManagedName + "DataAccess");
                DistributedCache.Local.Set("OracleClient_Assembly", ass, 10080);
                //}
                //catch(Exception err)
                //{
                //    Error.Throw(errMsg);
                //}
            }
            return ass as Assembly;
        }
        /// <summary>
        /// 抽象工厂
        /// </summary>
        protected override DbProviderFactory GetFactory()
        {
            if (IsUseOdpNet)
            {
                object factory = DistributedCache.Local.Get("OracleClient_Factory");
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
                        DistributedCache.Local.Set("OracleClient_Factory", factory, 10080);
                    }

                }
                return factory as DbProviderFactory;
            }
            else
            {
                return DbProviderFactories.GetFactory("System.Data.OracleClient");
            }
        }
        /// <summary>
        /// 值-1未初始化；0使用OracleClient；1使用DataAccess；2使用ManagedDataAccess
        /// </summary>
        internal static int clientType = -1;
        private static readonly object lockObj = new object();
        /// <summary>
        /// 是否使用Oracle的ODP.NET组件。
        /// </summary>
        private bool IsUseOdpNet
        {
            get
            {
                if (clientType == -1)
                {
                    lock (lockObj)
                    {
                        if (clientType == -1)
                        {
                            if (AppConfig.GetConn(base.ConnName).IndexOf("host", StringComparison.OrdinalIgnoreCase) == -1)
                            {
                                clientType = 0;
                            }
                            else
                            {
                                string path = AppConst.AssemblyPath;
                                if (System.IO.File.Exists(path + "Oracle." + ManagedName + "DataAccess.dll"))//Oracle 12
                                {
                                    clientType = 2;
                                }
                                else if (System.IO.File.Exists(path + "Oracle.DataAccess.dll")) ////Oracle 11
                                {
                                    ManagedName = "";
                                    clientType = 1;
                                }
                                else
                                {
                                    clientType = 0;
                                    Log.Write("Can't find Oracle.ManagedDataAccess.dll or Oracle.DataAccess.dll on the path:" + path, LogType.DataBase);
                                }
                            }
                        }
                    }
                }
                return clientType > 0;
            }
        }
        protected override bool IsExistsDbName(string dbName)
        {
            return DBTool.TestConn(GetConnString(dbName));
        }
    }

    internal partial class OracleDal
    {
        protected override string GetUVPSql(string type)
        {
            if (type == "U")
            {
                return "select TABLE_NAME AS TableName,COMMENTS AS Description from user_tab_comments where TABLE_TYPE='TABLE' and TABLE_NAME not like 'BIN$%' order by TABLE_NAME";
            }
            else if (type == "V")
            {
                return "select TABLE_NAME AS TableName,COMMENTS AS Description from user_tab_comments where TABLE_TYPE='VIEW' order by TABLE_NAME";
            }
            else if (type == "P")
            {
                return "select object_name as TableName,'' as Description from user_objects where object_type = 'PROCEDURE' order by object_name";
            }
            return "";
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