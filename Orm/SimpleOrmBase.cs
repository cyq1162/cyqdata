using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;
using System.Reflection;

using CYQ.Data.Tool;
using CYQ.Data.Cache;
using CYQ.Data.SQL;
using System.Data;


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
        /// 标识是否允许写日志。
        /// </summary>
        internal bool AllowWriteLog
        {
            set
            {
                action.dalHelper.isAllowInterWriteLog = value;
            }
        }
        /// <summary>
        /// 是否启用了AOP拦截设置字段值同步。
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
        MAction action;
        internal MAction Action
        {
            get
            {
                return action;
            }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public SimpleOrmBase()
        {

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
            conn = string.IsNullOrEmpty(conn) ? AppConfig.DB.DefaultConn : conn;
            entity = entityInstance;
            typeInfo = entity.GetType();
            try
            {
                if (string.IsNullOrEmpty(tableName))
                {
                    tableName = typeInfo.Name;
                }

                string key = tableName + MD5.Get(conn);
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
                        if (!DBTool.ExistsTable(tableName, conn))
                        {
                            DBTool.CreateTable(tableName, Columns, conn);
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
                        CacheManage.LocalInstance.Add(key, Columns, null, 1440);
                    }
                }
                else
                {
                    Columns = CacheManage.LocalInstance.Get(key) as MDataColumn;
                }

                action = new MAction(Columns.ToRow(tableName), conn);
                if (typeInfo.Name == "SysLogs")
                {
                    action.SetAopOff();
                }
                action.EndTransation();
            }
            catch (Exception err)
            {
                if (typeInfo.Name != "SysLogs")
                {
                    Log.WriteLogToTxt(err);
                }
                throw;
            }
        }
        internal void SetInit2(Object entityInstance, string tableName, string conn)
        {
            SetInit(entityInstance, tableName, conn);
        }
        internal void Set(object key, object value)
        {
            if (action != null)
            {
                action.Set(key, value);
            }
        }
        #region 基础增删改查 成员

        #region 插入
        /// <summary>
        /// 插入数据
        /// </summary>
        public bool Insert()
        {
            return Insert(false, InsertOp.ID);
        }
        /// <summary>
        ///  插入数据
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
        internal bool Insert(bool autoSetValue)
        {
            return Insert(autoSetValue, InsertOp.ID);
        }
        /// <summary>
        ///  插入数据
        /// </summary>
        /// <param name="autoSetValue">自动从控制获取值</param>
        /// <param name="option">插入选项</param>
        internal bool Insert(bool autoSetValue, InsertOp option)
        {
            if (autoSetValue)
            {
                action.UI.GetAll(true);
            }
            GetValueFromEntity();
            bool result = action.Insert(false, option);
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
                action.UI.GetAll(false);
            }
            GetValueFromEntity();
            bool result = action.Update(where);
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
            return action.Delete(where);
        }
        #endregion

        #region 查询


        /// <summary>
        /// 查询1条数据
        /// </summary>
        public bool Fill(object where)
        {
            bool result = action.Fill(where);
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
            return action.Select(pageIndex, pageSize, where, out count).ToList<T>();
        }
        internal MDataTable Select(int pageIndex, int pageSize, string where, out int count)
        {
            return action.Select(pageIndex, pageSize, where, out count);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int GetCount(object where)
        {
            return action.GetCount(where);
        }
        /// <summary>
        /// 查询是否存在指定的条件的数据
        /// </summary>
        public bool Exists(object where)
        {
            return action.Exists(where);
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
                    MDataCell cell = action.Data[propName];
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
                action.Data.SetToEntity(entity);
            }
        }
        private void GetValueFromEntity()
        {
            if (!IsUseAop)
            {
                action.Data.LoadFrom(entity, BreakOp.Null);
            }
        }
        #region IDisposable 成员
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (action != null)
            {
                action.Dispose();
            }
        }

        #endregion
    }
}
