using CYQ.Data.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Json
{
    /// <summary>
    /// Json Config
    /// </summary>
    public class JsonOp
    {
        /// <summary>
        /// Data Filter Option : RowOp.IgnoreNull （default）
        /// </summary>
        public RowOp RowOp = RowOp.IgnoreNull;
        /// <summary>
        /// Naming Policy : NameCaseOp.None （default）
        /// </summary>
        public NameCaseOp NameCaseOp = NameCaseOp.None;
        /// <summary>
        /// Json string value Escape : EscapeOp.No （default）
        /// </summary>
        public EscapeOp EscapeOp = EscapeOp.No;

        /// <summary>
        /// convert enum to string
        /// <para>是否将枚举转字符串</para>
        /// </summary>
        public bool IsConvertEnumToString = false;
        /// <summary>
        /// convert enum to DescriptionAttribute
        /// <para>是否将枚举转属性描述</para>
        /// </summary>
        public bool IsConvertEnumToDescription = false;
        /// <summary>
        /// formate datetime
        /// <para>日期的格式化（默认：yyyy-MM-dd HH:mm:ss）</para>
        /// </summary>
        public string DateTimeFormatter = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// 用于监测是否循环引用的层级计数器
        /// </summary>
        internal int Level = 0;
        /// <summary>
        /// 用于自循环检测列表。
        /// </summary>
        internal Dictionary<int, int> LoopCheckList = new Dictionary<int, int>();

        /// <summary>
        /// 进行浅克隆。
        /// </summary>
        /// <returns></returns>
        internal JsonOp Clone()
        {
            return this.MemberwiseClone() as JsonOp;
        }
    }
}
