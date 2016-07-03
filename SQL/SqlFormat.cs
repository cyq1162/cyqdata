
using CYQ.Data.Extension;
using CYQ.Data.Table;
using System;
using System.Text.RegularExpressions;

namespace CYQ.Data.SQL
{
    /// <summary>
    /// Sql 语句格式化类 (类似助手工具)
    /// </summary>
    internal class SqlFormat
    {
        /// <summary>
        /// Sql关键字处理
        /// </summary>
        public static string Keyword(string name, DalType dalType)
        {
            if (!string.IsNullOrEmpty(name))
            {
                name = name.Trim();
                if (name.IndexOfAny(new char[] { ' ', '[', ']', '`', '"', '(', ')' }) == -1)
                {
                    string pre = null;
                    int i = name.LastIndexOf('.');// 增加跨库支持（demo.dbo.users）
                    if (i > 0)
                    {
                        string[] items = name.Split('.');
                        pre = items[0];
                        name = items[items.Length - 1];
                    }
                    switch (dalType)
                    {
                        case DalType.Access:
                            return "[" + name + "]";
                        case DalType.MsSql:
                        case DalType.Sybase:
                            return (pre == null ? "" : pre + "..") + "[" + name + "]";
                        case DalType.MySql:
                            return (pre == null ? "" : pre + ".") + "`" + name + "`";
                        case DalType.SQLite:
                            return "\"" + name + "\"";
                        case DalType.Txt:
                        case DalType.Xml:
                            return NotKeyword(name);
                    }
                }
            }
            return name;
        }
        /// <summary>
        /// 去除关键字符号
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string NotKeyword(string name)
        {
            name = name.Trim();
            if (name.IndexOfAny(new char[] { '(', ')' }) == -1 && name.Split(' ').Length == 1)
            {
                //string pre = string.Empty;
                int i = name.LastIndexOf('.');// 增加跨库支持（demo.dbo.users）
                if (i > 0)
                {
                    // pre = name.Substring(0, i + 1);
                    name = name.Substring(i + 1);
                }
                name = name.Trim('[', ']', '`', '"');

            }
            return name;
        }
        /// <summary>
        /// Sql数据库兼容和Sql注入处理
        /// </summary>
        public static string Compatible(object where, DalType dalType, bool isFilterInjection)
        {
            string text = GetIFieldSql(where);
            if (isFilterInjection)
            {
                text = SqlInjection.Filter(text, dalType);
            }
            text = SqlCompatible.Format(text, dalType);

            return RemoveWhereOneEqualsOne(text);
        }

        /// <summary>
        /// 移除"where 1=1"
        /// </summary>
        internal static string RemoveWhereOneEqualsOne(string sql)
        {
            try
            {
                sql = sql.Trim();
                if (sql == "where 1=1")
                {
                    return string.Empty;
                }
                if (sql.EndsWith(" and 1=1"))
                {
                    return sql.Substring(0, sql.Length - 8);
                }
                int i = sql.IndexOf("where 1=1", StringComparison.OrdinalIgnoreCase);
                //do
                //{
                if (i > 0)
                {
                    if (i == sql.Length - 9)//以where 1=1 结束。
                    {
                        sql = sql.Substring(0, sql.Length - 10);
                    }
                    else if (sql.Substring(i + 10, 8).ToLower() == "order by")
                    {
                        sql = sql.Remove(i, 10);//可能有多个。
                    }
                    // i = sql.IndexOf("where 1=1", StringComparison.OrdinalIgnoreCase);
                }
                //}
                //while (i > 0);
            }
            catch
            {

            }

            return sql;
        }

        /// <summary>
        /// 创建补充1=2的SQL语句
        /// </summary>
        /// <param name="tableName">表名、或视图语句</param>
        /// <returns></returns>
        internal static string BuildSqlWithWhereOneEqualsTow(string tableName)
        {
            tableName = tableName.Trim();
            if (tableName[0] == '(' && tableName.IndexOf(')') > -1)
            {
                int end = tableName.LastIndexOf(')');
                string sql = tableName.Substring(1, end - 1);//.Replace("\r\n", "\n").Replace('\n', ' '); 保留注释的换行。
                if (sql.IndexOf(" where ", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    return Regex.Replace(sql, " where ", " where 1=2 and ", RegexOptions.IgnoreCase);
                }
                return sql + " where 1=2";
            }
            return string.Format("select * from {0} where 1=2", tableName);
        }

        /// <summary>
        /// Mysql Bit 类型不允许条件带引号 （字段='0' 不可以）
        /// </summary>
        /// <param name="where"></param>
        /// <param name="mdc"></param>
        /// <returns></returns>
        internal static string FormatMySqlBit(string where, MDataColumn mdc)
        {
            if (where.Contains("'0'"))
            {
                foreach (MCellStruct item in mdc)
                {
                    int groupID = DataType.GetGroup(item.SqlType);
                    if (groupID == 1 || groupID == 3)//视图模式里取到的bit是bigint,所以数字一并处理
                    {
                        if (where.IndexOf(item.ColumnName, StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            string pattern = " " + item.ColumnName + @"\s*=\s*'0'";
                            where = Regex.Replace(where, pattern, " " + item.ColumnName + "=0", RegexOptions.IgnoreCase);
                        }
                    }
                }
            }
            return where;
        }

        #region IField处理

        /// <summary>
        /// 静态的对IField接口处理
        /// </summary>
        public static string GetIFieldSql(object whereObj)
        {
            if (whereObj is IField)
            {
                IField filed = whereObj as IField;
                string where = filed.Sql;
                filed.Sql = "";
                return where;
            }
            return Convert.ToString(whereObj);
        }
        #endregion
    }
}
