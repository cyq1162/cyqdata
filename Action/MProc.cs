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
    /// 存储过程或SQL语句操作类
    /// </summary>
    /// <example><code>
    /// 使用示例：
    ///           MDataTable table;
    /// 实例化：  using(MProc proc = new MProc(ProcNames.GetList))
    ///           {
    /// 添加参数：    proc.Set(GetList.ID, 10);
    /// 获取列表：    table = proc.ExeMDataTable();
    /// 关闭链接：}
    /// 绑定控件：table.Bind(GridView1);
    /// </code></example>
    public class MProc : IDisposable
    {
        /// <summary>
        /// 将一个MAction 转换成一个MProc。
        /// </summary>
        /// <returns></returns>
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
        /// 调试信息[如需要查看所有执行的SQL语句,请设置配置文件OpenDebugInfo项为ture]
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
        /// 当前操作的数据库类型[Access/Mssql/Oracle/SQLite/MySql等]
        /// </summary>
        public DalType DalType
        {
            get
            {
                return dalHelper.dalType;
            }
        }
        /// <summary>
        /// 当前操作的数据库名称
        /// </summary>
        public string DataBase
        {
            get
            {
                return dalHelper.DataBase;
            }
        }
        /// <summary>
        /// 当前操作的数据库的版本号
        /// </summary>
        public string DalVersion
        {
            get
            {
                return dalHelper.Version;
            }
        }
        /// <summary>
        /// 执行SQL命令时受影响的行数（-2则为异常）。
        /// </summary>
        public int RecordsAffected
        {
            get
            {
                return dalHelper.recordsAffected;
            }
        }
        /// <summary>
        /// 获取当前数据库链接字符串
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
        /// 命令超时设置[单位秒]
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
        /// 构造函数
        /// </summary>
        /// <param name="procNameOrSql">存储过程名称,可通过枚举传入</param>
        /// <example><code>
        ///     MProc action=new MProc(ProcNames.SelectAll);
        /// 或  MProc action=new MProc("SelectAll");
        /// 或多数据库方式：
        /// MAction action=new MAction(P_DataBaseNameEnum.SelectAll);
        /// 说明：自动截取数据库链接[P_及Enum为前后缀],取到的数据库配置项为DataBaseNameConn
        /// U_为表 V_为视图 P_为存储过程
        /// </code></example>
        public MProc(object procNameOrSql)
        {
            Init(procNameOrSql, AppConfig.DB.DefaultConn);
        }
        /// <summary>
        /// 构造函数2
        /// </summary>
        /// <param name="procNameOrSql">存储过程名称,可通过枚举传入</param>
        /// <param name="conn">web.config下的connectionStrings的name配置项名称,或完整的链接字符串</param>
        /// <example><code>
        ///     MProc action=new MProc(ProcNames.SelectAll,"CYQ");
        /// 或  MProc action=new MProc(ProcNames.SelectAll,"server=.;database=CYQ;uid=sa;pwd=123456");
        /// </code></example>
        public MProc(object procNameOrSql, string conn, params bool[] isFixProc)
        {
            Init(procNameOrSql, conn, isFixProc);
        }
        internal MProc(DbBase dbBase)
        {
            _procName = string.Empty;
            _conn = dbBase.conn;
            SetDbBase(dbBase);

        }
        private void Init(object procNameOrSql, string conn, params bool[] isFixProc)
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
                if (isFixProc.Length > 0)
                {
                    _isProc = isFixProc[0];
                }
                else
                {
                    _isProc = _procName.IndexOf(' ') == -1;//不包含空格
                }
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
        ///  表切存储过程,在操作完A存储过程后，如果需要操作B存储过程,不需要重新new一个MProc,可直接换用本函数切换
        /// 用法参考MAction的ResetTable
        /// </summary>
        /// <param name="procNameOrSql">存储过程名或Sql语句</param>
        /// <param name="isClearParaAndisFixProc">允许多两个bool参数：1：是否清除参数；2：是否为存储过程</param>
        public void ResetProc(object procNameOrSql, params bool[] isClearParaAndisFixProc)
        {
            _procName = procNameOrSql.ToString().Trim();
            if (isClearParaAndisFixProc.Length > 0 && isClearParaAndisFixProc[0])
            {
                dalHelper.ClearParameters();
            }
            if (isClearParaAndisFixProc.Length > 1)
            {
                _isProc = isClearParaAndisFixProc[1];
            }
            else
            {
                _isProc = _procName.IndexOf(' ') == -1;//不包含空格
            }
        }
        /// <summary>
        ///  表切存储过程,在操作完A存储过程后，如果需要操作B存储过程,不需要重新new一个MProc,可直接换用本函数切换
        /// 用法参考MAction的ResetTable
        /// </summary>
        public void ResetProc(object procNameOrSql)
        {
            ResetProc(procNameOrSql, true);
        }
        private AopResult SetAopResult(AopEnum action)
        {
            if (_aop.IsCustomAop)
            {
                _aop.Para.MProc = this;
                _aop.Para.ProcName = _procName;
                _aop.Para.IsProc = _isProc;
                _aop.Para.DBParameters = dalHelper.Com.Parameters;
                _aop.Para.IsTransaction = dalHelper.isOpenTrans;
                return _aop.Begin(action);
            }
            return AopResult.Default;
        }
        /// <summary>
        /// 返回MDataTable
        /// </summary>
        /// <returns></returns>
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
                }
                if (aopResult != AopResult.Default)
                {
                    _aop.End(AopEnum.ExeMDataTable);
                }
                return _aop.Para.Table;
            }
        }

        /// <summary>
        /// 执行的语句有多个结果集返回（库此方法不支持文本数据和AOP）
        /// </summary>
        /// <returns></returns>
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
                            foreach (string sql in _procName.Split(';'))
                            {
                                _noSqlCommand.CommandText = sql;
                                dtList.Add(_noSqlCommand.ExeMDataTable());
                            }
                            break;
                        default:
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
                }
                if (aopResult != AopResult.Default)
                {
                    _aop.End(AopEnum.ExeMDataTableList);
                }
                return _aop.Para.TableList;
            }
        }

        /// <summary>
        /// 返回受影响的行数[用于更新或删除]，执行异常时返回-2
        /// </summary>
        /// <returns></returns>
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
                }
                if (aopResult != AopResult.Default)
                {
                    _aop.End(AopEnum.ExeNonQuery);
                }
                return _aop.Para.RowCount;
            }
        }
        /// <summary>
        /// 返回首行首列的单个值
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
        //public string ExeXmlScalar()
        //{
        //    return helper.ExeXmlScalar(procName, true);
        //}

        /// <summary>
        /// 设置存储过程参数
        /// </summary>
        /// <param name="paraName">参数名称如["ID"或Users.ID]</param>
        /// <param name="value">参数值如"11"</param>
        public MProc Set(object paraName, object value)
        {
            dalHelper.AddParameters(Convert.ToString(paraName), value); return this;
        }
        /// <summary>
        /// 设置存储过程参数
        /// </summary>
        public MProc Set(object paraName, object value, DbType dbType)
        {
            dalHelper.AddParameters(Convert.ToString(paraName), value, dbType, -1, ParameterDirection.Input); return this;
        }
        /// <summary>
        /// 设置特殊自定义参数
        /// </summary>
        public MProc SetCustom(object paraName, ParaType paraType)
        {
            dalHelper.AddCustomePara(Convert.ToString(paraName), paraType, null); return this;
        }
        /// <summary>
        /// 设置特殊自定义参数
        /// </summary>
        /// <param name="paraName">参数名称</param>
        /// <param name="paraType">参数类型</param>
        /// <param name="value">参数值</param>
        public MProc SetCustom(object paraName, ParaType paraType, object value)
        {
            dalHelper.AddCustomePara(Convert.ToString(paraName), paraType, value); return this;
        }
        /// <summary>
        ///设置存储过程参数
        /// </summary>
        /// <param name="paraName">参数名称如["ID"或Users.ID]</param>
        /// <param name="value">参数值如"11"</param>
        /// <param name="sqlDbType">值的sql类型</param>
        //public void Set(object paraName, object value,SqlDbType sqlDbType)
        //{
        //    string name = Convert.ToString(paraName);
        //    helper.AddParameters(name.Substring(0, 1) == "@" ? name : "@" + name, value, sqlDbType);
        //}
        /// <summary>
        /// 清除存储过程参数
        /// </summary>
        public void Clear()
        {
            dalHelper.ClearParameters();
        }
        /// <summary>
        /// 存储过程的返回值
        /// </summary>
        public int ReturnValue
        {
            get
            {
                return dalHelper.ReturnValue;
            }
        }
        /// <summary>
        /// 存储过程的OutPut值
        /// 单个输出时为值；
        /// 多个输出时为Dictionary
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
        /// 临时备份Aop，用于切换后的还原。
        /// </summary>
        // Aop.IAop _aopBak = null;

        /// <summary>
        /// 取消AOP
        /// </summary>
        public MProc SetAopOff()
        {
            _aop.IsCustomAop = false;
            //if (_aopInfo.IsCustomAop)
            //{
            //    _aopBak = _aop;//设置好备份。
            //    _aop = Aop.InterAop.Instance;
            //    _aopInfo.IsCustomAop = false;
            //} 
            return this;
        }
        /// <summary>
        /// 恢复默认配置的Aop。
        /// </summary>
        public MProc SetAopOn()
        {
            _aop.IsCustomAop = true;
            //if (!_aopInfo.IsCustomAop)
            //{
            //    SetAop(_aopBak);
            //}
            return this;
        }
        /// <summary>
        /// 设置Aop对象。
        /// </summary>
        //private MProc SetAop(Aop.IAop aop)
        //{
        //    _aop = aop;
        //    _aopInfo.IsCustomAop = true;
        //    return this;
        //}
        /// <summary>
        /// 需要传递额外的参数供Aop使用时可设置。
        /// </summary>
        /// <param name="para"></param>
        public MProc SetAopPara(object para)
        {
            _aop.Para.AopPara = para; return this;
        }

        void helper_OnExceptionEvent(string errorMsg)
        {
            _aop.OnError(errorMsg);
        }
        #endregion



        #region 事务操作
        /// <summary>
        /// 设置事务级别
        /// </summary>
        /// <param name="level">事务级别</param>
        public MProc SetTransLevel(IsolationLevel level)
        {
            dalHelper.tranLevel = level; return this;
        }

        /// <summary>
        /// 开启事务
        /// </summary>
        public void BeginTransation()
        {
            dalHelper.isOpenTrans = true;
        }
        /// <summary>
        /// 提交结束事务[默认调用Close/Disponse时会自动调用]
        /// 如果需要提前结束事务,可调用此方法
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
        /// 事务回滚
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
        /// 释放资源
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
