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
        Select,
        Insert,
        Update,
        Delete,
        Fill,
        GetCount,
        Exists,
        ExeMDataTableList,
        ExeMDataTable,
        ExeNonQuery,
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

    /// <summary>
    /// Aop开关选项
    /// </summary>
    public enum AopOp
    {
        /// <summary>
        /// 正常打开
        /// </summary>
        OpenAll,
        /// <summary>
        /// 仅打开内部Aop（即自动缓存，关闭外部Aop）
        /// </summary>
        OnlyInner,
        /// <summary>
        /// 仅打开外部Aop（关闭自动缓存）
        /// </summary>
        OnlyOuter,
        /// <summary>
        /// 内外都关（自动缓存和外部Aop）
        /// </summary>
        CloseAll
    }
}
