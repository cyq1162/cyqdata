using System;
using System.Collections.Generic;
using CYQ.Data.Table;
using System.Reflection;

using CYQ.Data.Tool;
using CYQ.Data.SQL;
using System.Data;
using CYQ.Data.Aop;
using CYQ.Data.UI;
using CYQ.Data.Json;

namespace CYQ.Data.Orm
{
    ///// <summary>
    ///// ORM CodeFirst 字段来源
    ///// </summary>
    //public enum FieldSource
    //{
    //    /// <summary>
    //    /// 从实体属性来
    //    /// </summary>
    //    Property,
    //    /// <summary>
    //    /// 从数据库或文本数据来
    //    /// </summary>
    //    Data,
    //    /// <summary>
    //    /// 综合以上两者
    //    /// </summary>
    //    BothOfAll
    //}
    /// <summary>
    /// 简单ORM基类（无属性拦截，实体全量更新或插入）
    /// </summary>
    public abstract class SimpleOrmBase<T> : SimpleOrmBase where T : class
    {
        public new T Get(object where)
        {
            return base.Get<T>(where);
        }
        public new List<T> Select()
        {
            return base.Select<T>();
        }
        public new List<T> Select(int pageIndex, int pageSize)
        {
            return base.Select<T>(pageIndex, pageSize);
        }
        public new List<T> Select(int topN, string where)
        {
            return base.Select<T>(topN, where);
        }
        public new List<T> Select(string where)
        {
            return base.Select<T>(where);
        }
        public new List<T> Select(int pageIndex, int pageSize, string where)
        {
            return base.Select<T>(pageIndex, pageSize, where);
        }
        public new List<T> Select(int pageIndex, int pageSize, string where, out int count)
        {
            return base.Select<T>(pageIndex, pageSize, where, out count);
        }
    }

    internal class SimpleOrmBaseDefaultInstance : SimpleOrmBase
    { }
    /// <summary>
    /// 简单ORM基类（无属性拦截，实体全量更新或插入）
    /// </summary>
    public abstract class SimpleOrmBase : IDisposable
    {
        OrmBaseInfo _BaseInfo;
        /// <summary>
        /// 获取 - 基类 ORM 的相关信息【Orm内部属性】
        /// </summary>
        [JsonIgnore]
        public OrmBaseInfo BaseInfo
        {
            get
            {
                if (_BaseInfo == null)
                {
                    _BaseInfo = new OrmBaseInfo(Action);
                }
                return _BaseInfo;
            }
        }


        internal MDataColumn Columns = null;
        private bool _IsWriteLogOnError = true;
        /// <summary>
        /// 标识是否允许写日志(默认true)
        /// </summary>
        [JsonIgnore]
        internal bool IsWriteLogOnError
        {
            set
            {
                _IsWriteLogOnError = value;
                if (_Action != null && _Action.dalHelper != null)
                {
                    _Action.dalHelper.IsWriteLogOnError = value;
                }
            }
        }
        /// <summary>
        /// 是否启用了AOP拦截设置字段值同步(默认false)
        /// </summary>
        internal bool IsUseAop = false;
        //private static FieldSource _FieldSource = FieldSource.BothOfAll;
        ///// <summary>
        /////  字段来源（当字段变更时，可以设置此属性来切换更新）
        ///// </summary>
        //public static FieldSource FieldSource
        //{
        //    get
        //    {
        //        return _FieldSource;
        //    }
        //    set
        //    {
        //        _FieldSource = value;
        //    }
        //}

        Object entity;//实体对象
        Type typeInfo;//实体对象类型
        private MAction _Action;
        [JsonIgnore]
        internal MAction Action
        {
            get
            {
                if (_Action == null && !string.IsNullOrEmpty(_conn))
                {
                    SetDelayInit(_entityInstance, _tableName, _conn);//延迟加载
                    if (_Action != null && _Action.dalHelper != null)
                    {
                        _Action.dalHelper.IsWriteLogOnError = _IsWriteLogOnError;
                    }
                }
                return _Action;
            }
        }

        /// <summary>
        /// Whether to allow manual insertion of ids for self-incrementing primary key identification
        ///<para>自增主键标识的，是否允许手动插入id</para> 
        /// </summary>
        [JsonIgnore]
        public bool AllowInsertID
        {
            get
            {
                if (Action != null)
                {
                    return Action.AllowInsertID;
                }
                return false;
            }
            set
            {
                if (Action != null)
                {
                    Action.AllowInsertID = value;
                }
            }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public SimpleOrmBase()
        {

        }
        /// <summary>
        /// 设置参数化
        /// </summary>
        public SimpleOrmBase SetPara(object paraName, object value)
        {
            if (Action != null)
            {
                Action.SetPara(paraName, value);
            }
            return this;
        }
        /// <summary>
        /// 设置参数化
        /// </summary>
        public SimpleOrmBase SetPara(object paraName, object value, DbType dbType)
        {
            if (Action != null)
            {
                Action.SetPara(paraName, value, dbType);
            }
            return this;
        }

        /// <summary>
        /// Sets a custom expression for the Update operation.
        /// <para>为Update操作设置自定义表达式。</para>
        /// </summary>
        /// <param name="updateExpression">as："a=a+1"<para>如："a=a+1"</para></param>
        public SimpleOrmBase SetExpression(string updateExpression)
        {
            if (Action != null)
            {
                Action.SetExpression(updateExpression);
            }
            return this;
        }

        /// <summary>
        /// 设置Aop状态
        /// </summary>
        /// <param name="op"></param>
        public SimpleOrmBase SetAopState(AopOp op)
        {
            if (Action != null)
            {
                Action.SetAopState(op);
            }
            return this;
        }
        /// <summary>
        /// 初始化状态[继承此基类的实体在构造函数中需调用此方法]
        /// </summary>
        /// <param name="entityInstance">实体对象,一般写:this</param>
        protected void SetInit(Object entityInstance)
        {
            SetInit(entityInstance, null, AppConfig.DB.DefaultConn);
        }
        /// <summary>
        /// 初始化状态[继承此基类的实体在构造函数中需调用此方法]
        /// </summary>
        /// <param name="entityInstance">实体对象,一般写:this</param>
        /// <param name="tableName">表名,如:Users</param>
        protected void SetInit(Object entityInstance, string tableName)
        {
            SetInit(entityInstance, tableName, AppConfig.DB.DefaultConn);
        }
        /// <summary>
        /// 初始化状态[继承此基类的实体在构造函数中需调用此方法]
        /// </summary>
        /// <param name="entityInstance">实体对象,一般写:this</param>
        /// <param name="tableName">表名,如:Users</param>
        /// <param name="conn">数据链接,单数据库时可写Null,或写默认链接配置项:"Conn",或直接数据库链接字符串</param>
        protected void SetInit(Object entityInstance, string tableName, string conn)
        {
            _entityInstance = entityInstance;
            _tableName = tableName;
            _conn = conn;
        }
        private object _entityInstance;
        private string _tableName = string.Empty;
        private string _conn = string.Empty;

        /// <summary>
        /// 将原有的初始化改造成延时加载。
        /// </summary>
        private void SetDelayInit(Object entityInstance, string tableName, string conn)
        {
            if (tableName == AppConfig.Log.TableName && string.IsNullOrEmpty(AppConfig.Log.Conn))
            {
                return;
            }
            //if (string.IsNullOrEmpty(conn))
            //{
            //    //不设置链接，则忽略（当成普通的实体类）
            //    return;
            //}
            if (entityInstance == null) { entityInstance = this; }
            entity = entityInstance;
            typeInfo = entity.GetType();
            try
            {
                if (string.IsNullOrEmpty(tableName))
                {
                    string tName, tConn;
                    tName = DBFast.GetTableName(typeInfo, out tConn);
                    if (string.IsNullOrEmpty(tableName)) { tableName = tName; }
                    //if (string.IsNullOrEmpty(conn)) { conn = tConn; }
                }
                string errMsg = string.Empty;
                Columns = DBTool.GetColumns(tableName, conn, out errMsg);//内部链接错误时抛异常。
                if (Columns == null || Columns.Count == 0)
                {
                    if (errMsg != string.Empty)
                    {
                        Error.Throw(errMsg);
                    }
                    Columns = TableSchema.GetColumnByType(typeInfo, conn);
                    ConnBean connBean = ConnBean.Create(conn);//下面指定链接，才不会在主从备时被切换到其它库。
                    if (connBean == null)
                    {
                        string err = "SimpleOrmBase<T>.SetDelayInit ConnBean can't create by " + conn;
                        Log.Write(err, LogType.DataBase);
                        Error.Throw(err);
                    }
                    if (!DBTool.Exists(tableName, "U", connBean.ConnStringOrg))
                    {
                        lock (tableName)
                        {
                            if (!DBTool.Exists(tableName, "U", connBean.ConnStringOrg))
                            {
                                DBTool.ErrorMsg = null;
                                if (!DBTool.CreateTable(tableName, Columns, connBean.ConnStringOrg))
                                {
                                    string err = "SimpleOrmBase ：Create Table " + tableName + " Error:" + DBTool.ErrorMsg;
                                    Log.Write(err, LogType.DataBase);
                                    Error.Throw(err);
                                }
                            }
                        }
                    }
                }
                _Action = new MAction(Columns.ToRow(tableName), conn);
                if (typeInfo.Name == "SysLogs")
                {
                    _Action.SetAopState(Aop.AopOp.CloseAll);
                }
            }
            catch (Exception err)
            {
                if (typeInfo.Name != "SysLogs")
                {
                    Log.Write(err, LogType.DataBase);
                }
                Error.Throw(err.Message);
            }
        }
        internal void SetInit2(Object entityInstance, string tableName, string conn)
        {
            SetInit(entityInstance, tableName, conn);
        }
        internal void Set(object key, object value)
        {
            if (Action != null)
            {
                Action.Set(key, value);
            }
        }
        #region 基础增删改查 成员

        #region 插入
        /// <summary>
        /// 插入数据
        /// </summary>
        public bool Insert()
        {
            return Insert(false, InsertOp.ID, false);
        }
        /// <summary>
        ///  插入数据
        /// </summary>
        /// <param name="option">插入选项</param>
        public bool Insert(InsertOp option)
        {
            return Insert(false, option, false);
        }
        /// <summary>
        ///  插入数据
        /// </summary>
        /// <param name="insertID">插入主键</param>
        public bool Insert(InsertOp option, bool insertID)
        {
            return Insert(false, option, insertID);
        }
        /// <summary>
        ///  插入数据
        /// </summary>
        /// <param name="autoSetValue">是否自动获取值[自动从控件获取值,需要先调用this.UI.SetAutoPrefix或this.UI.SetAutoParentControl方法设置控件前缀]</param>
        public bool Insert(bool autoSetValue)
        {
            return Insert(autoSetValue, InsertOp.ID, false);
        }
        /// <summary>
        ///  插入数据
        /// </summary>
        /// <param name="autoSetValue">是否自动获取值[自动从控件获取值,需要先调用this.UI.SetAutoPrefix或this.UI.SetAutoParentControl方法设置控件前缀]</param>
        public bool Insert(bool autoSetValue, InsertOp option)
        {
            return Insert(autoSetValue, option, false);
        }
        /// <summary>
        ///  插入数据
        /// </summary>
        /// <param name="autoSetValue">是否自动获取值[自动从控件获取值,需要先调用this.UI.SetAutoPrefix或this.UI.SetAutoParentControl方法设置控件前缀]</param>
        /// <param name="option">插入选项</param>
        /// <param name="insertID">插入主键</param>
        public bool Insert(bool autoSetValue, InsertOp option, bool insertID)
        {
            if (Action == null)
            {
                return false;
            }
            if (autoSetValue)
            {
                Action.UI.GetAll(!insertID);
            }
            GetValueFromEntity();
            Action.AllowInsertID = insertID;
            bool result = Action.Insert(false, option);
            if (autoSetValue || option != InsertOp.None)
            {
                SetValueToEntity();
            }
            return result;
        }
        #endregion

        #region 更新
        /// <summary>
        ///  更新数据(受影响行数>0才为true)
        /// </summary>
        public bool Update()
        {
            return Update(null, false, null);
        }
        /// <summary>
        ///  更新数据(受影响行数>0才为true)
        /// </summary>
        /// <param name="where">where条件,可直接传id的值如:[88],或传完整where条件如:[id=88 and name='路过秋天']</param>
        /// <param name="updateColumns">指定仅更新的列名，多个用逗号分隔</param>
        /// <returns></returns>
        public bool Update(object where, params string[] updateColumns)
        {
            return Update(where, false, updateColumns);
        }
        /// <summary>
        ///  更新数据(受影响行数>0才为true)
        /// </summary>
        /// <param name="where">where条件,可直接传id的值如:[88],或传完整where条件如:[id=88 and name='路过秋天']</param>
        /// <param name="autoSetValue">是否自动获取值[自动从控件获取值,需要先调用this.UI.SetAutoPrefix或this.UI.SetAutoParentControl方法设置控件前缀]</param>
        public bool Update(object where, bool autoSetValue, params string[] updateColumns)
        {
            if (Action == null)
            {
                return false;
            }
            if (autoSetValue)
            {
                Action.UI.GetAll(false);
            }
            GetValueFromEntity();

            if (updateColumns != null && updateColumns.Length > 0)
            {
                Action.Data.SetState(0);
                if (updateColumns.Length == 1)
                {
                    updateColumns = updateColumns[0].Split(',');
                }
                foreach (string item in updateColumns)
                {
                    MDataCell cell = Action.Data[item];
                    if (cell != null)
                    {
                        cell.State = 2;
                    }
                }
            }
            bool result = Action.Update(where) && Action.RecordsAffected > 0;
            if (autoSetValue)
            {
                SetValueToEntity();
            }
            return result;
        }
        #endregion

        #region 删除
        /// <summary>
        ///  删除数据(受影响行数>0才为true)
        /// </summary>
        public bool Delete()
        {
            return Delete(null);
        }
        /// <summary>
        ///  删除数据(受影响行数>0才为true)
        /// </summary>
        /// <param name="where">where条件,可直接传id的值如:[88],或传完整where条件如:[id=88 and name='路过秋天']</param>
        public bool Delete(object where)
        {
            return Delete(where, false);
        }

        /// <summary>
        ///  删除数据(受影响行数>0才为true)
        /// </summary>
        /// <param name="isIgnoreDeleteField">当表存在删除字段，如：IsDeleted标识时，默认会转成更新，此标识可以强制设置为删除</param>
        /// <returns></returns>
        public bool Delete(object where, bool isIgnoreDeleteField)
        {
            if (Action == null)
            {
                return false;
            }
            GetValueFromEntity();
            return Action.Delete(where, isIgnoreDeleteField) && Action.RecordsAffected > 0;
        }
        #endregion

        #region 查询

        /// <summary>
        /// 查询1条数据
        /// </summary>
        public bool Fill()
        {
            return Fill(null);
        }
        /// <summary>
        /// 查询1条数据
        /// </summary>
        public bool Fill(object where)
        {
            if (Action == null)
            {
                return false;
            }
            bool result = Action.Fill(where);
            if (result)
            {
                SetValueToEntity();
            }
            return result;
        }
        /// <summary>
        /// 查询一条数据并返回一个新的实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T Get<T>(object where) where T : class
        {
            if (Action != null)
            {
                bool result = Action.Fill(where);
                if (result)
                {
                    return Action.Data.ToEntity<T>();
                }
            }
            return default(T);
        }
        /// <summary>
        /// 列表查询
        /// </summary>
        public virtual List<T> Select<T>() where T : class
        {
            int count = 0;
            return Select<T>(0, 0, null, out count);
        }
        /// <summary>
        /// 列表查询
        /// </summary>
        /// <param name="where">查询条件[可附带 order by 语句]</param>
        /// <returns></returns>
        public virtual List<T> Select<T>(string where) where T : class
        {
            int count = 0;
            return Select<T>(0, 0, where, out count);
        }
        /// <summary>
        /// 列表查询
        /// </summary>
        /// <param name="topN">查询几条</param>
        /// <param name="where">查询条件[可附带 order by 语句]</param>
        /// <returns></returns>
        public virtual List<T> Select<T>(int topN, string where) where T : class
        {
            int count = 0;
            return Select<T>(0, topN, where, out count);
        }
        public virtual List<T> Select<T>(int pageIndex, int pageSize) where T : class
        {
            int count = 0;
            return Select<T>(pageIndex, pageSize, null, out count);
        }
        public virtual List<T> Select<T>(int pageIndex, int pageSize, string where) where T : class
        {
            int count = 0;
            return Select<T>(pageIndex, pageSize, where, out count);
        }
        /// <summary>
        /// 带分布功能的选择[多条件查询,选择所有时只需把PageIndex/PageSize设置为0]
        /// </summary>
        /// <param name="pageIndex">第几页</param>
        /// <param name="pageSize">每页数量[为0时默认选择所有]</param>
        /// <param name="where"> 查询条件[可附带 order by 语句]</param>
        /// <param name="count">返回的记录总数</param>
        /// <param name="selectColumns">指定返回的列</param>
        public virtual List<T> Select<T>(int pageIndex, int pageSize, string where, out int count) where T : class
        {
            if (Action == null)
            {
                count = 0;
                return null;
            }
            return Action.SelectList<T>(pageIndex, pageSize, where, out count);
        }

        internal MDataTable Select(int pageIndex, int pageSize, string where, out int count)
        {
            if (Action == null)
            {
                count = 0;
                return null;
            }
            return Action.Select(pageIndex, pageSize, where, out count);

        }
        public SimpleOrmBase SetSelectColumns(params object[] columnNames)
        {
            if (Action != null)
            {
                Action.SetSelectColumns(columnNames);
            }
            return this;
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int GetCount()
        {
            if (Action == null)
            {
                return 0;
            }
            return Action.GetCount();
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int GetCount(object where)
        {
            if (Action == null)
            {
                return 0;
            }
            return Action.GetCount(where);
        }
        /// <summary>
        /// 查询是否存在指定的条件的数据
        /// </summary>
        public bool Exists(object where)
        {
            if (Action == null)
            {
                return false;
            }
            return Action.Exists(where);
        }

        #endregion
        /// <summary>
        /// 批量加载值（可从Json、实体对象、MDataRow、泛型字段等批量加载值）
        /// </summary>
        /// <param name="jsonOrEntity">json字符串或实体对象</param>
        public void LoadFrom(object jsonOrEntity)
        {
            LoadFrom(jsonOrEntity, BreakOp.None);
        }
        public void LoadFrom(object jsonOrEntity, BreakOp op)
        {
            MDataRow newValueRow = MDataRow.CreateFrom(jsonOrEntity, null, op);
            if (Action != null)
            {
                Action.Data.LoadFrom(newValueRow);
            }
            List<PropertyInfo> piList = ReflectTool.GetPropertyList(typeInfo);
            foreach (PropertyInfo item in piList)
            {
                if (item.CanWrite)
                {
                    MDataCell cell = newValueRow[item.Name];
                    if (cell != null && !cell.IsNull)
                    {
                        try
                        {
                            item.SetValue(entity, ConvertTool.ChangeType(cell.Value, item.PropertyType), null);
                        }
                        catch (Exception err)
                        {
                            Log.Write(err, LogType.Error);
                        }
                    }
                }
            }
        }
        #endregion
        internal void SetValueToEntity()
        {
            SetValueToEntity(null, RowOp.IgnoreNull);
        }
        internal void SetValueToEntity(RowOp op)
        {
            SetValueToEntity(null, op);
        }
        internal void SetValueToEntity(string propName, RowOp op)
        {
            if (!string.IsNullOrEmpty(propName))
            {
                PropertyInfo pi = typeInfo.GetProperty(propName);
                if (pi != null && pi.CanWrite)
                {
                    MDataCell cell = Action.Data[propName];
                    if (cell != null && !cell.IsNull)
                    {
                        try
                        {
                            pi.SetValue(entity, cell.Value, null);
                        }
                        catch
                        {

                        }

                    }
                }
            }
            else
            {
                if (Action != null)
                {
                    Action.Data.SetToEntity(entity, op);
                }
            }
        }
        private void GetValueFromEntity()
        {
            if (!IsUseAop || AppConfig.IsNetCore)//ASPNETCore下，动态代理的Aop是无效的
            {
                MDataRow d = Action.Data;//先触发延时加载的。
                MDataRow row = MDataRow.CreateFrom(entity);//以实体原有值为基础。
                foreach (MDataCell cell in d)
                {
                    MDataCell valueCell = row[cell.ColumnName];
                    if (valueCell == null || valueCell.IsNull)
                    {
                        continue;
                    }
                    if (cell.State == 2 && cell.Struct.ValueType.IsValueType && (valueCell.StringValue == "0" || valueCell.StringValue == DateTime.MinValue.ToString()))
                    {
                        continue;
                    }
                    cell.Value = valueCell.Value;
                }
            }
        }
        /// <summary>
        /// 清除(SetPara设置的)自定义参数
        /// </summary>
        public void ClearPara()
        {
            if (_Action != null)
            {
                _Action.ClearPara();
            }
        }
        /// <summary>
        /// 清空所有值
        /// </summary>
        public void Clear()
        {
            if (_Action != null)
            {
                _Action.Data.Clear();
            }
        }

        /// <summary>
        /// UI操作
        /// </summary>
        [JsonIgnore]
        public MActionUI UI
        {
            get
            {
                if (Action == null) { return null; }
                if (Action.UI.IsOnAfterGetFromEventNull)
                {
                    Action.UI.OnAfterGetFromEvent += new CYQ.Data.UI.MActionUI.OnAfterGetFrom(UI_OnAfterGetFromEvent);
                }
                return Action.UI;
            }
        }

        void UI_OnAfterGetFromEvent(string propValue)
        {
            if (!string.IsNullOrEmpty(propValue))
            {
                SetValueToEntity(propValue, RowOp.IgnoreNull);
            }
        }


        #region IDisposable 成员
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_Action != null && !_Action.IsTransation)//ORM的事务，由全局控制，不在这里释放链接。
            {
                _Action.Dispose();
            }
        }

        #endregion
    }


}
