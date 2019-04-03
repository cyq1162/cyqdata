using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;
using System.Reflection;

using CYQ.Data.Tool;
using CYQ.Data.Cache;
using CYQ.Data.SQL;
using System.Data;
using CYQ.Data.Aop;


namespace CYQ.Data.Orm
{
    /// <summary>
    /// ORM CodeFirst 字段来源
    /// </summary>
    public enum FieldSource
    {
        /// <summary>
        /// 从实体属性来
        /// </summary>
        Property,
        /// <summary>
        /// 从数据库或文本数据来
        /// </summary>
        Data,
        /// <summary>
        /// 综合以上两者
        /// </summary>
        BothOfAll
    }
    /// <summary>
    /// 简单ORM基类（纯数据交互功能）
    /// </summary>
    public class SimpleOrmBase : IDisposable
    {
        internal MDataColumn Columns = null;
        /// <summary>
        /// 标识是否允许写日志(默认true)
        /// </summary>
        internal bool IsWriteLogOnError
        {
            set
            {
                if (Action != null && Action.dalHelper != null)
                {
                    Action.dalHelper.IsWriteLogOnError = value;
                }
            }
        }
        /// <summary>
        /// 是否启用了AOP拦截设置字段值同步(默认false)
        /// </summary>
        internal bool IsUseAop = false;
        private static FieldSource _FieldSource = FieldSource.BothOfAll;
        /// <summary>
        ///  字段来源（当字段变更时，可以设置此属性来切换更新）
        /// </summary>
        public static FieldSource FieldSource
        {
            get
            {
                return _FieldSource;
            }
            set
            {
                _FieldSource = value;
            }
        }

        Object entity;//实体对象
        Type typeInfo;//实体对象类型
        private MAction _Action;
        internal MAction Action
        {
            get
            {
                if (_Action == null)
                {
                    SetDelayInit(_entityInstance, _tableName, _conn, _op);//延迟加载
                }
                return _Action;
            }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public SimpleOrmBase()
        {

        }
        /// <summary>
        /// 设置Aop状态
        /// </summary>
        /// <param name="op"></param>
        public void SetAopState(AopOp op)
        {
            Action.SetAopState(op);
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
            SetInit(entityInstance, tableName, conn, AopOp.OpenAll);
        }
        private object _entityInstance;
        private string _tableName;
        private string _conn;
        private AopOp _op;
        protected void SetInit(Object entityInstance, string tableName, string conn, AopOp op)
        {
            _entityInstance = entityInstance;
            _tableName = tableName;
            _conn = conn;
            _op = op;
        }
        /// <summary>
        /// 将原有的初始化改造成延时加载。
        /// </summary>
        private void SetDelayInit(Object entityInstance, string tableName, string conn, AopOp op)
        {
            if (string.IsNullOrEmpty(conn))
            {
                //不设置链接，则忽略（当成普通的实体类）
                return;
            }
            entity = entityInstance;
            typeInfo = entity.GetType();
            try
            {
                if (string.IsNullOrEmpty(tableName))
                {
                    tableName = typeInfo.Name;
                    if (tableName.EndsWith(AppConfig.EntitySuffix))
                    {
                        tableName = tableName.Substring(0, tableName.Length - AppConfig.EntitySuffix.Length);
                    }
                }

                string key = tableName + StaticTool.GetHashKey(conn);
                if (!CacheManage.LocalInstance.Contains(key))
                {
                    DalType dal = DBTool.GetDalType(conn);
                    bool isTxtDal = dal == DalType.Txt || dal == DalType.Xml;
                    string errMsg = string.Empty;
                    Columns = DBTool.GetColumns(tableName, conn, out errMsg);//内部链接错误时抛异常。
                    if (Columns == null || Columns.Count == 0)
                    {
                        if (errMsg != string.Empty)
                        {
                            Error.Throw(errMsg);
                        }
                        Columns = TableSchema.GetColumns(typeInfo);
                        ConnBean connBean = ConnBean.Create(conn);//下面指定链接，才不会在主从备时被切换到其它库。
                        if (!DBTool.ExistsTable(tableName, connBean.ConnString))
                        {
                            DBTool.ErrorMsg = null;
                            if (!DBTool.CreateTable(tableName, Columns, connBean.ConnString))
                            {
                                Error.Throw("SimpleOrmBase ：Create Table " + tableName + " Error:" + DBTool.ErrorMsg);
                            }
                        }
                    }
                    else if (isTxtDal)//文本数据库
                    {
                        if (FieldSource != FieldSource.Data)
                        {
                            MDataColumn c2 = TableSchema.GetColumns(typeInfo);
                            if (FieldSource == FieldSource.BothOfAll)
                            {
                                Columns.AddRange(c2);
                            }
                            else
                            {
                                Columns = c2;
                            }
                        }
                    }

                    if (Columns != null && Columns.Count > 0)
                    {
                        CacheManage.LocalInstance.Set(key, Columns, 1440, null);
                    }
                }
                else
                {
                    Columns = CacheManage.LocalInstance.Get(key) as MDataColumn;
                }

                _Action = new MAction(Columns.ToRow(tableName), conn);
                if (typeInfo.Name == "SysLogs")
                {
                    _Action.SetAopState(Aop.AopOp.CloseAll);
                }
                else
                {
                    _Action.SetAopState(op);
                }
                _Action.EndTransation();
            }
            catch (Exception err)
            {
                if (typeInfo.Name != "SysLogs")
                {
                    Log.Write(err, LogType.DataBase);
                }
                throw;
            }
        }
        internal void SetInit2(Object entityInstance, string tableName, string conn, AopOp op)
        {
            SetInit(entityInstance, tableName, conn, op);
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
            return Insert(InsertOp.ID);
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
        /*
        /// <summary>
        ///  插入数据
        /// </summary>
        /// <param name="autoSetValue">自动从控制获取值</param>
        internal bool Insert(bool autoSetValue)
        {
            return Insert(autoSetValue, InsertOp.ID);
        }
        internal bool Insert(bool autoSetValue, InsertOp option)
        {
            return Insert(autoSetValue, InsertOp.ID, false);
        }*/
        /// <summary>
        ///  插入数据
        /// </summary>
        /// <param name="autoSetValue">自动从控制获取值</param>
        /// <param name="option">插入选项</param>
        /// <param name="insertID">插入主键</param>
        internal bool Insert(bool autoSetValue, InsertOp option, bool insertID)
        {
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
        ///  更新数据
        /// </summary>
        public bool Update()
        {
            return Update(null, false);
        }
        /// <summary>
        ///  更新数据
        /// </summary>
        /// <param name="where">where条件,可直接传id的值如:[88],或传完整where条件如:[id=88 and name='路过秋天']</param>
        public bool Update(object where)
        {
            return Update(where, false);
        }
        /// <summary>
        ///  更新数据
        /// </summary>
        /// <param name="where">where条件,可直接传id的值如:[88],或传完整where条件如:[id=88 and name='路过秋天']</param>
        /// <param name="autoSetValue">是否自动获取值[自动从控件获取值,需要先调用SetAutoPrefix或SetAutoParentControl方法设置控件前缀]</param>
        internal bool Update(object where, bool autoSetValue)
        {
            if (autoSetValue)
            {
                Action.UI.GetAll(false);
            }
            GetValueFromEntity();
            bool result = Action.Update(where);
            if (autoSetValue)
            {
                SetValueToEntity();
            }
            return result;
        }
        #endregion

        #region 删除
        /// <summary>
        ///  删除数据
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
            GetValueFromEntity();
            return Action.Delete(where);
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
            bool result = Action.Fill(where);
            if (result)
            {
                SetValueToEntity();
            }
            return result;
        }

        /// <summary>
        /// 列表查询
        /// </summary>
        public List<T> Select<T>()
        {
            int count = 0;
            return Select<T>(0, 0, null, out count);
        }
        /// <summary>
        /// 列表查询
        /// </summary>
        /// <param name="where">查询条件[可附带 order by 语句]</param>
        /// <returns></returns>
        public List<T> Select<T>(string where)
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
        public List<T> Select<T>(int topN, string where)
        {
            int count = 0;
            return Select<T>(0, topN, where, out count);
        }
        public List<T> Select<T>(int pageIndex, int pageSize)
        {
            int count = 0;
            return Select<T>(pageIndex, pageSize, null, out count);
        }
        public List<T> Select<T>(int pageIndex, int pageSize, string where)
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
        public List<T> Select<T>(int pageIndex, int pageSize, string where, out int count)
        {
            return Action.Select(pageIndex, pageSize, where, out count).ToList<T>();
        }
        internal MDataTable Select(int pageIndex, int pageSize, string where, out int count)
        {
            return Action.Select(pageIndex, pageSize, where, out count);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int GetCount()
        {
            return Action.GetCount();
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int GetCount(object where)
        {
            return Action.GetCount(where);
        }
        /// <summary>
        /// 查询是否存在指定的条件的数据
        /// </summary>
        public bool Exists(object where)
        {
            return Action.Exists(where);
        }

        #endregion

        #endregion
        internal void SetValueToEntity()
        {
            SetValueToEntity(null);
        }
        internal void SetValueToEntity(string propName)
        {
            if (!string.IsNullOrEmpty(propName))
            {
                PropertyInfo pi = typeInfo.GetProperty(propName);
                if (pi != null)
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
                Action.Data.SetToEntity(entity);
            }
        }
        private void GetValueFromEntity()
        {
            if (!IsUseAop || AppConfig.IsAspNetCore)//ASPNETCore下，动态代理的Aop是无效的
            {
                Action.Data.LoadFrom(entity, BreakOp.Null);
            }
        }
        #region IDisposable 成员
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (Action != null)
            {
                Action.Dispose();
            }
        }

        #endregion
    }
}
