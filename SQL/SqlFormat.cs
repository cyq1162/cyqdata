
using CYQ.Data.Extension;
using CYQ.Data.Table;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Data;

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
        public static string Keyword(string name, DataBaseType dalType)
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
                        case DataBaseType.Access:
                            return "[" + name + "]";
                        case DataBaseType.MsSql:
                        case DataBaseType.Sybase:
                            return (pre == null ? "" : pre + "..") + "[" + name + "]";
                        case DataBaseType.MySql:
                            return (pre == null ? "" : pre + ".") + "`" + name + "`";
                        case DataBaseType.SQLite:
                            return "\"" + name + "\"";
                        case DataBaseType.PostgreSQL:
                            if (AppConfig.DB.IsPostgreLower) { return name; }
                            return "\"" + name + "\"";
                        case DataBaseType.Txt:
                        case DataBaseType.Xml:
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
        public static string Compatible(object where, DataBaseType dalType, bool isFilterInjection)
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
                tableName = tableName.Substring(1, end - 1);//.Replace("\r\n", "\n").Replace('\n', ' '); 保留注释的换行。
            }
            if (tableName.Contains(" "))
            {
                //分成两半
                string partA = "", partB = "";//分成两半是为了兼容子查询语句
                int fromIndex = tableName.Replace("\n", " ").Replace("\r", " ").LastIndexOf(" from ", StringComparison.OrdinalIgnoreCase);
                if (fromIndex > -1)
                {
                    partA = tableName.Substring(0, fromIndex);
                    partB = tableName.Substring(fromIndex);
                    string[] keys = new string[] { " where ", "\nwhere ", "\nwhere\r", "\nwhere\n" };
                    if (partB.IndexOf("where", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        foreach (string key in keys)
                        {
                            if (partB.IndexOf(key, StringComparison.OrdinalIgnoreCase) > -1)
                            {
                                return partA + Regex.Replace(partB, key, " where 1=2 and ", RegexOptions.IgnoreCase);
                            }
                        }
                    }
                    else if (partA.IndexOf("where", StringComparison.OrdinalIgnoreCase) > -1)//处理select * from a where id in(select distinct id from a) 的情况
                    {
                        foreach (string key in keys)
                        {
                            if (partA.IndexOf(key, StringComparison.OrdinalIgnoreCase) > -1)
                            {
                                return Regex.Replace(partA, key, " where 1=2 and ", RegexOptions.IgnoreCase) + partB;
                            }
                        }
                    }
                    //检测是否有group by
                    keys = new string[] { " group by", "\ngroup by" };
                    foreach (string key in keys)
                    {
                        if (partB.IndexOf(key, StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            string newKey = key.Replace("group", "where 1=2 group");
                            return partA + Regex.Replace(partB, key, newKey, RegexOptions.IgnoreCase);
                        }
                    }
                }
                return tableName + " where 1=2";
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
                            string pattern = @"\s?" + item.ColumnName + @"\s*=\s*'0'";
                            where = Regex.Replace(where, pattern, " " + item.ColumnName + "=0", RegexOptions.IgnoreCase);
                        }
                    }
                }
            }
            return where;
        }
        internal static string FormatOracleDateTime(string where, MDataColumn mdc)
        {
            if (where.IndexOf(':') > -1 && where.IndexOfAny(new char[] { '>', '<', '-', '/' }) > -1)//判断是否存在日期的判断
            {

                foreach (MCellStruct item in mdc)
                {
                    if (DataType.GetGroup(item.SqlType) == 2 && where.IndexOf(item.ColumnName, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        string pattern = @"(\s?" + item.ColumnName + @"\s*[><]{1}[=]?\s*)('.{19,23}')";
                        Regex reg = new Regex(pattern, RegexOptions.IgnoreCase);
                        if (reg.IsMatch(where))
                        {
                            where = reg.Replace(where, delegate(Match match)
                            {
                                if (item.SqlType == SqlDbType.Timestamp)
                                {
                                    return match.Groups[1].Value + "to_timestamp(" + match.Groups[2].Value + ",'yyyy-MM-dd HH24:MI:ss.ff')";
                                }
                                else
                                {
                                    return match.Groups[1].Value + "to_date(" + match.Groups[2].Value + ",'yyyy-mm-dd hh24:mi:ss')";
                                }
                            });
                        }

                    }
                }
            }
            return where;
        }
        internal static List<string> GetTableNamesFromSql(string sql)
        {
            List<string> nameList = new List<string>();

            //获取原始表名
            string[] items = sql.Split(new char[] { ' ', ';', '(', ')', ',' });
            if (items.Length == 1) { return nameList; }//单表名
            if (items.Length > 3) // 总是包含空格的select * from xxx
            {
                bool isKeywork = false;
                foreach (string item in items)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        string lowerItem = item.ToLower();
                        switch (lowerItem)
                        {
                            case "from":
                            case "update":
                            case "into":
                            case "join":
                            case "table":
                                isKeywork = true;
                                break;
                            default:
                                if (isKeywork)
                                {
                                    if (item[0] == '(' || item.IndexOf('.') > -1) { isKeywork = false; }
                                    else
                                    {
                                        isKeywork = false;
                                        nameList.Add(NotKeyword(item));
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            return nameList;
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

        /// <summary>
        /// 将各数据库默认值格式化成标准值，将标准值还原成各数据库默认值
        /// </summary>
        /// <param name="flag">[0:转成标准值],[1:转成各数据库值],[2:转成各数据库值并补充字符串前后缀]</param>
        /// <param name="sqlDbType">该列的值</param>
        /// <returns></returns>
        public static string FormatDefaultValue(DataBaseType dalType, object value, int flag, SqlDbType sqlDbType)
        {
            string defaultValue = Convert.ToString(value).Trim().TrimEnd('\n');//oracle会自带\n结尾
            if (dalType != DataBaseType.Access)
            {
                defaultValue = defaultValue.Replace("GenGUID()", string.Empty);
            }
            if (defaultValue.Length == 0)
            {
                return null;
            }
            int groupID = DataType.GetGroup(sqlDbType);
            if (flag == 0)
            {
                #region 转标准值


                if (groupID == 2)//日期的标准值
                {
                    return SqlValue.GetDate;
                }
                else if (groupID == 4)
                {
                    return SqlValue.Guid;
                }
                switch (dalType)
                {
                    case DataBaseType.MySql://用转\' \"，所以不用替换。
                        defaultValue = defaultValue.Replace("\\\"", "\"").Replace("\\\'", "\'");
                        break;
                    case DataBaseType.Access:
                    case DataBaseType.SQLite:
                        defaultValue = defaultValue.Replace("\"\"", "≮");
                        break;
                    default:
                        defaultValue = defaultValue.Replace("''", "≯");
                        break;
                }
                switch (defaultValue.ToLower().Trim('(', ')'))
                {
                    case "newid":
                    case "guid":
                    case "sys_guid":
                    case "genguid":
                    case "uuid":
                        return SqlValue.Guid;
                }
                #endregion
            }
            else
            {
                if (defaultValue == SqlValue.Guid)
                {
                    switch (dalType)
                    {
                        case DataBaseType.MsSql:
                        case DataBaseType.Oracle:
                        case DataBaseType.Sybase:
                        case DataBaseType.PostgreSQL:
                            return SqlCompatible.FormatGUID(defaultValue, dalType);
                        default:
                            return "";
                    }

                }
            }
            switch (dalType)
            {
                case DataBaseType.Access:
                    if (flag == 0)
                    {
                        if (defaultValue[0] == '"' && defaultValue[defaultValue.Length - 1] == '"')
                        {
                            defaultValue = defaultValue.Substring(1, defaultValue.Length - 2);
                        }
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "Now()").Replace("\"", "\"\"");
                        if (groupID == 0)
                        {
                            defaultValue = "\"" + defaultValue + "\"";
                        }
                    }
                    break;
                case DataBaseType.MsSql:
                case DataBaseType.Sybase:
                    if (flag == 0)
                    {
                        if (defaultValue.StartsWith("(") && defaultValue.EndsWith(")"))//避免 (newid()) 被去掉()
                        {
                            defaultValue = defaultValue.Substring(1, defaultValue.Length - 2);
                        }
                        if (defaultValue.StartsWith("N'"))
                        {
                            defaultValue = defaultValue.TrimStart('N');
                        }
                        defaultValue = defaultValue.Trim('\'');//'(', ')',
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "getdate()").Replace("'", "''");
                        if (groupID == 0)
                        {
                            defaultValue = "(N'" + defaultValue + "')";
                        }
                    }
                    break;
                case DataBaseType.Oracle:
                    if (flag == 0)
                    {
                        defaultValue = defaultValue.Trim('\'');
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "sysdate").Replace("'", "''");
                        if (groupID == 0)
                        {
                            defaultValue = "'" + defaultValue + "'";
                        }
                    }
                    break;
                case DataBaseType.MySql:
                    if (flag == 0)
                    {
                        defaultValue = defaultValue.Replace("b'0", "0").Replace("b'1", "1").Trim(' ', '\'');
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "CURRENT_TIMESTAMP").Replace("'", "\\'").Replace("\"", "\\\"");
                        if (groupID == 0)
                        {
                            defaultValue = "\"" + defaultValue + "\"";
                        }
                    }
                    break;
                case DataBaseType.SQLite:
                    if (flag == 0)
                    {
                        defaultValue = defaultValue.Trim('"');
                        if (groupID > 0)//兼容一些不规范的写法。像数字型的加了引号 '0'
                        {
                            defaultValue = defaultValue.Trim('\'');
                        }
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "CURRENT_TIMESTAMP").Replace("\"", "\"\"");
                        if (groupID == 0)
                        {
                            defaultValue = "\"" + defaultValue + "\"";
                        }
                    }
                    break;
                case DataBaseType.PostgreSQL:
                    if (flag == 0)
                    {
                        defaultValue = defaultValue.Trim('"');
                        if (groupID == 0)
                        {
                            defaultValue = Regex.Split(defaultValue, "::", RegexOptions.IgnoreCase)[0];
                        }
                        if (groupID > 0)//兼容一些不规范的写法。像数字型的加了引号 '0'
                        {
                            defaultValue = defaultValue.Trim('\'');
                        }
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "now()").Replace("\"", "\"\"");
                        if (groupID == 0)
                        {
                            defaultValue = Regex.Split(defaultValue, "::", RegexOptions.IgnoreCase)[0];
                            defaultValue = "'" + defaultValue.Trim('\'') + "'";
                        }
                        else if (groupID == 3) // bool
                        {
                            defaultValue = defaultValue.Replace("1", "true").Replace("0", "false");
                        }
                    }
                    break;
                case DataBaseType.DB2:
                    if (flag == 0)
                    {
                        defaultValue = defaultValue.Trim(' ', '\'');
                        if (groupID == 0)
                        {
                            defaultValue = "'" + defaultValue.Trim('\'') + "'";
                        }
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "CURRENT TIMESTAMP");
                    }
                    break;
            }
            if (flag == 0)
            {
                return defaultValue.Replace("≮", "\"").Replace("≯", "'");
            }
            return defaultValue;
        }
    }
}
