using System;
using System.Data.Common;
using System.Data;
using System.Diagnostics;
using CYQ.Data.SQL;
using System.Collections.Generic;
using CYQ.Data.Tool;
using System.Data.SqlTypes;
using System.Threading;
using CYQ.Data.Orm;


namespace CYQ.Data
{

    /// <summary>
    /// 数据库操作基类 （模板模式：Template Method）
    /// 属性管理
    /// </summary>
    internal abstract partial class DalBase : IDisposable
    {
        #region 对外公开的属性
        /// <summary>
        /// 记录SQL语句信息
        /// </summary>
        public System.Text.StringBuilder DebugInfo = new System.Text.StringBuilder();
        /// <summary>
        /// 执行命令时所受影响的行数（-2为发生异常）。
        /// </summary>
        public int RecordsAffected = 0;
        /// <summary>
        /// 当前是否开启事务
        /// </summary>
        public bool IsOpenTrans = false;
        /// <summary>
        /// 原生态传进来的配置名（或链接）
        /// </summary>
        public string ConnName
        {
            get
            {
                return !ConnObj.Master.IsBackup ? ConnObj.Master.ConnName : ConnObj.BackUp.ConnName;
            }
        }
        /// <summary>
        /// 数据库主从备链接管理对象;
        /// </summary>
        public ConnObject ConnObj;

        /// <summary>
        /// 当前使用中的链接对象
        /// </summary>
        public ConnBean UsingConnBean;
        /// <summary>
        /// 获得链接的数据库名称
        /// </summary>
        public virtual string DataBaseName
        {
            get
            {
                if (!string.IsNullOrEmpty(_con.Database))
                {
                    return _con.Database;
                }
                else if (DataBaseType == DataBaseType.Oracle)
                {
                    // (DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT = 1521)))(CONNECT_DATA =(Sid = Aries)))
                    int i = _con.DataSource.LastIndexOf('=') + 1;
                    return _con.DataSource.Substring(i).Trim(' ', ')');
                }
                else if (DataBaseType == DataBaseType.DB2)
                {
                    string conn = _con.ConnectionString;
                    int i = conn.IndexOf("database=", StringComparison.OrdinalIgnoreCase);
                    int end = conn.IndexOf(';', i);
                    if (end == -1)
                    {
                        return conn.Substring(i + 9);
                    }
                    else
                    {
                        return conn.Substring(i + 9, end - i - 9);
                    }
                }
                else
                {
                    return System.IO.Path.GetFileNameWithoutExtension(_con.DataSource);
                }
            }
        }
        private static MDictionary<string, string> _VersionCache = new MDictionary<string, string>();
        private string _Version = string.Empty;
        /// <summary>
        /// 数据库的版本号
        /// </summary>
        public string Version
        {
            get
            {
                if (string.IsNullOrEmpty(_Version))
                {
                    if (_VersionCache.ContainsKey(ConnName))
                    {
                        _Version = _VersionCache[ConnName];
                    }
                    else
                    {
                        if (IsOpenTrans && UsingConnBean.IsSlave)// && 事务操作时，如果在从库，切回主库
                        {
                            ResetConn(ConnObj.Master);
                        }
                        if (OpenCon(UsingConnBean, AllowConnLevel.MaterBackupSlave))//这里可能切换链接
                        {
                            _Version = _con.ServerVersion;
                            if (!_VersionCache.ContainsKey(ConnName))
                            {
                                _VersionCache.Set(ConnName, _Version);
                            }
                            if (!IsOpenTrans)//避免把事务给关闭了。
                            {
                                CloseCon();
                            }
                        }
                    }

                }
                return _Version;
            }
        }
        /// <summary>
        /// 当前操作的数据库类型
        /// </summary>
        public DataBaseType DataBaseType
        {
            get
            {
                return ConnObj.Master.ConnDataBaseType;
            }
        }
        #endregion


        /// <summary>
        /// 是否允许进入写日志中模块(默认true)
        /// </summary>
        public bool IsWriteLogOnError = true;

        protected bool isUseUnsafeModeOnSqlite = false;
        private bool isAllowResetConn = true;//如果执行了非查询之后，为了数据的一致性，不允许切换到Slave数据库链接
        private string tempSql = string.Empty;//附加信息，包括调试信息



        private IsolationLevel _TranLevel = IsolationLevel.ReadCommitted;
        internal IsolationLevel TranLevel
        {
            get
            {
                if (_tran != null && _com != null && _com.Transaction != null)
                {
                    return _com.Transaction.IsolationLevel;
                }
                return _TranLevel;
            }
            set
            {
                if (_tran != null && _com != null && _com.Transaction != null)
                {
                    Error.Throw("IsolationLevel is readonly when transaction is begining!");
                }
                else
                {
                    _TranLevel = value;
                }
            }
        }
        protected DbProviderFactory _fac = null;
        protected DbConnection _con = null;
        protected DbCommand _com;
        internal DbTransaction _tran;
        private Stopwatch _watch;



        public DbConnection Con
        {
            get
            {
                return _con;
            }
        }
        public DbCommand Com
        {
            get
            {
                return _com;
            }
        }
        private bool _IsRecordDebugInfo = true;
        /// <summary>
        /// 是否允许记录SQL语句 (内部操作会关掉此值为False）
        /// </summary>
        public bool IsRecordDebugInfo
        {
            get
            {
                return (AppConfig.Debug.OpenDebugInfo || AppConfig.Debug.SqlFilter > -1) && _IsRecordDebugInfo;
            }
            set
            {
                _IsRecordDebugInfo = value;
            }
        }
        public DalBase(ConnObject co)
        {
            this.ConnObj = co;
            this.UsingConnBean = co.Master;
            _fac = GetFactory();
            _con = _fac.CreateConnection();
            try
            {
                _con.ConnectionString = co.Master.ConnString;
            }
            catch (Exception err)
            {
                Error.Throw("check the connectionstring is be ok!" + AppConst.BR + "error:" + err.Message + AppConst.BR + ConnName);
            }

            _com = _con.CreateCommand();
            if (_com != null)//Txt| Xml 时返回Null
            {
                _com.Connection = _con;
                _com.CommandTimeout = AppConfig.DB.CommandTimeout;
            }
            if (IsRecordDebugInfo)//开启秒表计算
            {
                _watch = new Stopwatch();
            }
            //if (AppConfig.DB.LockOnDbExe && dalType == DalType.Access)
            //{
            //    string dbName = DataBase;
            //    if (!_dbOperator.ContainsKey(dbName))
            //    {
            //        try
            //        {
            //            _dbOperator.Add(dbName, false);
            //        }
            //        catch
            //        {
            //        }
            //    }
            //}
            //_com.CommandTimeout = 1;
        }
        protected abstract DbProviderFactory GetFactory();
        #region 拿表、视图、存储过程等元数据。
        public virtual Dictionary<string, string> GetTables()
        {
            return GetSchemaDic(GetSchemaSql("U"));
        }
        public virtual Dictionary<string, string> GetViews()
        {
            return GetSchemaDic(GetSchemaSql("V"));
        }
        public virtual Dictionary<string, string> GetProcs()
        {
            return GetSchemaDic(GetSchemaSql("P"));
        }
        protected Dictionary<string, string> GetSchemaDic(string sql)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return null;
            }
            Dictionary<string, string> dic = null;
            string key = "UVPCache_" + StaticTool.GetHashKey(sql + ConnName);
            #region 缓存检测
            if (!string.IsNullOrEmpty(AppConfig.DB.SchemaMapPath))
            {
                string fullPath = AppConfig.RunPath + AppConfig.DB.SchemaMapPath + key + ".json";
                if (System.IO.File.Exists(fullPath))
                {
                    string json = IOHelper.ReadAllText(fullPath);
                    dic = JsonHelper.ToEntity<Dictionary<string, string>>(json);
                    if (dic != null && dic.Count > 0)
                    {
                        return dic;
                    }
                }
            }
            #endregion
            dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            IsRecordDebugInfo = false || AppDebug.IsContainSysSql;
            DbDataReader sdr = ExeDataReader(sql, false);
            IsRecordDebugInfo = true;
            if (sdr != null)
            {
                string tableName = string.Empty;
                while (sdr.Read())
                {
                    tableName = Convert.ToString(sdr["TableName"]);
                    if (!dic.ContainsKey(tableName))
                    {
                        dic.Add(tableName, Convert.ToString(sdr["Description"]));
                    }
                }
                sdr.Close();
                sdr = null;
            }
            #region 缓存设置
            if (!string.IsNullOrEmpty(AppConfig.DB.SchemaMapPath) && dic != null && dic.Count > 0)
            {
                string folderPath = AppConfig.RunPath + AppConfig.DB.SchemaMapPath;
                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }
                string json = JsonHelper.ToJson(dic);
                IOHelper.Write(folderPath + key + ".json", json);
            }
            #endregion
            return dic;
        }

        protected virtual string GetSchemaSql(string type)
        {
            return "";
        }
        #endregion
        #region 数据库链接切换相关逻辑
        /// <summary>
        /// 切换数据库（修改数据库链接）
        /// </summary>
        internal DBResetResult ChangeDatabase(string dbName)
        {
            if (_con.State == ConnectionState.Closed)//事务中。。不允许切换
            {
                try
                {
                    if (IsExistsDbNameWithCache(dbName))//新的数据库不存在。。不允许切换
                    {
                        string newConnString = GetConnString(dbName);
                        _con.ConnectionString = newConnString;
                        ConnObj = ConnObject.Create(dbName + "Conn");
                        ConnObj.Master.ConnName = dbName + "Conn";
                        ConnObj.Master.ConnString = newConnString;
                        return DBResetResult.Yes;
                    }
                    else
                    {
                        return DBResetResult.No_DBNoExists;
                    }
                }
                catch (Exception err)
                {
                    Log.Write(err, LogType.DataBase);
                }

            }
            return DBResetResult.No_Transationing;
        }

        //检测并切换数据库链接。
        internal DBResetResult ChangeDatabaseWithCheck(string dbTableName)//----------
        {
            if (IsOwnerOtherDb(dbTableName))//数据库名称变化了。
            {
                string dbName = dbTableName.Split('.')[0];
                return ChangeDatabase(dbName);

            }
            return DBResetResult.No_SaveDbName;
        }
        internal DalBase ResetDalBase(string dbTableName)
        {
            if (IsOwnerOtherDb(dbTableName))//是其它数据库名称。
            {
                if (_con.State != ConnectionState.Closed)//事务中。。创建新链接切换
                {
                    string dbName = dbTableName.Split('.')[0];
                    return DalCreate.CreateDal(GetConnString(dbName));
                }

            }
            return this;
        }
        /// <summary>
        /// 是否数据库名称变化了
        /// </summary>
        /// <param name="dbTableName"></param>
        /// <returns></returns>
        private bool IsOwnerOtherDb(string dbTableName)
        {
            int index = dbTableName.IndexOf('.');//DBName.TableName
            if (index > 0 && !dbTableName.Contains(" ")) //排除视图语句
            {

                string dbName = dbTableName.Split('.')[0];
                if (string.Compare(DataBaseName, dbName, StringComparison.OrdinalIgnoreCase) != 0 && ConnName.IndexOf(DataBaseName) == ConnName.LastIndexOf(DataBaseName))
                {
                    return true;
                }

            }
            return false;
        }
        protected string GetConnString(string dbName)
        {
            string newConn = AppConfig.GetConn(dbName + "Conn");
            if (!string.IsNullOrEmpty(newConn))
            {
                return ConnBean.Create(newConn).ConnString;
            }
            return UsingConnBean.ConnString.Replace(DataBaseName, dbName);
        }

        static MDictionary<string, bool> dbList = new MDictionary<string, bool>(3);
        private bool IsExistsDbNameWithCache(string dbName)
        {
            try
            {
                string key = DataBaseType.ToString() + "." + dbName;
                if (dbList.ContainsKey(key))
                {
                    return dbList[key];
                }
                bool result = IsExistsDbName(dbName);
                dbList.Add(key, result);
                return result;
            }
            catch
            {
                return true;
            }
        }
        protected abstract bool IsExistsDbName(string dbName);
        #endregion

        private int returnValue = -1;
        /// <summary>
        /// 存储过程返回值。
        /// </summary>
        public int ReturnValue
        {
            get
            {
                if (returnValue == -1 && _com != null && _com.Parameters != null && _com.Parameters.Count > 0)
                {
                    for (int i = _com.Parameters.Count - 1; i >= 0; i--)
                    {
                        if (_com.Parameters[i].Direction == ParameterDirection.ReturnValue)
                        {
                            int.TryParse(Convert.ToString(_com.Parameters[i].Value), out returnValue);
                            break;
                        }
                    }

                }
                return returnValue;
            }
            set
            {
                returnValue = value;
            }
        }
        /// <summary>
        /// 存储过程OutPut输出参数值。
        /// 如果只有一个输出，则为值；
        /// 如果有多个输出，则为Dictionary。
        /// </summary>
        public object OutPutValue
        {
            get
            {
                if (_com != null && _com.Parameters != null && _com.Parameters.Count > 0)
                {
                    Dictionary<string, object> opValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    object outPutValue = null;
                    foreach (DbParameter para in _com.Parameters)
                    {
                        if (para.Direction == ParameterDirection.Output)
                        {
                            opValues.Add(para.ParameterName, para.Value);
                            outPutValue = para.Value;
                        }
                    }
                    if (opValues.Count < 2)
                    {
                        opValues = null;
                        return outPutValue;
                    }
                    return opValues;
                }
                return null;
            }
        }
        public virtual char Pre
        {
            get
            {
                return '@';
            }
        }


        #region 参数化管理

        public bool AddParameters(string parameterName, object value)
        {
            return AddParameters(parameterName, value, DbType.String, -1, ParameterDirection.Input);
        }
        public virtual bool AddParameters(string parameterName, object value, DbType dbType, int size, ParameterDirection direction)
        {
            if (DataBaseType == DataBaseType.Oracle)
            {
                parameterName = parameterName.Replace(":", "").Replace("@", "");
                if (dbType == DbType.String && size > 4000)
                {
                    AddCustomePara(parameterName, size == int.MaxValue ? ParaType.CLOB : ParaType.NCLOB, value, null);
                    return true;
                }
            }
            else
            {
                parameterName = parameterName.Substring(0, 1) == Pre.ToString() ? parameterName : Pre + parameterName;
            }
            if (Com == null)
            {
                return false;
            }
            if (Com.Parameters.Contains(parameterName))//已经存在，不添加
            {
                return false;
            }
            DbParameter para = _com.CreateParameter();
            para.ParameterName = parameterName;
            para.Value = value == null ? DBNull.Value : value;
            if (dbType == DbType.Time)// && dalType != DalType.MySql
            {
                para.DbType = DbType.String;
            }
            else
            {
                if (dbType == DbType.DateTime && value != null)
                {
                    string time = Convert.ToString(value);
                    if (DataBaseType == DataBaseType.MsSql && time == DateTime.MinValue.ToString())
                    {
                        para.Value = SqlDateTime.MinValue;
                    }
                    else if (DataBaseType == DataBaseType.MySql && (time == SqlDateTime.MinValue.ToString() || time == DateTime.MinValue.ToString()))
                    {
                        para.Value = DateTime.MinValue;
                    }
                }
                para.DbType = dbType;
            }
            if (dbType == DbType.Binary && DataBaseType == DataBaseType.MySql)//（mysql不能设定长度，否则会报索引超出了数组界限错误【已过时，旧版本的MySql.Data.dll不能指定长度】）。
            {
                if (value != null)
                {
                    byte[] bytes = value as byte[];
                    para.Size = bytes.Length;//新版本的MySql.Data.dll 修正了长度指定（不指定就没数据进去），所以又要指定长度，Shit
                }
                else
                {
                    para.Size = -1;
                }
            }
            else if (dbType != DbType.Binary && size > 0)
            {
                if (size != para.Size)
                {
                    para.Size = size;
                }
            }
            para.Direction = direction;
            if (para.DbType == DbType.String && para.Value != null)
            {
                para.Value = Convert.ToString(para.Value);
            }
            Com.Parameters.Add(para);
            return true;
        }
        internal virtual void AddCustomePara(string paraName, ParaType paraType, object value, string typeName)
        {
            switch (paraType)
            {

                case ParaType.OutPut:
                    AddParameters(paraName, null, DbType.String, 2000, ParameterDirection.Output);
                    break;
                case ParaType.InputOutput:
                    AddParameters(paraName, null, DbType.String, 2000, ParameterDirection.InputOutput);
                    break;
                case ParaType.ReturnValue:
                    AddParameters(paraName, null, DbType.Int32, 32, ParameterDirection.ReturnValue);
                    break;
            }
        }
        //internal virtual void AddCustomePara(string paraName, ParaType paraType, object value)
        //{
        //}
        //public abstract DbParameter GetNewParameter();

        public void ClearParameters()
        {
            if (_com != null && _com.Parameters != null)
            {
                _com.Parameters.Clear();
            }
        }
        /// <summary>
        /// 处理内置的MSSQL和Oracle两种存储过程分页
        /// </summary>
        protected virtual void AddReturnPara() { }

        private void SetCommandText(string commandText, bool isProc)
        {
            if (OracleDal.clientType > 0)
            {
                Type t = _com.GetType();
                System.Reflection.PropertyInfo pi = t.GetProperty("BindByName");
                if (pi != null)
                {
                    pi.SetValue(_com, true, null);
                }
            }
            _com.CommandText = isProc ? commandText : SqlFormat.Compatible(commandText, DataBaseType, false);
            if (!isProc && DataBaseType == DataBaseType.SQLite && _com.CommandText.Contains("charindex"))
            {
                _com.CommandText += " COLLATE NOCASE";//忽略大小写
            }
            //else if (isProc && dalType == DalType.MySql)
            //{
            //    _com.CommandText = "Call " + _com.CommandText;
            //}
            _com.CommandType = isProc ? CommandType.StoredProcedure : CommandType.Text;
            //if (isProc)
            //{
            //    if (commandText.Contains("SelectBase") && !_com.Parameters.Contains("ReturnValue"))
            //    {
            //        AddReturnPara();
            //        //检测是否存在分页存储过程，若不存在，则创建。
            //        Tool.DBTool.CreateSelectBaseProc(DataBaseType, ConnName);//内部分检测是否已创建过。
            //    }
            //}
            //else
            //{
            //上面if代码被注释了，下面代码忘了加!isProc判断，现补上。 取消多余的参数，新加的小贴心，过滤掉用户不小心写多的参数。
            if (!isProc && _com != null && _com.Parameters != null && _com.Parameters.Count > 0)
            {
                bool needToReplace = (DataBaseType == DataBaseType.Oracle || DataBaseType == DataBaseType.MySql || DataBaseType == Data.DataBaseType.PostgreSQL) && _com.CommandText.Contains("@");
                string paraName;
                for (int i = 0; i < _com.Parameters.Count; i++)
                {
                    paraName = _com.Parameters[i].ParameterName.TrimStart(Pre);//默认自带前缀的，取消再判断
                    if (needToReplace && _com.CommandText.IndexOf("@" + paraName) > -1)
                    {
                        //兼容多数据库的参数（虽然提供了=:?"为兼容语法，但还是贴心的再处理一下）
                        switch (DataBaseType)
                        {
                            case DataBaseType.Oracle:
                            case DataBaseType.MySql:
                            case Data.DataBaseType.PostgreSQL:
                                _com.CommandText = _com.CommandText.Replace("@" + paraName, Pre + paraName);
                                break;
                        }
                    }
                    if (_com.CommandText.IndexOf(Pre + paraName, StringComparison.OrdinalIgnoreCase) == -1)
                    {
                        _com.Parameters.RemoveAt(i);
                        i--;
                    }
                }
            }
            // }
            //else
            //{
            //    string checkText = commandText.ToLower();
            //    //int index=
            //    //if (checkText.IndexOf("table") > -1 && (checkText.IndexOf("delete") > -1 || checkText.IndexOf("drop") > -1 || checkText.IndexOf("truncate") > -1))
            //    //{
            //    //    Log.WriteLog(commandText);
            //    //}
            //}
            if (IsRecordDebugInfo)
            {
                tempSql = GetParaInfo(_com.CommandText) + AppConst.BR + "execute time is: ";
            }
        }
        #endregion

        #region 调试信息管理

        private string GetParaInfo(string commandText)
        {
            string paraInfo = DataBaseType + "." + DataBaseName + ".SQL: " + AppConst.BR + commandText;
            foreach (DbParameter item in _com.Parameters)
            {
                paraInfo += AppConst.BR + "Para: " + item.ParameterName + "-> " + (item.Value == DBNull.Value ? "DBNull.Value" : item.Value);
            }
            return paraInfo;
        }

        /// <summary>
        /// 记录执行时间
        /// </summary>
        private void WriteTime()
        {
            if (IsRecordDebugInfo && _watch != null)
            {
                _watch.Stop();
                double ms = _watch.Elapsed.TotalMilliseconds;
                tempSql += ms + " (ms)" + AppConst.HR;
                if (AppConfig.Debug.OpenDebugInfo)
                {
                    DebugInfo.Append(tempSql);
                    if (AppDebug.IsRecording && ms >= AppConfig.Debug.InfoFilter)
                    {
                        AppDebug.Add(tempSql);
                    }
                }
                if (AppConfig.Debug.SqlFilter >= 0 && ms >= AppConfig.Debug.SqlFilter)
                {
                    Log.Write(tempSql, LogType.Debug);
                }
                _watch.Reset();
                tempSql = null;
            }
        }
        #endregion

        #region 事务管理


        public bool EndTransaction()
        {
            IsOpenTrans = false;
            if (_tran != null)
            {
                try
                {
                    if (_tran.Connection == null)
                    {
                        return false;//上一个执行语句发生了异常（特殊情况在ExeReader guid='xxx' 但不抛异常)
                    }
                    _tran.Commit();

                }
                catch (Exception err)
                {
                    RollBack();
                    WriteError("EndTransaction():" + err.Message);
                    return false;
                }
                finally
                {
                    _tran = null;
                    CloseCon();
                }
            }
            return true;
        }
        /// <summary>
        /// 事务（有则）回滚
        /// </summary>
        /// <returns></returns>
        public bool RollBack()
        {
            if (_tran != null)
            {
                try
                {
                    if (_tran.Connection != null)
                    {
                        _tran.Rollback();
                    }
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    _tran = null;//以便重启事务，避免无法二次回滚。
                }
            }
            return true;
        }

        #endregion

        #region 异常处理

        internal delegate void OnException(string msg);
        internal event OnException OnExceptionEvent;
        internal bool IsOnExceptionEventNull
        {
            get
            {
                return OnExceptionEvent == null;
            }
        }
        /// <summary>
        /// 输出错误（若事务中，回滚事务）
        /// </summary>
        /// <param name="err"></param>
        internal void WriteError(string err)
        {
            err = DataBaseType + " Call Function::" + err;
            if (_watch != null && _watch.IsRunning)
            {
                _watch.Stop();
                _watch.Reset();
            }
            RollBack();
            if (IsWriteLogOnError)
            {
                Log.Write(err + AppConst.BR + DebugInfo, LogType.DataBase);
            }
            if (OnExceptionEvent != null)
            {
                try
                {
                    OnExceptionEvent(err);
                }
                catch
                {

                }
            }
            if (IsOpenTrans)
            {
                Dispose();//事务中发生语句语法错误，直接关掉资源，避免因后续代码继续执行。
            }
        }

        #endregion

        public void Dispose()
        {
            string key = StaticTool.GetTransationKey(UsingConnBean.ConnName);
            if (DBFast.HasTransation(key))
            {
                return;//全局事务由全局控制（全局事务会在移除key后重新调用）。
            }
            if (_con != null)
            {
                CloseCon();
                _con = null;
            }
            if (_com != null)
            {
                _com.Dispose();
                _com = null;
            }
            if (_watch != null)
            {
                _watch = null;
            }
        }
    }

    /// <summary>
    /// 执行管理
    /// </summary>
    internal abstract partial class DalBase
    {
        private DbDataReader ExeDataReaderSQL(string cmdText, bool isProc)
        {
            DbDataReader sdr = null;
            ConnBean coSlave = null;
            if (!IsOpenTrans)// && _IsAllowRecordSql
            {
                coSlave = ConnObj.GetSlave();
            }
            else if (UsingConnBean.IsSlave)// && 事务操作时，如果在从库，切回主库
            {
                ResetConn(ConnObj.Master);
            }
            if (OpenCon(coSlave, AllowConnLevel.MaterBackupSlave))
            {
                try
                {
                    CommandBehavior cb = CommandBehavior.CloseConnection;
                    if (_IsRecordDebugInfo)//外部SQL，带表结构返回
                    {
                        cb = IsOpenTrans ? CommandBehavior.KeyInfo : CommandBehavior.CloseConnection | CommandBehavior.KeyInfo;
                    }
                    else if (IsOpenTrans)
                    {
                        cb = CommandBehavior.Default;//避免事务时第一次拿表结构链接被关闭。
                    }
                    sdr = _com.ExecuteReader(cb);
                    if (sdr != null)
                    {
                        RecordsAffected = sdr.RecordsAffected;
                    }
                }
                catch (DbException err)
                {
                    string msg = "ExeDataReader():" + err.Message;
                    DebugInfo.Append(msg + AppConst.BR);
                    RecordsAffected = -2;
                    WriteError(msg + (isProc ? "" : AppConst.BR + GetParaInfo(cmdText)));
                }
                //finally
                //{
                //    if (coSlave != null)
                //    {
                //        ChangeConn(connObject.Master);//恢复链接。
                //    }
                //}
            }
            return sdr;
        }
        public DbDataReader ExeDataReader(string cmdText, bool isProc)
        {

            SetCommandText(cmdText, isProc);
            DbDataReader sdr = null;
            //if (_dbOperator.ContainsKey(DataBase))
            //{
            //    if (!CheckIsConcurrent())
            //    {
            //        // dbOperator[DataBase] = true;
            //        sdr = ExeDataReaderSQL(cmdText, isProc);
            //        // dbOperator[DataBase] = false;
            //    }
            //}
            //else
            //{
            sdr = ExeDataReaderSQL(cmdText, isProc);
            //}
            WriteTime();
            return sdr;

        }
        private int ExeNonQuerySQL(string cmdText, bool isProc)
        {
            RecordsAffected = -2;
            if (IsOpenTrans && UsingConnBean.IsSlave)// && 事务操作时，如果在从库，切回主库
            {
                ResetConn(ConnObj.Master);
            }
            if (OpenCon())//这里也会切库了，同时设置了10秒切到主库。
            {
                try
                {
                    if (UsingConnBean.ConnString != ConnObj.Master.ConnString)
                    {
                        // recordsAffected = -2;//从库不允许执行非查询操作。
                        string msg = "You can't do ExeNonQuerySQL() on Slave DataBase!";
                        DebugInfo.Append(msg + AppConst.BR);
                        WriteError(msg + (isProc ? "" : AppConst.BR + GetParaInfo(cmdText)));
                    }
                    else
                    {
                        if (isUseUnsafeModeOnSqlite && !isProc && DataBaseType == DataBaseType.SQLite && !IsOpenTrans)
                        {
                            _com.CommandText = "PRAGMA synchronous=Off;" + _com.CommandText;
                        }
                        RecordsAffected = _com.ExecuteNonQuery();
                    }
                }
                catch (DbException err)
                {

                    string msg = "ExeNonQuery():" + err.Message;
                    DebugInfo.Append(msg + AppConst.BR);
                    //recordsAffected = -2;
                    WriteError(msg + (isProc ? "" : AppConst.BR + GetParaInfo(cmdText)));
                }
                finally
                {
                    if (!IsOpenTrans)
                    {
                        CloseCon();
                    }
                }
            }
            return RecordsAffected;
        }
        public int ExeNonQuery(string cmdText, bool isProc)
        {
            SetCommandText(cmdText, isProc);
            int rowCount = 0;
            //if (_dbOperator.ContainsKey(DataBase))
            //{
            //    if (!CheckIsConcurrent())
            //    {
            //        _dbOperator[DataBase] = true;
            //        rowCount = ExeNonQuerySQL(cmdText, isProc);
            //        _dbOperator[DataBase] = false;
            //    }
            //}
            //else
            //{
            rowCount = ExeNonQuerySQL(cmdText, isProc);
            //}
            WriteTime();
            return rowCount;
        }
        private object ExeScalarSQL(string cmdText, bool isProc)
        {
            object returnValue = null;
            ConnBean coSlave = null;
            //mssql 有 insert into ...select 操作。
            bool isSelectSql = !IsOpenTrans && !cmdText.ToLower().TrimStart().StartsWith("insert ");//&& _IsAllowRecordSql
            bool isOpenOK;
            if (isSelectSql)
            {
                coSlave = ConnObj.GetSlave();
                isOpenOK = OpenCon(coSlave, AllowConnLevel.MaterBackupSlave);
            }
            else
            {
                if (UsingConnBean.IsSlave) // 如果是在从库，切回主库。(insert ...select 操作)
                {
                    ResetConn(ConnObj.Master);
                }
                isOpenOK = OpenCon();
            }
            if (isOpenOK)
            {
                try
                {
                    if (!isSelectSql && UsingConnBean.ConnString != ConnObj.Master.ConnString)
                    {
                        RecordsAffected = -2;//从库不允许执行非查询操作。
                        string msg = "You can't do ExeScalarSQL(with transaction or insert) on Slave DataBase!";
                        DebugInfo.Append(msg + AppConst.BR);
                        WriteError(msg + (isProc ? "" : AppConst.BR + GetParaInfo(cmdText)));
                    }
                    else
                    {
                        returnValue = _com.ExecuteScalar();
                        RecordsAffected = returnValue == null ? 0 : 1;
                    }
                }
                catch (DbException err)
                {
                    string msg = "ExeScalar():" + err.Message;
                    DebugInfo.Append(msg + AppConst.BR);
                    RecordsAffected = -2;
                    WriteError(msg + (isProc ? "" : AppConst.BR + GetParaInfo(cmdText)));
                }
                finally
                {
                    if (!IsOpenTrans)
                    {
                        CloseCon();
                    }
                }
            }
            return returnValue;
        }
        public object ExeScalar(string cmdText, bool isProc)
        {
            SetCommandText(cmdText, isProc);
            object returnValue = null;
            //if (_dbOperator.ContainsKey(DataBase))
            //{
            //    if (!CheckIsConcurrent())
            //    {
            //        returnValue = ExeScalarSQL(cmdText, isProc);
            //    }
            //}
            //else
            //{
            returnValue = ExeScalarSQL(cmdText, isProc);
            //}
            WriteTime();
            return returnValue;
        }
        /*
        private DataTable ExeDataTableSQL(string cmdText, bool isProc)
        {
            DbDataAdapter sdr = _fac.CreateDataAdapter();
            sdr.SelectCommand = _com;
            DataTable dataTable = null;
            if (OpenCon())
            {
                try
                {
                    dataTable = new DataTable();
                    recordsAffected = sdr.Fill(dataTable);
                }
                catch (DbException err)
                {
                    recordsAffected = -2;
                    WriteError("ExeDataTable():" + err.Message + (isProc ? "" : AppConst.BR + GetParaInfo(cmdText)));
                }
                finally
                {
                    sdr.Dispose();
                    if (!isOpenTrans)
                    {
                        CloseCon();
                    }
                }
            }
            return dataTable;
        }
        public DataTable ExeDataTable(string cmdText, bool isProc)
        {
            SetCommandText(cmdText, isProc);
            DataTable dataTable = null;
            //if (_dbOperator.ContainsKey(DataBase))
            //{
            //    if (!CheckIsConcurrent())
            //    {
            //        dataTable = ExeDataTableSQL(cmdText, isProc);
            //    }
            //}
            //else
            //{
                dataTable = ExeDataTableSQL(cmdText, isProc);
            //}
            WriteTime();
            return dataTable;
        }
        */
    }
    /// <summary>
    /// 链接管理
    /// </summary>
    internal abstract partial class DalBase
    {
        int threadCount = 0;
        /// <summary>
        /// 测试链接
        /// </summary>
        /// <param name="allowLevel">1：只允许当前主链接；2：允许主备链接；3：允许主备从链接</param>
        /// <returns></returns>
        internal bool TestConn(AllowConnLevel allowLevel)
        {
            threadCount = 0;
            openOKFlag = -1;
            ConnObject obj = ConnObj;
            if (obj.Master != null && obj.Master.IsOK && (int)allowLevel >= 1)
            {
                threadCount++;
                Thread thread = new Thread(new ParameterizedThreadStart(TestOpen));
                thread.Start(obj.Master);

            }
            Thread.Sleep(30);
            if (openOKFlag == -1 && obj.BackUp != null && obj.BackUp.IsOK && (int)allowLevel >= 2)
            {
                threadCount++;
                Thread.Sleep(30);
                Thread thread = new Thread(new ParameterizedThreadStart(TestOpen));
                thread.Start(obj.BackUp);

            }
            if (openOKFlag == -1 && obj.Slave != null && obj.Slave.Count > 0 && (int)allowLevel >= 3)
            {
                for (int i = 0; i < obj.Slave.Count; i++)
                {
                    Thread.Sleep(30);
                    if (openOKFlag == -1 && obj.Slave[i].IsOK)
                    {
                        threadCount++;
                        Thread thread = new Thread(new ParameterizedThreadStart(TestOpen));
                        thread.Start(obj.Slave[i]);

                    }
                }
            }

            int sleepTimes = 0;
            while (openOKFlag == -1)
            {
                sleepTimes += 30;
                if (sleepTimes > 3000 || threadCount == errorCount)
                {
                    break;
                }
                Thread.Sleep(30);
            }
            return openOKFlag == 1;
        }
        private int openOKFlag = -1;
        private int errorCount = 0;
        private void TestOpen(object para)
        {
            ConnBean connBean = para as ConnBean;
            if (connBean != null && connBean.IsOK && openOKFlag == -1)
            {
                if (connBean.TryTestConn() && openOKFlag != 1)
                {
                    openOKFlag = 1;
                    _Version = connBean.Version;//设置版本号。
                    ResetConn(connBean);//切到正常的去。
                }
                else
                {
                    errorCount++;
                    DebugInfo.Append(connBean.ErrorMsg);
                }
            }
        }

        /// <summary>
        /// 切换链接
        /// </summary>
        private bool ResetConn(ConnBean cb)//, bool isAllowReset
        {
            if (cb != null && cb.IsOK && _con != null && _con.State != ConnectionState.Open && UsingConnBean.ConnString != cb.ConnString)
            {
                UsingConnBean = cb;
                _con.ConnectionString = cb.ConnString;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 打开链接，只切主备(同时有N秒的主库定位)
        /// </summary>
        /// <returns></returns>
        internal bool OpenCon()
        {
            ConnBean master = ConnObj.Master;
            if (!master.IsOK && ConnObj.BackUp != null && ConnObj.BackUp.IsOK)
            {
                master = ConnObj.BackUp;
                ConnObj.InterChange();//主从换位置
            }
            if (master.IsOK || (ConnObj.BackUp != null && ConnObj.BackUp.IsOK))
            {
                bool result = OpenCon(master, AllowConnLevel.MasterBackup);
                if (result && _IsRecordDebugInfo)
                {
                    ConnObj.SetFocusOnMaster();
                    isAllowResetConn = false;
                }
                return result;
            }
            if (!DebugInfo.ToString().EndsWith(master.ErrorMsg))
            {
                DebugInfo.Append(master.ErrorMsg);
            }
            return false;
        }
        /// <summary>
        /// 打开链接，允许切从。
        /// </summary>
        internal bool OpenCon(ConnBean cb, AllowConnLevel leve)
        {
            try
            {
                if (cb == null)
                {
                    cb = UsingConnBean;
                    if (IsOpenTrans && cb.IsSlave)
                    {
                        ResetConn(ConnObj.Master);

                    }
                }
                if (!cb.IsOK)
                {
                    if ((int)leve > 1 && ConnObj.BackUp != null && ConnObj.BackUp.IsOK)
                    {
                        ResetConn(ConnObj.BackUp);//重置链接。
                        ConnObj.InterChange();//主从换位置
                        return OpenCon(ConnObj.Master, leve);
                    }
                    else if ((int)leve > 2)
                    {
                        //主挂了，备也挂了（因为备会替主）
                        ConnBean nextSlaveBean = ConnObj.GetSlave();
                        if (nextSlaveBean != null)
                        {
                            ResetConn(nextSlaveBean);//重置链接。
                            return OpenCon(nextSlaveBean, leve);
                        }
                    }
                }
                else if (!IsOpenTrans && cb != UsingConnBean && isAllowResetConn && ConnObj.IsAllowSlave())
                {
                    ResetConn(cb);//,_IsAllowRecordSql只有读数据错误才切，表结构错误不切？
                }
                if (UsingConnBean.IsOK)
                {
                    Open();//异常抛
                }
                else
                {
                    WriteError(UsingConnBean.ConnDataBaseType + ".OpenCon():" + UsingConnBean.ErrorMsg);
                }
                if (IsRecordDebugInfo)
                {
                    _watch.Start();
                }
                return UsingConnBean.IsOK;
            }
            catch (DbException err)
            {
                UsingConnBean.IsOK = false;
                UsingConnBean.ErrorMsg = err.Message;
                return OpenCon(null, leve);

            }
        }
        private void Open()
        {
            if (_con.State == ConnectionState.Closed)
            {
                if (DataBaseType == DataBaseType.Sybase)
                {
                    _com.Connection = _con;//重新赋值（Sybase每次Close后命令的Con都丢失）
                }
                _con.Open();
                //if (useConnBean.ConfigName == "Conn")
                //{
                //System.Console.WriteLine(useConnBean.ConfigName);
                //}
            }
            if (IsOpenTrans)
            {
                if (_tran == null)
                {
                    _tran = _con.BeginTransaction(TranLevel);
                    _com.Transaction = _tran;
                }
                else if (_tran.Connection == null)
                {
                    //ADO.NET Bug：在 guid='123' 时不抛异常，但自动关闭链接，不关闭对象，会引发后续的业务不在事务中。
                    Dispose();
                    Error.Throw("Transation：Last execute command has syntax error：" + DebugInfo.ToString());
                }
            }
        }
        /*
        private bool OpenConBak()
        {
            try
            {
                if (connObject.ProviderName == connObject.ProviderNameBak)//同种数据库链接。
                {
                    conn = connObject.ConnBak;//切换到备用。
                    _con.ConnectionString = conn;//切换到备用。
                    Open();
                    //交换主从链接
                    connObject.ExchangeConn();
                    if (IsAllowRecordSql)
                    {
                        _watch.Start();
                    }
                    return true;
                }
                else
                {
                    connObject.ExchangeConn();//不同种，切换链接后，本次操作直接抛异常
                }
            }
            catch (DbException err)
            {
                WriteError("OpenConBak():" + err.Message);
            }
            return false;
        }
         */
        internal void CloseCon()
        {
            try
            {
                if (_con.State != ConnectionState.Closed)
                {
                    if (_tran != null)
                    {
                        IsOpenTrans = false;
                        if (_tran.Connection != null)
                        {
                            _tran.Commit();
                        }
                        _tran = null;
                    }
                    _con.Close();
                }
            }
            catch (DbException err)
            {
                WriteError("CloseCon():" + err.Message);
            }

        }
    }

}