﻿using CYQ.Data.Orm;
using CYQ.Data.Table;
using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;
using System.Reflection;

namespace CYQ.Data.SQL
{
    /// <summary>
    /// 管理表（字段结构）
    /// </summary>
    internal partial class TableSchema
    {
        /// <summary>
        /// 全局缓存实体类的表结构（仅会对Type读取的结构）
        /// </summary>
        private static MDictionary<string, MDataColumn> _ColumnCache = new MDictionary<string, MDataColumn>(StringComparer.OrdinalIgnoreCase);

        public static MDataColumn GetColumns(string tableName, string conn)
        {
            return GetColumns(tableName, conn, false);
        }
        public static MDataColumn GetColumns(string tableName, string conn, bool isIgnoreCache)
        {
            //先修正Conn，否则之前的key和修正后的key对不上。
            string fixName;
            conn = CrossDB.GetConn(tableName, out fixName, conn ?? AppConfig.DB.DefaultConn);
            tableName = fixName;
            if (conn == null)
            {
                return null;
            }
            string key = GetSchemaKey(tableName, conn);

            #region 缓存检测
            if (_ColumnCache.ContainsKey(key))
            {
                if (isIgnoreCache)
                {
                    _ColumnCache.Remove(key);
                }
                else
                {
                    return _ColumnCache[key].Clone();
                }
            }

            if (!isIgnoreCache)
            {
                if (!string.IsNullOrEmpty(AppConfig.DB.SchemaMapPath))
                {
                    string fullPath = GetSchemaFile(key);
                    if (System.IO.File.Exists(fullPath))
                    {
                        MDataColumn columns = MDataColumn.CreateFrom(fullPath);
                        if (columns.Count > 0)
                        {
                            _ColumnCache.Add(key, columns.Clone());
                            return columns;
                        }
                    }
                }
            }
            #endregion

            MDataColumn mdcs = null;
            bool isTxtDB = false;
            using (DalBase dbHelper = DalCreate.CreateDal(conn))
            {
                DataBaseType dalType = dbHelper.DataBaseType;
                isTxtDB = dalType == DataBaseType.Txt || dalType == DataBaseType.Xml;
                if (isTxtDB)
                {
                    #region 文本数据库处理。
                    if (!tableName.Contains(" "))// || tableName.IndexOfAny(Path.GetInvalidPathChars()) == -1
                    {
                        tableName = SqlFormat.NotKeyword(tableName);//处理database..tableName;
                        tableName = Path.GetFileNameWithoutExtension(tableName);//视图表，带“.“的，会出问题
                        string fileName = dbHelper.Con.DataSource + tableName + (dalType == DataBaseType.Txt ? ".txt" : ".xml");
                        mdcs = MDataColumn.CreateFrom(fileName);
                        mdcs.DataBaseType = dalType;
                        mdcs.DataBaseVersion = dbHelper.Version;
                        mdcs.Conn = conn;
                    }
                    #endregion
                }
                else
                {
                    #region 其它数据库
                    mdcs = new MDataColumn();
                    mdcs.Conn = conn;
                    mdcs.TableName = tableName;
                    mdcs.DataBaseType = dalType;
                    mdcs.DataBaseVersion = dbHelper.Version;

                    tableName = SqlFormat.Keyword(tableName, dbHelper.DataBaseType);//加上关键字：引号，并转大小写。
                    string noKeywordTableName = SqlFormat.NotKeyword(tableName);//进行去关键字。
                    //如果table和helper不在同一个库
                    DalBase helper = dbHelper.ResetDalBase(tableName);

                    helper.IsRecordDebugInfo = false || AppDebug.IsContainSysSql;//内部系统，不记录SQL表结构语句。
                    try
                    {
                        bool isView = tableName.Contains(" ");//是否视图。
                        if (!isView)
                        {
                            isView = CrossDB.Exists(noKeywordTableName, "V", conn);
                        }
                        if (!isView)
                        {
                            TableInfo info = CrossDB.GetTableInfoByName(noKeywordTableName, conn);//先查缓存
                            if (info != null)
                            {
                                mdcs.Description = info.Description;
                            }
                            else
                            {
                                Dictionary<string, string> tables = helper.GetTables();
                                if (tables.ContainsKey(noKeywordTableName))
                                {
                                    mdcs.Description = tables[noKeywordTableName];
                                }
                            }
                        }
                        MCellStruct mStruct = null;
                        SqlDbType sqlType = SqlDbType.NVarChar;
                        if (isView)
                        {
                            string sqlText = SqlFormat.BuildSqlWithWhereOneEqualsTow(tableName);// string.Format("select * from {0} where 1=2", tableName);
                            mdcs = GetViewColumns(sqlText, ref helper);
                            if (mdcs != null && mdcs.Count > 0)
                            {
                                #region  处理字段描述
                                Dictionary<string, MDataColumn> dic = new Dictionary<string, MDataColumn>();
                                MDataColumn temp;

                                for (int i = 0; i < mdcs.Count; i++)
                                {
                                    MCellStruct cell = mdcs[i];
                                    if (string.IsNullOrEmpty(cell.Description) && !string.IsNullOrEmpty(cell.TableName) && string.Compare(cell.TableName, noKeywordTableName, true) != 0)
                                    {
                                        if (!dic.ContainsKey(cell.TableName))
                                        {
                                            temp = GetColumns(cell.TableName, conn);
                                            dic.Add(cell.TableName, temp);
                                        }
                                        else
                                        {
                                            temp = dic[cell.TableName];
                                        }
                                        if (temp != null && temp.Contains(cell.ColumnName))
                                        {
                                            cell.Description = temp[cell.ColumnName].Description;
                                        }
                                    }
                                }
                                dic.Clear();
                                dic = null;
                                #endregion
                            }
                        }
                        else
                        {
                            mdcs.AddRelateionTableName(noKeywordTableName);
                            switch (dalType)
                            {
                                case DataBaseType.MsSql:
                                case DataBaseType.Oracle:
                                case DataBaseType.MySql:
                                case DataBaseType.Sybase:
                                case DataBaseType.PostgreSQL:
                                case DataBaseType.DB2:
                                case DataBaseType.FireBird:
                                case DataBaseType.DaMeng:
                                case DataBaseType.KingBaseES:
                                    #region Sql
                                    string sql = string.Empty;
                                    if (dalType == DataBaseType.MsSql)
                                    {
                                        #region Mssql
                                        string dbName = null;
                                        if (!helper.Version.StartsWith("08"))
                                        {
                                            //先获取同义词，检测是否跨库
                                            string realTableName = Convert.ToString(helper.ExeScalar(string.Format(MSSQL_SynonymsName, noKeywordTableName), false));
                                            if (!string.IsNullOrEmpty(realTableName))
                                            {
                                                string[] items = realTableName.Split('.');
                                                noKeywordTableName = realTableName;
                                                if (items.Length > 0)//跨库了
                                                {
                                                    dbName = realTableName.Split('.')[0];
                                                }
                                            }
                                        }

                                        sql = GetMSSQLColumns(helper.Version.StartsWith("08"), dbName ?? helper.DataBaseName);
                                        #endregion
                                    }
                                    else if (dalType == DataBaseType.MySql)
                                    {
                                        sql = GetMySqlColumns(helper.DataBaseName);
                                    }
                                    else if (dalType == DataBaseType.Oracle)
                                    {
                                        //先获取同义词，不检测是否跨库
                                        string realTableName = Convert.ToString(helper.ExeScalar(string.Format(Oracle_SynonymsName, noKeywordTableName), false));
                                        if (!string.IsNullOrEmpty(realTableName))
                                        {
                                            noKeywordTableName = realTableName;
                                        }
                                        sql = GetOracleColumns();
                                    }
                                    else if (dalType == DataBaseType.Sybase)
                                    {
                                       // tableName = SqlFormat.NotKeyword(tableName);
                                        sql = GetSybaseColumns();
                                    }
                                    else if (dalType == DataBaseType.PostgreSQL)
                                    {
                                        sql = GetPostgreColumns(float.Parse(dbHelper.Version));
                                    }
                                    else if (dalType == DataBaseType.DB2)
                                    {
                                        //tableName = SqlFormat.NotKeyword(tableName).ToUpper();
                                        sql = GetDB2Columns();
                                    }
                                    else if (dalType == DataBaseType.FireBird)
                                    {
                                        //tableName = SqlFormat.NotKeyword(tableName).ToUpper();
                                        sql = GetFireBirdColumns();
                                    }
                                    else if (dalType == DataBaseType.DaMeng)
                                    {
                                        //tableName = SqlFormat.NotKeyword(tableName).ToUpper();
                                        sql = GetDaMengColumns(helper.SchemaName);
                                    }
                                    else if (dalType == DataBaseType.KingBaseES)
                                    {
                                        //tableName = SqlFormat.NotKeyword(tableName).ToUpper();
                                        sql = GetKingBaseESColumns(helper.SchemaName);
                                    }
                                    helper.AddParameters("TableName", noKeywordTableName, DbType.String, 150, ParameterDirection.Input);
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
                                            sqlTypeName = Convert.ToString(sdr["SqlType"]).Trim();
                                            sqlType = DataType.GetSqlType(sqlTypeName);
                                            isAutoIncrement = Convert.ToBoolean(sdr["IsAutoIncrement"]);
                                            mStruct = new MCellStruct(mdcs.DataBaseType);
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
                                                case DataBaseType.MsSql:
                                                case DataBaseType.MySql:
                                                case DataBaseType.Oracle:
                                                    mStruct.IsUniqueKey = Convert.ToString(sdr["IsUniqueKey"]) == "1";
                                                    mStruct.IsForeignKey = Convert.ToString(sdr["IsForeignKey"]) == "1";
                                                    mStruct.FKTableName = Convert.ToString(sdr["FKTableName"]);
                                                    break;
                                                case DataBaseType.FireBird:
                                                case DataBaseType.DaMeng:
                                                case DataBaseType.KingBaseES:
                                                    mStruct.IsUniqueKey = Convert.ToString(sdr["IsUniqueKey"]) == "1";
                                                    break;
                                            }

                                            mStruct.SqlTypeName = sqlTypeName;
                                            mStruct.TableName = noKeywordTableName;
                                            mdcs.Add(mStruct);
                                        }
                                        sdr.Close();
                                        if (dalType == DataBaseType.Oracle && mdcs.Count > 0)//默认没有自增概念，只能根据情况判断。
                                        {
                                            MCellStruct firstColumn = mdcs[0];
                                            if (firstColumn.IsPrimaryKey && firstColumn.ColumnName.ToLower().Contains("id") && firstColumn.Scale == 0 && DataType.GetGroup(firstColumn.SqlType) == DataGroupType.Number && mdcs.JointPrimary.Count == 1)
                                            {
                                                firstColumn.IsAutoIncrement = true;
                                            }
                                        }
                                    }
                                    #endregion
                                    break;
                                case DataBaseType.SQLite:
                                    #region SQlite
                                    if (helper.Con.State != ConnectionState.Open)
                                    {
                                        helper.Con.Open();
                                    }
                                    DataTable sqliteDt = helper.Con.GetSchema("Columns", new string[] { null, null, noKeywordTableName });
                                    if (!helper.IsOpenTrans)
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
                                        mStruct.TableName = noKeywordTableName;
                                        mdcs.Add(mStruct);
                                    }
                                    #endregion
                                    break;
                                case DataBaseType.Access:
                                case DataBaseType.Excel:
                                case DataBaseType.FoxPro:
                                    #region Access
                                    DataTable keyDt, valueDt;
                                    string sqlText = SqlFormat.BuildSqlWithWhereOneEqualsTow(tableName);// string.Format("select * from {0} where 1=2", tableName);
                                    OleDbConnection con = new OleDbConnection(helper.Con.ConnectionString);
                                    OleDbCommand com = new OleDbCommand(sqlText, con);
                                    con.Open();
                                    keyDt = com.ExecuteReader(CommandBehavior.KeyInfo).GetSchemaTable();
                                    valueDt = con.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, new object[] { null, null, noKeywordTableName });
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
                                            mStruct.TableName = noKeywordTableName;
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
                    }
                    catch (Exception err)
                    {
                        Log.Write(err, LogType.DataBase);
                        //helper.DebugInfo.Append(err.Message);
                    }
                    #endregion
                }
            }
            if (mdcs != null && mdcs.Count > 0)
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
                #region 缓存设置

                if (mdcs.Count > 0)
                {
                    if (!isTxtDB && !string.IsNullOrEmpty(AppConfig.DB.SchemaMapPath))
                    {
                        string path = GetSchemaFile(key);
                        mdcs.WriteSchema(path);
                    }
                    else if (!_ColumnCache.ContainsKey(key))
                    {
                        _ColumnCache.Add(key, mdcs.Clone());
                    }
                }
                #endregion
            }

            return mdcs;
        }

        private static MDataColumn GetViewColumns(string sqlText, ref DalBase helper)
        {
            MDataColumn mdc = null;

            helper.OpenCon(null, AllowConnLevel.MaterBackupSlave);
            helper.Com.CommandText = sqlText;
            DbDataReader sdr = helper.Com.ExecuteReader(CommandBehavior.KeyInfo);
            DataTable keyDt = null;
            if (sdr != null)
            {
                keyDt = sdr.GetSchemaTable();
                mdc = GetColumnByTable(keyDt, sdr, true);
                mdc.DataBaseType = helper.DataBaseType;
                mdc.DataBaseVersion = helper.Version;
            }

            return mdc;

        }
        /// <summary>
        /// 清空对应表的缓存
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="conn"></param>
        public static void Remove(string tableName, string conn)
        {
            string key = GetSchemaKey(tableName, conn);
            Remove(key);
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        /// <param name="key"></param>
        public static void Remove(string key)
        {
            _ColumnCache.Remove(key);
            if (!string.IsNullOrEmpty(AppConfig.DB.SchemaMapPath))
            {
                string path = GetSchemaFile(key);
                IOHelper.Delete(path);
            }
        }
        public static void Clear()
        {
            _ColumnCache.Clear();
        }
    }
    internal partial class TableSchema
    {
        #region 表结构处理
        // private static CacheManage _SchemaCache = CacheManage.Instance;//Cache操作
        internal static bool FillTableSchema(ref MDataRow row, string tableName, string sourceTableName)
        {
            MDataColumn mdcs = GetColumns(tableName, row.Conn);
            if (mdcs == null || mdcs.Count == 0)
            {
                return false;
            }
            row = mdcs.ToRow(sourceTableName);
            return true;
        }

        /// <summary>
        /// 获取表结构Key：*.txt
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static string GetSchemaFile(string key)
        {
            string folderPath = AppConfig.WebRootPath + AppConfig.DB.SchemaMapPath.Trim('/', '\\');

            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }
            return folderPath + "/" + key + ".ts";
        }
        /// <summary>
        /// 缓存表架构Key
        /// </summary>
        internal static string GetSchemaKey(string tableName, string conn)
        {
            tableName = SqlFormat.NotKeyword(tableName);//xxx..[TTTT]
            if (string.IsNullOrEmpty(conn))
            {
                conn = CrossDB.GetConn(tableName, out tableName, conn);//[TTTT]
            }
            tableName = SqlFormat.NotKeyword(tableName);
            return ConnBean.GetHashKey(conn) + "/" + GetKey(tableName) + "_" + TableInfo.GetHashKey(tableName);
        }
        private static string GetKey(string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) { return tableName; }
            int i = tableName.LastIndexOf(')');
            string pre = "T_";
            if (i > 0)
            {
                tableName = tableName.Substring(i + 1);
                pre = "S_";
            }
            if (tableName.StartsWith("V_"))
            {
                pre = "V_";
            }
            else if (tableName.StartsWith("P_"))
            {
                pre = "P_";
            }
            tableName = pre + tableName.Replace("-", "").Replace("_", "").Replace(".", "").Replace("*", "").Replace(" ", "");
            //if (i > 0)
            //{
            //    tableName = "S_" + tableName;//sql
            //}
            //else
            //{
            //    tableName = "T_" + tableName;//table
            //}
            if (tableName.Length > 30)
            {
                return tableName.Substring(0, 30);
            }
            return tableName;
        }
        //    private static bool FillSchemaFromCache(ref MDataRow row, string tableName, string sourceTableName)
        //    {
        //        bool returnResult = false;

        //        string key = GetSchemaKey(tableName, row.Conn);
        //        if (CacheManage.LocalInstance.Contains(key))//缓存里获取
        //        {
        //            try
        //            {
        //                row = ((MDataColumn)CacheManage.LocalInstance.Get(key)).ToRow(sourceTableName);
        //                returnResult = row.Count > 0;
        //            }
        //            catch (Exception err)
        //            {
        //                Log.Write(err, LogType.DataBase);
        //            }
        //        }
        //        else if (!string.IsNullOrEmpty(AppConfig.DB.SchemaMapPath))
        //        {
        //            string fullPath = AppConfig.RunPath + AppConfig.DB.SchemaMapPath + key + ".ts";
        //            if (System.IO.File.Exists(fullPath))
        //            {
        //                MDataColumn mdcs = MDataColumn.CreateFrom(fullPath);
        //                if (mdcs.Count > 0)
        //                {
        //                    row = mdcs.ToRow(sourceTableName);
        //                    returnResult = row.Count > 0;
        //                    CacheManage.LocalInstance.Set(key, mdcs.Clone(), 1440);
        //                }
        //            }
        //        }

        //        return returnResult;
        //    }
        //    private static bool FillSchemaFromDb(ref MDataRow row, string tableName, string sourceTableName)
        //    {
        //        try
        //        {
        //            MDataColumn mdcs = TableSchema.GetColumns(tableName, row.Conn);
        //            if (mdcs == null || mdcs.Count == 0)
        //            {
        //                return false;
        //            }
        //            row = mdcs.ToRow(sourceTableName);
        //            string key = GetSchemaKey(tableName, mdcs.Conn);
        //            CacheManage.LocalInstance.Set(key, mdcs.Clone(), 1440);
        //            if (!string.IsNullOrEmpty(AppConfig.DB.SchemaMapPath))
        //            {
        //                string folderPath = AppConfig.RunPath + AppConfig.DB.SchemaMapPath;

        //                if (!System.IO.Directory.Exists(folderPath))
        //                {
        //                    System.IO.Directory.CreateDirectory(folderPath);
        //                }
        //                mdcs.WriteSchema(folderPath + key + ".ts");
        //            }
        //            return true;

        //        }
        //        catch (Exception err)
        //        {
        //            Log.Write(err, LogType.DataBase);
        //            return false;
        //        }
        //    }
        #endregion
    }
    /// <summary>
    /// 从实体转成列
    /// </summary>
    internal partial class TableSchema
    {
        public static MDataColumn GetColumnByType(Type typeInfo)
        {
            return GetColumnByType(typeInfo, null);
        }
        public static MDataColumn GetColumnByType(Type typeInfo, string conn)
        {
            if (typeInfo == null) { return null; }
            string key = "ColumnsCache_" + typeInfo.FullName;

            if (_ColumnCache.ContainsKey(key))
            {
                return _ColumnCache[key].Clone();
            }
            else
            {

                MDataColumn mdc = GetColumns(typeInfo);
                if (!_ColumnCache.ContainsKey(key))
                {
                    _ColumnCache.Set(key, mdc);
                }
                string outConn;
                string tableName = DBFast.GetTableName(typeInfo, out outConn);
                if (!string.IsNullOrEmpty(tableName) && (!string.IsNullOrEmpty(conn) || !string.IsNullOrEmpty(outConn)))
                {
                    key = GetSchemaKey(tableName, outConn ?? conn);
                    //如果是刚创建表的情况，存档多一份
                    if (!_ColumnCache.ContainsKey(key))
                    {
                        _ColumnCache.Set(key, mdc);
                    }
                }
                return mdc.Clone();
            }

        }
        private static MDataColumn GetColumns(Type typeInfo)
        {

            MDataColumn mdc = new MDataColumn();
            mdc.TableName = typeInfo.Name;
            SysType st = ReflectTool.GetSystemType(ref typeInfo);
            switch (st)
            {
                case SysType.Base:
                case SysType.Enum:
                    mdc.Add(typeInfo.Name, DataType.GetSqlType(typeInfo), false);
                    return mdc;
                case SysType.Generic:
                case SysType.Collection:
                    Type[] argTypes;
                    Tool.ReflectTool.GetArgumentLength(ref typeInfo, out argTypes);
                    if (argTypes.Length == 2)
                    {
                        if (st == SysType.Collection)
                        {
                            mdc.Add("Name", DataType.GetSqlType(argTypes[0]), false);
                            mdc.Add("Value", DataType.GetSqlType(argTypes[1]), false);
                        }
                        else
                        {
                            mdc.Add("Key", DataType.GetSqlType(argTypes[0]), false);
                            mdc.Add("Value", DataType.GetSqlType(argTypes[1]), false);
                        }
                    }
                    else
                    {
                        foreach (Type type in argTypes)
                        {
                            mdc.Add(type.Name, DataType.GetSqlType(type), false);
                        }
                    }
                    argTypes = null;
                    return mdc;

            }

            List<PropertyInfo> pis = ReflectTool.GetPropertyList(typeInfo);
            if (pis.Count > 0)
            {
                for (int i = 0; i < pis.Count; i++)
                {
                    SetStruct(mdc, pis[i], null, i, pis.Count);
                }
            }
            List<FieldInfo> fis = ReflectTool.GetFieldList(typeInfo);
            if (fis.Count > 0)
            {
                for (int i = 0; i < fis.Count; i++)
                {
                    SetStruct(mdc, null, fis[i], i, fis.Count);
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
            return mdc;
        }
        private static void SetStruct(MDataColumn mdc, PropertyInfo pi, FieldInfo fi, int i, int count)
        {
            Type type = pi != null ? pi.PropertyType : fi.FieldType;
            if (ReflectTool.ExistsAttr(AppConst.JsonIgnoreType, pi, fi))//获取Json忽略标识
            {
                return;//被Json忽略的列，不在返回列结构中。
            }
            string name = pi != null ? pi.Name : fi.Name;
            SqlDbType sqlType = SQL.DataType.GetSqlType(type);

            if (type.IsEnum)
            {
                if (ReflectTool.ExistsAttr(AppConst.JsonEnumToStringType, pi, fi) || ReflectTool.ExistsAttr(AppConst.JsonEnumToDescriptionType, pi, fi))//获取Json忽略标识
                {
                    sqlType = SqlDbType.NVarChar;
                }
            }
            mdc.Add(name, sqlType);
            MCellStruct column = mdc[mdc.Count - 1];

            LengthAttribute la = ReflectTool.GetAttr<LengthAttribute>(pi, fi);//获取长度设置
            if (la != null)
            {
                column.MaxSize = la.MaxSize;
                column.Scale = la.Scale;
            }
            if (column.MaxSize <= 0)
            {
                column.MaxSize = DataType.GetMaxSize(sqlType);
            }

            KeyAttribute ka = ReflectTool.GetAttr<KeyAttribute>(pi, fi);//获取关键字判断
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
            DefaultValueAttribute dva = ReflectTool.GetAttr<DefaultValueAttribute>(pi, fi);
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
            else if (i > count - 3 && sqlType == SqlDbType.DateTime && name.EndsWith("Time"))
            {
                column.DefaultValue = SqlValue.GetDate;
            }
            DescriptionAttribute da = ReflectTool.GetAttr<DescriptionAttribute>(pi, fi);//看是否有字段描述属性。
            if (da != null)
            {
                column.Description = da.Description;
            }

        }
    }
    /// <summary>
    /// 从DataTable 架构转列
    /// </summary>
    internal partial class TableSchema
    {
        /// <summary>
        /// DbDataReader的GetSchema拿到的DataType、Size、Scale很不靠谱
        /// </summary>
        private static void FixTableSchemaType(DataTable tableSchema, DbDataReader sdr, bool isCloseReader)
        {
            if (sdr != null)
            {
                if (tableSchema != null)
                {
                    tableSchema.Columns.Add("DataTypeString");
                    for (int i = 0; i < sdr.FieldCount; i++)
                    {
                        tableSchema.Rows[i]["DataTypeString"] = sdr.GetDataTypeName(i);
                    }
                }
                if (isCloseReader)
                {
                    sdr.Close();
                }
            }
        }
        internal static MDataColumn GetColumnByTable(DataTable tableSchema, DbDataReader sdr, bool isCloseReader)
        {
            FixTableSchemaType(tableSchema, sdr, isCloseReader);
            MDataColumn mdcs = new MDataColumn();
            if (tableSchema != null && tableSchema.Rows.Count > 0)
            {
                mdcs.TableName = tableSchema.TableName;
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
                    if (sqlTypeName == "TINYINT")
                    {
                        switch (Convert.ToString(row["DataType"]))
                        {
                            case "System.SByte":
                                mStruct.valueType = typeof(System.SByte);
                                break;

                            case "System.Byte":
                                mStruct.valueType = typeof(System.Byte);
                                break;
                            case "System.Boolean":
                                mStruct.valueType = typeof(System.Boolean);
                                break;

                        }
                    }
                    mdcs.Add(mStruct);

                }
                tableSchema = null;
            }
            return mdcs;
        }
    }
    internal partial class TableSchema
    {
        #region 获取数据库表的列字段
        private const string MSSQL_SynonymsName = "SELECT TOP 1 base_object_name from sys.synonyms WHERE NAME = '{0}'";
        private const string Oracle_SynonymsName = "SELECT TABLE_NAME FROM USER_SYNONYMS WHERE SYNONYM_NAME='{0}' and rownum=1";
        internal static string GetMSSQLColumns(bool for2000, string dbName)
        {
            // 2005以上增加同义词支持。 case s2.name WHEN 'timestamp' THEN 'variant' ELSE s2.name END as [SqlType],
            return string.Format(@"select s1.name as ColumnName,case s2.name WHEN 'uniqueidentifier' THEN 36 
                     WHEN 'ntext' THEN -1 WHEN 'text' THEN -1 WHEN 'image' THEN -1 else s1.[prec] end  as [MaxSize],s1.scale as [Scale],
                     isnullable as [IsNullable],colstat&1 as [IsAutoIncrement],
                     case s2.name when 'timestamp' then 'b_timestamp' else s2.name end as [SqlType],
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
                                when DATA_TYPE='NUMBER' then 'decimal'                   
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
                    where A.TABLE_NAME=:TableName order by COLUMN_id";
            //            left join  user_constraints uc2 on uc1.r_constraint_name=uc2.constraint_name
            // where A.TABLE_NAME= nvl((SELECT TABLE_NAME FROM USER_SYNONYMS WHERE SYNONYM_NAME=UPPER(:TableName) and rownum=1),UPPER(:TableName)) order by COLUMN_id";
        }
        internal static string GetMySqlColumns(string dbName)
        {
            return string.Format(@"SELECT s1.COLUMN_NAME as ColumnName,case DATA_TYPE when 'int' then 10 when 'date' then 10 when 'time' then 8  when 'datetime' then 23 when 'year' then 4
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
case s2.name when 'timestamp' then 'b_timestamp' else s2.name end as SqlType,
case when exists(SELECT 1 FROM sysindexes WHERE id=s1.id AND s1.name=index_col(@TableName,indid,s1.colid)) then 1 else 0 end as IsPrimaryKey,
               str_replace(s3.text,'DEFAULT  ',null) as DefaultValue,
               null as Description
from syscolumns s1 left join systypes s2 on s1.usertype=s2.usertype
left join syscomments s3 on s1.cdefault=s3.id
where s1.id =object_id(@TableName) and s2.usertype<100
order by s1.colid";
        }
        internal static string GetPostgreColumns(float version)
        {
            string key = "case  when position('nextval' in column_default)>0 then 1 else 0 end";
            if (version >= 10)
            {
                key = "case i.is_identity when 'NO' then 0 else 1 end";
            }
            return string.Format(@"select
a.attname AS ColumnName,
i.data_type AS SqlType,
coalesce(character_maximum_length,numeric_precision,-1) as MaxSize,numeric_scale as Scale,
case a.attnotnull when 'true' then 0 else 1 end AS IsNullable,
{0} as IsAutoIncrement, 
case when o.conkey[a.attnum] is null then 0 else 1 end as IsPrimaryKey,
d.description AS Description,
i.column_default as DefaultValue
from pg_class c 
left join pg_attribute a on c.oid=a.attrelid
left join pg_description d on a.attrelid=d.objoid AND a.attnum = d.objsubid
left join pg_type t on a.atttypid = t.oid
left join information_schema.columns i on i.table_schema='public' and i.table_name=c.relname and i.column_name=a.attname
left join pg_constraint o on o.contype='p' and o.conrelid=c.oid
where c.relname =:TableName
and a.attnum > 0 and a.atttypid>0
ORDER BY a.attnum", key);
        }

        internal static string GetDB2Columns()
        {
            return @"select a.colname as ColumnName ,
a.length as MaxSize,
a.typename as SqlType,
a.Scale,
case a.Nulls when 'Y' then 1 else 0 end AS IsNullable,
case a.keyseq when 1 then 1 ELSE 0 END as IsAutoIncrement,
case c.type when 'P' then 1 else 0 end  as IsPrimaryKey,
case c.type when 'U' then 1 else 0 end  as IsUniqueKey,
a.remarks as Description,
a.default as DefaultValue
from SYSCAT.COLUMNS a 
left join syscat.keycoluse b on a.tabname=b.tabname and a.colname=b.colname 
left join syscat.tabconst c on b.tabname=c.tabname   and b.constname=c.constname  
where a.tabname=@TableName 
order by a.colno

";
        }

        internal static string GetFireBirdColumns()
        {
            return @"SELECT
    RF.RDB$FIELD_NAME AS ColumnName,
        CASE WHEN (FTY.RDB$TYPE_NAME='INT128' AND FLD.RDB$FIELD_SUB_TYPE>0) OR (FTY.RDB$TYPE_NAME IN('DECFLOAT(16)','DECFLOAT(34)')) THEN FLD.RDB$FIELD_PRECISION
         WHEN FTY.RDB$TYPE_NAME IN('BLOB') THEN FLD.RDB$SEGMENT_LENGTH
         ELSE FLD.RDB$FIELD_LENGTH END AS MaxSize,
    CASE  when (FLD.RDB$FIELD_SUB_TYPE=1 AND FTY.RDB$TYPE_NAME='INT128') THEN 'NUMERIC'
          when (FLD.RDB$FIELD_SUB_TYPE=2 AND FTY.RDB$TYPE_NAME='INT128') THEN 'DECIMAL'
          when (FLD.RDB$FIELD_SUB_TYPE=0 AND FTY.RDB$TYPE_NAME='BLOB') then 'BINARY' 
          when (FLD.RDB$FIELD_SUB_TYPE=1 AND FTY.RDB$TYPE_NAME='BLOB') then 'TEXT' 
          when  FTY.RDB$TYPE_NAME='SHORT' then 'SMALLINT' 
          when FTY.RDB$TYPE_NAME='LONG' then 'INTEGER' 
          when FTY.RDB$TYPE_NAME='INT64' then 'BIGINT' 
          when FTY.RDB$TYPE_NAME='TEXT' then 'CHAR' 
          when FTY.RDB$TYPE_NAME='VARYING' then 'VARCHAR' 
    else FTY.RDB$TYPE_NAME end AS SqlType,
    ABS(FLD.RDB$FIELD_SCALE) AS Scale,
   CASE RF.RDB$NULL_FLAG when 1 then 0 ELSE 1 END AS IsNullable,
   RF.RDB$DEFAULT_SOURCE AS DefaultValue,
    CASE
        WHEN IIF(V.RDB$CONSTRAINT_TYPE = 'PRIMARY KEY', 1, 0) = 1 THEN 1
        ELSE 0
    END AS IsPrimaryKey,
   case RF.RDB$IDENTITY_TYPE when 1 then 1 ELSE 0 END as IsAutoIncrement,
   case when (RF.RDB$IDENTITY_TYPE=1 OR V.RDB$UNIQUE_FLAG IS NULL) then 0 ELSE 1 END as IsUniqueKey,
    RF.RDB$DESCRIPTION AS Description
FROM
    RDB$RELATION_FIELDS RF
LEFT JOIN
    RDB$FIELDS FLD ON RF.RDB$FIELD_SOURCE = FLD.RDB$FIELD_NAME
LEFT JOIN 
    rdb$types FTY ON FTY.RDB$FIELD_NAME='RDB$FIELD_TYPE' AND FTY.RDB$TYPE=FLD.RDB$FIELD_TYPE
LEFT JOIN
(
SELECT RI.RDB$RELATION_NAME,RI.RDB$UNIQUE_FLAG,RS.RDB$FIELD_NAME,RC.RDB$CONSTRAINT_TYPE FROM  RDB$INDICES RI
LEFT JOIN
    RDB$RELATION_CONSTRAINTS RC ON RC.RDB$INDEX_NAME = RI.RDB$INDEX_NAME 
LEFT JOIN
    RDB$INDEX_SEGMENTS RS ON  RS.RDB$FIELD_POSITION=0  AND RI.RDB$INDEX_NAME = RS.RDB$INDEX_NAME
) V ON RF.RDB$FIELD_NAME = V.RDB$FIELD_NAME AND RF.RDB$RELATION_NAME=V.RDB$RELATION_NAME
WHERE
    RF.RDB$RELATION_NAME =@TableName
ORDER BY
    RF.RDB$FIELD_POSITION";
        }

        internal static string GetDaMengColumns(string schemaName)
        {
            return string.Format(@"SELECT 
a.table_name As tableName,
 a.column_name As ColumnName,
 a.data_length As MaxSize,
 a.data_type As SqlType, 
 a.data_scale As Scale, 
 b.comments As Description,
 e.data_default as DefaultValue,
 case when (a.nullable = 'Y') then 1 else 0 end as IsNullable,
 case when (d.constraint_type = 'P') then 1 else 0 end As IsPrimaryKey,
 case when (d.constraint_type= 'U') then 1 else 0 end As IsUniqueKey,
 case when f.info2 = 1 then 1 else 0 end as  IsAutoIncrement
FROM all_tab_cols a
LEFT JOIN all_col_comments b ON b.table_name = a.table_name AND b.column_name = a.column_name AND a.owner = b.schema_name
LEFT JOIN all_tab_columns e ON a.table_name = e.table_name AND a.owner = e.owner and a.column_name = e.column_name
LEFT JOIN (SELECT f1.owner,f1.object_name As table_name,f0.name,f0.info2 FROM syscolumns f0
INNER JOIN dba_objects f1 ON f1.object_type = 'TABLE' AND info2 =1 AND f1.object_id = f0.id
) f ON f.name = a.column_name AND f.table_name = a.table_name AND f.owner = a.owner
LEFT JOIN (select ai.table_owner,ai.column_name,ai.table_name,ac.constraint_type
           from all_ind_columns ai
           right join (select table_owner,index_name from all_ind_columns group by table_owner,index_name having count(*)=1) v
           on ai.table_owner=v.table_owner and ai.index_name=v.index_name
           left join all_constraints ac  on ac.owner=ai.table_owner and ac.index_name=ai.index_name
)d on d.TABLE_OWNER=a.OWNER and d.TABLE_NAME=a.TABLE_NAME and d.COLUMN_NAME=a.COLUMN_NAME

WHERE a.owner = UPPER('{0}') and a.TABLE_NAME=:TableName
ORDER BY a.column_id", schemaName);
        }

        internal static string GetKingBaseESColumns(string schemaName)
        {
            return String.Format(@"SELECT 
a.column_name AS ColumnName,
CASE WHEN a.CHARACTER_MAXIMUM_LENGTH IS NOT NULL THEN a.CHARACTER_MAXIMUM_LENGTH ELSE a.NUMERIC_PRECISION END as MaxSize,
a.NUMERIC_SCALE as Scale,
case a.IS_NULLABLE when 'YES' then 1 else 0 end as IsNullable,
CASE d.typname WHEN 'int2' THEN 'smallint' WHEN 'int4' THEN 'integer' WHEN 'int8' THEN 'bigint' ELSE d.typname END AS SqlType,
CASE WHEN LEFT(a.column_default,8)='nextval(' THEN 1 ELSE 0 END AS IsAutoIncrement,
                    case c.CONSTRAINT_TYPE WHEN 'PRIMARY KEY' then 1 else 0 end as IsPrimaryKey,
                    case c.CONSTRAINT_TYPE when 'UNIQUE' then 1 else 0 end as IsUniqueKey,
					case c.CONSTRAINT_TYPE when 'FOREIGN KEY' then 1 else 0 end as IsForeignKey,
                    a.COLUMN_DEFAULT AS DefaultValue,
                    d.description AS Description

 FROM information_schema.columns a
LEFT JOIN information_schema.key_column_usage b
on b.TABLE_SCHEMA = a.TABLE_SCHEMA and b.TABLE_NAME = a.TABLE_NAME and b.COLUMN_NAME = a.COLUMN_NAME
LEFT JOIN information_schema.TABLE_CONSTRAINTS c on c.TABLE_SCHEMA = b.TABLE_SCHEMA and c.TABLE_NAME = b.TABLE_NAME and c.CONSTRAINT_NAME = b.CONSTRAINT_NAME
LEFT JOIN(SELECT s2.nspname, s1.relname, s3.attname, s4.description,s5.typname
FROM sys_class s1
LEFT JOIN  sys_namespace s2 ON s2.oid = s1.relnamespace
LEFT JOIN sys_attribute s3 ON s3.attrelid = s1.oid
LEFT JOIN sys_description s4 ON s3.attrelid= s4.objoid AND s3.attnum = s4.objsubid
LEFT JOIN sys_type s5 ON s5.oid=s3.atttypid
) d ON d.nspname = a.table_schema AND d.relname = a.table_name AND d.attname = a.column_name
WHERE a.table_schema = '{0}' AND a.table_name = :TableName", schemaName);
        }
        #endregion
    }

}
