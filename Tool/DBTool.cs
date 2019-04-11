using System;
using CYQ.Data.Table;
using System.Data;
using CYQ.Data.SQL;
using System.IO;
using System.Collections.Generic;
using System.Text;


namespace CYQ.Data.Tool
{
    /// <summary>
    /// 数据库工具类[都是静态方法]
    /// </summary>
    public static class DBTool
    {

        private static StringBuilder _ErrorMsg = new StringBuilder();

        /// <summary>
        /// 获取异常的信息
        /// </summary>
        public static string ErrorMsg
        {
            get
            {
                return _ErrorMsg.ToString();
            }
            set
            {
                _ErrorMsg.Length = 0;
                if (value != null)
                {
                    _ErrorMsg.Append(value);
                }
            }
        }


        #region 库层面操作
        /// <summary>
        /// 获取数据库链接的数据库类型
        /// </summary>
        /// <param name="conn">链接配置Key或数据库链接语句</param>
        /// <returns></returns>
        public static DalType GetDalType(string conn)
        {
            return ConnBean.Create(conn).ConnDalType;
        }
        /// <summary>
        /// 获取指定数据库的数据类型
        /// </summary>
        /// <param name="ms">单元格结构</param>
        /// <param name="dalType">数据库类型</param>
        /// <param name="version">数据库版本号</param>
        /// <returns></returns>
        public static string GetDataType(MCellStruct ms, DalType dalType, string version)
        {
            return DataType.GetDataType(ms, dalType, version);
        }
        /// <summary>
        /// 测试数据库链接语句
        /// </summary>
        /// <param name="conn">链接配置Key或数据库链接语句</param>
        /// <returns></returns>
        public static bool TestConn(string conn)
        {
            string msg;
            return TestConn(conn, out msg);
        }
        public static bool TestConn(string conn, out string msg)
        {
            bool result = false;
            try
            {
                DalBase helper = DalCreate.CreateDal(conn);
                result = helper.TestConn(AllowConnLevel.Master);
                if (result)
                {
                    msg = helper.Version;
                }
                else
                {
                    msg = helper.DebugInfo.ToString();
                }
                helper.Dispose();
            }
            catch (Exception err)
            {
                msg = err.Message;
            }
            return result;
        }

        #endregion

        #region 表的相关操作

        #region 获取所有表
        /// <summary>
        /// 获取数据库表
        /// </summary>
        public static Dictionary<string, string> GetTables(string conn)
        {
            string dbName;
            return GetTables(conn, out dbName);
        }
        /// <summary>
        /// 获取数据库表
        /// </summary>
        public static Dictionary<string, string> GetTables(string conn, out string dbName)
        {
            string errInfo;
            return GetTables(conn, out dbName, out errInfo);
        }
        /// <summary>
        /// 获取所有表（表名+表说明）【链接错误时，抛异常】
        /// </summary>
        /// <param name="conn">数据库链接</param>
        /// <param name="dbName">返回指定链接的数据库名称</param>
        /// <param name="errInfo">链接错误时的信息信息</param>
        public static Dictionary<string, string> GetTables(string conn, out string dbName, out string errInfo)
        {
            errInfo = string.Empty;
            using (DalBase helper = DalCreate.CreateDal(conn))
            {
                dbName = helper.DataBase;
                if (!helper.TestConn(AllowConnLevel.MaterBackupSlave))
                {
                    errInfo = helper.DebugInfo.ToString();
                    if (string.IsNullOrEmpty(errInfo))
                    {
                        errInfo = helper.DataBaseType + " Open database fail : " + dbName;
                    }
                    return null;
                }
            }
            return TableSchema.GetTables(conn);//内部有缓存
        }
        #endregion

        #region 表是否存在
        /// <summary>
        /// 是否存在表
        /// </summary>
        public static bool ExistsTable(string tableName)
        {
            return ExistsTable(tableName, AppConfig.DB.DefaultConn);
        }
        /// <summary>
        /// 是否存在指定的表
        /// </summary>
        /// <param name="tableName">表名[或文件名]</param>
        /// <param name="conn">数据库链接</param>
        public static bool ExistsTable(string tableName, string conn)
        {
            DalType dal;
            return ExistsTable(tableName, conn, out dal);
        }
        /// <summary>
        /// 检测表是否存在
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="conn">数据库链接</param>
        /// <param name="dalType">数据库类型</param>
        public static bool ExistsTable(string tableName, string conn, out DalType dalType)
        {
            string database;
            return ExistsTable(tableName, conn, out dalType, out database);
        }
        /// <summary>
        /// 检测表是否存在
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="conn">数据库链接</param>
        /// <param name="dalType">数据库类型</param>
        public static bool ExistsTable(string tableName, string conn, out DalType dalType, out string database)
        {
            dalType = DalType.None;
            database = string.Empty;
            if (string.IsNullOrEmpty(tableName) || tableName.Contains("(") && tableName.Contains(")"))
            {
                return false;
            }
            using (DalBase helper = DalCreate.CreateDal(conn))
            {
                dalType = helper.DataBaseType;
                database = helper.DataBase;
            }
            return TableSchema.Exists(tableName, "U", conn);
        }
        #endregion

        #region 创建表语句
        /// <summary>
        /// 为指定的表架构生成SQL(Create Table)语句
        /// </summary>
        public static bool CreateTable(string tableName, MDataColumn columns)
        {
            return CreateTable(tableName, columns, AppConfig.DB.DefaultConn);
        }
        /// <summary>
        /// 为指定的表架构生成SQL(Create Table)语句
        /// </summary>
        public static bool CreateTable(string tableName, MDataColumn columns, string conn)
        {
            if (string.IsNullOrEmpty(tableName) || tableName.Contains("(") && tableName.Contains(")"))
            {
                return false;
            }
            bool result = false;
            DalType dalType = GetDalType(conn);
            string dataBase = string.Empty;
            switch (dalType)
            {
                case DalType.Txt:
                case DalType.Xml:
                    // string a, b, c;
                    conn = AppConfig.GetConn(conn);// CYQ.Data.DAL.DalCreate.GetConnString(conn, out a, out b, out c);
                    if (conn.ToLower().Contains(";ts=0"))//不写入表架构。
                    {
                        return true;
                    }
                    else
                    {
                        tableName = Path.GetFileNameWithoutExtension(tableName);
                        string fileName = NoSqlConnection.GetFilePath(conn) + tableName + ".ts";
                        result = columns.WriteSchema(fileName);
                        dataBase = new NoSqlConnection(conn).Database;
                    }
                    break;
                default:
                    using (MProc proc = new MProc(null, conn))
                    {
                        dataBase = proc.DataBase;
                        try
                        {
                            proc.dalHelper.IsRecordDebugInfo = false;
                            proc.SetAopState(Aop.AopOp.CloseAll);
                            proc.ResetProc(GetCreateTableSql(tableName, columns, proc.DalType, proc.DalVersion));//.Replace("\n", string.Empty)
                            result = proc.ExeNonQuery() > -2;
                            if (result)
                            {
                                //获取扩展说明
                                string descriptionSql = GetCreateTableDescriptionSql(tableName, columns, proc.DalType).Replace("\r\n", " ").Trim(' ', ';');
                                if (!string.IsNullOrEmpty(descriptionSql))
                                {
                                    if (proc.DalType == DalType.Oracle)
                                    {
                                        foreach (string sql in descriptionSql.Split(';'))
                                        {
                                            proc.ResetProc(sql);
                                            if (proc.ExeNonQuery() == -2)
                                            {
                                                break;
                                            }


                                        }
                                    }
                                    else
                                    {
                                        proc.ResetProc(descriptionSql);
                                        proc.ExeNonQuery();
                                    }
                                }
                            }

                        }
                        catch (Exception err)
                        {
                            Log.Write(err, LogType.DataBase);
                        }
                        finally
                        {
                            if (proc.RecordsAffected == -2)
                            {
                                _ErrorMsg.AppendLine("CreateTable:" + proc.DebugInfo);
                            }
                        }
                    }
                    break;


            }
            if (result)
            {
                TableSchema.Add(tableName, "U", conn);//修改缓存。
            }
            return result;
        }
        /// <summary>
        /// 获取指定的表架构生成的SQL(Create Table)的说明语句
        /// </summary>
        public static string GetCreateTableDescriptionSql(string tableName, MDataColumn columns, DalType dalType)
        {
            return SqlCreateForSchema.CreateTableDescriptionSql(tableName, columns, dalType);
        }
        /// <summary>
        /// 获取指定的表架构生成的SQL(Create Table)的说明语句
        /// </summary>
        public static string GetCreateTableSql(string tableName, MDataColumn columns, DalType dalType, string version)
        {
            return SqlCreateForSchema.CreateTableSql(tableName, columns, dalType, version);
        }
        internal static void CheckAndCreateOracleSequence(string seqName, string conn, string primaryKey, string tableName)
        {
            seqName = seqName.ToUpper();
            using (DalBase db = DalCreate.CreateDal(conn))
            {
                object o = db.ExeScalar(string.Format(TableSchema.ExistOracleSequence, seqName), false);
                if (db.RecordsAffected != -2 && (o == null || Convert.ToString(o) == "0"))
                {
                    int startWith = 1;
                    if (!string.IsNullOrEmpty(primaryKey))
                    {
                        o = db.ExeScalar(string.Format(TableSchema.GetOracleMaxID, primaryKey, tableName), false);
                        if (db.RecordsAffected != -2)
                        {
                            if (!int.TryParse(Convert.ToString(o), out startWith) || startWith < 1)
                            {
                                startWith = 1;
                            }
                            else
                            {
                                startWith++;
                            }
                        }
                    }
                    db.ExeNonQuery(string.Format(TableSchema.CreateOracleSequence, seqName, startWith), false);
                }
                if (db.RecordsAffected == -2)
                {
                    _ErrorMsg.AppendLine("CheckAndCreateOracleSequence:" + db.DebugInfo.ToString());
                }
            }

        }
        #endregion

        #region 修改表语句
        /// <summary>
        /// 获取指定的表架构生成的SQL(Alter Table)的说明语句
        /// </summary>
        public static string GetAlterTableSql(string tableName, MDataColumn columns)
        {
            return GetAlterTableSql(tableName, columns, AppConfig.DB.DefaultConn);
        }
        /// <summary>
        /// 获取指定的表架构生成的SQL(Alter Table)的说明语句
        /// </summary>
        public static string GetAlterTableSql(string tableName, MDataColumn columns, string conn)
        {
            List<string> sqlItems = SqlCreateForSchema.AlterTableSql(tableName, columns, conn);
            if (sqlItems.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (string sql in sqlItems)
                {
                    sb.AppendLine(sql);
                }
                sqlItems = null;
                return sb.ToString();
            }
            return string.Empty;
        }
        /// <summary>
        /// 修改表的列结构
        /// </summary>
        public static bool AlterTable(string tableName, MDataColumn columns)
        {
            return AlterTable(tableName, columns, AppConfig.DB.DefaultConn);
        }
        /// <summary>
        /// 修改表的列结构
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="columns">列结构</param>
        /// <param name="conn">数据库链接</param>
        /// <returns></returns>
        public static bool AlterTable(string tableName, MDataColumn columns, string conn)
        {
            if (columns == null) { return false; }
            List<string> sqls = SqlCreateForSchema.AlterTableSql(tableName, columns, conn);
            if (sqls.Count > 0)
            {
                DalType dalType = DalType.None;
                string database = string.Empty;

                using (MProc proc = new MProc(null, conn))
                {
                    dalType = proc.DalType;
                    database = proc.dalHelper.DataBase;
                    proc.SetAopState(Aop.AopOp.CloseAll);
                    if (proc.DalType == DalType.MsSql)
                    {
                        proc.BeginTransation();//仅对mssql有效。
                    }
                    foreach (string sql in sqls)
                    {
                        proc.ResetProc(sql);
                        if (proc.ExeNonQuery() == -2)
                        {
                            proc.RollBack();
                            _ErrorMsg.AppendLine("AlterTable:" + proc.DebugInfo);
                            Log.Write(proc.DebugInfo, LogType.DataBase);

                            return false;
                        }
                    }
                    proc.EndTransation();
                }
                RemoveCache(tableName, conn);
                return true;
            }
            return false;
        }
        #endregion

        #region 删除表语句
        /// <summary>
        /// 移除一张表
        /// </summary>
        /// <returns></returns>
        public static bool DropTable(string tableName)
        {
            return DropTable(tableName, AppConfig.DB.DefaultConn);
        }
        /// <summary>
        /// 移除一张表
        /// <param name="conn">数据库链接</param>
        /// </summary>
        public static bool DropTable(string tableName, string conn)
        {
            bool result = false;
            string key = string.Empty;
            using (DalBase helper = DalCreate.CreateDal(conn))
            {
                DalType dalType = helper.DataBaseType;
                switch (dalType)
                {
                    case DalType.Txt:
                    case DalType.Xml:
                        string folder = helper.Con.DataSource + Path.GetFileNameWithoutExtension(tableName);
                        string path = folder + ".ts";
                        try
                        {
                            if (File.Exists(path))
                            {
                                result = IOHelper.Delete(path);
                            }
                            path = folder + (dalType == DalType.Txt ? ".txt" : ".xml");
                            if (File.Exists(path))
                            {
                                result = IOHelper.Delete(path);
                            }
                        }
                        catch
                        {

                        }
                        break;
                    default:
                        result = helper.ExeNonQuery("drop table " + Keyword(tableName, dalType), false) != -2;
                        if (result)
                        {
                            //处理表相关的元数据和数据缓存。
                            RemoveCache(tableName, conn);
                        }
                        break;
                }
                if (helper.RecordsAffected == -2)
                {
                    _ErrorMsg.AppendLine(helper.DebugInfo.ToString());
                }
            }
            if (result)
            {
                //处理数据库表字典缓存
                TableSchema.Remove(tableName, "U", conn);
            }
            return result;
        }

        #endregion


        #endregion

        #region 获取结构
        /// <summary>
        /// 获取表架构
        /// </summary>
        /// <param name="conn">数据库链接</param>
        /// <param name="connectionName">指定要返回架构的名称</param>
        /// <param name="restrictionValues">为指定的架构返回一组限制值</param>
        public static DataTable GetSchema(string conn, string connectionName, string[] restrictionValues)
        {
            DalBase helper = DalCreate.CreateDal(conn);
            if (!helper.TestConn(AllowConnLevel.MaterBackupSlave))
            {
                return null;
            }
            helper.Con.Open();
            DataTable dt = helper.Con.GetSchema(connectionName, restrictionValues);
            helper.Con.Close();
            helper.Dispose();
            return dt;
        }

        /// <summary>
        /// 获取表列架构
        /// </summary>
        public static MDataColumn GetColumns(Type typeInfo)
        {
            return ColumnSchema.GetColumns(typeInfo);
        }
        /// <summary>
        /// 获取表列架构
        /// </summary>
        /// <param name="tableName">表名</param>
        public static MDataColumn GetColumns(object tableNameObj)
        {
            string conn = string.Empty;
            if (tableNameObj is Enum)
            {
                conn = CrossDB.GetConnByEnum(tableNameObj as Enum);
            }
            return GetColumns(Convert.ToString(tableNameObj), conn);
        }
        /// <summary>
        /// 获取表列架构（链接错误时，抛异常）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="conn">数据库链接</param>
        /// <param name="errInfo">出错时的错误信息</param>
        /// <returns></returns>
        public static MDataColumn GetColumns(string tableName, string conn, out string errInfo)
        {
            errInfo = string.Empty;
            try
            {
                return ColumnSchema.GetColumns(tableName, conn);

            }
            catch(Exception err)
            {
                errInfo = err.Message;
                return null;
            }
        }
        /// <summary>
        /// 获取表列架构
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="conn">数据库链接</param>
        /// <returns></returns>
        public static MDataColumn GetColumns(string tableName, string conn)
        {
            string err;
            return GetColumns(tableName, conn, out err);
        }
        #endregion


        #region 其它操作
        //private static List<string> flag = new List<string>(2);
        //internal static void CreateSelectBaseProc(DalType dal, string conn)
        //{
        //    try
        //    {
        //        switch (dal)
        //        {
        //            //case DalType.Oracle:
        //            //    if (!flag.Contains("oracle"))
        //            //    {
        //            //        flag.Add("oracle");
        //            //        using (DbBase db = DalCreate.CreateDal(conn))
        //            //        {
        //            //            db.AllowRecordSql = false;
        //            //            object o = db.ExeScalar(string.Format(ExistOracle.Replace("TABLE", "PROCEDURE"), "MyPackage.SelectBase"), false);
        //            //            if (o != null && Convert.ToInt32(o) < 1)
        //            //            {
        //            //                db.ExeNonQuery(SqlPager.GetPackageHeadForOracle(), false);
        //            //                db.ExeNonQuery(SqlPager.GetPackageBodyForOracle(), false);
        //            //            }
        //            //        }
        //            //    }
        //            //    break;
        //            case DalType.MsSql:
        //                if (!flag.Contains("sql"))
        //                {
        //                    flag.Add("sql");//考虑到一个应用不太可能同时使用mssql的不同版本，只使用一个标识。
        //                    using (DbBase db = DalCreate.CreateDal(conn))
        //                    {
        //                        db.IsRecordDebugInfo = false;
        //                        object o = null;
        //                        if (!db.Version.StartsWith("08"))
        //                        {
        //                            //    o = db.ExeScalar(string.Format(Exist2000.Replace("U", "P"), "SelectBase"), false);
        //                            //    if (o != null && Convert.ToInt32(o) < 1)
        //                            //    {
        //                            //        db.ExeNonQuery(SqlPager.GetSelectBaseForSql2000(), false);
        //                            //    }
        //                            //}
        //                            //else
        //                            //{
        //                            o = db.ExeScalar(string.Format(TableSchema.Exist2005, "SelectBase", "P"), false);
        //                            if (o != null && Convert.ToInt32(o) < 1)
        //                            {
        //                                db.ExeNonQuery(SqlCreateForPager.GetSelectBaseForSql2005(), false);
        //                            }
        //                        }
        //                    }
        //                }
        //                break;
        //        }
        //    }
        //    catch (Exception err)
        //    {
        //        Log.Write(err, LogType.DataBase);
        //    }
        //}

        /// <summary>
        /// 为字段或表名添加关键字标签：如[],''等符号
        /// </summary>
        /// <param name="name">表名或字段名</param>
        /// <param name="dalType">数据类型</param>
        /// <returns></returns>
        public static string Keyword(string name, DalType dalType)
        {
            return SqlFormat.Keyword(name, dalType);
        }
        /// <summary>
        /// 取消字段或表名添加关键字标签：如[],''等符号
        /// </summary>
        /// <param name="name">表名或字段名</param>
        public static string NotKeyword(string name)
        {
            return SqlFormat.NotKeyword(name);
        }


        /// <summary>
        /// 将各数据库默认值格式化成标准值，将标准值还原成各数据库默认值
        /// </summary>
        /// <param name="flag">[0:转成标准值],[1:转成各数据库值]</param>
        /// <returns></returns>
        public static string FormatDefaultValue(DalType dalType, object value, int flag, SqlDbType sqlDbType)
        {
            return SqlFormat.FormatDefaultValue(dalType, value, flag, sqlDbType);
        }

        #endregion

        private static void RemoveCache(string tableName, string conn)
        {
            //清缓存
            string key = Cache.CacheManage.GetKey(Cache.CacheKeyType.Schema, tableName, conn);
            Cache.CacheManage.LocalInstance.Remove(key);
            key = Cache.CacheManage.GetKey(Cache.CacheKeyType.AutoCache, tableName, conn);
            Cache.AutoCache.ReadyForRemove(key);
        }
    }
}
