
namespace CYQ.Data.SQL
{
    /// <summary>
    /// 用于多数据库兼容的替换关键字/函数
    /// </summary>
    public class SqlValue
    {
        /// <summary>
        /// 对于Bit类型[是/否] 类型的排序：[#DESC]
        /// </summary>
        public const string Desc = "[#DESC]";
        /// <summary>
        /// 对于Bit类型[是/否] 类型的排序：[#ASC]
        /// </summary>
        public const string Asc = "[#ASC]";
        /// <summary>
        /// 对于Bit类型[是/否] 的条件值：[#TRUE]
        /// </summary>
        public const string True = "[#TRUE]";
        /// <summary>
        /// 对于Bit类型[是/否] 的条件值：[#FALSE]
        /// </summary>
        public const string False = "[#FALSE]";

        /// <summary>
        /// 数据库函数 Len 取长度：[#LEN](字段)
        /// </summary>
        public const string Len = "[#LEN]";//length

        /// <summary>
        /// 数据库函数 GUID 获取：[#GETDATE]
        /// </summary>
        public const string GUID = "[#GUID]";

        /// <summary>
        /// 数据库函数 ISNULL 判断：[#ISNULL](Exr1,Exr2)
        /// </summary>
        public const string ISNULL = "[#ISNULL]";

        /// <summary>
        /// 数据库函数 GetDate 获取当前时间：[#GETDATE]
        /// </summary>
        public const string GetDate = "[#GETDATE]";
        /// <summary>
        /// 数据库函数 Year 获取时间的年：[#YEAR](字段)
        /// </summary>
        public const string Year = "[#YEAR]";
        /// <summary>
        ///  数据库函数 Month 获取时间的月：[#MONTH](字段)
        /// </summary>
        public const string Month = "[#MONTH]";
        /// <summary>
        /// 数据库函数 Day 获取时间的日：[#DAY](字段)
        /// </summary>
        public const string Day = "[#DAY]";

        /// <summary>
        /// 数据库函数 Substring 截取字符串：[#SUBSTRING](字段,起始索引int,长度int)
        /// <example>
        /// <code>
        /// 示例： [#Substring](Title,0,2)
        /// </code>
        /// </example>
        /// </summary>
        public const string Substring = "[#SUBSTRING]";


        /// <summary>
        /// 数据库函数 CharIndex 查询字符所在的位置：[#CHARINDEX]('要查询的字符',字段)
        /// <example>
        /// <code>
        /// 示例： [#CHARINDEX]('findtitle',Title)>0
        /// </code>
        /// </example>
        /// </summary>
        public const string CharIndex = "[#CHARINDEX]";
        /// <summary>
        /// 数据库函数 DateDiff 比较日期的差异天数：[#DATEDIFF](参数,开始时间,结束时间)
        /// </summary>
        public const string DateDiff = "[#DATEDIFF]";
        /// <summary>
        /// 数据库函数 Case 分支语句，其它Case 一起的关键字也需要包含。
        /// <example>
        /// <code>
        /// 示例： [#CASE] [#WHEN] languageID={1} [#THEN] {2} [#ELSE] 0 [#END]
        /// </code>
        /// </example>
        /// </summary>
        public const string Case = "[#CASE]";//单条件分支
        /// <summary>
        /// 数据库函数 Case When 分支语句，其它Case 一起的关键字也需要包含。
        /// <example>
        /// <code>
        /// 示例： [#CASE#WHEN] languageID={0} [#THEN] 1000000 [#ELSE] 0 [#END]
        /// </code>
        /// </example>
        /// </summary>
        public const string CaseWhen = "[#CASE#WHEN]";//多条件分支
    }
}
