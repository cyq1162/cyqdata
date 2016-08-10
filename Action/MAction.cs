using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Configuration;
using System.ComponentModel;
using CYQ.Data.SQL;
using CYQ.Data.Cache;
using CYQ.Data.Table;

using CYQ.Data.Aop;
using CYQ.Data.Tool;
using CYQ.Data.UI;


namespace CYQ.Data
{
    /// <summary>
    /// 数据操作类[可操作单表/视图]
    /// </summary>
    public partial class MAction : IDisposable
    {
        #region 全局变量

        internal DbBase dalHelper;//数据操作

        private SqlCreate _sqlCreate;



        private InsertOp _option = InsertOp.Fill;

        private NoSqlAction _noSqlAction = null;
        private MDataRow _Data;//表示一行
        /// <summary>
        /// Fill完之后返回的行数据
        /// </summary>
        public MDataRow Data
        {
            get
            {
                return _Data;
            }
        }
        /// <summary>
        /// 原始传进来的表名/视图名，未经过[多数据库转换处理]格式化。
        /// </summary>
        private string _sourceTableName;
        private string _TableName; //表名
        /// <summary>
        /// 当前操作的表名(若操作的是视图语句，则为最后的视图名字）
        /// </summary>
        public string TableName
        {
            get
            {
                return _TableName;
            }
            set
            {
                _TableName = value;
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
        private string _debugInfo = string.Empty;
        /// <summary>
        /// 调试信息[如需要查看所有执行的SQL语句,请设置AppDebug.OpenDebugInfo或配置文件OpenDebugInfo项为ture]
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
            set
            {
                if (dalHelper != null)
                {
                    dalHelper.debugInfo.Length = 0;
                    dalHelper.debugInfo.Append(value);
                }
            }
        }
        /// <summary>
        /// 当前操作的数据库类型[Access/Mssql/Oracle/SQLite/MySql/Txt/Xml等]
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
        private bool _AllowInsertID = false;
        /// <summary>
        /// 对于主键列为：Int自增标识的，是否允许手动插入ID
        /// </summary>
        public bool AllowInsertID
        {
            get
            {
                return _AllowInsertID;
            }
            set
            {
                _AllowInsertID = value;
            }
        }
        private bool _isInsertCommand; //是否执行插入命令(区分于Update命令)

        private bool _setIdentityResult = true;
        /// <summary>
        /// MSSQL插入标识ID时开启此选项[事务开启时有效]
        /// </summary>
        internal void SetIdentityInsertOn()
        {
            _setIdentityResult = true;
            if (dalHelper != null && dalHelper.isOpenTrans)
            {
                switch (dalHelper.dalType)
                {
                    case DalType.MsSql:
                    case DalType.Sybase:
                        if (_Data.Columns.FirstPrimary.IsAutoIncrement)//数字型
                        {
                            try
                            {
                                string lastTable = Convert.ToString(CacheManage.LocalInstance.Get("MAction_IdentityInsertForSql"));
                                if (!string.IsNullOrEmpty(lastTable))
                                {
                                    lastTable = "set identity_insert " + SqlFormat.Keyword(lastTable, dalHelper.dalType) + " off";
                                    dalHelper.ExeNonQuery(lastTable, false);
                                }
                                _setIdentityResult = dalHelper.ExeNonQuery("set identity_insert " + SqlFormat.Keyword(_TableName, dalHelper.dalType) + " on", false) > -2;
                                if (_setIdentityResult)
                                {
                                    CacheManage.LocalInstance.Set("MAction_IdentityInsertForSql", _TableName, 30);
                                }
                            }
                            catch
                            {
                            }
                        }
                        break;
                }

            }
            _AllowInsertID = true;
        }
        /// <summary>
        /// MSSQL插入标识ID后关闭此选项[事务开启时有效]
        /// </summary>;
        internal void SetIdentityInsertOff()
        {
            if (_setIdentityResult && dalHelper != null && dalHelper.isOpenTrans)
            {
                switch (dalHelper.dalType)
                {
                    case DalType.MsSql:
                    case DalType.Sybase:
                        if (_Data.Columns.FirstPrimary.IsAutoIncrement)//数字型
                        {
                            try
                            {
                                if (dalHelper.ExeNonQuery("set identity_insert " + SqlFormat.Keyword(_TableName, DalType.MsSql) + " off", false) > -2)
                                {
                                    _setIdentityResult = false;
                                    CacheManage.LocalInstance.Remove("MAction_IdentityInsertForSql");
                                }
                            }
                            catch
                            {
                            }
                        }
                        break;
                }

            }
            _AllowInsertID = false;
        }
        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tableNamesEnum">表名/视图名称</param>
        /// <example><code>
        ///     MAction action=new MAction(TableNames.Users);
        /// 或  MAction action=new MAction("Users");
        /// 或  MAction action=new MAction("(select m.*,u.UserName from Users u left join Message m on u.ID=m.UserID) v");
        /// 或  MAction action=new MAction(ViewNames.Users);//传视图
        /// 或多数据库方式：
        /// MAction action=new MAction(U_DataBaseNameEnum.Users);
        /// 说明：自动截取数据库链接[U_及Enum为前后缀],取到的数据库配置项为DataBaseNameConn
        /// U_为表 V_为视图 P_为存储过程
        /// </code></example>
        public MAction(object tableNamesEnum)
        {
            Init(tableNamesEnum, AppConfig.DB.DefaultConn);
        }

        /// <summary>
        /// 构造函数2
        /// </summary>
        /// <param name="tableNamesEnum">表名/视图名称</param>
        /// <param name="conn">web.config下的connectionStrings的name配置项名称,或完整的链接字符串</param>
        /// <example><code>
        ///     MAction action=new MAction(TableNames.Users,"Conn");
        /// 或  MAction action=new MAction(TableNames.Users,"server=.;database=CYQ;uid=sa;pwd=123456");
        /// </code></example>
        public MAction(object tableNamesEnum, string conn)
        {
            Init(tableNamesEnum, conn);
        }
        #endregion

        #region 初始化

        private void Init(object tableObj, string conn)
        {
            tableObj = SqlCreate.SqlToViewSql(tableObj);
            string dbName = StaticTool.GetDbName(ref tableObj);
            if (conn == AppConfig.DB.DefaultConn && !string.IsNullOrEmpty(dbName))
            {
                conn = dbName + "Conn";
            }
            MDataRow newRow;
            InitConn(tableObj, conn, out newRow);//尝试从MDataRow提取新的Conn链接
            InitSqlHelper(newRow, dbName);
            InitRowSchema(newRow, true);
            InitGlobalObject(true);
            //Aop.IAop myAop = Aop.InterAop.Instance.GetFromConfig();//试图从配置文件加载自定义Aop
            //if (myAop != null)
            //{
            //    SetAop(myAop);
            //}
        }
        private void InitConn(object tableObj, string conn, out MDataRow newRow)
        {
            if (tableObj is MDataRow)
            {
                newRow = tableObj as MDataRow;
            }
            else
            {
                newRow = new MDataRow();
                newRow.TableName = SqlFormat.NotKeyword(tableObj.ToString());
            }
            if (!string.IsNullOrEmpty(conn))
            {
                newRow.Conn = conn;
            }
            _sourceTableName = newRow.TableName;
        }
        private void InitSqlHelper(MDataRow newRow, string newDbName)
        {
            if (dalHelper == null)// || newCreate
            {
                dalHelper = DalCreate.CreateDal(newRow.Conn);
                //创建错误事件。
                if (dalHelper.IsOnExceptionEventNull)
                {
                    dalHelper.OnExceptionEvent += new DbBase.OnException(_DataSqlHelper_OnExceptionEvent);
                }
            }
            else
            {
                dalHelper.ClearParameters();//oracle 11g(某用户的电脑上会出问题，切换表操作，参数未清）
            }
            if (!string.IsNullOrEmpty(newDbName))//需要切换数据库。
            {
                if (string.Compare(dalHelper.DataBase, newDbName, StringComparison.OrdinalIgnoreCase) != 0)//数据库名称不相同。
                {
                    if (newRow.TableName.Contains(" "))//视图语句，则直接切换数据库链接。
                    {
                        dalHelper.ChangeDatabase(newDbName);
                    }
                    else
                    {
                        bool isWithDbName = newRow.TableName.Contains(".");//是否DBName.TableName
                        string fullTableName = isWithDbName ? newRow.TableName : newDbName + "." + newRow.TableName;
                        string sourceDbName = dalHelper.DataBase;
                        DbResetResult result = dalHelper.ChangeDatabaseWithCheck(fullTableName);
                        switch (result)
                        {
                            case DbResetResult.Yes://数据库切换了 (不需要前缀）
                            case DbResetResult.No_SaveDbName:
                            case DbResetResult.No_DBNoExists:
                                if (isWithDbName) //带有前缀的，取消前缀
                                {
                                    _sourceTableName = newRow.TableName = SqlFormat.NotKeyword(fullTableName);
                                }
                                break;
                            case DbResetResult.No_Transationing:
                                if (!isWithDbName)//如果不同的数据库，需要带有数据库前缀
                                {
                                    _sourceTableName = newRow.TableName = fullTableName;
                                }
                                break;
                        }
                    }
                }
            }
        }
        void _DataSqlHelper_OnExceptionEvent(string msg)
        {
            _aop.OnError(msg);
        }
        private static DateTime lastGCTime = DateTime.Now;
        private void InitRowSchema(MDataRow row, bool resetState)
        {
            _Data = row;
            _TableName = SqlCompatible.Format(_sourceTableName, dalHelper.dalType);
            _TableName = DBTool.GetMapTableName(dalHelper.conn, _TableName);//处理数据库映射兼容。
            if (_Data.Count == 0)
            {
                if (!TableSchema.FillTableSchema(ref _Data, ref dalHelper, _TableName, _sourceTableName))
                {
                    if (!dalHelper.TestConn())
                    {
                        Error.Throw(dalHelper.dalType + "." + dalHelper.DataBase + ":open database failed! check the connectionstring is be ok!\r\nerror:" + dalHelper.debugInfo.ToString());
                    }
                    Error.Throw(dalHelper.dalType + "." + dalHelper.DataBase + ":check the tablename  \"" + _TableName + "\" is exist?\r\nerror:" + dalHelper.debugInfo.ToString());
                }
            }
            else if (resetState)
            {
                _Data.SetState(0);
            }
            _Data.Conn = row.Conn;//FillTableSchema会改变_Row的对象。
        }


        /// <summary>
        /// 表切换,在A表时，如果需要操作B,不需要重新new一个MAaction,可直接换用本函数切换
        /// </summary>
        /// <param name="tableObj">要切换的表/视图名</param>
        /// <example><code>
        ///     using(MAction action = new MAction(TableNames.Users))
        ///     {
        ///         if (action.Fill("UserName='路过秋天'"))
        ///         {
        ///             int id = action.Get&lt;int&gt;(Users.ID);
        ///             if (action.ResetTable(TableNames.Message))
        ///             {
        ///                  //other logic...
        ///             }
        ///         }
        ///     }
        /// </code></example>
        public void ResetTable(object tableObj)
        {
            ResetTable(tableObj, true, null);
        }
        /// <summary>
        /// 表切换
        /// </summary>
        /// <param name="tableObj">要切换的表/视图名</param>
        /// <param name="resetState">是否重置原有的数据状态（默认true)</param>
        public void ResetTable(object tableObj, bool resetState)
        {
            ResetTable(tableObj, resetState, null);
        }
        /// <summary>
        /// 表切换
        /// </summary>
        /// <param name="tableObj">要切换的表/视图名</param>
        /// <param name="newDbName">要切换的数据库名称</param>
        public void ResetTable(object tableObj, string newDbName)
        {
            ResetTable(tableObj, true, newDbName);
        }
        private void ResetTable(object tableObj, bool resetState, string newDbName)
        {
            tableObj = SqlCreate.SqlToViewSql(tableObj);
            newDbName = newDbName ?? StaticTool.GetDbName(ref tableObj);
            MDataRow newRow;
            InitConn(tableObj, string.Empty, out newRow);

            //newRow.Conn = newDbName;//除非指定链接，否则不切换数据库
            InitSqlHelper(newRow, newDbName);
            InitRowSchema(newRow, resetState);
            InitGlobalObject(false);
        }

        private void InitGlobalObject(bool allowCreate)
        {
            if (_Data != null)
            {
                if (_sqlCreate == null)
                {
                    _sqlCreate = new SqlCreate(this);
                }
                if (_UI != null)
                {
                    _UI._Data = _Data;
                }
                else if (allowCreate)
                {
                    _UI = new MActionUI(ref _Data, dalHelper, _sqlCreate);
                }
                if (_noSqlAction != null)
                {
                    _noSqlAction.Reset(ref _Data, _TableName, dalHelper.Con.DataSource, dalHelper.dalType);
                }
                else if (allowCreate)
                {
                    switch (dalHelper.dalType)
                    {
                        case DalType.Txt:
                        case DalType.Xml:
                            _noSqlAction = new NoSqlAction(ref _Data, _TableName, dalHelper.Con.DataSource, dalHelper.dalType);
                            break;
                    }
                }
            }
        }

        #endregion

        #region 数据库操作方法

        private bool InsertOrUpdate(string sqlCommandText)
        {
            bool returnResult = false;
            if (_sqlCreate.isCanDo)
            {
                if (_isInsertCommand) //插入
                {
                    _isInsertCommand = false;
                    object ID;
                    switch (dalHelper.dalType)
                    {
                        case DalType.MsSql:
                        case DalType.Sybase:
                            ID = dalHelper.ExeScalar(sqlCommandText, false);
                            if (ID == null && AllowInsertID && dalHelper.recordsAffected > -2)
                            {
                                ID = _Data.PrimaryCell.Value;
                            }
                            break;
                        default:
                            ID = dalHelper.ExeNonQuery(sqlCommandText, false);
                            if (ID != null && Convert.ToInt32(ID) > 0 && _option != InsertOp.None)
                            {
                                if (DataType.GetGroup(_Data.PrimaryCell.Struct.SqlType) == 1)
                                {
                                    ClearParameters();
                                    ID = dalHelper.ExeScalar(_sqlCreate.GetMaxID(), false);
                                }
                                else
                                {
                                    ID = null;
                                    returnResult = true;
                                }

                            }
                            break;
                    }
                    if ((ID != null && Convert.ToString(ID) != "-2") || (dalHelper.recordsAffected > -2 && _option == InsertOp.None))
                    {
                        if (_option != InsertOp.None)
                        {
                            _Data.PrimaryCell.Value = ID;
                        }
                        returnResult = (_option == InsertOp.Fill) ? Fill(ID) : true;
                    }
                }
                else //更新
                {
                    returnResult = dalHelper.ExeNonQuery(sqlCommandText, false) > 0;
                }
            }
            else if (!_isInsertCommand && _Data.GetState() == 1) // 更新操作。
            {
                //输出警告信息
                return true;
            }
            return returnResult;
        }

        #region 插入

        /// <summary>
        ///  插入数据
        /// </summary>
        /// <example><code>
        /// using(MAction action=new MAction(TableNames.Users))
        /// {
        ///     action.Set(Users.UserName,"路过秋天");
        ///     action.Insert();
        /// }
        /// </code></example>
        public bool Insert()
        {
            return Insert(false, _option);
        }
        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="option">插入选项</param>
        public bool Insert(InsertOp option)
        {
            return Insert(false, option);
        }
        /// <summary>
        ///  插入数据
        /// </summary>
        /// <param name="autoSetValue">自动从控制获取值</param>
        public bool Insert(bool autoSetValue)
        {
            return Insert(autoSetValue, _option);
        }
        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="autoSetValue">是否自动获取值[自动从控件获取值,需要先调用SetAutoPrefix方法设置控件前缀]</param>
        /// <example><code>
        /// using(MAction action=new MAction(TableNames.Users))
        /// {
        ///     action.SetAutoPrefix("txt","ddl");
        ///     action.Insert(true);
        /// }
        /// </code></example>
        public bool Insert(bool autoSetValue, InsertOp option)
        {
            CheckDisposed();
            if (autoSetValue)
            {
                _UI.GetAll(!AllowInsertID);//允许插入ID时，也需要获取主键。
            }
            AopResult aopResult = AopResult.Default;
            if (_aop.IsCustomAop)
            {
                _aop.Para.MAction = this;
                _aop.Para.TableName = _sourceTableName;
                _aop.Para.Row = _Data;
                _aop.Para.AutoSetValue = autoSetValue;
                _aop.Para.InsertOp = option;
                _aop.Para.IsTransaction = dalHelper.isOpenTrans;
                aopResult = _aop.Begin(Aop.AopEnum.Insert);
            }
            if (aopResult == AopResult.Default || aopResult == AopResult.Continue)
            {
                switch (dalHelper.dalType)
                {
                    case DalType.Txt:
                    case DalType.Xml:
                        _aop.Para.IsSuccess = _noSqlAction.Insert(dalHelper.isOpenTrans);
                        break;
                    default:
                        ClearParameters();
                        string sql = _sqlCreate.GetInsertSql();
                        _isInsertCommand = true;
                        _option = option;
                        _aop.Para.IsSuccess = InsertOrUpdate(sql);
                        break;
                }
            }
            else if (option != InsertOp.None)
            {
                _Data = _aop.Para.Row;
                InitGlobalObject(false);
            }
            if (_aop.IsCustomAop && (aopResult == AopResult.Break || aopResult == AopResult.Continue))
            {
                _aop.End(Aop.AopEnum.Insert);
            }
            if (dalHelper.recordsAffected == -2)
            {
                OnError();
            }
            return _aop.Para.IsSuccess;
        }
        #endregion

        #region 更新
        /// <summary>
        ///  更新数据[不传where条件时将自动尝试从UI获取]
        /// </summary>
        /// <example><code>
        /// using(MAction action=new MAction(TableNames.Users))
        /// {
        ///     action.Set(Users.UserName,"路过秋天");
        ///     action.Set(Users.ID,1);
        ///     action.Update();
        /// }
        /// </code></example>
        public bool Update()
        {
            return Update(null, true);
        }
        /// <summary>
        ///  更新数据
        /// </summary>
        /// <param name="where">where条件,可直接传id的值如:[88],或传完整where条件如:[id=88 and name='路过秋天']</param>
        /// <example><code>
        /// using(MAction action=new MAction(TableNames.Users))
        /// {
        ///     action.Set(Users.UserName,"路过秋天");
        ///     action.Update("id=1");
        /// }
        /// </code></example>
        public bool Update(object where)
        {
            return Update(where, false);
        }
        /// <summary>
        ///  更新数据
        /// </summary>
        /// <param name="autoSetValue">是否自动获取值[自动从控件获取值,需要先调用SetAutoPrefix或SetAutoParentControl方法设置控件前缀]</param>
        public bool Update(bool autoSetValue)
        {
            return Update(null, autoSetValue);
        }

        /// <summary>
        ///  更新数据
        /// </summary>
        /// <param name="where">where条件,可直接传id的值如:[88],或传完整where条件如:[id=88 and name='路过秋天']</param>
        /// <param name="autoSetValue">是否自动获取值[自动从控件获取值,需要先调用SetAutoPrefix或SetAutoParentControl方法设置控件前缀]</param>
        /// <example><code>
        /// using(MAction action=new MAction(TableNames.Users))
        /// {
        ///     action.SetAutoPrefix("txt","ddl");
        ///     action.Update("name='路过秋天'",true);
        /// }
        /// </code></example>
        public bool Update(object where, bool autoSetValue)
        {
            CheckDisposed();
            if (autoSetValue)
            {
                _UI.GetAll(false);
            }
            if (where == null || Convert.ToString(where) == "")
            {
                where = _sqlCreate.GetPrimaryWhere();
            }
            AopResult aopResult = AopResult.Default;
            if (_aop.IsCustomAop)
            {
                _aop.Para.MAction = this;
                _aop.Para.TableName = _sourceTableName;
                _aop.Para.Row = _Data;
                _aop.Para.Where = where;
                //_aop.Para.AopPara = aopPara;
                _aop.Para.AutoSetValue = autoSetValue;
                _aop.Para.UpdateExpression = _sqlCreate.updateExpression;
                _aop.Para.IsTransaction = dalHelper.isOpenTrans;
                aopResult = _aop.Begin(Aop.AopEnum.Update);
            }
            if (aopResult == AopResult.Default || aopResult == AopResult.Continue)
            {
                switch (dalHelper.dalType)
                {
                    case DalType.Txt:
                    case DalType.Xml:
                        _aop.Para.IsSuccess = _noSqlAction.Update(_sqlCreate.FormatWhere(where));
                        break;
                    default:
                        ClearParameters();
                        string sql = _sqlCreate.GetUpdateSql(where);
                        _aop.Para.IsSuccess = InsertOrUpdate(sql);
                        break;
                }
            }
            if (_aop.IsCustomAop && (aopResult == AopResult.Break || aopResult == AopResult.Continue))
            {
                _aop.End(Aop.AopEnum.Update);
            }
            if (dalHelper.recordsAffected == -2)
            {
                OnError();
            }
            return _aop.Para.IsSuccess;
        }
        #endregion

        /// <summary>
        ///  删除数据[不传where条件时将自动尝试从UI获取]
        /// </summary>
        public bool Delete()
        {
            return Delete(null);
        }
        /// <summary>
        ///  删除数据
        /// </summary>
        /// <param name="where">where条件,可直接传id的值如:[88],或传完整where条件如:[id=88 and name='路过秋天']</param>
        public bool Delete(object where)
        {
            CheckDisposed();
            if (where == null || Convert.ToString(where) == "")
            {
                _UI.PrimayAutoGetValue();
                where = _sqlCreate.GetPrimaryWhere();
            }
            AopResult aopResult = AopResult.Default;
            if (_aop.IsCustomAop)
            {
                _aop.Para.MAction = this;
                _aop.Para.TableName = _sourceTableName;
                _aop.Para.Row = _Data;
                _aop.Para.Where = where;
                //_aop.Para.AopPara = aopPara;
                _aop.Para.IsTransaction = dalHelper.isOpenTrans;
                aopResult = _aop.Begin(Aop.AopEnum.Delete);
            }
            if (aopResult == AopResult.Default || aopResult == AopResult.Continue)
            {
                string deleteField = AppConfig.DB.DeleteField;
                bool isToUpdate = !string.IsNullOrEmpty(deleteField) && _Data.Columns.Contains(deleteField);
                switch (dalHelper.dalType)
                {
                    case DalType.Txt:
                    case DalType.Xml:
                        string sqlWhere = _sqlCreate.FormatWhere(where);
                        if (isToUpdate)
                        {
                            _Data.Set(deleteField, true);
                            _aop.Para.IsSuccess = _noSqlAction.Update(sqlWhere);
                        }
                        else
                        {
                            _aop.Para.IsSuccess = _noSqlAction.Delete(sqlWhere);
                        }
                        break;
                    default:
                        ClearParameters();
                        string sql = isToUpdate ? _sqlCreate.GetDeleteToUpdateSql(where) : _sqlCreate.GetDeleteSql(where);
                        _aop.Para.IsSuccess = dalHelper.ExeNonQuery(sql, false) > 0;
                        break;
                }
            }
            if (_aop.IsCustomAop && (aopResult == AopResult.Break || aopResult == AopResult.Continue))
            {
                _aop.End(Aop.AopEnum.Delete);
            }
            if (dalHelper.recordsAffected == -2)
            {
                OnError();
            }
            return _aop.Para.IsSuccess;
        }

        /// <summary>
        /// 选择所有数据
        /// </summary>
        public MDataTable Select()
        {
            int count;
            return Select(0, 0, null, out count);
        }
        /// <summary>
        /// 根据条件查询所有数据
        /// </summary>
        /// <param name="where">where条件如:id>1</param>
        public MDataTable Select(object where)
        {
            int count;
            return Select(0, 0, where, out count);
        }
        public MDataTable Select(int topN, object where)
        {
            int count;
            return Select(0, topN, where, out count);
        }
        public MDataTable Select(int pageIndex, int pageSize)
        {
            int count;
            return Select(pageIndex, pageSize, null, out count);
        }
        public MDataTable Select(int pageIndex, int pageSize, object where)
        {
            int count;
            return Select(pageIndex, pageSize, where, out count);
        }
        /// <summary>
        /// 带分布功能的选择[多条件查询,选择所有时只需把PageIndex/PageSize设置为0]
        /// </summary>
        /// <param name="pageIndex">第几页</param>
        /// <param name="pageSize">每页数量[为0时默认选择所有]</param>
        /// <param name="where"> 查询条件[可附带 order by 语句]</param>
        /// <param name="rowCount">返回的记录总数</param>
        /// <returns>返回值MDataTable</returns>
        public MDataTable Select(int pageIndex, int pageSize, object where, out int rowCount)
        {
            CheckDisposed();
            rowCount = 0;
            AopResult aopResult = AopResult.Default;
            if (_aop.IsCustomAop)
            {
                _aop.Para.MAction = this;
                _aop.Para.PageIndex = pageIndex;
                _aop.Para.PageSize = pageSize;
                _aop.Para.TableName = _sourceTableName;
                _aop.Para.Row = _Data;
                _aop.Para.Where = where;
                _aop.Para.SelectColumns = _sqlCreate.selectColumns;
                //_aop.Para.AopPara = aopPara;
                _aop.Para.IsTransaction = dalHelper.isOpenTrans;
                aopResult = _aop.Begin(Aop.AopEnum.Select);
            }
            if (aopResult == AopResult.Default || aopResult == AopResult.Continue)
            {
                string primaryKey = SqlFormat.Keyword(_Data.Columns.FirstPrimary.ColumnName, dalHelper.dalType);//主键列名。
                switch (dalHelper.dalType)
                {
                    case DalType.Txt:
                    case DalType.Xml:
                        _aop.Para.Table = _noSqlAction.Select(pageIndex, pageSize, _sqlCreate.FormatWhere(where), out rowCount, _sqlCreate.selectColumns);
                        break;
                    default:
                        _aop.Para.Table = new MDataTable(_TableName.Contains("(") ? "SysDefaultCustomTable" : _TableName);
                        _aop.Para.Table.LoadRow(_Data);
                        ClearParameters();//------------------------参数清除
                        DbDataReader sdReader = null;
                        string whereSql = string.Empty;//已格式化过的原生whereSql语句
                        if (_sqlCreate != null)
                        {
                            whereSql = _sqlCreate.FormatWhere(where);
                        }
                        else
                        {
                            whereSql = SqlFormat.Compatible(where, dalHelper.dalType, dalHelper.Com.Parameters.Count == 0);
                        }
                        bool byPager = pageIndex > 0 && pageSize > 0;//分页查询(第一页也要分页查询，因为要计算总数）
                        if (byPager && AppConfig.DB.PagerBySelectBase && dalHelper.dalType == DalType.MsSql && !dalHelper.Version.StartsWith("08"))// || dalHelper.dalType == DalType.Oracle
                        {
                            #region 存储过程执行
                            if (dalHelper.Com.Parameters.Count > 0)
                            {
                                dalHelper.debugInfo.Append(AppConst.HR + "error : select method deny call SetPara() method to add custom parameters!");
                            }
                            dalHelper.AddParameters("@PageIndex", pageIndex, DbType.Int32, -1, ParameterDirection.Input);
                            dalHelper.AddParameters("@PageSize", pageSize, DbType.Int32, -1, ParameterDirection.Input);
                            dalHelper.AddParameters("@TableName", _sqlCreate.GetSelectTableName(ref whereSql), DbType.String, -1, ParameterDirection.Input);

                            whereSql = _sqlCreate.AddOrderByWithCheck(whereSql, primaryKey);

                            dalHelper.AddParameters("@Where", whereSql, DbType.String, -1, ParameterDirection.Input);
                            sdReader = dalHelper.ExeDataReader("SelectBase", true);
                            #endregion
                        }
                        else
                        {
                            #region SQL语句分页执行
                            if (byPager)
                            {
                                rowCount = Convert.ToInt32(dalHelper.ExeScalar(_sqlCreate.GetCountSql(whereSql), false));//分页查询先记算总数
                            }
                            if (!byPager || (rowCount > 0 && (pageIndex - 1) * pageSize < rowCount))
                            {
                                string sql = SqlCreateForPager.GetSql(dalHelper.dalType, dalHelper.Version, pageIndex, pageSize, whereSql, SqlFormat.Keyword(_TableName, dalHelper.dalType), rowCount, _sqlCreate.GetColumnsSql(), primaryKey, _Data.PrimaryCell.Struct.IsAutoIncrement);
                                sdReader = dalHelper.ExeDataReader(sql, false);
                            }
                            #endregion
                        }
                        if (sdReader != null)
                        {
                            // _aop.Para.Table.ReadFromDbDataReader(sdReader);//内部有关闭。
                            _aop.Para.Table = sdReader;
                            if (!byPager)
                            {
                                rowCount = _aop.Para.Table.Rows.Count;
                            }
                            else if (dalHelper.dalType == DalType.MsSql && AppConfig.DB.PagerBySelectBase)
                            {
                                rowCount = dalHelper.ReturnValue;
                            }
                            _aop.Para.Table.RecordsAffected = rowCount;
                        }
                        else
                        {
                            _aop.Para.Table.Rows.Clear();//预防之前的插入操作产生了一个数据行。
                        }
                        _aop.Para.IsSuccess = _aop.Para.Table.Rows.Count > 0;
                        ClearParameters();//------------------------参数清除
                        break;
                }
            }
            else if (_aop.Para.Table.RecordsAffected > 0)
            {
                rowCount = _aop.Para.Table.RecordsAffected;//返回记录总数
            }
            if (_aop.IsCustomAop && (aopResult == AopResult.Break || aopResult == AopResult.Continue))
            {
                _aop.End(Aop.AopEnum.Select);
            }
            _aop.Para.Table.TableName = TableName;//Aop从Json缓存加载时会丢失表名。
            _aop.Para.Table.Conn = _Data.Conn;
            //修正DataType和Size、Scale
            for (int i = 0; i < _aop.Para.Table.Columns.Count; i++)
            {
                MCellStruct msTable = _aop.Para.Table.Columns[i];
                MCellStruct ms = _Data.Columns[msTable.ColumnName];
                if (ms != null)
                {
                    msTable.Load(ms);
                }
            }
            if (_sqlCreate != null)
            {
                _sqlCreate.selectColumns = null;
            }
            return _aop.Para.Table;
        }

        /// <summary>
        /// 填充自身，即单行选择[不传where条件时将自动尝试从UI获取]
        /// </summary>
        public bool Fill()
        {
            return Fill(null);
        }

        /// <summary>
        /// 填充自身[即单行选择]（填充后所有非Null值的状态会变更为1）
        /// </summary>
        /// <param name="where">where条件,可直接传id的值如:[88],或传完整where条件如:"id=88 or name='路过秋天'"</param>
        /// <example><code>
        /// using(MAction action=new MAction(TableNames.Users))
        /// {
        ///     if(action.Fill("name='路过秋天'")) //或者action.Fill(888) 或者 action.Fill(id=888)
        ///     {
        ///         action.SetTo(labUserName);
        ///     }
        /// }
        /// </code></example>
        public bool Fill(object where)
        {
            CheckDisposed();
            if (where == null || Convert.ToString(where) == "")
            {
                _UI.PrimayAutoGetValue();
                where = _sqlCreate.GetPrimaryWhere();
            }
            AopResult aopResult = AopResult.Default;
            if (_aop.IsCustomAop)
            {
                _aop.Para.MAction = this;
                _aop.Para.TableName = _sourceTableName;
                _aop.Para.Row = _Data;
                _aop.Para.Where = where;
                _aop.Para.SelectColumns = _sqlCreate.selectColumns;
                //_aop.Para.AopPara = aopPara;
                _aop.Para.IsTransaction = dalHelper.isOpenTrans;
                aopResult = _aop.Begin(Aop.AopEnum.Fill);
            }
            if (aopResult == AopResult.Default || aopResult == AopResult.Continue)
            {
                switch (dalHelper.dalType)
                {
                    case DalType.Txt:
                    case DalType.Xml:
                        _aop.Para.IsSuccess = _noSqlAction.Fill(_sqlCreate.FormatWhere(where));
                        break;
                    default:
                        ClearParameters();
                        MDataTable mTable = dalHelper.ExeDataReader(_sqlCreate.GetTopOneSql(where), false);
                        // dalHelper.ResetConn();//重置Slave
                        if (mTable != null && mTable.Rows.Count > 0)
                        {
                            _Data.LoadFrom(mTable.Rows[0], RowOp.None, true);//setselectcolumn("aa as bb")时
                            _aop.Para.IsSuccess = true;
                        }
                        else
                        {
                            _aop.Para.IsSuccess = false;
                        }
                        break;
                }
            }
            else if (_aop.Para.IsSuccess)
            {
                _Data.LoadFrom(_aop.Para.Row, RowOp.None, true);
            }

            if (_aop.IsCustomAop && (aopResult == AopResult.Break || aopResult == AopResult.Continue))
            {
                _aop.Para.Row = _Data;
                _aop.End(Aop.AopEnum.Fill);
            }
            if (_aop.Para.IsSuccess)
            {
                if (_sqlCreate.selectColumns != null)
                {
                    string name;
                    string[] items;
                    foreach (object columnName in _sqlCreate.selectColumns)
                    {
                        items = columnName.ToString().Split(' ');
                        name = items[items.Length - 1];
                        MDataCell cell = _Data[name];
                        if (cell != null)
                        {
                            cell.State = 1;
                        }
                    }
                    items = null;
                }
                else
                {
                    _Data.SetState(1, BreakOp.Null);//查询时，定位状态为1
                }
            }
            if (dalHelper.recordsAffected == -2)
            {
                OnError();
            }
            if (_sqlCreate != null)
            {
                _sqlCreate.selectColumns = null;
            }
            return _aop.Para.IsSuccess;
        }
        /// <summary>
        /// 返回记录总数
        /// </summary>
        public int GetCount()
        {
            return GetCount(null);
        }
        /// <summary>
        /// 返回记录总数
        /// </summary>
        /// <param name="where">where条件,可直接传id的值如:[88],或传完整where条件如:[id=88 and name='路过秋天']</param>
        public int GetCount(object where)
        {
            CheckDisposed();
            AopResult aopResult = AopResult.Default;
            if (_aop.IsCustomAop)
            {
                _aop.Para.MAction = this;
                _aop.Para.TableName = _sourceTableName;
                _aop.Para.Row = _Data;
                _aop.Para.Where = where;
                // _aop.Para.AopPara = aopPara;
                _aop.Para.IsTransaction = dalHelper.isOpenTrans;
                aopResult = _aop.Begin(Aop.AopEnum.GetCount);
            }
            if (aopResult == AopResult.Default || aopResult == AopResult.Continue)
            {
                switch (dalHelper.dalType)
                {
                    case DalType.Txt:
                    case DalType.Xml:
                        _aop.Para.RowCount = _noSqlAction.GetCount(_sqlCreate.FormatWhere(where));
                        _aop.Para.IsSuccess = _aop.Para.RowCount > 0;
                        break;
                    default:
                        ClearParameters();//清除系统参数
                        string countSql = _sqlCreate.GetCountSql(where);
                        object result = dalHelper.ExeScalar(countSql, false);

                        _aop.Para.IsSuccess = result != null;
                        if (_aop.Para.IsSuccess)
                        {
                            _aop.Para.RowCount = Convert.ToInt32(result);
                        }
                        else
                        {
                            _aop.Para.RowCount = -1;
                        }

                        //ClearSysPara(); //清除内部自定义参数[FormatWhere带自定义参数]
                        break;
                }
            }
            if (_aop.IsCustomAop && (aopResult == AopResult.Break || aopResult == AopResult.Continue))
            {
                _aop.End(Aop.AopEnum.GetCount);
            }
            if (dalHelper.recordsAffected == -2)
            {
                OnError();
            }
            return _aop.Para.RowCount;
        }
        /// <summary>
        /// 是否存在指定条件的数据
        /// </summary>
        /// <param name="where">where条件,可直接传id的值如:[88],或传完整where条件如:[id=88 and name='路过秋天']</param>
        public bool Exists(object where)
        {
            CheckDisposed();
            switch (dalHelper.dalType)
            {
                case DalType.Txt:
                case DalType.Xml:
                    return _noSqlAction.Exists(_sqlCreate.FormatWhere(where));
                default:
                    return GetCount(where) > 0;
            }
        }
        #endregion

        #region 其它方法



        /// <summary>
        /// 取值
        /// </summary>
        public T Get<T>(object key)
        {
            return _Data.Get<T>(key);
        }
        /// <summary>
        /// 取值
        /// </summary>
        /// <param name="key">字段名</param>
        /// <param name="defaultValue">值为Null时的默认替换值</param>
        public T Get<T>(object key, T defaultValue)
        {
            return _Data.Get<T>(key, defaultValue);
        }
        /// <summary>
        /// 设置值,例如:[action.Set(TableName.ID,10);]
        /// </summary>
        /// <param name="key">字段名称,可用枚举如:[TableName.ID]</param>
        /// <param name="value">要设置给字段的值</param>
        /// <example><code>
        /// set示例：action.Set(Users.UserName,"路过秋天");
        /// get示例：int id=action.Get&lt;int&gt;(Users.ID);
        /// </code></example>
        public MAction Set(object key, object value)
        {
            return Set(key, value, -1);
        }
        /// <summary>
        /// 设置值
        /// </summary>
        /// <param name="state">手工设置状态[0:未更改；1:已赋值,值相同[可插入]；2:已赋值,值不同[可更新]]</param>
        /// <returns></returns>
        public MAction Set(object key, object value, int state)
        {
            MDataCell cell = _Data[key];
            if (cell != null)
            {
                cell.Value = value;
                if (state > 0 && state < 3)
                {
                    cell.State = state;
                }
            }
            else
            {
                dalHelper.debugInfo.Append(AppConst.HR + "Alarm : can't find the ColumnName:" + key);
            }
            return this;
        }
        /// <summary>
        /// 更新(Update)操作的自定义表达式设置。
        /// </summary>
        /// <param name="updateExpression">例如a字段值自加1："a=a+1"</param>
        public MAction SetExpression(string updateExpression)
        {
            _sqlCreate.updateExpression = updateExpression;
            return this;
        }
        List<AopCustomDbPara> customParaNames = new List<AopCustomDbPara>();
        /// <summary>
        /// 参数化传参[当Where条件为参数化(如：name=@name)语句时使用]
        /// </summary>
        /// <param name="paraName">参数名称</param>
        /// <param name="value">参数值</param>
        /// <param name="dbType">参数类型</param>
        public MAction SetPara(object paraName, object value, DbType dbType)
        {
            if (dalHelper.AddParameters(Convert.ToString(paraName), value, dbType, -1, ParameterDirection.Input))
            {
                AopCustomDbPara para = new AopCustomDbPara();
                para.ParaName = Convert.ToString(paraName).Replace(":", "").Replace("@", "");
                para.Value = value;
                para.ParaDbType = dbType;
                customParaNames.Add(para);
                if (_aop.IsCustomAop)
                {
                    _aop.Para.CustomDbPara = customParaNames;
                }
            }
            return this;
        }
        /// <summary>
        /// 参数化传参[传进多个参数列表]
        /// </summary>
        /// <param name="customParas">Aop场景使用，传进多个参数。</param>
        public MAction SetPara(List<AopCustomDbPara> customParas)
        {
            if (customParas != null && customParas.Count > 0)
            {
                foreach (AopCustomDbPara para in customParas)
                {
                    SetPara(para.ParaName, para.Value, para.ParaDbType);
                }
            }
            return this;
        }
        /// <summary>
        /// 清除(SetPara设置的)自定义参数
        /// </summary>
        public void ClearPara()
        {
            if (customParaNames.Count > 0)
            {
                if (dalHelper != null && dalHelper.Com.Parameters.Count > 0)
                {
                    string paraName = string.Empty;
                    foreach (AopCustomDbPara item in customParaNames)
                    {
                        for (int i = dalHelper.Com.Parameters.Count - 1; i > -1; i--)
                        {
                            if (string.Compare(dalHelper.Com.Parameters[i].ParameterName.TrimStart(dalHelper.Pre), item.ParaName.ToString()) == 0)
                            {
                                dalHelper.Com.Parameters.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
                customParaNames.Clear();
            }
        }
        /// <summary>
        /// 清除内部系统定义的参数
        /// </summary>
        /// <param name="withSysPara"></param>
        //private void ClearSysPara()
        //{
        //    if (customParaNames.Count > 0)
        //    {
        //        string paraName = string.Empty;
        //        for (int i = customParaNames.Count - 1; i >= 0; i--)
        //        {
        //            paraName = _DalHelper.Pre + customParaNames[i].ParaName;
        //            if (customParaNames[i].IsSysPara && _DalHelper.Com.Parameters.Contains(paraName))
        //            {
        //                _DalHelper.Com.Parameters.Remove(_DalHelper.Com.Parameters[paraName]);
        //            }
        //        }
        //    }
        //}
        /// <summary>
        /// 清除系统参数[保留自定义参数]
        /// </summary>
        private void ClearParameters()
        {
            if (dalHelper != null)
            {
                if (customParaNames.Count > 0)//带有自定义参数
                {
                    if (dalHelper.Com.Parameters.Count > 0)
                    {
                        bool isBreak = false;
                        for (int i = dalHelper.Com.Parameters.Count - 1; i > -1; i--)
                        {
                            for (int j = 0; j < customParaNames.Count; j++)
                            {
                                if (string.Compare(dalHelper.Com.Parameters[i].ParameterName.TrimStart(dalHelper.Pre), customParaNames[j].ParaName.ToString()) == 0)
                                {
                                    isBreak = true;
                                }
                            }
                            if (!isBreak)
                            {
                                dalHelper.Com.Parameters.RemoveAt(i);
                                isBreak = false;
                            }
                        }
                    }
                }
                else
                {
                    dalHelper.ClearParameters();
                }
            }
        }

        /// <summary>
        /// 本方法可以在单表使用时查询指定的列[设置后可使用Fill与Select方法]
        /// 提示：分页查询时，排序条件的列必须指定选择。
        /// </summary>
        /// <param name="columnNames">可设置多个列名[调用Fill或Select后,本参数将被清除]</param>
        public MAction SetSelectColumns(params object[] columnNames)
        {
            _sqlCreate.selectColumns = columnNames;
            return this;
        }

        /// <summary>
        /// 根据元数据列组合where条件。
        /// </summary>
        /// <param name="isAnd">true为and连接，反之为or链接</param>
        /// <param name="cells">单元格</param>
        /// <returns></returns>
        public string GetWhere(bool isAnd, params MDataCell[] cells)
        {
            return SqlCreate.GetWhere(DalType, isAnd, cells);
        }

        /// <summary>
        /// 根据元数据列组合and连接的where条件。
        /// </summary>
        /// <param name="cells">单元格</param>
        /// <returns></returns>
        public string GetWhere(params MDataCell[] cells)
        {
            return SqlCreate.GetWhere(DalType, cells);
        }



        #endregion

        #region 事务操作
        /// <summary>
        /// 设置事务级别
        /// </summary>
        /// <param name="level"></param>
        public MAction SetTransLevel(IsolationLevel level)
        {
            dalHelper.tranLevel = level;
            return this;
        }

        /// <summary>
        /// 开始事务
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
                    dalHelper.OnExceptionEvent -= new DbBase.OnException(_DataSqlHelper_OnExceptionEvent);
                }
                _debugInfo = dalHelper.debugInfo.ToString();
                dalHelper.Dispose();
                dalHelper = null;
                if (_sqlCreate != null)
                {
                    _sqlCreate = null;
                }
            }
            if (_noSqlAction != null)
            {
                _noSqlAction.Dispose();
            }
            if (_aop != null)
            {
                _aop = null;
            }
        }
        internal void OnError()
        {
            if (dalHelper != null && dalHelper.isOpenTrans)
            {
                Dispose();
            }
        }
        bool hasDisposed = false;
        private void CheckDisposed()
        {
            if (hasDisposed)
            {
                Error.Throw("The current object 'MAction' has been disposed");
            }
        }
        #endregion


    }
    //AOP 部分
    public partial class MAction
    {
        #region Aop操作
        private InterAop _aop = new InterAop();
        //private IAop _aop = Aop.InterAop.Instance;//切入点
        //private AopInfo _aopInfo = new AopInfo();
        /// <summary>
        /// 临时备份Aop，用于切换后的还原。
        /// </summary>
        // Aop.IAop _aopBak = null;
        /// <summary>
        /// 取消Aop，在Aop独立模块使用MAction时必须调用
        /// </summary>
        public MAction SetAopOff()
        {
            _aop.IsCustomAop = false;
            //if (_aop.IsCustomAop)
            //{
            //    _aopBak = _aop;//设置好备份。
            //    _aop = Aop.InterAop.Instance;
            //    _aop.IsCustomAop = false;
            //}
            return this;
        }
        /// <summary>
        /// 恢复默认配置的Aop。
        /// </summary>
        public MAction SetAopOn()
        {
            _aop.IsCustomAop = true;
            //if (!_aop.IsCustomAop)
            //{
            //    SetAop(_aopBak);
            //}
            return this;
        }
        /// <summary>
        /// 主动设置注入新的Aop，一般情况下不需要用到。
        /// </summary>
        /// <param name="aop"></param>
        //private MAction SetAop(Aop.IAop aop)
        //{
        //    _aop = aop;
        //    _aop.IsCustomAop = true;
        //    return this;
        //}
        /// <summary>
        /// 需要传递额外的参数供Aop使用时可设置。
        /// </summary>
        /// <param name="para"></param>
        public MAction SetAopPara(object para)
        {
            _aop.Para.AopPara = para;
            return this;
        }

        #endregion
    }
    //UI 部分
    public partial class MAction
    {
        private MActionUI _UI;
        /// <summary>
        /// UI操作
        /// </summary>
        public MActionUI UI
        {
            get
            {
                return _UI;
            }
        }
    }

}
