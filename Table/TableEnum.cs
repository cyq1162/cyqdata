using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Table
{
    /// <summary>
    /// 批量更新选项
    /// </summary>
    public enum AcceptOp
    {
        /// <summary>
        /// 批量插入（由系统产生自增加ID）
        /// 该执行会开启事务。
        /// </summary>
        Insert,
        /// <summary>
        /// 批量插入（由用户指定ID插入）
        /// 该执行会开启事务。
        /// </summary>
        InsertWithID,
        /// <summary>
        /// 批量更新
        /// 该执行会开启事务。
        /// </summary>
        Update,
        /// <summary>
        /// 批量自动插入或更新（检测主键数据若存在，则更新；不存在，则插入）
        /// 该执行不会开启事务。
        /// </summary>
        Auto
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

    /// <summary>
    /// 转实体所用参数
    /// </summary>
    internal enum SysType
    {
        /// <summary>
        /// 基础类型
        /// </summary>
        Base = 1,
        /// <summary>
        /// 枚举类型
        /// </summary>
        Enum = 2,
        /// <summary>
        /// 数组
        /// </summary>
        Array = 3,
        /// <summary>
        /// 集合类型
        /// </summary>
        Collection = 4,
        /// <summary>
        /// 泛型
        /// </summary>
        Generic = 5,
        /// <summary>
        /// 自定义类
        /// </summary>
        Custom = 99
    }
}
