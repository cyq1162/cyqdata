using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Table
{
    /// <summary>
    /// 批量更新选项
    /// </summary>
    [Flags]
    public enum AcceptOp
    {
        /// <summary>
        /// 批量插入（由系统产生自增加id）
        /// 该执行会开启事务。
        /// </summary>
        Insert = 1,
        /// <summary>
        /// 批量插入（由用户指定id插入）
        /// 该执行会开启事务。
        /// </summary>
        InsertWithID = 2,
        /// <summary>
        /// 批量更新
        /// 该执行会开启事务。
        /// </summary>
        Update = 4,
        /// <summary>
        /// 批量删除
        /// </summary>
        Delete = 8,
        /// <summary>
        /// 批量自动插入或更新（检测主键数据若存在，则更新；不存在，则插入）
        /// 该执行不会开启事务。
        /// </summary>
        Auto = 16,
        /// <summary>
        /// 清空表（只有和Insert或InsertWithID组合使用时才有效）
        /// </summary>
        Truncate = 32
    }
    /// <summary>
    /// MDataTable 与 MDataRow SetState 的过滤选项
    /// </summary>
    public enum BreakOp
    {
        /// <summary>
        /// 未设置，设置所有
        /// </summary>
        None = -1,
        /// <summary>
        /// 跳过设置值为Null的。
        /// </summary>
        Null = 0,
        /// <summary>
        /// 跳过设置值为空的。
        /// </summary>
        Empty = 1,
        /// <summary>
        /// 跳过设置值为Null或空的。
        /// </summary>
        NullOrEmpty = 2
    }

    /// <summary>
    /// MDataRow 与 JsonHelper 行数据的过滤选项
    /// </summary>
    public enum RowOp
    {
        /// <summary>
        /// 未设置，输出所有，包括Null值的列
        /// </summary>
        None = -1,
        /// <summary>
        /// 输出所有，但不包括Null值的列
        /// </summary>
        IgnoreNull = 0,
        /// <summary>
        /// 输出具有插入状态的值
        /// </summary>
        Insert = 1,
        /// <summary>
        /// 输出具有更新状态的值
        /// </summary>
        Update = 2
    }


}
