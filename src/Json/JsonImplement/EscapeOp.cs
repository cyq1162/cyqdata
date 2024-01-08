using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Json
{
    /// <summary>
    /// Escape json char options
    /// <para>JsonHelper 的符号转义选项</para>
    /// </summary>
    public enum EscapeOp
    {
        /// <summary>
        /// 过滤ascii小于32的特殊值、并对\n "（双引号）进行转义，对\转义符 （仅\\"或\\n时不转义，其它情况转义）
        /// </summary>
        Default,
        /// <summary>
        ///  不进行任何转义，只用于保留原如数据（注意：存在双引号时，[或ascii小于32的值都会破坏json格式]，从而json数据无法被解析）
        /// </summary>
        No,
        /// <summary>
        ///  过滤ascii小于32的特殊值、并对 ：\r \n \t "（双引号） \(转义符号) 直接进行转义
        /// </summary>
        Yes,
        /// <summary>
        /// 系统内部使用： ascii小于32（包括\n \t \r）、"(双引号)，\(转义符号) 进行编码（规则为：@#{0}#@ {0}为asciii值，系统转的时候会自动解码）
        /// </summary>
        Encode
    }
}
