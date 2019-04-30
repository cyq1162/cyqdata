using CYQ.Data.Table;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;

namespace CYQ.Data
{
    internal sealed class NoSqlConnection : DbConnection
    {
        NoSqlDal _NoSqlDal;
        public NoSqlTransaction Transaction;
        public NoSqlCommand Command;
        public NoSqlConnection(string conn, NoSqlDal noSqlDal)
        {
            _Conn = conn;
            _NoSqlDal = noSqlDal;
        }
        protected override DbCommand CreateDbCommand()
        {
            if (Command == null)
            {
                Command = new NoSqlCommand(null, this);
            }
            return Command;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            if (Transaction == null)
            {
                Transaction = new NoSqlTransaction(this, isolationLevel);
            }
            return Transaction;
        }
        public override void ChangeDatabase(string databaseName)
        {
            //
        }

        private string _Conn;
        public override string ConnectionString
        {
            get
            {
                return _Conn;
            }
            set
            {
                _Conn = value;
            }
        }
        private string filePath = "";
        /// <summary>
        /// 返回文件夹所在的目录
        /// </summary>
        public override string DataSource
        {
            get
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = GetFilePath(ConnectionString);
                }
                return filePath;
            }
        }
        private string folderName;
        /// <summary>
        /// 返回 目录名 即数据库名
        /// </summary>
        public override string Database
        {
            get
            {
                if (string.IsNullOrEmpty(folderName))
                {
                    folderName = DataSource.Replace("/", "\\").TrimEnd('\\');
                    folderName = folderName.Substring(folderName.LastIndexOf('\\') + 1);
                }
                return folderName;
            }
        }
        public DataBaseType DatabaseType
        {
            get
            {
                return ConnBean.GetDataBaseType(ConnectionString);
            }
        }
        public override void Open()
        {
            if (!Directory.Exists(DataSource))
            {
                Error.Throw("Error for this directory:" + DataSource);
            }
            state = ConnectionState.Open;
        }
        public override void Close()
        {
            //重新写回数据。
            Command.Dispose(true);
            state = ConnectionState.Closed;
        }
        public override string ServerVersion
        {
            get
            {
                return "CYQ.Data.NoSql V3.0";
            }
        }
        private ConnectionState state = ConnectionState.Closed;
        public override ConnectionState State
        {
            get
            {
                return state;
            }
        }
        public override DataTable GetSchema()
        {
            return GetSchema("Tables", null);
        }
        public override DataTable GetSchema(string collectionName)
        {
            return GetSchema(collectionName, null);
        }
        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            DataTable dt = new DataTable(collectionName);
            dt.Columns.Add("TABLE_NAME");
            switch (collectionName.ToLower())
            {
                case "tables":
                    #region tables


                    string[] tsList = Directory.GetFiles(DataSource, "*.ts", SearchOption.TopDirectoryOnly);
                    if (tsList != null && tsList.Length > 0)
                    {
                        string dalType = ".txt";
                        if (restrictionValues != null && restrictionValues.Length > 0)
                        {
                            if (restrictionValues[0] != null && Convert.ToString(restrictionValues[0]).ToLower() == "xml")
                            {
                                dalType = ".xml";
                            }
                        }
                        DataRow row = null;
                        string tName = string.Empty;
                        foreach (string tsName in tsList)
                        {
                            tName = Path.GetFileNameWithoutExtension(tsName);//获得表名。
                            string[] tableList = Directory.GetFiles(DataSource, tName + ".*", SearchOption.TopDirectoryOnly);
                            foreach (string tableName in tableList)
                            {
                                if (tableName.EndsWith(dalType))
                                {
                                    continue;
                                }
                                row = dt.NewRow();
                                row[0] = tableName.EndsWith(".ts") ? Path.GetFileNameWithoutExtension(tableName) : Path.GetFileName(tableName);
                                dt.Rows.Add(row);
                            }

                        }
                    }
                    #endregion
                    break;
                case "columns":
                    #region columns


                    dt.Columns.Add("COLUMN_NAME");
                    dt.Columns.Add("COLUMN_LCid", typeof(int));
                    dt.Columns.Add("DATA_TYPE", typeof(int));
                    dt.Columns.Add("TABLE_MaxSize", typeof(int));
                    dt.Columns.Add("COLUMN_ISREADONLY", typeof(bool));
                    dt.Columns.Add("TABLE_ISCANNULL", typeof(bool));
                    dt.Columns.Add("COLUMN_DEFAULT");
                    tsList = Directory.GetFiles(DataSource, "*.ts");
                    if (tsList != null && tsList.Length > 0)
                    {
                        DataRow row = null;
                        string tName = string.Empty;
                        foreach (string tsName in tsList)
                        {
                            MDataColumn mdc = MDataColumn.CreateFrom(tsName);
                            if (mdc.Count > 0)
                            {
                                tName = Path.GetFileNameWithoutExtension(tsName);
                                MCellStruct cs = null;
                                for (int i = 0; i < mdc.Count; i++)
                                {
                                    cs = mdc[i];
                                    row = dt.NewRow();
                                    row[0] = tName;
                                    row[1] = cs.ColumnName;
                                    row[2] = i;
                                    row[3] = (int)cs.SqlType;
                                    row[4] = cs.MaxSize;
                                    row[5] = cs.IsAutoIncrement;
                                    row[6] = cs.IsCanNull;
                                    row[7] = Convert.ToString(cs.DefaultValue);
                                    dt.Rows.Add(row);
                                }
                            }
                        }
                    }
                    #endregion
                    break;
            }
            return dt;
        }

        internal static string GetFilePath(string conn)
        {
            if (conn.IndexOf("{0}") > -1)
            {
                conn = string.Format(conn, AppConfig.WebRootPath);//链接相关的用WebRootPath，支持ASPNETCore
            }
            int start = conn.IndexOf('=') + 1;//1=2;3 -- 1
            int end = conn.IndexOf(";");//3
            string filePath = conn.Substring(start, end > 0 ? end - start : conn.Length - start);
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);//
            }
            filePath = filePath.TrimEnd('/', '\\') + "/";
            return filePath;
        }
    }
}
