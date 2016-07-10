using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Aop
{
    /// <summary>
    /// 框架内部数据库操作方法枚举
    /// </summary>
    public enum AopEnum
    {
        /// <summary>
        /// 查询多条记录方法 [参数为顺序为：Aop.AopEnum.Fill, out result, _TableName, aop_where, aop_otherInfo]
        /// </summary>
        Select,
        /// <summary>
        /// 插入方法 [参数为顺序为：Aop.AopEnum.Insert, out result, _TableName, aop_Row, aop_otherInfo]
        /// </summary>
        Insert,
        /// <summary>
        /// 更新方法  [参数为顺序为：Aop.AopEnum.Update, out result, _TableName, aop_Row, aop_otherInfo]
        /// </summary>
        Update,
        /// <summary>
        /// 删除方法  [参数为顺序为：Aop.AopEnum.Fill, out result, _TableName,aop_Row,aop_where, aop_otherInfo]
        /// </summary>
        Delete,
        /// <summary>
        /// 查询一条记录方法 [参数为顺序为：Aop.AopEnum.Fill, out result, _TableName, aop_where, aop_otherInfo]
        /// </summary>
        Fill,
        /// <summary>
        /// 取记录总数 [参数为顺序为：Aop.AopEnum.GetCount, out result, _TableName, aop_where, aop_otherInfo]
        /// </summary>
        GetCount,
        /// <summary>
        /// MProc查询返回List<MDataTable>方法 [参数为顺序为：AopEnum.ExeMDataTableList, out result, procName, isProc, DbParameterCollection, aopInfo]
        /// </summary>
        ExeMDataTableList,
        /// <summary>
        /// MProc查询返回MDataTable方法 [参数为顺序为：AopEnum.ExeMDataTable, out result, procName, isProc, DbParameterCollection, aopInfo]
        /// </summary>
        ExeMDataTable,
        /// <summary>
        /// MProc执行返回受影响行数方法 [参数为顺序为：AopEnum.ExeNonQuery, out result, procName, isProc, DbParameterCollection, aopInfo]
        /// </summary>
        ExeNonQuery,
        /// <summary>
        /// MProc执行返回首行首列方法 [参数为顺序为：AopEnum.ExeScalar, out result, procName, isProc, DbParameterCollection, aopInfo]
        /// </summary>
        ExeScalar
    }
    /// <summary>
    /// Aop函数的处理结果
    /// </summary>
    public enum AopResult
    {
        /// <summary>
        /// 本结果将执行原有事件，但跳过Aop.End事件
        /// </summary>
        Default,
        /// <summary>
        /// 本结果将继续执行原有事件和Aop.End事件
        /// </summary>
        Continue,
        /// <summary>
        /// 本结果将跳过原有事件,但会执行Aop End事件
        /// </summary>
        Break,
        /// <summary>
        /// 本结果将直接跳出原有函数的执行
        /// </summary>
        Return,
    }
}
