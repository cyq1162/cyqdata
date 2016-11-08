using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Tool
{
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
