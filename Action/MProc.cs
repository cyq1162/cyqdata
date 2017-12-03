using CYQ.Data.SQL;
using System.Data.SqlClient;
using System.Data;
using System;
using CYQ.Data.Table;

using CYQ.Data.Aop;
using System.Collections.Generic;
using CYQ.Data.Tool;
using System.Data.Common;


namespace CYQ.Data
{
    /// <summary>
    /// Manipulate：sql / procedure
    ///<para>操作：SQL或存储过程</para>
    /// </summary>
    public class MProc : IDisposable
    {
        /// <summary>
        /// Change MAction to MProc
        /// <para>将一个MAction 转换成一个MProc。</para>
        /// </summary>
        public static implicit operator MProc(MAction action)
        {
            return new MProc(action.dalHelper);
        }

        internal DbBase dalHelper;
        private NoSqlCommand _noSqlCommand;
        private InterAop _aop = new InterAop();
        // private AopInfo _aopInfo = new AopInfo();
        private string _procName = string.Empty;
        private bool _isProc = true;
        private string _debugInfo = string.Empty;
        /// <summary>
        /// 原始传进进来的链接。
        /// </summary>
        private string _conn = string.Empty;

        /// <summary>
        /// Get or set debugging information [need to set App Config.Debug.Open DebugInfo to ture]
        ///<para>获取或设置调试信息[需要设置AppConfig.Debug.OpenDebugInfo为ture]</para>
        /// </summary>
        public string DebugInfo
        {
            get
            {
                if (dalHelper != null)
                {
                    return dalHelper.debugInfo.ToString();
                }
                return _debugInfo;
            }
        }
        /// <summary>
        /// The database type
        /// <para>数据库类型</para>
        /// </summary>
        public DalType DalType
        {
            get
            {
                return dalHelper.dalType;
            }
        }
        /// <summary>
        /// The database name
        /// <para>数据库名称</para>
        /// </summary>
        public string DataBase
        {
            get
            {
                return dalHelper.DataBase;
            }
        }
        /// <summary>
        /// The database version
        /// 数据库的版本号
        /// </summary>
        public string DalVersion
        {
            get
            {
                return dalHelper.Version;
            }
        }
        /// <summary>
        /// The number of rows affected when executing a SQL command (-2 is an exception)
        /// <para>执行SQL命令时受影响的行数（-2则为异常）。</para>
        /// </summary>
        public int RecordsAffected
        {
            get
            {
                return dalHelper.recordsAffected;
            }
        }
        /// <summary>
        /// The database connection string
        ///<para>数据库链接字符串</para> 
        /// </summary>
        public string ConnectionString
        {
            get
            {
                if (dalHelper != null)
                {
                    return dalHelper.conn;
                }
                return string.Empty;
            }
        }
        /// <summary>
        /// Command Timeout[seconds]
        ///<para>命令超时设置[单位秒]</para>
        /// </summary>
        public int TimeOut
        {
            get
            {
                if (dalHelper.Com != null)
                {
                    return dalHelper.Com.CommandTimeout;
                }
                return -1;
            }
            set
            {
                if (dalHelper.Com != null)
                {
                    dalHelper.Com.CommandTimeout = value;
                }
            }
        }

        /// <summary>
        /// Instantiation
        /// <para>实例化</para>
        /// </summary>
        /// <param name="procNameOrSql">Parameters: sql or procedure
        /// <para>参数：SQL语句或存储过程名称</para></param>
        public MProc(object procNameOrSql)
        {
            Init(procNameOrSql, AppConfig.DB.DefaultConn);
        }

        public MProc(object procNameOrSql, string conn)
        {
            Init(procNameOrSql, conn);
        }
        internal MProc(DbBase dbBase)
        {
            _procName = string.Empty;
            _conn = dbBase.conn;
            SetDbBase(dbBase);

        }
        private void Init(object procNameOrSql, string conn)
        {
            #region 分析是Sql或者存储过程
            if (procNameOrSql != null)
            {
                if (procNameOrSql is Enum)
                {
                    Type t = procNameOrSql.GetType();
                    string enumName = t.Name;
                    if (enumName != "ProcNames")
                    {
                        if (enumName.Length > 1 && enumName[1] == '_')
                        {
                            conn = enumName.Substring(2).Replace("Enum", "Conn");
                        }
                        else
                        {
                            string[] items = t.FullName.Split('.');
                            if (items.Length > 1)
                            {
                                conn = items[items.Length - 2] + "Conn";
                                items = null;
                            }
                        }
                    }
                    t = null;
                }
                _procName = procNameOrSql.ToString().Trim();
                _isProc = _procName.IndexOf(' ') == -1;//不包含空格

            }
            #endregion
            _conn = conn;
            SetDbBase(DalCreate.CreateDal(conn));
        }
        private void SetDbBase(DbBase dbBase)
        {
            dalHelper = dbBase;
            if (dalHelper.IsOnExceptionEventNull)
            {
                dalHelper.OnExceptionEvent += new DbBase.OnException(helper_OnExceptionEvent);
            }
            switch (dalHelper.dalType)
            {
                case DalType.Txt:
                case DalType.Xml:
                    _noSqlCommand = new NoSqlCommand(_procName, dalHelper);
                    break;
            }
            //Aop.IAop myAop = Aop.InterAop.Instance.GetFromConfig();//试图从配置文件加载自定义Aop
            //if (myAop != null)
            //{
            //    SetAop(myAop);
            //}
        }
        /// <summary>
        /// <param name="isClearPara">IsClearParameters
        /// <para>是否清除参数</para></param>
        /// </summary>
        public void ResetProc(object procNameOrSql, bool isClearPara)
        {
            _procName = procNameOrSql.ToString().Trim();
            if (isClearPara)
            {
                dalHelper.ClearParameters();
            }
            _isProc = _procName.IndexOf(' ') == -1;//不包含空格
            switch (dalHelper.dalType)
            {
                case DalType.Txt:
                case DalType.Xml:
                    _noSqlCommand = null;
                    _noSqlCommand = new NoSqlCommand(_procName, dalHelper);
                    break;
            }
        }
       
        ///<summary>
        /// Toggle tProc Action: To switch between other sql/procedure, use this method
        /// <para>切换操作：如需操作其它语句或存储过程，通过此方法切换</para>
        /// </summary>
        /// <param name="procNameOrSql">Parameters: sql or procedure
        /// <para>参数：存储过程名或Sql语句</para></param>
        public void ResetProc(object procNameOrSql)
        {
            ResetProc(procNameOrSql, true);
        }
        private AopResult SetAopResult(AopEnum action)
        {
            if (_aop.IsLoadAop)
            {
                _aop.Para.MProc = this;
                _aop.Para.ProcName = _procName;
                _aop.Para.IsProc = _isProc;
                if (dalHelper.Com != null)
                {
                    _aop.Para.DBParameters = dalHelper.Com.Parameters;
                }
                _aop.Para.IsTransaction = dalHelper.isOpenTrans;
                return _aop.Begin(action);
            }
            return AopResult.Default;
        }
        /// <summary>
        /// Get MDataTable
        /// </summary>
        public MDataTable ExeMDataTable()
        {
            CheckDisposed();
            AopResult aopResult = SetAopResult(AopEnum.ExeMDataTable);
            if (aopResult == AopResult.Return)
            {
                return _aop.Para.Table;
            }
            else
            {
                if (aopResult != AopResult.Break)
                {
                    switch (dalHelper.dalType)
                    {
                        case DalType.Txt:
                        case DalType.Xml:
                            _aop.Para.Table = _noSqlCommand.ExeMDataTable();
                            break;
                        default:
                            _aop.Para.Table = dalHelper.ExeDataReader(_procName, _isProc);
                            _aop.Para.Table.Columns.dalType = DalType;
                            // dalHelper.ResetConn();//重置Slave
                            break;
                    }
                    _aop.Para.Table.Conn = _conn;
                    _aop.Para.IsSuccess = _aop.Para.Table.Rows.Count > 0;
                }
                if (aopResult != AopResult.Default)
                {
                    _aop.End(AopEnum.ExeMDataTable);
                }
                return _aop.Para.Table;
            }
        }

        /// <summary>
        /// Get MDataTables
        /// </summary>
        public List<MDataTable> ExeMDataTableList()
        {
            CheckDisposed();
            AopResult aopResult = SetAopResult(AopEnum.ExeMDataTableList);
            if (aopResult == AopResult.Return)
            {
                return _aop.Para.TableList;
            }
            else
            {
                if (aopResult != AopResult.Break)
                {
                    List<MDataTable> dtList = new List<MDataTable>();
                    switch (dalHelper.dalType)
                    {
                        case DalType.Txt:
                        case DalType.Xml:
                        case DalType.Oracle:
                            if (_isProc && dalHelper.dalType == DalType.Oracle)
                            {
                                goto isProc;
                            }
                            foreach (string sql in _procName.TrimEnd(';').Split(';'))
                            {
                                MDataTable dt = null;
                                if (dalHelper.dalType == DalType.Oracle)
                                {
                                    dt = dalHelper.ExeDataReader(sql, false);
                                }
                                else
                                {
                                    _noSqlCommand.CommandText = sql;
                                    dt = _noSqlCommand.ExeMDataTable();
                                }
                                if (dt != null)
                                {
                                    dtList.Add(dt);
                                }
                            }
                            break;
                        default:
                        isProc:
                            DbDataReader reader = dalHelper.ExeDataReader(_procName, _isProc);
                            if (reader != null)
                            {
                                do
                                {
                                    dtList.Add(MDataTable.CreateFrom(reader));
                                }
                                while (reader.NextResult());
                                reader.Close();
                                reader.Dispose();
                                reader = null;
                            }
                            break;
                    }
                    _aop.Para.TableList = dtList;
                    _aop.Para.IsSuccess = dtList.Count > 0;
                }
                if (aopResult != AopResult.Default)
                {
                    _aop.End(AopEnum.ExeMDataTableList);
                }
                return _aop.Para.TableList;
            }
        }

        /// <summary>
        /// Returns the number of rows affected [used to insert update or delete], and returns -2 if an exception is executed
        /// <para>返回受影响的行数[用于更新或删除]，执行异常时返回-2</para>
        /// </summary>
        public int ExeNonQuery()
        {
            CheckDisposed();
            AopResult aopResult = SetAopResult(AopEnum.ExeNonQuery);
            if (aopResult == AopResult.Return)
            {
                return _aop.Para.RowCount;
            }
            else
            {
                if (aopResult != AopResult.Break)
                {
                    switch (dalHelper.dalType)
                    {
                        case DalType.Txt:
                        case DalType.Xml:
                            _aop.Para.RowCount = _noSqlCommand.ExeNonQuery();
                            break;
                        default:
                            _aop.Para.RowCount = dalHelper.ExeNonQuery(_procName, _isProc);
                            break;
                    }
                    _aop.Para.IsSuccess = _aop.Para.RowCount > 0;
                }
                if (aopResult != AopResult.Default)
                {
                    _aop.End(AopEnum.ExeNonQuery);
                }
                return _aop.Para.RowCount;
            }
        }
        /// <summary>
        /// Returns the value of the first column of the first row
        /// <para>返回首行首列的值</para>
        /// </summary>
        public T ExeScalar<T>()
        {
            CheckDisposed();
            AopResult aopResult = SetAopResult(AopEnum.ExeScalar);
            if (aopResult == AopResult.Default || aopResult == AopResult.Continue)
            {
                switch (dalHelper.dalType)
                {
                    case DalType.Txt:
                    case DalType.Xml:
                        _aop.Para.ExeResult = _noSqlCommand.ExeScalar();
                        break;
                    default:
                        _aop.Para.ExeResult = dalHelper.ExeScalar(_procName, _isProc);
                        break;
                }
                _aop.Para.IsSuccess = _aop.Para.ExeResult != null;
            }
            if (aopResult == AopResult.Continue || aopResult == AopResult.Break)
            {
                _aop.End(AopEnum.ExeScalar);
            }
            if (_aop.Para.ExeResult == null || _aop.Para.ExeResult == DBNull.Value)
            {
                return default(T);
            }
            Type t = typeof(T);
            object value = _aop.Para.ExeResult;
            switch (t.Name)
            {
                case "Int32":
                    int intValue = 0;
                    if (!int.TryParse(Convert.ToString(value), out intValue))
                    {
                        return default(T);
                    }
                    value = intValue;
                    break;
                default:
                    try
                    {
                        value = StaticTool.ChangeType(value, t);
                    }
                    catch
                    {
                    }

                    break;

            }
            return (T)value;
        }
     

        /// <summary>
        /// <para>Set Input Para</para>
        /// <para>设置存储过程Input参数</para>
        /// </summary>
        public MProc Set(object paraName, object value)
        {
            dalHelper.AddParameters(Convert.ToString(paraName), value); return this;
        }

        public MProc Set(object paraName, object value, DbType dbType)
        {
            dalHelper.AddParameters(Convert.ToString(paraName), value, dbType, -1, ParameterDirection.Input); return this;
        }
        /// <summary>
        /// 设置特殊自定义参数
        /// </summary>
        public MProc SetCustom(object paraName, ParaType paraType)
        {
            return SetCustom(paraName, paraType, null, null);
        }

        /// <summary>
        /// <para>Set the stored procedure OutPut, the return value and other special types of parameters</para>
        /// <para>设置存储过程OutPut、返回值等特殊类型参数</para>
        /// </summary>
        public MProc SetCustom(object paraName, ParaType paraType, object value)
        {
            return SetCustom(paraName, paraType, value, null);
        }


        /// <param name="typeName">MSSQL The name of the user-defined table type<para>MSSQL的用户定义表类型的名称</para></param>
        public MProc SetCustom(object paraName, ParaType paraType, object value, string typeName)
        {
            dalHelper.AddCustomePara(Convert.ToString(paraName), paraType, value, typeName); return this;
        }
       

        /// <summary>
        /// Clear Parameters
        /// <para>清除存储过程参数</para>
        /// </summary>
        public void Clear()
        {
            dalHelper.ClearParameters();
        }

        /// <summary>
        /// Get Procedure Return Value
        /// <para>存储过程的返回值</para>
        /// </summary>
        public int ReturnValue
        {
            get
            {
                return dalHelper.ReturnValue;
            }
        }
        /// <summary>
        /// The OutPut value for the stored procedure: Dictionary for multiple values
        /// <para>存储过程的OutPut值：多个值时为Dictionary</para>
        /// </summary>
        public object OutPutValue
        {
            get
            {
                return dalHelper.OutPutValue;
            }
        }

        #region Aop 相关操作
        /// <summary>
        /// Set Aop State
        /// <para>设置Aop状态</para>
        /// </summary>
        public MProc SetAopState(AopOp op)
        {
            _aop.aopOp = op;
            return this;
        }

        /// <summary>
        /// Pass additional parameters for Aop use
        /// <para>传递额外的参数供Aop使用</para>
        /// </summary>
        public MProc SetAopPara(object para)
        {
            _aop.Para.AopPara = para;
            return this;
        }

        void helper_OnExceptionEvent(string errorMsg)
        {
            _aop.OnError(errorMsg);
        }
        #endregion



        #region 事务操作
        /// <summary>
        /// Set the transaction level
        /// <para>设置事务级别</para>
        /// </summary>
        /// <param name="level">IsolationLevel</param>
        public MProc SetTransLevel(IsolationLevel level)
        {
            dalHelper.TranLevel = level; return this;
        }

        /// <summary>
        /// Begin Transation
        /// <para>开始事务</para>
        /// </summary>
        public void BeginTransation()
        {
            dalHelper.isOpenTrans = true;
        }
        /// <summary>
        /// Commit Transation
        /// <para>提交事务</para>
        /// </summary>
        public bool EndTransation()
        {
            if (dalHelper != null && dalHelper.isOpenTrans)
            {
                return dalHelper.EndTransaction();
            }
            return false;
        }
        /// <summary>
        /// RollBack Transation
        /// <para>事务回滚</para>
        /// </summary>
        public bool RollBack()
        {
            if (dalHelper != null && dalHelper.isOpenTrans)
            {
                return dalHelper.RollBack();
            }
            return false;
        }
        #endregion
        #region IDisposable 成员
        /// <summary>
        /// Dispose
        /// <para>释放资源</para>
        /// </summary>
        public void Dispose()
        {
            hasDisposed = true;
            if (dalHelper != null)
            {
                if (!dalHelper.IsOnExceptionEventNull)
                {
                    dalHelper.OnExceptionEvent -= new DbBase.OnException(helper_OnExceptionEvent);
                }
                _debugInfo = dalHelper.debugInfo.ToString();
                dalHelper.Dispose();
                dalHelper = null;
            }
            if (_noSqlCommand != null)
            {
                _noSqlCommand.Dispose();
            }
        }
        bool hasDisposed = false;
        private void CheckDisposed()
        {
            if (hasDisposed)
            {
                Error.Throw("The current object 'MProc' has been disposed");
            }
        }
        #endregion
    }

}
