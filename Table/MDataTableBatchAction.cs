using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.SQL;
using CYQ.Data.Tool;

using System.Data.SqlClient;
using System.Reflection;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Diagnostics;

//using Oracle.DataAccess.Client;


namespace CYQ.Data.Table
{
    /// <summary>
    /// 批量操作。
    /// </summary>
    internal partial class MDataTableBatchAction
    {
        /// <summary>
        /// 联合主键
        /// </summary>
        List<int> jointPrimaryIndex;
        MDataTable mdt, sourceTable;
        internal DataBaseType dalTypeTo = DataBaseType.None;
        internal string database = string.Empty;
        private bool _IsTruncate;
        /// <summary>
        /// Insert前是否先清表
        /// </summary>
        public bool IsTruncate
        {
            get { return _IsTruncate; }
            set { _IsTruncate = value; }
        }

        string _Conn = string.Empty;
        /// <summary>
        /// 内部操作对象（需要同一个事务处理）。
        /// </summary>
        private DalBase _dalHelper;

        public MDataTableBatchAction(MDataTable mTable)
        {
            Init(mTable, string.Empty);
        }
        public MDataTableBatchAction(MDataTable mTable, string conn)
        {
            Init(mTable, conn);
        }
        private void Init(MDataTable mTable, string conn)
        {
            if (mTable.Columns == null || mTable.Columns.Count == 0)
            {
                Error.Throw("MDataTable's columns can't be null or columns'length can't be zero");
            }
            if (string.IsNullOrEmpty(mTable.TableName))
            {
                Error.Throw("MDataTable's tablename can't  null or empty");
            }
            mdt = sourceTable = mTable;

            if (mdt.TableName.IndexOfAny(new char[] { '(', ')' }) > -1)
            {
                mdt.TableName = mdt.TableName.Substring(mdt.TableName.LastIndexOf(')') + 1).Trim();
            }
            if (!string.IsNullOrEmpty(conn))
            {
                _Conn = conn;
            }
            else
            {
                if (mTable.DynamicData != null && mTable.DynamicData is MAction)//尝试多动态中获取链接
                {
                    _Conn = ((MAction)mTable.DynamicData).ConnString;
                }
                else if (mTable.DynamicData != null && mTable.DynamicData is MProc)
                {
                    _Conn = ((MProc)mTable.DynamicData).ConnString;
                }
                else
                {
                    _Conn = mTable.Conn;
                }
            }

            if (!DBTool.Exists(mdt.TableName, "U", _Conn))
            {
                DBTool.ErrorMsg = null;
                if (!DBTool.CreateTable(mdt.TableName, mdt.Columns, _Conn))
                {
                    Error.Throw("Create Table Error:" + mdt.TableName + DBTool.ErrorMsg);
                }
            }
            MDataColumn column = DBTool.GetColumns(mdt.TableName, _Conn);
            FixTable(column);//
            if (mdt.Columns.Count == 0)
            {
                Error.Throw("After fix table columns, length can't be zero");
            }
            dalTypeTo = column.DataBaseType;
            SetDalBaseForTransaction();
        }
        private void SetDalBaseForTransaction()
        {
            if (mdt.DynamicData != null)
            {
                if (mdt.DynamicData is MProc)
                {
                    _dalHelper = ((MProc)mdt.DynamicData).dalHelper;
                }
                else if (mdt.DynamicData is MAction)
                {
                    _dalHelper = ((MAction)mdt.DynamicData).dalHelper;
                }
            }
        }
        /// <summary>
        /// 进行列修正（只有移除 和 修正类型，若无主键列，则新增主键列）
        /// </summary>
        private void FixTable(MDataColumn column)
        {
            if (column.Count > 0)
            {
                bool tableIsChange = false;
                for (int i = mdt.Columns.Count - 1; i >= 0; i--)
                {
                    if (!column.Contains(mdt.Columns[i].ColumnName))//没有此列
                    {
                        if (!tableIsChange)
                        {
                            mdt = mdt.Clone();//列需要变化时，克隆一份，不变更原有数据。
                            tableIsChange = true;
                        }
                        mdt.Columns.RemoveAt(i);
                    }
                    else
                    {
                        MCellStruct ms = column[mdt.Columns[i].ColumnName];//新表的字段
                        Type valueType = mdt.Columns[i].ValueType;//存档的字段的值的原始类型。
                        bool isChangeType = mdt.Columns[i].SqlType != ms.SqlType;
                        mdt.Columns[i].Load(ms);
                        if (isChangeType)
                        {
                            //修正数据的数据类型。
                            foreach (MDataRow row in mdt.Rows)
                            {
                                row[i].FixValue();//重新自我赋值修正数据类型。
                            }
                        }

                    }
                }
                //主键检测，若没有，则补充主键
                if (column.JointPrimary != null && column.JointPrimary.Count > 0)
                {
                    if (!mdt.Columns.Contains(column[0].ColumnName) && (column[0].IsPrimaryKey || column[0].IsAutoIncrement))
                    {
                        MCellStruct ms = column[0].Clone();
                        mdt = mdt.Clone();//列需要变化时，克隆一份，不变更原有数据。
                        ms.MDataColumn = null;
                        mdt.Columns.Insert(0, ms);
                    }
                }
            }
        }
        /// <summary>
        /// 设置联合主键
        /// </summary>
        /// <param name="jointPrimaryKeys">联合主键</param>
        internal void SetJoinPrimaryKeys(object[] jointPrimaryKeys)
        {
            if (jointPrimaryKeys != null && jointPrimaryKeys.Length > 0)
            {
                int index = -1;
                jointPrimaryIndex = new List<int>();
                foreach (object o in jointPrimaryKeys) // 检测列名是否存在，不存在则抛异常
                {
                    index = mdt.Columns.GetIndex(Convert.ToString(o));
                    if (index == -1)
                    {
                        Error.Throw("table " + mdt.TableName + " not exist the column name : " + Convert.ToString(o));
                    }
                    else
                    {
                        if (!jointPrimaryIndex.Contains(index))
                        {
                            jointPrimaryIndex.Add(index);
                        }
                    }
                }
            }
        }
        internal List<MDataCell> GetJoinPrimaryCell(MDataRow row)
        {
            if (jointPrimaryIndex != null && jointPrimaryIndex.Count > 0)
            {
                List<MDataCell> cells = new List<MDataCell>(jointPrimaryIndex.Count);
                for (int i = 0; i < jointPrimaryIndex.Count; i++)
                {
                    cells.Add(row[jointPrimaryIndex[i]]);
                }
                return cells;
            }
            else
            {
                return row.JointPrimaryCell;
            }
        }

        internal bool Insert(bool keepID)
        {
            try
            {
                if (dalTypeTo == DataBaseType.MsSql)
                {
                    return MsSqlBulkCopyInsert(keepID);
                }
                else if (dalTypeTo == DataBaseType.Oracle && _dalHelper == null && !IsTruncate)
                {
                    if (OracleDal.clientType == 1 && keepID)
                    {
                        return OracleBulkCopyInsert();//不支持外部事务合并（因为参数只能传链接字符串。）
                    }
                    //else if (IsAllowBulkCopy(DalType.Oracle))
                    //{
                    //    return LoadDataInsert(dalTypeTo, keepid);
                    //}
                }
                else if (dalTypeTo == DataBaseType.MySql && IsAllowBulkCopy(DataBaseType.MySql))
                {
                    return LoadDataInsert(dalTypeTo, keepID);
                }

                //if (dalTypeTo == DalType.Txt || dalTypeTo == DalType.Xml)
                //{
                //    NoSqlAction.ResetStaticVar();//重置一下缓存
                //}
                return NomalInsert(keepID);

            }
            catch (Exception err)
            {
                if (err.InnerException != null)
                {
                    err = err.InnerException;
                }
                sourceTable.DynamicData = err;
                Log.Write(err, LogType.DataBase);
                return false;
            }
        }
        internal bool Update()
        {
            bool hasFK = (jointPrimaryIndex != null && jointPrimaryIndex.Count > 1) || mdt.Columns.JointPrimary.Count > 1;
            if (!hasFK)
            {
                foreach (MCellStruct item in mdt.Columns)
                {
                    if ((item.IsForeignKey && string.IsNullOrEmpty(item.FKTableName))
                        || item.MaxSize > 8000 || DataType.GetGroup(item.SqlType) == 999)
                    {
                        hasFK = true;
                        break;
                    }
                }
                if (!hasFK && !string.IsNullOrEmpty(AppConfig.DB.DeleteField))
                {
                    hasFK = mdt.Columns.Contains(AppConfig.DB.DeleteField);
                }
            }
            if (hasFK)
            {
                return NormalUpdate();
            }
            else
            {
                return BulkCopyUpdate();//只有一个主键，没有外键关联，同时只有基础类型。
            }
        }
        internal bool Auto()
        {
            bool result = true;

            using (MAction action = new MAction(mdt.TableName, _Conn))
            {
                action.SetAopState(Aop.AopOp.CloseAll);
                DalBase sourceHelper = action.dalHelper;
                if (_dalHelper != null)
                {
                    action.dalHelper = _dalHelper;
                }
                else
                {
                    action.BeginTransation();
                }
                action.dalHelper.IsRecordDebugInfo = false || AppDebug.IsContainSysSql;//屏蔽SQL日志记录 2000数据库大量的In条件会超时。

                if ((jointPrimaryIndex != null && jointPrimaryIndex.Count == 1) || (jointPrimaryIndex == null && mdt.Columns.JointPrimary.Count == 1))
                //jointPrimaryIndex == null && mdt.Columns.JointPrimary.Count == 1 && mdt.Rows.Count <= 10000
                //&& (!action.DalVersion.StartsWith("08") || mdt.Rows.Count < 1001)) //只有一个主键-》组合成In远程查询返回数据-》
                {
                    #region 新逻辑

                    MCellStruct keyColumn = jointPrimaryIndex != null ? mdt.Columns[jointPrimaryIndex[0]] : mdt.Columns.FirstPrimary;
                    string columnName = keyColumn.ColumnName;
                    //计算分组处理
                    int pageSize = 5000;
                    if (action.DataBaseVersion.StartsWith("08")) { pageSize = 1000; }
                    int count = mdt.Rows.Count / pageSize + 1;
                    for (int i = 0; i < count; i++)
                    {
                        MDataTable dt = mdt.Select(i + 1, pageSize, null);//分页读取
                        if (dt != null && dt.Rows.Count > 0)
                        {
                            string whereIn = SqlCreate.GetWhereIn(keyColumn, dt.GetColumnItems<string>(columnName, BreakOp.NullOrEmpty, true), action.DataBaseType);
                            action.SetSelectColumns(columnName);
                            MDataTable keyTable = action.Select(whereIn);//拿到数据，准备分拆上市

                            MDataTable[] dt2 = dt.Split(SqlCreate.GetWhereIn(keyColumn, keyTable.GetColumnItems<string>(columnName, BreakOp.NullOrEmpty, true), DataBaseType.None));//这里不需要格式化查询条件。
                            result = dt2[0].Rows.Count == 0;
                            if (!result)
                            {
                                MDataTable updateTable = dt2[0];
                                updateTable.SetState(2, BreakOp.Null);
                                updateTable.DynamicData = action;
                                result = updateTable.AcceptChanges(AcceptOp.Update, _Conn, columnName);
                                if (!result)
                                {
                                    sourceTable.DynamicData = updateTable.DynamicData;
                                }
                            }
                            if (result && dt2[1].Rows.Count > 0)
                            {
                                MDataTable insertTable = dt2[1];
                                insertTable.DynamicData = action;
                                bool keepid = !insertTable.Rows[0].PrimaryCell.IsNullOrEmpty;
                                result = insertTable.AcceptChanges((keepid ? AcceptOp.InsertWithID : AcceptOp.Insert), _Conn, columnName);
                                if (!result)
                                {
                                    sourceTable.DynamicData = insertTable.DynamicData;
                                }
                            }
                        }
                    }

                    #endregion

                    #region 旧逻辑，已不用 分拆处理 本地比较分拆两个表格【更新和插入】-》分开独立处理。
                    /*
                    string columnName = mdt.Columns.FirstPrimary.ColumnName;
                    string whereIn = SqlCreate.GetWhereIn(mdt.Columns.FirstPrimary, mdt.GetColumnItems<string>(columnName, BreakOp.NullOrEmpty, true), action.DalType);
                    action.SetSelectColumns(mdt.Columns.FirstPrimary.ColumnName);
                    dt = action.Select(whereIn);

                    MDataTable[] dt2 = mdt.Split(SqlCreate.GetWhereIn(mdt.Columns.FirstPrimary, dt.GetColumnItems<string>(columnName, BreakOp.NullOrEmpty, true), DalType.None));//这里不需要格式化查询条件。
                    result = dt2[0].Rows.Count == 0;
                    if (!result)
                    {
                        dt2[0].SetState(2, BreakOp.Null);
                        dt2[0].DynamicData = action;
                        MDataTableBatchAction m1 = new MDataTableBatchAction(dt2[0], _Conn);
                        m1.SetJoinPrimaryKeys(new string[] { columnName });
                        result = m1.Update();
                        if (!result)
                        {
                            sourceTable.DynamicData = dt2[0].DynamicData;
                        }
                    }
                    if (result && dt2[1].Rows.Count > 0)
                    {
                        dt2[1].DynamicData = action;
                        MDataTableBatchAction m2 = new MDataTableBatchAction(dt2[1], _Conn);
                        m2.SetJoinPrimaryKeys(new string[] { columnName });
                        result = m2.Insert(!dt2[1].Rows[0].PrimaryCell.IsNullOrEmpty);
                        if (!result)
                        {
                            sourceTable.DynamicData = dt2[1].DynamicData;
                        }
                    }
                     */
                    #endregion

                }
                else
                {
                    // action.BeginTransation();
                    foreach (MDataRow row in mdt.Rows)
                    {
                        #region 循环处理
                        action.ResetTable(row, false);
                        string where = SqlCreate.GetWhere(action.DataBaseType, GetJoinPrimaryCell(row));
                        bool isExists = action.Exists(where);
                        if (action.RecordsAffected == -2)
                        {
                            result = false;
                        }
                        else
                        {
                            if (!isExists)
                            {
                                action.AllowInsertID = !row.PrimaryCell.IsNullOrEmpty;
                                action.Data.SetState(1, BreakOp.Null);
                                result = action.Insert(InsertOp.None);
                            }
                            else
                            {
                                action.Data.SetState(2);
                                result = action.Update(where);
                            }
                        }
                        if (!result)
                        {
                            string msg = "Error On : MDataTable.AcceptChanges.Auto." + mdt.TableName + " : [" + where + "] : " + action.DebugInfo;
                            sourceTable.DynamicData = msg;
                            Log.Write(msg, LogType.DataBase);
                            break;
                        }
                        #endregion
                    }

                }
                action.dalHelper.IsRecordDebugInfo = true;//恢复SQL日志记录
                if (_dalHelper == null)
                {
                    action.EndTransation();
                }
                else
                {
                    action.dalHelper = sourceHelper;//还原
                }
            }

            return result;
        }

        internal bool Delete()
        {
            bool hasFK = (jointPrimaryIndex != null && jointPrimaryIndex.Count > 1) || mdt.Columns.JointPrimary.Count > 1;
            if (hasFK)
            {
                return NormalDelete();
            }
            else
            {
                return BulkCopyDelete();//只有一个主键，没有外键关联，同时只有基础类型。
            }
        }


        #region 批量插入
        internal bool MsSqlBulkCopyInsert(bool keepid)
        {
            SqlTransaction sqlTran = null;
            SqlConnection con = null;
            bool isCreateDal = false;
            try
            {
                CheckGUIDAndDateTime(DataBaseType.MsSql);
                string conn = AppConfig.GetConn(_Conn);
                if (_dalHelper == null)
                {
                    if (IsTruncate)
                    {
                        isCreateDal = true;
                        _dalHelper = DalCreate.CreateDal(conn);
                    }
                    else
                    {
                        con = new SqlConnection(conn);
                        con.Open();
                    }
                }
                bool isGoOn = true;
                if (_dalHelper != null)
                {
                    if (IsTruncate)
                    {
                        _dalHelper.IsOpenTrans = true;
                        if (_dalHelper.ExeNonQuery(string.Format(SqlCreate.TruncateTable, SqlFormat.Keyword(mdt.TableName, dalTypeTo)), false) == -2)
                        {
                            isGoOn = false;
                            sourceTable.DynamicData = _dalHelper.DebugInfo;
                            Log.Write(_dalHelper.DebugInfo.ToString(), LogType.DataBase);
                        }
                    }
                    if (isGoOn)
                    {
                        con = _dalHelper.Con as SqlConnection;
                        _dalHelper.OpenCon(null, AllowConnLevel.MasterBackup);//如果未开启，则开启，打开链接后，如果以前没执行过数据，事务对象为空，这时会产生事务对象
                        sqlTran = _dalHelper._tran as SqlTransaction;
                    }
                }
                if (isGoOn)
                {
                    using (SqlBulkCopy sbc = new SqlBulkCopy(con, (keepid ? SqlBulkCopyOptions.KeepIdentity : SqlBulkCopyOptions.Default) | SqlBulkCopyOptions.FireTriggers, sqlTran))
                    {
                        sbc.BatchSize = 100000;
                        sbc.DestinationTableName = SqlFormat.Keyword(mdt.TableName, DataBaseType.MsSql);
                        sbc.BulkCopyTimeout = AppConfig.DB.CommandTimeout;
                        foreach (MCellStruct column in mdt.Columns)
                        {
                            sbc.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                        }
                        if (AppConfig.IsAspNetCore)
                        {
                            sbc.WriteToServer(mdt.ToDataTable());
                        }
                        else
                        {
                            sbc.WriteToServer(mdt);
                        }
                    }
                }
                return true;
            }
            catch (Exception err)
            {
                sourceTable.DynamicData = err;
                Log.Write(err, LogType.DataBase);
            }
            finally
            {
                if (_dalHelper == null)
                {
                    con.Close();
                    con = null;
                }
                else if (isCreateDal)
                {
                    _dalHelper.EndTransaction();
                    _dalHelper.Dispose();
                }
            }
            return false;
        }
        internal bool OracleBulkCopyInsert()
        {
            CheckGUIDAndDateTime(DataBaseType.Oracle);
            string conn = ConnBean.Create(_Conn).ConnString;
            Assembly ass = OracleDal.GetAssembly();
            object sbc = ass.CreateInstance("Oracle.DataAccess.Client.OracleBulkCopy", false, BindingFlags.CreateInstance, null, new object[] { conn }, null, null);
            Type sbcType = sbc.GetType();
            try
            {

                sbcType.GetProperty("BatchSize").SetValue(sbc, 100000, null);
                sbcType.GetProperty("BulkCopyTimeout").SetValue(sbc, AppConfig.DB.CommandTimeout, null);
                sbcType.GetProperty("DestinationTableName").SetValue(sbc, SqlFormat.Keyword(mdt.TableName, DataBaseType.Oracle), null);
                PropertyInfo cInfo = sbcType.GetProperty("ColumnMappings");
                object cObj = cInfo.GetValue(sbc, null);
                MethodInfo addMethod = cInfo.PropertyType.GetMethods()[4];
                foreach (MCellStruct column in mdt.Columns)
                {
                    addMethod.Invoke(cObj, new object[] { column.ColumnName, column.ColumnName });
                }

                sbcType.GetMethods()[4].Invoke(sbc, new object[] { mdt });

                return true;
            }
            catch (Exception err)
            {
                if (err.InnerException != null)
                {
                    err = err.InnerException;
                }
                sourceTable.DynamicData = err;
                Log.Write(err, LogType.DataBase);
                return false;
            }
            finally
            {
                sbcType.GetMethod("Dispose").Invoke(sbc, null);
            }
            //using (Oracle.DataAccess.Client.OracleBulkCopy sbc = new OracleBulkCopy(conn, OracleBulkCopyOptions.Default))
            //{
            //    sbc.BatchSize = 100000;
            //    sbc.DestinationTableName = mdt.TableName;
            //    foreach (MCellStruct column in mdt.Columns)
            //    {
            //        sbc.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            //    }
            //    sbc.WriteToServer(mdt);
            //}
            //return true;



        }
        bool IsAllowBulkCopy(DataBaseType dalType)
        {
            if (!AppConfig.IsAspNetCore)
            {
                foreach (MCellStruct st in mdt.Columns)
                {
                    switch (DataType.GetGroup(st.SqlType))
                    {
                        case 999:
                            return false;
                    }
                }
                try
                {
                    if (dalType == DataBaseType.Oracle && !HasSqlLoader())
                    {
                        return false;
                    }
                    string path = Path.GetTempPath() + "t.t";
                    if (!File.Exists(path))
                    {
                        File.Create(path).Close();//检测文件夹的读写权限
                    }
                    return IOHelper.Delete(path);
                }
                catch
                {

                }
            }
            return false;
        }
        internal bool LoadDataInsert(DataBaseType dalType, bool keepid)
        {
            bool fillGUID = CheckGUIDAndDateTime(dalType);
            bool isNeedCreateDal = (_dalHelper == null);
            if (isNeedCreateDal && dalType != DataBaseType.Oracle)
            {
                _dalHelper = DalCreate.CreateDal(_Conn);
                _dalHelper.IsWriteLogOnError = false;
            }
            string path = MDataTableToFile(mdt, fillGUID ? true : keepid, dalType);
            string formatSql = dalType == DataBaseType.MySql ? SqlCreate.MySqlBulkCopySql : SqlCreate.OracleBulkCopySql;
            string sql = string.Format(formatSql, path, SqlFormat.Keyword(mdt.TableName, dalType),
                AppConst.SplitChar, SqlCreate.GetColumnName(mdt.Columns, keepid, dalType));
            if (dalType == DataBaseType.Oracle)
            {
                string ctlPath = CreateCTL(sql, path);
                sql = string.Format(SqlCreate.OracleSqlldr, "sa/123456@ORCL", ctlPath);//只能用进程处理
            }
            try
            {
                if (dalType == DataBaseType.Oracle)
                {
                    return ExeSqlLoader(sql);
                }
                else
                {
                    bool isGoOn = true;
                    if (IsTruncate)
                    {
                        _dalHelper.IsOpenTrans = true;//开启事务
                        isGoOn = _dalHelper.ExeNonQuery(string.Format(SqlCreate.TruncateTable, SqlFormat.Keyword(mdt.TableName, dalTypeTo)), false) != -2;
                    }
                    if (isGoOn && _dalHelper.ExeNonQuery(sql, false) != -2)
                    {
                        return true;
                    }

                }

            }
            catch (Exception err)
            {
                if (err.InnerException != null)
                {
                    err = err.InnerException;
                }
                sourceTable.DynamicData = err;
                Log.Write(err, LogType.DataBase);
            }
            finally
            {
                if (isNeedCreateDal && _dalHelper != null)
                {
                    _dalHelper.EndTransaction();
                    _dalHelper.Dispose();
                    _dalHelper = null;
                }
                IOHelper.Delete(path);//删除文件。
            }
            return false;
        }
        private static string CreateCTL(string sql, string path)
        {
            path = path.Replace(".csv", ".ctl");
            IOHelper.Write(path, sql);
            return path;
        }
        private static string MDataTableToFile(MDataTable dt, bool keepid, DataBaseType dalType)
        {
            string path = Path.GetTempPath() + dt.TableName + ".csv";
            using (StreamWriter sw = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                MCellStruct ms;
                string value;
                foreach (MDataRow row in dt.Rows)
                {
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        #region 设置值
                        ms = dt.Columns[i];
                        if (!keepid && ms.IsAutoIncrement)
                        {
                            continue;
                        }
                        else if (dalType == DataBaseType.MySql && row[i].IsNull)
                        {
                            sw.Write("\\N");//Mysql用\N表示null值。
                        }
                        else
                        {
                            value = row[i].ToString();
                            if (ms.SqlType == SqlDbType.Bit)
                            {
                                int v = (value.ToLower() == "true" || value == "1") ? 1 : 0;
                                if (dalType == DataBaseType.MySql)
                                {
                                    byte[] b = new byte[1];
                                    b[0] = (byte)v;
                                    value = System.Text.Encoding.UTF8.GetString(b);//mysql必须用字节存档。
                                }
                                else
                                {
                                    value = v.ToString();
                                }
                            }
                            else
                            {
                                value = value.Replace("\\", "\\\\");//处理转义符号
                            }
                            sw.Write(value);
                        }

                        if (i != dt.Columns.Count - 1)//不是最后一个就输出
                        {
                            sw.Write(AppConst.SplitChar);
                        }
                        #endregion
                    }
                    sw.Write('|');
                    sw.WriteLine();
                    sw.Write('|');
                }
            }
            if (Path.DirectorySeparatorChar == '\\')
            {
                path = path.Replace(@"\", @"\\");
            }
            return path;
        }
        /// <summary>
        /// 检测GUID，若空，补值。
        /// </summary>
        private bool CheckGUIDAndDateTime(DataBaseType dal)
        {
            bool fillGUID = false;
            int groupID;
            for (int i = 0; i < mdt.Columns.Count; i++)
            {
                MCellStruct ms = mdt.Columns[i];
                groupID = DataType.GetGroup(ms.SqlType);
                if (groupID == 2)
                {
                    for (int j = 0; j < mdt.Rows.Count; j++)
                    {
                        if (dal == DataBaseType.MsSql && mdt.Rows[j][i].StringValue == DateTime.MinValue.ToString())
                        {
                            mdt.Rows[j][i].Value = SqlDateTime.MinValue;
                        }
                        else if (dal == DataBaseType.Oracle && mdt.Rows[j][i].StringValue == SqlDateTime.MinValue.ToString())
                        {
                            mdt.Rows[j][i].Value = SqlDateTime.MinValue;
                        }
                    }
                }
                else if (ms.IsPrimaryKey && (groupID == 4 || (groupID == 0 && ms.MaxSize >= 36)))
                {
                    string defaultValue = Convert.ToString(ms.DefaultValue);
                    bool isGuid = defaultValue == "" || defaultValue == "newid" || defaultValue == SqlValue.Guid;
                    if (isGuid && !fillGUID)
                    {
                        fillGUID = true;
                    }
                    for (int k = 0; k < mdt.Rows.Count; k++)
                    {
                        if (mdt.Rows[k][i].IsNullOrEmpty)
                        {
                            mdt.Rows[k][i].Value = isGuid ? Guid.NewGuid().ToString() : defaultValue;
                        }
                    }
                }
            }
            return fillGUID;
        }
        #endregion

        #region 批量更新
        internal bool BulkCopyUpdate()
        {
            int count = 0, pageSize = 5000;
            MDataTable dt = null;
            bool result = false;
            using (MAction action = new MAction(mdt.TableName, _Conn))
            {
                action.SetAopState(Aop.AopOp.CloseAll);
                MCellStruct keyColumn = jointPrimaryIndex != null ? mdt.Columns[jointPrimaryIndex[0]] : mdt.Columns.FirstPrimary;
                if (action.DataBaseVersion.StartsWith("08"))
                {
                    pageSize = 1000;
                }
                else if (keyColumn.SqlType == SqlDbType.UniqueIdentifier)
                {
                    pageSize = 2000;
                }
                count = mdt.Rows.Count / pageSize + 1;
                DalBase sourceHelper = action.dalHelper;
                if (_dalHelper != null)
                {
                    action.dalHelper = _dalHelper;
                }
                else
                {
                    action.BeginTransation();
                }


                string columnName = keyColumn.ColumnName;
                for (int i = 0; i < count; i++)
                {
                    dt = mdt.Select(i + 1, pageSize, null);//分页读取
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        #region 核心逻辑
                        string whereIn = SqlCreate.GetWhereIn(keyColumn, dt.GetColumnItems<string>(columnName, BreakOp.NullOrEmpty, true), action.DataBaseType);
                        MDataTable dtData = action.Select(whereIn);//获取远程数据。
                        dtData.Load(dt, keyColumn);//重新加载赋值。
                        //处理如果存在IsDeleted，会被转Update（导致后续无法Insert）、外层也有判断，不会进来。
                        result = action.Delete(whereIn, true);

                        if (result)
                        {
                            dtData.DynamicData = action;
                            result = dtData.AcceptChanges(AcceptOp.InsertWithID);
                        }
                        if (!result)
                        {
                            if (_dalHelper == null)//有外部时由外部控制，没外部时直接回滚。
                            {
                                action.RollBack();//回滚被删除的代码。
                            }
                            break;
                        }
                        #endregion
                    }
                }
                if (_dalHelper == null)
                {
                    action.EndTransation();
                }
                else
                {
                    action.dalHelper = sourceHelper;//还原。
                }
            }
            return result;
        }
        #endregion

        #region 批量删除
        internal bool BulkCopyDelete()
        {
            int count = 0, pageSize = 5000;
            MDataTable dt = null;
            bool result = false;
            using (MAction action = new MAction(mdt.TableName, _Conn))
            {
                action.SetAopState(Aop.AopOp.CloseAll);
                if (action.DataBaseVersion.StartsWith("08"))
                {
                    pageSize = 1000;
                }
                count = mdt.Rows.Count / pageSize + 1;
                DalBase sourceHelper = action.dalHelper;
                if (_dalHelper != null)
                {
                    action.dalHelper = _dalHelper;
                }
                else
                {
                    action.BeginTransation();
                }

                MCellStruct keyColumn = jointPrimaryIndex != null ? mdt.Columns[jointPrimaryIndex[0]] : mdt.Columns.FirstPrimary;
                string columnName = keyColumn.ColumnName;
                for (int i = 0; i < count; i++)
                {
                    dt = mdt.Select(i + 1, pageSize, null);//分页读取
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        #region 核心逻辑
                        string whereIn = SqlCreate.GetWhereIn(keyColumn, dt.GetColumnItems<string>(columnName, BreakOp.NullOrEmpty, true), action.DataBaseType);
                        result = action.Delete(whereIn) || action.RecordsAffected == 0;
                        if (result)
                        {
                            sourceTable.RecordsAffected += action.RecordsAffected;//记录总删除的数量。
                        }
                        else
                        {
                            sourceTable.RecordsAffected = 0;
                            string msg = "Error On : MDataTable.AcceptChanges.Delete." + mdt.TableName + " : where (" + whereIn + ") : " + action.DebugInfo;
                            sourceTable.DynamicData = msg;
                            Log.Write(msg, LogType.DataBase);
                            break;
                        }
                        #endregion
                    }
                }
                if (_dalHelper == null)
                {
                    action.EndTransation();
                }
                else
                {
                    action.dalHelper = sourceHelper;//还原。
                }
            }
            return result;
        }
        #endregion

        #region 注释掉
        /*
        internal bool SybaseBulkCopyInsert()
        {

            // string a, b, c;
            string conn = DalCreate.FormatConn(DalType.Sybase, AppConfig.GetConn(_Conn));

            using (Sybase.Data.AseClient.AseBulkCopy sbc = new Sybase.Data.AseClient.AseBulkCopy(conn, Sybase.Data.AseClient.AseBulkCopyOptions.Keepidentity))
            {
                sbc.BatchSize = 100000;
                sbc.DestinationTableName = mdt.TableName;
                foreach (MCellStruct column in mdt.Columns)
                {
                    Sybase.Data.AseClient.AseBulkCopyColumnMapping ac = new Sybase.Data.AseClient.AseBulkCopyColumnMapping();
                    ac.SourceColumn = ac.DestinationColumn = column.ColumnName;
                    sbc.ColumnMappings.Add(ac);
                }
                sbc.WriteToServer(mdt.ToDataTable());
            }
            return true;


            //Assembly ass = SybaseDal.GetAssembly();

            //object sbc = ass.CreateInstance("Sybase.Data.AseClient.AseBulkCopy", false, BindingFlags.CreateInstance, null, new object[] { conn }, null, null);

            //Type sbcType = sbc.GetType();
            //try
            //{

            //    sbcType.GetProperty("BatchSize").SetValue(sbc, 100000, null);
            //    sbcType.GetProperty("DestinationTableName").SetValue(sbc, SqlFormat.Keyword(mdt.TableName, DalType.Sybase), null);
            //    PropertyInfo cInfo = sbcType.GetProperty("ColumnMappings");
            //    object cObj = cInfo.GetValue(sbc, null);
            //    MethodInfo addMethod = cInfo.PropertyType.GetMethods()[2];
            //    foreach (MCellStruct column in mdt.Columns)
            //    {
            //        object columnMapping = ass.CreateInstance("Sybase.Data.AseClient.AseBulkCopyColumnMapping", false, BindingFlags.CreateInstance, null, new object[] { column.ColumnName, column.ColumnName }, null, null);
            //        addMethod.Invoke(cObj, new object[] { columnMapping });
            //    }
            //    //Oracle.DataAccess.Client.OracleBulkCopy ttt = sbc as Oracle.DataAccess.Client.OracleBulkCopy;
            //    //ttt.WriteToServer(mdt);
            //    sbcType.GetMethods()[14].Invoke(sbc, new object[] { mdt.ToDataTable() });
            //    return true;
            //}
            //catch (Exception err)
            //{
            //    Log.Write(err);
            //    return false;
            //}
            //finally
            //{
            //    sbcType.GetMethod("Dispose").Invoke(sbc, null);
            //}
        } 
        */
        #endregion

        internal bool NomalInsert(bool keepid)
        {
            bool result = true;
            using (MAction action = new MAction(mdt.TableName, _Conn))
            {
                DalBase sourceHelper = action.dalHelper;
                action.SetAopState(Aop.AopOp.CloseAll);
                if (_dalHelper != null)
                {
                    action.dalHelper = _dalHelper;
                }
                else
                {
                    action.BeginTransation();//事务由外部控制
                }
                action.dalHelper.IsRecordDebugInfo = false || AppDebug.IsContainSysSql;//屏蔽SQL日志记录
                if (keepid)
                {
                    action.SetidentityInsertOn();
                }
                bool isGoOn = true;
                if (IsTruncate)
                {
                    if (dalTypeTo == DataBaseType.Txt || dalTypeTo == DataBaseType.Xml)
                    {
                        action.Delete("1=1");
                    }
                    else if (action.dalHelper.ExeNonQuery(string.Format(SqlCreate.TruncateTable, SqlFormat.Keyword(action.TableName, dalTypeTo)), false) == -2)
                    {
                        isGoOn = false;
                        sourceTable.DynamicData = action.DebugInfo;
                        Log.Write(action.DebugInfo, LogType.DataBase);
                    }
                }
                if (isGoOn)
                {
                    MDataRow row;
                    for (int i = 0; i < mdt.Rows.Count; i++)
                    {
                        row = mdt.Rows[i];
                        action.ResetTable(row, false);
                        action.Data.SetState(1, BreakOp.Null);
                        result = action.Insert(InsertOp.None);
                        sourceTable.RecordsAffected = i;
                        if (!result)
                        {
                            string msg = "Error On : MDataTable.AcceptChanges.Insert." + mdt.TableName + " : [" + row.PrimaryCell.Value + "] : " + action.DebugInfo;
                            sourceTable.DynamicData = msg;
                            Log.Write(msg, LogType.DataBase);
                            break;
                        }
                    }
                }
                if (keepid)
                {
                    action.SetidentityInsertOff();
                }
                if (_dalHelper == null)
                {
                    action.EndTransation();
                }
                action.dalHelper.IsRecordDebugInfo = true;//恢复SQL日志记录
                action.dalHelper = sourceHelper;//恢复原来，避免外来的链接被关闭。
            }
            return result;
        }
        internal bool NormalUpdate()
        {
            List<int> indexList = new List<int>();
            bool result = true;
            using (MAction action = new MAction(mdt.TableName, _Conn))
            {
                action.SetAopState(Aop.AopOp.CloseAll);
                DalBase sourceHelper = action.dalHelper;
                if (_dalHelper != null)
                {
                    action.dalHelper = _dalHelper;
                }
                else
                {
                    action.BeginTransation();
                }
                action.dalHelper.IsRecordDebugInfo = false || AppDebug.IsContainSysSql;//屏蔽SQL日志记录

                MDataRow row;
                for (int i = 0; i < mdt.Rows.Count; i++)
                {
                    row = mdt.Rows[i];
                    if (row.GetState(true) > 1)
                    {
                        action.ResetTable(row, false);
                        string where = SqlCreate.GetWhere(action.DataBaseType, GetJoinPrimaryCell(row));
                        result = action.Update(where) || action.RecordsAffected == 0;//没有可更新的数据，也返回true
                        if (action.RecordsAffected > 0)
                        {
                            sourceTable.RecordsAffected += action.RecordsAffected;//记录总更新的数量。
                        }
                        if (!result)
                        {
                            sourceTable.RecordsAffected = 0;
                            string msg = "Error On : MDataTable.AcceptChanges.Update." + mdt.TableName + " : where (" + where + ") : " + action.DebugInfo;
                            sourceTable.DynamicData = msg;
                            Log.Write(msg, LogType.DataBase);
                            break;
                        }
                        else
                        {
                            indexList.Add(i);
                        }
                    }
                }
                action.dalHelper.IsRecordDebugInfo = true;//恢复SQL日志记录
                if (_dalHelper == null)
                {
                    action.EndTransation();
                }
                else
                {
                    action.dalHelper = sourceHelper;//恢复原来，避免外来的链接被关闭。
                }
            }
            if (result)
            {
                foreach (int index in indexList)
                {
                    mdt.Rows[index].SetState(0);
                }
                indexList.Clear();
                indexList = null;
            }
            return result;
        }
        internal bool NormalDelete()
        {
            bool result = true;
            using (MAction action = new MAction(mdt.TableName, _Conn))
            {
                action.SetAopState(Aop.AopOp.CloseAll);
                DalBase sourceHelper = action.dalHelper;
                if (_dalHelper != null)
                {
                    action.dalHelper = _dalHelper;
                }
                else
                {
                    action.BeginTransation();
                }
                action.dalHelper.IsRecordDebugInfo = false || AppDebug.IsContainSysSql;//屏蔽SQL日志记录

                MDataRow row;
                for (int i = 0; i < mdt.Rows.Count; i++)
                {
                    row = mdt.Rows[i];
                    action.ResetTable(row, false);
                    string where = SqlCreate.GetWhere(action.DataBaseType, GetJoinPrimaryCell(row));
                    result = action.Delete(where) || action.RecordsAffected == 0;//没有可更新的数据，也返回true
                    if (action.RecordsAffected > 0)
                    {
                        sourceTable.RecordsAffected += action.RecordsAffected;//记录总删除的数量。
                    }
                    if (!result)
                    {
                        sourceTable.RecordsAffected = 0;
                        string msg = "Error On : MDataTable.AcceptChanges.Delete." + mdt.TableName + " : where (" + where + ") : " + action.DebugInfo;
                        sourceTable.DynamicData = msg;
                        Log.Write(msg, LogType.DataBase);
                        break;
                    }
                }
                action.dalHelper.IsRecordDebugInfo = true;//恢复SQL日志记录
                if (_dalHelper == null)
                {
                    action.EndTransation();
                }
                else
                {
                    action.dalHelper = sourceHelper;//恢复原来，避免外来的链接被关闭。
                }
            }

            return result;
        }
    }
    internal partial class MDataTableBatchAction
    {
        bool hasSqlLoader = false;
        private bool HasSqlLoader()
        {
            hasSqlLoader = false;
            Process proc = new Process();
            proc.StartInfo.FileName = "sqlldr";
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;

            proc.OutputDataReceived += new DataReceivedEventHandler(proc_OutputDataReceived);
            proc.Start();
            proc.BeginOutputReadLine();
            proc.WaitForExit();
            return hasSqlLoader;
        }

        void proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!hasSqlLoader)
            {
                hasSqlLoader = e.Data.StartsWith("SQL*Loader:");
            }
        }
        //已经实现，但没有事务，所以暂时先不引入。
        private bool ExeSqlLoader(string arg)
        {
            try
            {
                Process proc = new Process();
                proc.StartInfo.FileName = "sqlldr";
                proc.StartInfo.Arguments = arg;
                proc.Start();
                proc.WaitForExit();
                return true;
            }
            catch
            {

            }
            return false;
        }
    }
}
