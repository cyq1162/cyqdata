﻿using System;
using CYQ.Data.Table;
using CYQ.Data.Tool;
using System.Data;
using CYQ.Data.UI;
using CYQ.Data.Aop;
using CYQ.Data.Json;

namespace CYQ.Data.Orm
{

    /// <summary>
    /// ORM 基类【内置RealProxy，能拦截属性赋值变化，实现按需要插入或更新，NetCore下该属性无效】
    /// </summary>
    [AopAttribute]
    public abstract partial class OrmBase : ContextBoundObject, IDisposable
    {
        /// <summary>
        /// 获取 - 基类 ORM 的相关信息【Orm内部属性】
        /// </summary>
        [JsonIgnore]
        public OrmBaseInfo BaseInfo
        {
            get
            {
                return sob.BaseInfo;
            }
        }
        private SimpleOrmBaseDefaultInstance sob = new SimpleOrmBaseDefaultInstance();
        ///// <summary>
        /////  字段来源（当字段变更时，可以设置此属性来切换更新）
        ///// </summary>
        //public static FieldSource FieldSource
        //{
        //    get
        //    {
        //        return SimpleOrmBase.FieldSource;
        //    }
        //    set
        //    {
        //        SimpleOrmBase.FieldSource = value;
        //    }
        //}


        //Object entity;//实体对象
        //Type typeInfo;//实体对象类型
        internal MAction Action
        {
            get
            {
                return sob.Action;
            }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public OrmBase()
        {
            sob.IsUseAop = true;
        }
        /// <summary>
        /// 设置Aop状态
        /// </summary>
        /// <param name="op"></param>
        public void SetAopState(AopOp op)
        {
            sob.SetAopState(op);
        }
        /// <summary>
        /// 初始化状态[继承此基类的实体在构造函数中需调用此方法]
        /// </summary>
        /// <param name="entityInstance">实体对象,一般写:this</param>
        protected void SetInit(Object entityInstance)
        {
            sob.SetInit2(entityInstance, null, null, true);
        }
        /// <summary>
        /// 初始化状态[继承此基类的实体在构造函数中需调用此方法]
        /// </summary>
        /// <param name="entityInstance">实体对象,一般写:this</param>
        /// <param name="tableName">表名,如:Users</param>
        protected void SetInit(Object entityInstance, string tableName)
        {
            sob.SetInit2(entityInstance, tableName, null, true);
        }
        /// <summary>
        /// 初始化状态[继承此基类的实体在构造函数中需调用此方法]
        /// </summary>
        /// <param name="entityInstance">实体对象,一般写:this</param>
        /// <param name="tableName">表名,如:Users</param>
        /// <param name="conn">数据链接,单数据库时可写Null,或写默认链接配置项:"Conn",或直接数据库链接字符串</param>
        protected void SetInit(Object entityInstance, string tableName, string conn)
        {
            sob.SetInit2(entityInstance, tableName, conn, true);
        }

        /// <summary>
        /// 初始化状态[继承此基类的实体在构造函数中需调用此方法]
        /// </summary>
        /// <param name="entityInstance">实体对象,一般写:this</param>
        /// <param name="tableName">表名,如:Users</param>
        /// <param name="conn">数据链接,单数据库时可写Null,或写默认链接配置项:"Conn",或直接数据库链接字符串</param>
        /// <param name="isWriteLogOnError">当执行发生异常时，是否输出日志</param>
        protected void SetInit(Object entityInstance, string tableName, string conn, bool isWriteLogOnError)
        {
            sob.SetInit2(entityInstance, tableName, conn, isWriteLogOnError);
        }

        /// <summary>
        /// 设置值,例如:[action.Set(TableName.id,10);]
        /// </summary>
        /// <param name="key">字段名称,可用枚举如:[TableName.id]</param>
        /// <param name="value">要设置给字段的值</param>
        /// <example><code>
        /// set示例：action.Set(Users.UserName,"路过秋天");
        /// get示例：int id=action.Get&lt;int&gt;(Users.id);
        /// </code></example>
        protected void Set(object key, object value)
        {
            if (sob != null)
            {
                sob.Set(key, value);
            }
        }

        #region 基础增删改查 成员

        #region 插入
        /// <summary>
        /// 插入数据
        /// </summary>
        public bool Insert()
        {
            return sob.Insert(false, InsertOp.ID, false);
        }
        /// <summary>
        ///  插入数据
        /// </summary>
        /// <param name="option">插入选项</param>
        public bool Insert(InsertOp option)
        {
            return sob.Insert(false, option, false);
        }
        /// <summary>
        ///  插入数据
        /// </summary>
        /// <param name="insertID">插入主键</param>
        public bool Insert(InsertOp option, bool insertID)
        {
            return sob.Insert(false, option, insertID);
        }
        /// <summary>
        ///  插入数据
        /// </summary>
        /// <param name="autoSetValue">是否自动获取值[自动从控件获取值,需要先调用this.UI.SetAutoPrefix或this.UI.SetAutoParentControl方法设置控件前缀]</param>
        public bool Insert(bool autoSetValue)
        {
            return sob.Insert(autoSetValue, InsertOp.ID, false);
        }
        /// <summary>
        ///  插入数据
        /// </summary>
        /// <param name="autoSetValue">是否自动获取值[自动从控件获取值,需要先调用this.UI.SetAutoPrefix或this.UI.SetAutoParentControl方法设置控件前缀]</param>
        public bool Insert(bool autoSetValue, InsertOp option)
        {
            return sob.Insert(autoSetValue, option, false);
        }
        /// <summary>
        ///  插入数据
        /// </summary>
        /// <param name="autoSetValue">是否自动获取值[自动从控件获取值,需要先调用this.UI.SetAutoPrefix或this.UI.SetAutoParentControl方法设置控件前缀]</param>
        /// <param name="option">插入选项</param>
        /// <param name="insertID">自定义插入主键</param>
        public bool Insert(bool autoSetValue, InsertOp option, bool insertID)
        {
            return sob.Insert(autoSetValue, option, insertID);
        }
        #endregion

        #region 更新
        /// <summary>
        ///  更新数据
        /// </summary>
        public bool Update()
        {
            return sob.Update();
        }
        /// <summary>
        ///  更新数据
        /// </summary>
        /// <param name="where">where条件,可直接传id的值如:[88],或传完整where条件如:[id=88 and name='路过秋天']</param>
        public bool Update(object where)
        {
            return sob.Update(where);
        }
        /// <summary>
        ///  更新数据
        /// </summary>
        /// <param name="where">where条件,可直接传id的值如:[88],或传完整where条件如:[id=88 and name='路过秋天']</param>
        /// <param name="autoSetValue">是否自动获取值[自动从控件获取值,需要先调用SetAutoPrefix或SetAutoParentControl方法设置控件前缀]</param>
        public bool Update(object where, bool autoSetValue)
        {
            return sob.Update(where, autoSetValue);
        }
        #endregion

        #region 删除
        /// <summary>
        ///  删除数据
        /// </summary>
        public bool Delete()
        {
            return sob.Delete();
        }
        /// <summary>
        ///  删除数据
        /// </summary>
        /// <param name="where">where条件,可直接传id的值如:[88],或传完整where条件如:[id=88 and name='路过秋天']</param>
        public bool Delete(object where)
        {
            return sob.Delete(where);
        }
        #endregion

        #region 查询

        /// <summary>
        /// 查询1条数据（自动取值）
        /// </summary>
        /// <returns></returns>
        public bool Fill()
        {
            return sob.Fill();
        }
        /// <summary>
        /// 查询1条数据
        /// </summary>
        public bool Fill(object where)
        {
            return sob.Fill(where);
        }
        /// <summary>
        /// 查询1条数据并返回新的一行
        /// </summary>
        public MDataRow Get(object where)
        {
            if (sob.Fill(where))
            {
                return sob.Action.Data.Clone();
            }
            return null;
        }
        /// <summary>
        /// 列表查询
        /// </summary>
        public MDataTable Select()
        {
            int count;
            return Select(0, 0, null, out count);
        }
        /// <summary>
        /// 列表查询
        /// </summary>
        /// <param name="where">查询条件[可附带 order by 语句]</param>
        /// <returns></returns>
        public MDataTable Select(string where)
        {
            int count;
            return Select(0, 0, where, out count);
        }
        /// <summary>
        /// 列表查询
        /// </summary>
        /// <param name="topN">查询几条</param>
        /// <param name="where">查询条件[可附带 order by 语句]</param>
        /// <returns></returns>
        public MDataTable Select(int topN, string where)
        {
            int count;
            return Select(0, topN, where, out count);
        }
        public MDataTable Select(int pageIndex, int pageSize)
        {
            int count;
            return Select(pageIndex, pageSize, null, out count);
        }
        public MDataTable Select(int pageIndex, int pageSize, string where)
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
        /// <param name="count">返回的记录总数</param>
        public MDataTable Select(int pageIndex, int pageSize, string where, out int count)
        {
            return sob.Select(pageIndex, pageSize, where, out count);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int GetCount()
        {
            return sob.GetCount();
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
        public int GetCount(object where)
        {
            return sob.GetCount(where);
        }
        /// <summary>
        /// 查询是否存在指定的条件的数据
        /// </summary>
        public bool Exists(object where)
        {
            return sob.Exists(where);
        }

        #endregion

        #endregion

        #region IDisposable 成员
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (sob != null)
            {
                sob.Dispose();
            }
        }

        #endregion

        /// <summary>
        /// 批量加载值（可从Json、实体对象、MDataRow、泛型字段等批量加载值）
        /// </summary>
        /// <param name="jsonOrEntity">json字符串或实体对象</param>
        public void LoadFrom(object jsonOrEntity)
        {
            sob.LoadFrom(jsonOrEntity);
        }
        /// <summary>
        /// 本方法可以在单表使用时查询指定的列[设置后可使用Fill与Select方法]
        /// 提示：分页查询时，排序条件的列必须指定选择。
        /// </summary>
        /// <param name="columnNames">可设置多个列名[调用Fill或Select后,本参数将被清除]</param>
        public void SetSelectColumns(params object[] columnNames)
        {
            sob.SetSelectColumns(columnNames);
        }
        /// <summary>
        /// 参数化传参[当Where条件为参数化(如：name=@name)语句时使用]
        /// </summary>
        /// <param name="paraName">参数名称</param>
        /// <param name="value">参数值</param>
        /// <param name="dbType">参数类型</param>
        public void SetPara(object paraName, object value, DbType dbType)
        {
            sob.SetPara(paraName, value, dbType);
        }
        /// <summary>
        /// 设置参数化
        /// </summary>
        public void SetPara(object paraName, object value)
        {
            sob.SetPara(paraName, value);
        }
        /// <summary>
        /// 清除(SetPara设置的)自定义参数
        /// </summary>
        public void ClearPara()
        {
            sob.ClearPara();
        }
        /// <summary>
        /// 清空所有值
        /// </summary>
        public void Clear()
        {
            sob.Clear();
        }
        /// <summary>
        /// 更新操作的自定义表达式设置。
        /// </summary>
        /// <param name="updateExpression">例如a字段值自加1："a=a+1"</param>
        public void SetExpression(string updateExpression)
        {
            sob.SetExpression(updateExpression);
        }
        /// <summary>
        /// UI 操作【WebForm、Winform、WPF等自动取值或赋值】【Orm内部属性】
        /// </summary>
        [JsonIgnore]
        public MActionUI UI
        {
            get
            {
                return sob.UI;
            }
        }
    }
}
