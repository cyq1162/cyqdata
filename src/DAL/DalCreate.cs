using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Tool;
using System.Threading;
using System.IO;
using CYQ.Data.Orm;


namespace CYQ.Data
{
    /// <summary>
    /// ���ݿ����Ͳ�����
    /// </summary>
    internal static class DalCreate
    {
        //private const string SqlClient = "System.Data.SqlClient";
        //private const string OleDb = "System.Data.OleDb";
        //private const string OracleClient = "System.Data.OracleClient";
        //private const string SQLiteClient = "System.Data.SQLite";
        //private const string MySqlClient = "MySql.Data.MySqlClient";
        //private const string SybaseClient = "Sybase.Data.AseClient";
        //private const string PostgreClient = "System.Data.NpgSqlClient";
        //private const string TxtClient = "CYQ.Data.TxtClient";
        //private const string XmlClient = "CYQ.Data.XmlClient";
        //private const string XHtmlClient = "CYQ.Data.XHtmlClient";

        /// <summary>
        /// ȫ�ִ浵����Ϊ���õ�����ʵ��ȫ������
        /// </summary>
        private static MDictionary<string, DalBase> dalBaseDic = new MDictionary<string, DalBase>();
        public static DalBase Get(string key)
        {
            if (dalBaseDic.ContainsKey(key))
            {
                return dalBaseDic[key];
            }
            return null;
        }
        public static bool Remove(string key)
        {
            return dalBaseDic.Remove(key);
        }
        /// <summary>
        /// �򵥹�����Factory Method��
        /// </summary>
        public static DalBase CreateDal(string connNameOrString)
        {
            string key = StaticTool.GetTransationKey(connNameOrString);
            //����Ƿ�����ȫ������;
            bool isTrans = DBFast.HasTransation(key);
            if (isTrans)
            {
                if (dalBaseDic.ContainsKey(key))
                {
                    return dalBaseDic[key];
                }

            }
            DalBase dal = CreateDalBase(connNameOrString);
            if (isTrans)
            {
                dal.TranLevel = DBFast.GetTransationLevel(key);
                dal.IsOpenTrans = true;
                dalBaseDic.Add(key, dal);
            }
            return dal;
        }
        private static DalBase CreateDalBase(string connNameOrString)
        {
            //ABCConn
            DalBase db = GetDalBaseBy(ConnObject.Create(connNameOrString));

            if (!string.IsNullOrEmpty(connNameOrString) && db.ConnObj.Master.ConnName.ToLower() != connNameOrString.ToLower() && connNameOrString.EndsWith("Conn"))//��Ҫ�л����á�
            {
                //Conn  A��
                //BConn  xxx �Ҳ���ʱ����Ĭ�Ͽ⡣
                DBResetResult result = db.ChangeDatabase(connNameOrString.Substring(0, connNameOrString.Length - 4));
                if (result == DBResetResult.Yes) // д�뻺��
                {
                    db.ConnObj.SaveToCache(connNameOrString);
                }
            }
            return db;
        }
        private static DalBase GetDalBaseBy(ConnObject co)
        {
            DataBaseType dalType = co.Master.ConnDataBaseType;
            //License.Check(providerName);//���ģ����Ȩ��⡣
            switch (dalType)
            {
                case DataBaseType.MsSql:
                    return new MsSqlDal(co);
                case DataBaseType.Access:
                case DataBaseType.Excel:
                case DataBaseType.FoxPro:
                    return new OleDbDal(co);
                case DataBaseType.Oracle:
                    return new OracleDal(co);
                case DataBaseType.SQLite:
                    return new SQLiteDal(co);
                case DataBaseType.MySql:
                    return new MySQLDal(co);
                case DataBaseType.Sybase:
                    return new SybaseDal(co);
                case DataBaseType.PostgreSQL:
                    return new PostgreDal(co);
                case DataBaseType.DB2:
                    return new DB2Dal(co);
                case DataBaseType.Txt:
                case DataBaseType.Xml:
                    return new NoSqlDal(co);
            }
            return (DalBase)Error.Throw(string.Format("GetHelper:{0} No Be Support Now!", dalType.ToString()));
        }

        public static DataBaseType GetDalTypeByReaderName(string typeName)
        {
            switch (typeName.Replace("DataReader", "").ToLower())
            {
                case "oracle":
                    return DataBaseType.Oracle;
                case "sql":
                    return DataBaseType.MsSql;
                case "sqlite":
                    return DataBaseType.SQLite;
                case "oledb":
                    return DataBaseType.Access;
                case "mysql":
                    return DataBaseType.MySql;
                case "odbc":
                case "ase":
                    return DataBaseType.Sybase;
                case "pgsql":
                case "npgsql":
                    return DataBaseType.PostgreSQL;
                case "db2":
                    return DataBaseType.DB2;
                default:
                    return DataBaseType.None;

            }
        }

    }


}