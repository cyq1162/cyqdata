using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;

using System.Data.Common;
using System.Data;
using System.IO;
using System.Data.OleDb;
using CYQ.Data.Cache;
using System.Reflection;
using CYQ.Data.Tool;
using CYQ.Data.Orm;


namespace CYQ.Data.SQL
{
    /// <summary>
    /// 表结构类
    /// </summary>
    internal partial class TableSchema
    {
        /// <summary>
        /// 全局表名缓存（只缓存表名和表名的描述）
        /// </summary>
        internal static Dictionary<string, Dictionary<string, string>> tableCache = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// 全局缓存实体类的表结构（仅会对Type读取的结构）
        /// </summary>
        internal static MDictionary<string, MDataColumn> columnCache = new MDictionary<string, MDataColumn>(StringComparer.OrdinalIgnoreCase);
        internal static string GetTableCacheKey(DbBase dbBase)
        {
            return GetTableCacheKey(dbBase.dalType, dbBase.DataBase, dbBase.conn);
        }
        internal static string GetTableCacheKey(DalType dalType, string dataBase, string conn)
        {
            return "TableCache_" + dalType + "." + dataBase + Math.Abs(conn.GetHashCode());
        }

        public static MDataColumn GetColumns(Type typeInfo)
        {
            string key = "ColumnCache_" + typeInfo.FullName;
            if (columnCache.ContainsKey(key))
            {
                return columnCache[key].Clone();
            }
            else
            {
                #region 获取列结构
                MDataColumn mdc = new MDataColumn();
                switch (StaticTool.GetSystemType(ref typeInfo))
                {
                    case SysType.Base:
                    case SysType.Enum:
                        mdc.Add(typeInfo.Name, DataType.GetSqlType(typeInfo), false);
                        return mdc;
                    case SysType.Generic:
                    case SysType.Collection:
                        Type[] argTypes;
                        Tool.StaticTool.GetArgumentLength(ref typeInfo, out argTypes);
                        foreach (Type type in argTypes)
                        {
                            mdc.Add(type.Name, DataType.GetSqlType(type), false);
                        }
                        argTypes = null;
                        return mdc;

                }

                List<PropertyInfo> pis = StaticTool.GetPropertyInfo(typeInfo);

                SqlDbType sqlType;
                for (int i = 0; i < pis.Count; i++)
                {
                    sqlType = SQL.DataType.GetSqlType(pis[i].PropertyType);
                    mdc.Add(pis[i].Name, sqlType);
                    MCellStruct column = mdc[i];
                    LengthAttribute la = GetAttr<LengthAttribute>(pis[i]);//获取长度设置
                    if (la != null)
                    {
                        column.MaxSize = la.MaxSize;
                        column.Scale = la.Scale;
                    }
                    if (column.MaxSize <= 0)
                    {
                        column.MaxSize = DataType.GetMaxSize(sqlType);
                    }
                    KeyAttribute ka = GetAttr<KeyAttribute>(pis[i]);//获取关键字判断
                    if (ka != null)
                    {
                        column.IsPrimaryKey = ka.IsPrimaryKey;
                        column.IsAutoIncrement = ka.IsAutoIncrement;
                        column.IsCanNull = ka.IsCanNull;
                    }
                    else if (i == 0)
                    {
                        column.IsPrimaryKey = true;
                        column.IsCanNull = false;
                        if (column.ColumnName.ToLower().Contains("id") && (column.SqlType == System.Data.SqlDbType.Int || column.SqlType == SqlDbType.BigInt))
                        {
                            column.IsAutoIncrement = true;
                        }
                    }
                    DefaultValueAttribute dva = GetAttr<DefaultValueAttribute>(pis[i]);
                    if (dva != null && dva.DefaultValue != null)
                    {
                        if (column.SqlType == SqlDbType.Bit)
                        {
                            column.DefaultValue = (dva.DefaultValue.ToString() == "True" || dva.DefaultValue.ToString() == "1") ? 1 : 0;
                        }
                        else
                        {
                            column.DefaultValue = dva.DefaultValue;
                        }
                    }
                    else if (i > pis.Count - 3 && sqlType == SqlDbType.DateTime && pis[i].Name.EndsWith("Time"))
                    {
                        column.DefaultValue = SqlValue.GetDate;
                    }
                    DescriptionAttribute da = GetAttr<DescriptionAttribute>(pis[i]);//看是否有字段描述属性。
                    if (da != null)
                    {
                        column.Description = da.Description;
                    }
                }
                object[] tableAttr = typeInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);//看是否设置了表特性，获取表名和表描述
                if (tableAttr != null && tableAttr.Length == 1)
                {
                    DescriptionAttribute attr = tableAttr[0] as DescriptionAttribute;
                    if (attr != null && !string.IsNullOrEmpty(attr.Description))
                    {
                        mdc.Description = attr.Description;
                    }
                }
                pis = null;
                #endregion

                if (!columnCache.ContainsKey(key))
                {
                    columnCache.Set(key, mdc.Clone());
                }

                return mdc;
            }

        }
        private static T GetAttr<T>(PropertyInfo pi)
        {
            object[] attr = pi.GetCustomAttributes(typeof(T), false);//看是否设置了特性
            if (attr != null && attr.Length == 1)
            {
                return (T)attr[0];
            }
            return default(T);
        }

        //private static KeyAttribute GetKeyAttr(PropertyInfo pi)
        //{
        //    object[] attr = pi.GetCustomAttributes(typeof(KeyAttribute), false);//看是否设置了表特性，获取表名和表描述
        //    if (attr != null && attr.Length == 1)
        //    {
        //        return attr[0] as KeyAttribute;
        //    }
        //    return null;
        //}
        //private static LengthAttribute GetLengthAttr(PropertyInfo pi)
        //{
        //    object[] attr = pi.GetCustomAttributes(typeof(LengthAttribute), false);//看是否设置了表特性，获取表名和表描述
        //    if (attr != null && attr.Length == 1)
        //    {
        //        return attr[0] as LengthAttribute;
        //    }
        //    return null;
        //}
        //private static DefaultValueAttribute GetDefaultValueAttr(PropertyInfo pi)
        //{
        //    object[] attr = pi.GetCustomAttributes(typeof(DefaultValueAttribute), false);//看是否设置了表特性，获取表名和表描述
        //    if (attr != null && attr.Length == 1)
        //    {
        //        return attr[0] as DefaultValueAttribute;
        //    }
        //    return null;
        //}
        private static DescriptionAttribute GetDescriptionAttr(PropertyInfo pi)
        {
            object[] attr = pi.GetCustomAttributes(typeof(DescriptionAttribute), false);//看是否设置了表特性，获取表名和表描述
            if (attr != null && attr.Length == 1)
            {
                return attr[0] as DescriptionAttribute;
            }
            return null;
        }
        public static MDataColumn GetColumns(string tableName, ref DbBase dbHelper)
        {
            tableName = Convert.ToString(SqlCreate.SqlToViewSql(tableName));
            string key = GetSchemaKey(tableName, dbHelper.DataBase, dbHelper.dalType);
            if (CacheManage.LocalInstance.Contains(key))//缓存里获取
            {
                return CacheManage.LocalInstance.Get<MDataColumn>(key).Clone();
            }
            DalType dalType = dbHelper.dalType;

            #region 文本数据库处理。
            if (dalType == DalType.Txt || dalType == DalType.Xml)
            {
                if (!tableName.Contains(" "))// || tableName.IndexOfAny(Path.GetInvalidPathChars()) == -1
                {
                    tableName = SqlFormat.NotKeyword(tableName);//处理database..tableName;
                    tableName = Path.GetFileNameWithoutExtension(tableName);//视图表，带“.“的，会出问题
                    string fileName = dbHelper.Con.DataSource + tableName + (dalType == DalType.Txt ? ".txt" : ".xml");
                    MDataColumn mdc = MDataColumn.CreateFrom(fileName);
                    mdc.dalType = dalType;
                    return mdc;
                }
                return GetTxtDBViewColumns(tableName);//处理视图
            }
            #endregion



            tableName = SqlFormat.Keyword(tableName, dbHelper.dalType);

            //switch (dalType)
            //{
            //    case DalType.SQLite:
            //    case DalType.MySql:
            //        tableName = SqlFormat.NotKeyword(tableName);
            //        break;

            //}


            MDataColumn mdcs = new MDataColumn();
            mdcs.dalType = dalType
                ;
            //如果table和helper不在同一个库
            DbBase helper = dbHelper.ResetDbBase(tableName);

            helper.IsAllowRecordSql = false;//内部系统，不记录SQL表结构语句。
            try
            {
                bool isView = tableName.Contains(" ");//是否视图。
                if (!isView)
                {
                    isView = Exists("V", tableName, ref helper);
                }
                MCellStruct mStruct = null;
                SqlDbType sqlType = SqlDbType.NVarChar;
                if (isView)
                {
                    string sqlText = SqlFormat.BuildSqlWithWhereOneEqualsTow(tableName);// string.Format("select * from {0} where 1=2", tableName);
                    mdcs = GetViewColumns(sqlText, ref helper);
                }
                else
                {
                    mdcs.AddRelateionTableName(SqlFormat.NotKeyword(tableName));
                    switch (dalType)
                    {
                        case DalType.MsSql:
                        case DalType.Oracle:
                        case DalType.MySql:
                        case DalType.Sybase:
                        case DalType.PostgreSQL:
                            #region Sql
                            string sql = string.Empty;
                            if (dalType == DalType.MsSql)
                            {
                                #region Mssql
                                string dbName = null;
                                if (!helper.Version.StartsWith("08"))
                                {
                                    //先获取同义词，检测是否跨库
                                    string realTableName = Convert.ToString(helper.ExeScalar(string.Format(MSSQL_SynonymsName, SqlFormat.NotKeyword(tableName)), false));
                                    if (!string.IsNullOrEmpty(realTableName))
                                    {
                                        string[] items = realTableName.Split('.');
                                        tableName = realTableName;
                                        if (items.Length > 0)//跨库了
                                        {
                                            dbName = realTableName.Split('.')[0];
                                        }
                                    }
                                }

                                sql = GetMSSQLColumns(helper.Version.StartsWith("08"), dbName ?? helper.DataBase);
                                #endregion
                            }
                            else if (dalType == DalType.MySql)
                            {
                                sql = GetMySqlColumns(helper.DataBase);
                            }
                            else if (dalType == DalType.Oracle)
                            {
                                tableName = tableName.ToUpper();//Oracle转大写。
                                //先获取同义词，不检测是否跨库
                                string realTableName = Convert.ToString(helper.ExeScalar(string.Format(Oracle_SynonymsName, SqlFormat.NotKeyword(tableName)), false));
                                if (!string.IsNullOrEmpty(realTableName))
                                {
                                    tableName = realTableName;
                                }

                                sql = GetOracleColumns();
                            }
                            else if (dalType == DalType.Sybase)
                            {
                                tableName = SqlFormat.NotKeyword(tableName);
                                sql = GetSybaseColumns();
                            }
                            else if (dalType == DalType.PostgreSQL)
                            {
                                sql = GetPostgreColumns();
                            }
                            helper.AddParameters("TableName", SqlFormat.NotKeyword(tableName), DbType.String, 150, ParameterDirection.Input);
                            DbDataReader sdr = helper.ExeDataReader(sql, false);
                            if (sdr != null)
                            {
                                long maxLength;
                                bool isAutoIncrement = false;
                                short scale = 0;
                                string sqlTypeName = string.Empty;
                                while (sdr.Read())
                                {
                                    short.TryParse(Convert.ToString(sdr["Scale"]), out scale);
                                    if (!long.TryParse(Convert.ToString(sdr["MaxSize"]), out maxLength))//mysql的长度可能大于int.MaxValue
                                    {
                                        maxLength = -1;
                                    }
                                    else if (maxLength > int.MaxValue)
                                    {
                                        maxLength = int.MaxValue;
                                    }
                                    sqlTypeName = Convert.ToString(sdr["SqlType"]);
                                    sqlType = DataType.GetSqlType(sqlTypeName);
                                    isAutoIncrement = Convert.ToBoolean(sdr["IsAutoIncrement"]);
                                    mStruct = new MCellStruct(mdcs.dalType);
                                    mStruct.ColumnName = Convert.ToString(sdr["ColumnName"]).Trim();
                                    mStruct.OldName = mStruct.ColumnName;
                                    mStruct.SqlType = sqlType;
                                    mStruct.IsAutoIncrement = isAutoIncrement;
                                    mStruct.IsCanNull = Convert.ToBoolean(sdr["IsNullable"]);
                                    mStruct.MaxSize = (int)maxLength;
                                    mStruct.Scale = scale;
                                    mStruct.Description = Convert.ToString(sdr["Description"]);
                                    mStruct.DefaultValue = SqlFormat.FormatDefaultValue(dalType, sdr["DefaultValue"], 0, sqlType);
                                    mStruct.IsPrimaryKey = Convert.ToString(sdr["IsPrimaryKey"]) == "1";
                                    switch (dalType)
                                    {
                                        case DalType.MsSql:
                                        case DalType.MySql:
                                        case DalType.Oracle:
                                            mStruct.IsUniqueKey = Convert.ToString(sdr["IsUniqueKey"]) == "1";
                                            mStruct.IsForeignKey = Convert.ToString(sdr["IsForeignKey"]) == "1";
                                            mStruct.FKTableName = Convert.ToString(sdr["FKTableName"]);
                                            break;
                                    }

                                    mStruct.SqlTypeName = sqlTypeName;
                                    mStruct.TableName = SqlFormat.NotKeyword(tableName);
                                    mdcs.Add(mStruct);
                                }
                                sdr.Close();
                                if (dalType == DalType.Oracle && mdcs.Count > 0)//默认没有自增概念，只能根据情况判断。
                                {
                                    MCellStruct firstColumn = mdcs[0];
                                    if (firstColumn.IsPrimaryKey && firstColumn.ColumnName.ToLower().Contains("id") && firstColumn.Scale == 0 && DataType.GetGroup(firstColumn.SqlType) == 1 && mdcs.JointPrimary.Count == 1)
                                    {
                                        firstColumn.IsAutoIncrement = true;
                                    }
                                }
                            }
                            #endregion
                            break;
                        case DalType.SQLite:
                            #region SQlite
                            if (helper.Con.State != ConnectionState.Open)
                            {
                                helper.Con.Open();
                            }
                            DataTable sqliteDt = helper.Con.GetSchema("Columns", new string[] { null, null, SqlFormat.NotKeyword(tableName) });
                            if (!helper.isOpenTrans)
                            {
                                helper.Con.Close();
                            }
                            int size;
                            short sizeScale;
                            string dataTypeName = string.Empty;

                            foreach (DataRow row in sqliteDt.Rows)
                            {
                                object len = row["NUMERIC_PRECISION"];
                                if (len == null || len == DBNull.Value)
                                {
                                    len = row["CHARACTER_MAXIMUM_LENGTH"];
                                }
                                short.TryParse(Convert.ToString(row["NUMERIC_SCALE"]), out sizeScale);
                                if (!int.TryParse(Convert.ToString(len), out size))//mysql的长度可能大于int.MaxValue
                                {
                                    size = -1;
                                }
                                dataTypeName = Convert.ToString(row["DATA_TYPE"]);
                                if (dataTypeName == "text" && size > 0)
                                {
                                    sqlType = DataType.GetSqlType("varchar");
                                }
                                else
                                {
                                    sqlType = DataType.GetSqlType(dataTypeName);
                                }
                                //COLUMN_NAME,DATA_TYPE,PRIMARY_KEY,IS_NULLABLE,CHARACTER_MAXIMUM_LENGTH AUTOINCREMENT

                                mStruct = new MCellStruct(row["COLUMN_NAME"].ToString(), sqlType, Convert.ToBoolean(row["AUTOINCREMENT"]), Convert.ToBoolean(row["IS_NULLABLE"]), size);
                                mStruct.Scale = sizeScale;
                                mStruct.Description = Convert.ToString(row["DESCRIPTION"]);
                                mStruct.DefaultValue = SqlFormat.FormatDefaultValue(dalType, row["COLUMN_DEFAULT"], 0, sqlType);//"COLUMN_DEFAULT"
                                mStruct.IsPrimaryKey = Convert.ToBoolean(row["PRIMARY_KEY"]);
                                mStruct.SqlTypeName = dataTypeName;
                                mStruct.TableName = SqlFormat.NotKeyword(tableName);
                                mdcs.Add(mStruct);
                            }
                            #endregion
                            break;
                        case DalType.Access:
                            #region Access
                            DataTable keyDt, valueDt;
                            string sqlText = SqlFormat.BuildSqlWithWhereOneEqualsTow(tableName);// string.Format("select * from {0} where 1=2", tableName);
                            OleDbConnection con = new OleDbConnection(helper.Con.ConnectionString);
                            OleDbCommand com = new OleDbCommand(sqlText, con);
                            con.Open();
                            keyDt = com.ExecuteReader(CommandBehavior.KeyInfo).GetSchemaTable();
                            valueDt = con.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, new object[] { null, null, SqlFormat.NotKeyword(tableName) });
                            con.Close();
                            con.Dispose();

                            if (keyDt != null && valueDt != null)
                            {
                                string columnName = string.Empty, sqlTypeName = string.Empty;
                                bool isKey = false, isCanNull = true, isAutoIncrement = false;
                                int maxSize = -1;
                                short maxSizeScale = 0;
                                SqlDbType sqlDbType;
                                foreach (DataRow row in keyDt.Rows)
                                {
                                    columnName = row["ColumnName"].ToString();
                                    isKey = Convert.ToBoolean(row["IsKey"]);//IsKey
                                    isCanNull = Convert.ToBoolean(row["AllowDBNull"]);//AllowDBNull
                                    isAutoIncrement = Convert.ToBoolean(row["IsAutoIncrement"]);
                                    sqlTypeName = Convert.ToString(row["DataType"]);
                                    sqlDbType = DataType.GetSqlType(sqlTypeName);
                                    short.TryParse(Convert.ToString(row["NumericScale"]), out maxSizeScale);
                                    if (Convert.ToInt32(row["NumericPrecision"]) > 0)//NumericPrecision
                                    {
                                        maxSize = Convert.ToInt32(row["NumericPrecision"]);
                                    }
                                    else
                                    {
                                        long len = Convert.ToInt64(row["ColumnSize"]);
                                        if (len > int.MaxValue)
                                        {
                                            maxSize = int.MaxValue;
                                        }
                                        else
                                        {
                                            maxSize = (int)len;
                                        }
                                    }
                                    mStruct = new MCellStruct(columnName, sqlDbType, isAutoIncrement, isCanNull, maxSize);
                                    mStruct.Scale = maxSizeScale;
                                    mStruct.IsPrimaryKey = isKey;
                                    mStruct.SqlTypeName = sqlTypeName;
                                    mStruct.TableName = SqlFormat.NotKeyword(tableName);
                                    foreach (DataRow item in valueDt.Rows)
                                    {
                                        if (columnName == item[3].ToString())//COLUMN_NAME
                                        {
                                            if (item[8].ToString() != "")
                                            {
                                                mStruct.DefaultValue = SqlFormat.FormatDefaultValue(dalType, item[8], 0, sqlDbType);//"COLUMN_DEFAULT"
                                            }
                                            break;
                                        }
                                    }
                                    mdcs.Add(mStruct);
                                }

                            }

                            #endregion
                            break;
                    }
                }
                helper.ClearParameters();
            }
            catch (Exception err)
            {
                helper.debugInfo.Append(err.Message);
            }
            finally
            {
                helper.IsAllowRecordSql = true;//恢复记录SQL表结构语句。
                if (helper != dbHelper)
                {
                    helper.Dispose();
                }
            }
            if (mdcs.Count > 0)
            {
                //移除被标志的列：
                string[] fields = AppConfig.DB.HiddenFields.Split(',');
                foreach (string item in fields)
                {
                    string field = item.Trim();
                    if (!string.IsNullOrEmpty(field) & mdcs.Contains(field))
                    {
                        mdcs.Remove(field);
                    }
                }
            }
            if (!CacheManage.LocalInstance.Contains(key) && mdcs.Count > 0)//拿不到表结构时不缓存。
            {
                CacheManage.LocalInstance.Set(key, mdcs.Clone());
            }
            return mdcs;
        }
        /// <summary>
        /// DbDataReader的GetSchema拿到的DataType、Size、Scale很不靠谱
        /// </summary>
        internal static void FixTableSchemaType(DbDataReader sdr, DataTable tableSchema)
        {
            if (sdr != null && tableSchema != null)
            {
                tableSchema.Columns.Add("DataTypeString");
                for (int i = 0; i < sdr.FieldCount; i++)
                {
                    tableSchema.Rows[i]["DataTypeString"] = sdr.GetDataTypeName(i);
                }
            }
        }
        internal static MDataColumn GetColumns(DataTable tableSchema)
        {
            MDataColumn mdcs = new MDataColumn();
            if (tableSchema != null && tableSchema.Rows.Count > 0)
            {
                mdcs.isViewOwner = true;
                string columnName = string.Empty, sqlTypeName = string.Empty, tableName = string.Empty;
                bool isKey = false, isCanNull = true, isAutoIncrement = false;
                int maxSize = -1;
                short maxSizeScale = 0;
                SqlDbType sqlDbType;
                string dataTypeName = "DataTypeString";
                if (!tableSchema.Columns.Contains(dataTypeName))
                {
                    dataTypeName = "DataType";
                    if (!tableSchema.Columns.Contains(dataTypeName))
                    {
                        dataTypeName = "DataTypeName";
                    }
                }
                bool isHasAutoIncrement = tableSchema.Columns.Contains("IsAutoIncrement");
                bool isHasHidden = tableSchema.Columns.Contains("IsHidden");
                string hiddenFields = "," + AppConfig.DB.HiddenFields.ToLower() + ",";
                for (int i = 0; i < tableSchema.Rows.Count; i++)
                {
                    DataRow row = tableSchema.Rows[i];
                    tableName = Convert.ToString(row["BaseTableName"]);
                    mdcs.AddRelateionTableName(tableName);
                    if (isHasHidden && Convert.ToString(row["IsHidden"]) == "True")// !dcList.Contains(columnName))
                    {
                        continue;//后面那个会多出关联字段。
                    }
                    columnName = row["ColumnName"].ToString().Trim('"');//sqlite视图时会带引号
                    if (string.IsNullOrEmpty(columnName))
                    {
                        columnName = "Empty_" + i;
                    }
                    #region 处理是否隐藏列
                    bool isHiddenField = hiddenFields.IndexOf("," + columnName + ",", StringComparison.OrdinalIgnoreCase) > -1;
                    if (isHiddenField)
                    {
                        continue;
                    }
                    #endregion

                    bool.TryParse(Convert.ToString(row["IsKey"]), out isKey);
                    bool.TryParse(Convert.ToString(row["AllowDBNull"]), out isCanNull);
                    // isKey = Convert.ToBoolean();//IsKey
                    //isCanNull = Convert.ToBoolean(row["AllowDBNull"]);//AllowDBNull
                    if (isHasAutoIncrement)
                    {
                        isAutoIncrement = Convert.ToBoolean(row["IsAutoIncrement"]);
                    }
                    sqlTypeName = Convert.ToString(row[dataTypeName]);
                    sqlDbType = DataType.GetSqlType(sqlTypeName);

                    if (short.TryParse(Convert.ToString(row["NumericScale"]), out maxSizeScale) && maxSizeScale == 255)
                    {
                        maxSizeScale = 0;
                    }
                    if (!int.TryParse(Convert.ToString(row["NumericPrecision"]), out maxSize) || maxSize == 255)//NumericPrecision
                    {
                        long len;
                        if (long.TryParse(Convert.ToString(row["ColumnSize"]), out len))
                        {
                            if (len > int.MaxValue)
                            {
                                maxSize = int.MaxValue;
                            }
                            else
                            {
                                maxSize = (int)len;
                            }
                        }
                    }
                    MCellStruct mStruct = new MCellStruct(columnName, sqlDbType, isAutoIncrement, isCanNull, maxSize);
                    mStruct.Scale = maxSizeScale;
                    mStruct.IsPrimaryKey = isKey;
                    mStruct.SqlTypeName = sqlTypeName;
                    mStruct.TableName = tableName;
                    mStruct.OldName = mStruct.ColumnName;
                    mStruct.ReaderIndex = i;
                    mdcs.Add(mStruct);

                }
                tableSchema = null;
            }
            return mdcs;
        }
        private static MDataColumn GetViewColumns(string sqlText, ref DbBase helper)
        {
            helper.OpenCon(null, AllowConnLevel.MaterBackupSlave);
            helper.Com.CommandText = sqlText;
            DbDataReader sdr = helper.Com.ExecuteReader(CommandBehavior.KeyInfo);
            DataTable keyDt = null;
            if (sdr != null)
            {
                keyDt = sdr.GetSchemaTable();
                FixTableSchemaType(sdr, keyDt);
                sdr.Close();
            }
            MDataColumn mdc = GetColumns(keyDt);
            mdc.dalType = helper.dalType;
            return mdc;

        }
        private static MDataColumn GetTxtDBViewColumns(string sqlText)
        {
            //sqlText=sqlText.ToLower();
            //List<string> tables = SqlFormat.GetTableNamesFromSql(sqlText);
            ////string key="select "
            //string selectItems=sqlText.Substring("select 
            return null;//暂未实现
        }
        private static readonly object lockGetTables = new object();
        /// <summary>
        /// 获取表（带缓存）
        /// </summary>
        public static Dictionary<string, string> GetTables(ref DbBase helper)
        {
            string key = GetTableCacheKey(helper);
            if (!tableCache.ContainsKey(key))
            {
                lock (lockGetTables)
                {
                    if (!tableCache.ContainsKey(key))
                    {
                        Dictionary<string, string> tables = null;
                        if (!string.IsNullOrEmpty(AppConfig.DB.SchemaMapPath))
                        {
                            string fullPath = AppConfig.RunPath + AppConfig.DB.SchemaMapPath + key + ".ts";
                            if (System.IO.File.Exists(fullPath))
                            {
                                tables = JsonHelper.Split(IOHelper.ReadAllText(fullPath));
                            }
                        }
                        if (tables == null)
                        {
                            helper.IsAllowRecordSql = false;
                            string sql = string.Empty;
                            switch (helper.dalType)
                            {
                                case DalType.MsSql:
                                    sql = GetMSSQLTables(helper.Version.StartsWith("08"));
                                    break;
                                case DalType.Oracle:
                                    sql = GetOracleTables();
                                    break;
                                case DalType.MySql:
                                    sql = GetMySqlTables(helper.DataBase);
                                    break;
                                case DalType.PostgreSQL:
                                    sql = GetPostgreTables(helper.DataBase);
                                    break;
                                case DalType.Txt:
                                case DalType.Xml:
                                    tables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                    // string folder = Path.GetDirectoryName(helper.conn.Split(';')[0].Split('=')[1] + "\\");
                                    string[] files = Directory.GetFiles(helper.Con.DataSource, "*.ts");
                                    foreach (string file in files)
                                    {
                                        tables.Add(Path.GetFileNameWithoutExtension(file), "");
                                    }
                                    files = null;
                                    break;
                                case DalType.Access:
                                case DalType.SQLite:
                                case DalType.Sybase:
                                    #region 用ADO.NET属性拿数据
                                    string restrict = "TABLE";
                                    if (helper.dalType == DalType.Sybase)
                                    {
                                        restrict = "BASE " + restrict;
                                    }
                                    tables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                    helper.Con.Open();
                                    DataTable dt = helper.Con.GetSchema("Tables", new string[] { null, null, null, restrict });
                                    helper.Con.Close();
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
                                            else
                                            {
                                                Log.WriteLogToTxt("Dictionary Has The Same TableName：" + tableName);
                                            }
                                        }
                                        dt = null;
                                    }
                                    #endregion
                                    break;
                            }
                            if (tables == null)
                            {
                                #region 读表到字典中
                                tables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                DbDataReader sdr = helper.ExeDataReader(sql, false);
                                if (sdr != null)
                                {
                                    string tableName = string.Empty;
                                    while (sdr.Read())
                                    {
                                        tableName = Convert.ToString(sdr["TableName"]);
                                        if (!tables.ContainsKey(tableName))
                                        {
                                            if (!tableName.StartsWith("BIN$"))//Oracle的已删除的表。
                                            {
                                                tables.Add(tableName, Convert.ToString(sdr["Description"]));
                                            }
                                        }
                                        else
                                        {
                                            Log.WriteLogToTxt("Dictionary Has The Same TableName：" + tableName);
                                        }
                                    }
                                    sdr.Close();
                                    sdr = null;
                                }
                                #endregion

                                if (!string.IsNullOrEmpty(AppConfig.DB.SchemaMapPath))
                                {
                                    string fullPath = AppConfig.RunPath + AppConfig.DB.SchemaMapPath + key + ".ts";
                                    IOHelper.Save(fullPath, JsonHelper.ToJson(tables), false, true);
                                }
                            }
                        }
                        if (!tableCache.ContainsKey(key) && tables.Count > 0)//读不到表不缓存。
                        {
                            tableCache.Add(key, tables);
                        }
                    }
                }
            }
            if (tableCache.ContainsKey(key))
            {
                return tableCache[key];
            }
            return null;
        }



        #region 表结构处理
        // private static CacheManage _SchemaCache = CacheManage.Instance;//Cache操作
        internal static bool FillTableSchema(ref MDataRow row, ref DbBase dbBase, string tableName, string sourceTableName)
        {
            if (FillSchemaFromCache(ref row, ref dbBase, tableName, sourceTableName))
            {
                return true;
            }
            else//从Cache加载失败
            {
                return FillSchemaFromDb(ref row, ref dbBase, tableName, sourceTableName);
            }
        }

        /// <summary>
        /// 缓存表架构Key
        /// </summary>
        internal static string GetSchemaKey(string tableName, string dbName, DalType dalType)
        {
            string key = tableName;
            int start = key.IndexOf('(');
            int end = key.LastIndexOf(')');
            if (start > -1 && end > -1)//自定义table
            {
                key = "View" + StaticTool.GetHashKey(key);
            }
            else
            {
                if (key.IndexOf('.') > 0)
                {
                    dbName = key.Split('.')[0];
                }
                key = SqlFormat.NotKeyword(key);
            }
            return "ColumnsCache_" + dalType + "_" + dbName + "_" + key;
        }
        private static bool FillSchemaFromCache(ref MDataRow row, ref DbBase dbBase, string tableName, string sourceTableName)
        {
            bool returnResult = false;

            string key = GetSchemaKey(tableName, dbBase.DataBase, dbBase.dalType);
            if (CacheManage.LocalInstance.Contains(key))//缓存里获取
            {
                try
                {
                    row = ((MDataColumn)CacheManage.LocalInstance.Get(key)).ToRow(sourceTableName);
                    returnResult = row.Count > 0;
                }
                catch (Exception err)
                {
                    Log.WriteLogToTxt(err);
                }
            }
            else if (!string.IsNullOrEmpty(AppConfig.DB.SchemaMapPath))
            {
                string fullPath = AppConfig.RunPath + AppConfig.DB.SchemaMapPath + key + ".ts";
                if (System.IO.File.Exists(fullPath))
                {
                    MDataColumn mdcs = MDataColumn.CreateFrom(fullPath);
                    if (mdcs.Count > 0)
                    {
                        row = mdcs.ToRow(sourceTableName);
                        returnResult = row.Count > 0;
                        CacheManage.LocalInstance.Set(key, mdcs.Clone(), 1440);
                    }
                }
            }

            return returnResult;
        }
        private static bool FillSchemaFromDb(ref MDataRow row, ref DbBase dbBase, string tableName, string sourceTableName)
        {
            try
            {
                MDataColumn mdcs = null;
                //if (tableName.IndexOf('(') > -1 && tableName.IndexOf(')') > -1)//自定义视图table
                //{
                //    dbBase.tempSql = "view";//使用access方式加载列
                //}
                mdcs = GetColumns(tableName, ref dbBase);
                if (dbBase != null && dbBase.Con != null && !dbBase.isOpenTrans && dbBase.Con.State != ConnectionState.Closed)
                {
                    dbBase.Con.Close();//非事务下，链接先关闭
                }
                if (mdcs.Count == 0)
                {
                    return false;
                }
                row = mdcs.ToRow(sourceTableName);
                row.TableName = sourceTableName;
                string key = GetSchemaKey(tableName, dbBase.DataBase, dbBase.dalType);
                CacheManage.LocalInstance.Set(key, mdcs.Clone(), 1440);

                switch (dbBase.dalType)//文本数据库不保存。
                {
                    //case DalType.Access:
                    //case DalType.SQLite:
                    //case DalType.MsSql:
                    //case DalType.MySql:
                    //case DalType.Oracle:

                    case DalType.Txt:
                    case DalType.Xml:
                        break;
                    default:
                        if (!string.IsNullOrEmpty(AppConfig.DB.SchemaMapPath))
                        {
                            string folderPath = AppConfig.RunPath + AppConfig.DB.SchemaMapPath;
                            if (System.IO.Directory.Exists(folderPath))
                            {
                                mdcs.WriteSchema(folderPath + key + ".ts");
                            }
                        }
                        break;
                }
                return true;

            }
            catch (Exception err)
            {
                Log.WriteLogToTxt(err);
                return false;
            }
        }
        #endregion

        /// <summary>
        /// 是否存在表或视图
        /// </summary>
        /// <param name="type">"U"或"V"</param>
        /// <param name="name">表名或视图名，允许database.tableName</param>
        public static bool Exists(string type, string name, ref DbBase helper)
        {
            if (type == "U" && tableCache.Count > 0)
            {
                string key = GetTableCacheKey(helper);
                if (tableCache.ContainsKey(key))
                {
                    return tableCache[key].ContainsKey(name);
                }
            }
            int result = 0;
            string exist = string.Empty;
            helper.IsAllowRecordSql = false;
            DalType dalType = helper.dalType;
            name = SqlFormat.Keyword(name, helper.dalType);
            switch (dalType)
            {
                case DalType.Access:
                    try
                    {
                        System.Data.OleDb.OleDbConnection con = new System.Data.OleDb.OleDbConnection(helper.Con.ConnectionString);
                        con.Open();
                        DataTable dt = null;
                        if (type == "U")
                        {
                            dt = con.GetOleDbSchemaTable(System.Data.OleDb.OleDbSchemaGuid.Tables, new object[] { null, null, SqlFormat.NotKeyword(name), "Table" });
                        }
                        else if (type == "V")
                        {
                            dt = con.GetOleDbSchemaTable(System.Data.OleDb.OleDbSchemaGuid.Views, new object[] { null, null, SqlFormat.NotKeyword(name) });
                        }
                        if (dt != null)
                        {
                            result = dt.Rows.Count;
                        }
                        con.Close();
                    }
                    catch (Exception err)
                    {
                        Log.WriteLogToTxt(err);
                    }
                    break;
                case DalType.MySql:
                    if (type != "V" || (type == "V" && name.ToLower().StartsWith("v_")))//视图必须v_开头
                    {
                        exist = string.Format(ExistMySql, SqlFormat.NotKeyword(name), helper.DataBase);
                    }
                    break;
                case DalType.Oracle:
                    exist = string.Format(ExistOracle, (type == "U" ? "TABLE" : "VIEW"), name);
                    break;
                case DalType.MsSql:
                    exist = string.Format(helper.Version.StartsWith("08") ? Exist2000 : Exist2005, name, type);
                    break;
                case DalType.SQLite:
                    exist = string.Format(ExistSqlite, (type == "U" ? "table" : "view"), SqlFormat.NotKeyword(name));
                    break;
                case DalType.Sybase:
                    exist = string.Format(ExistSybase, SqlFormat.NotKeyword(name), type);
                    break;
                case DalType.Txt:
                case DalType.Xml:
                    string folder = helper.Con.DataSource + Path.GetFileNameWithoutExtension(name);
                    FileInfo info = new FileInfo(folder + ".ts");
                    result = (info.Exists && info.Length > 10) ? 1 : 0;
                    if (result == 0)
                    {
                        info = new FileInfo(folder + (dalType == DalType.Txt ? ".txt" : ".xml"));
                        result = (info.Exists && info.Length > 10) ? 1 : 0;
                    }
                    break;
            }
            if (exist != string.Empty)
            {
                helper.IsAllowRecordSql = false;
                result = Convert.ToInt32(helper.ExeScalar(exist, false));
            }
            return result > 0;
        }
        /// <summary>
        /// 获得表的描述
        /// </summary>
        internal static string GetTableDescription(string conn, string tableName)
        {
            using (DbBase dbBase = DalCreate.CreateDal(conn))
            {
                string key = GetTableCacheKey(dbBase);
                if (tableCache.ContainsKey(key))
                {
                    if (tableCache[key].ContainsKey(tableName))
                    {
                        return tableCache[key][tableName];
                    }
                }
                return string.Empty;
            }
        }

        #region ICloneable 成员


        #endregion
    }
    internal partial class TableSchema
    {
        internal const string Exist2000 = "SELECT count(*) FROM sysobjects where id = OBJECT_ID(N'{0}') AND xtype in (N'{1}')";
        internal const string Exist2005 = "SELECT count(*) FROM sys.objects where object_id = OBJECT_ID(N'{0}') AND type in (N'{1}')";
        internal const string ExistOracle = "Select count(*)  From user_objects where object_type='{0}' and object_name=upper('{1}')";
        internal const string ExistMySql = "SELECT count(*)  FROM  `information_schema`.`COLUMNS`  where TABLE_NAME='{0}' and TABLE_SCHEMA='{1}'";
        internal const string ExistSybase = "SELECT count(*) FROM sysobjects where id = OBJECT_ID(N'{0}') AND type in (N'{1}')";
        internal const string ExistSqlite = "SELECT count(*) FROM sqlite_master where type='{0}' and name='{1}'";
        internal const string ExistPostgre = "SELECT count(*) FROM information_schema.tables where table_schema = 'public' and table_name='{0}'";
        internal const string ExistOracleSequence = "SELECT count(*) FROM All_Sequences where Sequence_name='{0}'";
        internal const string CreateOracleSequence = "create sequence {0} start with {1} increment by 1";
        internal const string GetOracleMaxID = "select max({0}) from {1}";

        #region 获取数据库表的列字段
        private const string MSSQL_SynonymsName = "SELECT TOP 1 base_object_name from sys.synonyms WHERE NAME = '{0}'";
        private const string Oracle_SynonymsName = "SELECT TABLE_NAME FROM USER_SYNONYMS WHERE SYNONYM_NAME='{0}' and rownum=1";
        internal static string GetMSSQLColumns(bool for2000, string dbName)
        {
            // 2005以上增加同义词支持。 case s2.name WHEN 'timestamp' THEN 'variant' ELSE s2.name END as [SqlType],
            return string.Format(@"select s1.name as ColumnName,case s2.name WHEN 'uniqueidentifier' THEN 36 
                     WHEN 'ntext' THEN -1 WHEN 'text' THEN -1 WHEN 'image' THEN -1 else s1.[prec] end  as [MaxSize],s1.scale as [Scale],
                     isnullable as [IsNullable],colstat&1 as [IsAutoIncrement],s2.name as [SqlType],
                     case when exists(SELECT 1 FROM {0}..sysobjects where xtype='PK' and name in (SELECT name FROM {0}..sysindexes WHERE id=s1.id and 
                     indid in(SELECT indid FROM {0}..sysindexkeys WHERE id=s1.id AND colid=s1.colid))) then 1 else 0 end as [IsPrimaryKey],
                     case when exists(SELECT 1 FROM {0}..sysobjects where xtype='UQ' and name in (SELECT name FROM {0}..sysindexes WHERE id=s1.id and 
                     indid in(SELECT indid FROM {0}..sysindexkeys WHERE id=s1.id AND colid=s1.colid))) then 1 else 0 end as [IsUniqueKey],
                     case when s5.rkey=s1.colid or s5.fkey=s1.colid then 1 else 0 end as [IsForeignKey],
                     case when s5.fkey=s1.colid then object_name(s5.rkeyid) else null end [FKTableName],
                     isnull(s3.text,'') as [DefaultValue],
                     s4.value as Description
                     from {0}..syscolumns s1 right join {0}..systypes s2 on s2.xtype =s1.xtype  
                     left join {0}..syscomments s3 on s1.cdefault=s3.id  " +
                     (for2000 ? "left join {0}..sysproperties s4 on s4.id=s1.id and s4.smallid=s1.colid  " : "left join {0}.sys.extended_properties s4 on s4.major_id=s1.id and s4.minor_id=s1.colid")
                     + " left join {0}..sysforeignkeys s5 on (s5.rkeyid=s1.id and s5.rkey=s1.colid) or (s5.fkeyid=s1.id and s5.fkey=s1.colid) where s1.id=object_id('{0}..'+@TableName) and s2.name<>'sysname' and s2.usertype<100 order by s1.colid", "[" + dbName + "]");
        }
        internal static string GetOracleColumns()
        {
            //同义词已被提取到外面执行，外部对表名已转大写，对语句进行了优化。
            return @"select A.COLUMN_NAME as ColumnName,case DATA_TYPE when 'DATE' then 23 when 'CLOB' then 2147483647 when 'NCLOB' then 1073741823 else case when CHAR_COL_DECL_LENGTH is not null then CHAR_COL_DECL_LENGTH
                    else   case when DATA_PRECISION is not null then DATA_PRECISION else DATA_LENGTH end   end end as MaxSize,DATA_SCALE as Scale,
                    case NULLABLE when 'Y' then 1 else 0 end as IsNullable,
                    0 as IsAutoIncrement,
                    case  when (DATA_TYPE='NUMBER' and DATA_SCALE>0 and DATA_PRECISION<13)  then 'float'
                      when (DATA_TYPE='NUMBER' and DATA_SCALE>0 and DATA_PRECISION<22)  then 'double'
                        when (DATA_TYPE='NUMBER' and DATA_SCALE=0 and DATA_PRECISION<11)  then 'int'
                          when (DATA_TYPE='NUMBER' and DATA_SCALE=0 and DATA_PRECISION<20)  then 'long'
                                when DATA_TYPE='NUMBER' then'decimal'                   
                    else DATA_TYPE end as SqlType,
                    case when v.CONSTRAINT_TYPE='P' then 1 else 0 end as IsPrimaryKey,
                      case when v.CONSTRAINT_TYPE='U' then 1 else 0 end as IsUniqueKey,
                        case when v.CONSTRAINT_TYPE='R' then 1 else 0 end as IsForeignKey,
                         case when length(r_constraint_name)>1
                         then (select table_name from user_constraints s where s.constraint_name=v.r_constraint_name)
                            else null
                              end as FKTableName ,
                    data_default as DefaultValue,
                    COMMENTS AS Description
                    from USER_TAB_COLS A left join user_col_comments B on A.Table_Name = B.Table_Name and A.Column_Name = B.Column_Name 
                    left join
                    (select uc1.table_name,ucc.column_name, uc1.constraint_type,uc1.r_constraint_name from user_constraints uc1
                    left join (SELECT column_name,constraint_name FROM user_cons_columns WHERE TABLE_NAME=:TableName) ucc on ucc.constraint_name=uc1.constraint_name
                    where uc1.constraint_type in('P','U','R') ) v
                    on A.TABLE_NAME=v.table_name and A.COLUMN_NAME=v.column_name
                    where A.TABLE_NAME=:TableName order by COLUMN_ID";
            //            left join  user_constraints uc2 on uc1.r_constraint_name=uc2.constraint_name
            // where A.TABLE_NAME= nvl((SELECT TABLE_NAME FROM USER_SYNONYMS WHERE SYNONYM_NAME=UPPER(:TableName) and rownum=1),UPPER(:TableName)) order by COLUMN_ID";
        }
        internal static string GetMySqlColumns(string dbName)
        {
            return string.Format(@"SELECT DISTINCT s1.COLUMN_NAME as ColumnName,case DATA_TYPE when 'int' then 10 when 'date' then 10 when 'time' then 8  when 'datetime' then 23 when 'year' then 4
                    else IFNULL(CHARACTER_MAXIMUM_LENGTH,NUMERIC_PRECISION) end as MaxSize,NUMERIC_SCALE as Scale,
                    case IS_NULLABLE when 'YES' then 1 else 0 end as IsNullable,
                    CASE extra when 'auto_increment' then 1 else 0 END AS IsAutoIncrement,
                    DATA_TYPE as SqlType,
                    case Column_key WHEN 'PRI' then 1 else 0 end as IsPrimaryKey,
                    case s3.CONSTRAINT_TYPE when 'UNIQUE' then 1 else 0 end as IsUniqueKey,
					case s3.CONSTRAINT_TYPE when 'FOREIGN KEY' then 1 else 0 end as IsForeignKey,
					s2.REFERENCED_TABLE_NAME as FKTableName,
                    COLUMN_DEFAULT AS DefaultValue,
                    COLUMN_COMMENT AS Description
                    FROM  `information_schema`.`COLUMNS` s1
					LEFT JOIN `information_schema`.KEY_COLUMN_USAGE s2 on s2.TABLE_SCHEMA=s1.TABLE_SCHEMA and s2.TABLE_NAME=s1.TABLE_NAME and s2.COLUMN_NAME=s1.COLUMN_NAME
					LEFT JOIN `information_schema`.TABLE_CONSTRAINTS s3 on s3.TABLE_SCHEMA=s2.TABLE_SCHEMA and s3.TABLE_NAME=s2.TABLE_NAME and s3.CONSTRAINT_NAME=s2.CONSTRAINT_NAME
                    where s1.TABLE_SCHEMA='{0}' and  s1.TABLE_NAME=?TableName order by s1.ORDINAL_POSITION", dbName);
        }
        internal static string GetSybaseColumns()
        {
            return @"select 
s1.name as ColumnName,
s1.length as MaxSize,
s1.scale as Scale,
case s1.status&8 when 8 then 1 ELSE 0 END AS IsNullable,
case s1.status&128 when 128 then 1 ELSE 0 END as IsAutoIncrement,
s2.name as SqlType,
case when exists(SELECT 1 FROM sysindexes WHERE id=s1.id AND s1.name=index_col(@TableName,indid,s1.colid)) then 1 else 0 end as IsPrimaryKey,
               str_replace(s3.text,'DEFAULT  ',null) as DefaultValue,
               null as Description
from syscolumns s1 left join systypes s2 on s1.usertype=s2.usertype
left join syscomments s3 on s1.cdefault=s3.id
where s1.id =object_id(@TableName) and s2.usertype<100
order by s1.colid";
        }
        internal static string GetPostgreColumns()
        {
            return @"select
a.attname AS ColumnName,
case t.typname when 'int4' then 'int' when 'int8' then 'bigint' else t.typname end AS SqlType,
coalesce(character_maximum_length,numeric_precision,-1) as MaxSize,numeric_scale as Scale,
case a.attnotnull when 'true' then 0 else 1 end AS IsNullable,
case  when position('nextval' in column_default)>0 then 1 else 0 end as IsAutoIncrement, 
case when o.conname is null then 0 else 1 end as IsPrimaryKey,
d.description AS Description,
i.column_default as DefaultValue
from pg_class c 
left join pg_attribute a on c.oid=a.attrelid
left join pg_description d on a.attrelid=d.objoid AND a.attnum = d.objsubid
left join pg_type t on a.atttypid = t.oid
left join information_schema.columns i on i.table_schema='public' and i.table_name=c.relname and i.column_name=a.attname
left join pg_constraint o on a.attnum = o.conkey[1] and o.contype='p'
where c.relname =:TableName
and a.attnum > 0 and a.atttypid>0
ORDER BY a.attnum";
        }
        
       
        #endregion

        #region 读取所有表语句
        internal static string GetMSSQLTables(bool for2000)
        {
            return @"Select o.name as TableName, p.value as Description from sysobjects o " + (for2000 ? "left join sysproperties p on p.id = o.id and smallid = 0" : "left join sys.extended_properties p on p.major_id = o.id and minor_id = 0")
                + " and p.name = 'MS_Description' where o.type = 'U' AND o.name<>'dtproperties' AND o.name<>'sysdiagrams'" + (for2000 ? "" : " and category=0");
        }
        internal static string GetOracleTables()
        {
            return "select TABLE_NAME AS TableName,COMMENTS AS Description from user_tab_comments where TABLE_TYPE='TABLE'";
        }
        internal static string GetMySqlTables(string dbName)
        {
            return string.Format("select TABLE_NAME as TableName,TABLE_COMMENT as Description from `information_schema`.`TABLES`  where TABLE_SCHEMA='{0}'", dbName);
        }
        internal static string GetPostgreTables(string dbName)
        {
            return string.Format("select table_name as TableName,cast(obj_description(relfilenode,'pg_class') as varchar) as Description from information_schema.tables t left join  pg_class p on t.table_name=p.relname  where table_schema='public' and table_catalog='{0}'", dbName);
        }
        #endregion
    }
}
