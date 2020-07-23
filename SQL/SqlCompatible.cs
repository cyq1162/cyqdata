using System;
using System.Text.RegularExpressions;
using System.Text;




namespace CYQ.Data.SQL
{
    /// <summary>
    /// Sql语句多数据库兼容
    /// </summary>
    internal class SqlCompatible
    {
        /// <summary>
        /// 同语句多数据库兼容处理
        /// </summary>
        internal static string Format(string text, DataBaseType dalType)
        {
            if (!string.IsNullOrEmpty(text))
            {
                if (text.IndexOf("=") > -1)
                {
                    text = FormatPara(text, dalType);
                }
                if (text.Contains("[#") && text.Contains("]"))
                {
                    text = FormatTrueFalseAscDesc(text, dalType);
                    text = FormatDateDiff(text, dalType);//必须在日期替换之前出现
                    text = FormatGetDate(text, dalType);
                    text = FormatCaseWhen(text, dalType);
                    text = FormatCharIndex(text, dalType);
                    text = FormatLen(text, dalType);
                    text = FormatGUID(text, dalType);
                    text = FormatIsNull(text, dalType);
                    text = FormatContact(text, dalType);
                    text = FormatLeft(text, dalType);
                    text = FormatRight(text, dalType);
                    text = FormatDate(text, dalType, SqlValue.Year, "Year");
                    text = FormatDate(text, dalType, SqlValue.Month, "Month");
                    text = FormatDate(text, dalType, SqlValue.Day, "Day");
                }
            }
            return text;
        }
        #region 过滤与多数据库标签解析
        internal static string FormatLeft(string text, DataBaseType dalType)
        {
            switch (dalType)
            {
                //substr(MAX(SheetID),1,4)) IS NULL THEN 0 ELSE substr(MAX(SheetID)length(MAX(SheetID))-4,4) 
                case DataBaseType.Oracle:
                    int index = text.IndexOf(SqlValue.Left, StringComparison.OrdinalIgnoreCase);//left(a,4) =>to_char(substr(a,1,4))
                    if (index > -1)
                    {
                        do
                        {
                            index = text.IndexOf('(', index);
                            int end = text.IndexOf(',', index);
                            int end2 = text.IndexOf(')', end + 1);
                            text = text.Insert(end2, ")");
                            text = text.Insert(end + 1, "1,");
                            index = text.IndexOf(SqlValue.Left, end, StringComparison.OrdinalIgnoreCase);//寻找还有没有第二次出现的函数字段
                        }
                        while (index > -1);
                        return Replace(text, SqlValue.Left, "to_char(substr");
                    }
                    return text;
                default:
                    return Replace(text, SqlValue.Left, "Left");
            }
        }
        internal static string FormatRight(string text, DataBaseType dalType)
        {
            switch (dalType)
            {
                case DataBaseType.Oracle:
                    int index = text.IndexOf(SqlValue.Right, StringComparison.OrdinalIgnoreCase);//right(a,4) => to_char(substr(a,length(a)-4,4))
                    if (index > -1)
                    {
                        do
                        {
                            ////substr(MAX(SheetID),1,4)) IS NULL THEN 0 ELSE substr(MAX(SheetID)length(MAX(SheetID))-4,4) 
                            index = text.IndexOf('(', index);
                            int end = text.IndexOf(',', index);
                            string key = text.Substring(index + 1, end - index - 1);//找到 a
                            int end2 = text.IndexOf(')', end + 1);
                            string key2 = text.Substring(end + 1, end2 - end - 1);//找到b
                            text = text.Insert(end2, ")");
                            text = text.Insert(end + 1, "length(" + key + ")+1-" + key2 + ",");//
                            index = text.IndexOf(SqlValue.Right, end, StringComparison.OrdinalIgnoreCase);//寻找还有没有第二次出现的函数字段
                        }
                        while (index > -1);
                        return Replace(text, SqlValue.Right, "to_char(substr");
                    }
                    return text;
                default:
                    return Replace(text, SqlValue.Right, "Right");
            }
        }
        internal static string FormatContact(string text, DataBaseType dalType)
        {
            switch (dalType)
            {
                case DataBaseType.Oracle:
                case DataBaseType.PostgreSQL:
                case DataBaseType.DB2:
                    return Replace(text, SqlValue.Contact, "||");
                default:
                    return Replace(text, SqlValue.Contact, "+");
            }
        }
        internal static string FormatIsNull(string text, DataBaseType dalType)
        {
            switch (dalType)
            {
                case DataBaseType.Access:
                    int index = text.IndexOf(SqlValue.IsNull, StringComparison.OrdinalIgnoreCase);//isnull  (isnull(aaa),'3,3')   iif(isnull   (aaa),333,aaa)
                    if (index > -1)
                    {

                        do
                        {
                            index = text.IndexOf('(', index);
                            int end = text.IndexOf(',', index);
                            string key = text.Substring(index + 1, end - index - 1);//找到 aaa
                            text = text.Insert(end, ")");//
                            end = text.IndexOf(')', end + 3);
                            text = text.Insert(end, "," + key);
                            index = text.IndexOf(SqlValue.IsNull, end, StringComparison.OrdinalIgnoreCase);//寻找还有没有第二次出现的函数字段
                        }
                        while (index > -1);
                        return Replace(text, SqlValue.IsNull, "iif(isnull");
                    }
                    break;
                case DataBaseType.SQLite:
                case DataBaseType.MySql:
                    return Replace(text, SqlValue.IsNull, "IfNull");
                case DataBaseType.Oracle:
                case DataBaseType.DB2:
                    return Replace(text, SqlValue.IsNull, "NVL");
                case DataBaseType.PostgreSQL:
                    return Replace(text, SqlValue.IsNull, "COALESCE");
                case DataBaseType.MsSql:
                case DataBaseType.Sybase:
                default:
                    return Replace(text, SqlValue.IsNull, "IsNull");

            }
            return text;
        }
        internal static string FormatGUID(string text, DataBaseType dalType)
        {
            switch (dalType)
            {
                case DataBaseType.Access:
                    return Replace(text, SqlValue.Guid, "GenGUID()");
                case DataBaseType.MySql:
                    return Replace(text, SqlValue.Guid, "UUID()");
                case DataBaseType.MsSql:
                case DataBaseType.Sybase:
                    return Replace(text, SqlValue.Guid, "newid()");
                case DataBaseType.Oracle:
                    return Replace(text, SqlValue.Guid, "SYS_GUID()");
                case DataBaseType.SQLite:
                case DataBaseType.DB2:
                    return Replace(text, SqlValue.Guid, Guid.NewGuid().ToString());
                case DataBaseType.PostgreSQL:
                    return Replace(text, SqlValue.Guid, "uuid_generate_v4()");
            }
            return text;
        }

        private static string FormatPara(string text, DataBaseType dalType)
        {
            switch (dalType)
            {
                case DataBaseType.MySql:
                    return text.Replace("=:?", "=?");
                case DataBaseType.Oracle:
                case DataBaseType.PostgreSQL:
                    return text.Replace("=:?", "=:");
                default:
                    return text.Replace("=:?", "=@");
            }
        }

        private static string FormatTrueFalseAscDesc(string text, DataBaseType dalType)
        {
            switch (dalType)
            {
                case DataBaseType.Access:
                    text = Replace(text, SqlValue.True, "true");
                    text = Replace(text, SqlValue.False, "false");
                    text = Replace(text, SqlValue.Desc, "asc");
                    return Replace(text, SqlValue.Asc, "desc");
                default:
                    text = Replace(text, SqlValue.True, "1");
                    text = Replace(text, SqlValue.False, "0");
                    text = Replace(text, SqlValue.Desc, "desc");
                    return Replace(text, SqlValue.Asc, "asc");
            }
        }

        private static string FormatLen(string text, DataBaseType dalType)
        {
            switch (dalType)//处理函数替换
            {
                case DataBaseType.Access:
                case DataBaseType.MsSql:
                    text = Replace(text, SqlValue.Len, "len");
                    return Replace(text, SqlValue.Substring, "substring");
                case DataBaseType.Oracle:
                case DataBaseType.SQLite:
                case DataBaseType.DB2:
                    text = Replace(text, SqlValue.Len, "length");
                    return Replace(text, SqlValue.Substring, "substr");
                case DataBaseType.MySql:
                    text = Replace(text, SqlValue.Len, "char_length");
                    return Replace(text, SqlValue.Substring, "substring");
                case DataBaseType.Sybase:
                    text = Replace(text, SqlValue.Len, "datalength");
                    return Replace(text, SqlValue.Substring, "substring");
                case DataBaseType.PostgreSQL:
                    text = Replace(text, SqlValue.Len, "length");
                    return Replace(text, SqlValue.Substring, "substring");
            }
            return text;
        }
        private static string GetFormatDateKey(DataBaseType dalType, string key)
        {
            switch (dalType)
            {
                case DataBaseType.SQLite:
                    switch (key)
                    {
                        case SqlValue.Year:
                            return "'%Y',";
                        case SqlValue.Month:
                            return "'%m',";
                        case SqlValue.Day:
                            return "'%d',";
                    }
                    break;
                case DataBaseType.Sybase:
                    switch (key)
                    {
                        case SqlValue.Year:
                            return "yy,";
                        case SqlValue.Month:
                            return "mm,";
                        case SqlValue.Day:
                            return "dd,";
                    }
                    break;
                default:
                    switch (key)
                    {
                        case SqlValue.Year:
                            return ",'yyyy'";
                        case SqlValue.Month:
                            return ",'MM'";
                        case SqlValue.Day:
                            return ",'dd'";
                    }
                    break;
            }
            return string.Empty;
        }
        private static string FormatDate(string text, DataBaseType dalType, string key, string func)
        {
            int index = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);//[#year](字段)
            if (index > -1)//存在[#year]函数
            {
                string format = GetFormatDateKey(dalType, key);
                int found = 0;
                switch (dalType)
                {
                    case DataBaseType.Oracle:
                        do
                        {
                            text = text.Insert(index + 2, "_");//[#_year](字段)
                            found = text.IndexOf(')', index + 4);//从[#_year(字段)]找到 ')'的位置
                            text = text.Insert(found, format);//->[#_year](字段,'yyyy')
                            index = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);//寻找还有没有第二次出现的函数字段
                        }
                        while (index > -1);
                        text = text.Replace("#_", "#");
                        text = Replace(text, key, "to_char");//[#year](字段,'yyyy')
                        break;
                    case DataBaseType.SQLite:
                        do
                        {
                            text = text.Insert(index + 2, "_");//[#_year](字段)
                            found = text.IndexOf('(', index + 4);//从[#_year(字段)]找到 '('的位置
                            text = text.Insert(found + 1, format);//->[#_year]('%Y',字段)
                            found = text.IndexOf(')', found + 1);
                            text = text.Insert(found + 1, " as int)");
                            index = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);//寻找还有没有第二次出现的函数字段
                        }
                        while (index > -1);
                        text = text.Replace("#_", "#");
                        text = Replace(text, key, "cast(strftime");//cast(strftime('%Y', UpdateTime) as int) [%Y,%m,%d]
                        break;
                    case DataBaseType.Sybase:
                        text = Replace(text, key + "(", "datepart(" + format);
                        //// [#YEAR](getdate())  datepart(mm,getdate()) datepart(mm,getdate()) datepart(mm,getdate())
                        break;
                    case DataBaseType.PostgreSQL:
                        text = Replace(text, key + "(", "EXTRACT(" + func + " from ");
                        break;
                    default:
                        text = Replace(text, key, func);
                        break;
                }
            }
            return text;
        }
        internal static string FormatGetDate(string text, DataBaseType dalType)
        {
            switch (dalType)
            {
                case DataBaseType.Access:
                case DataBaseType.MySql:
                case DataBaseType.PostgreSQL:
                    return Replace(text, SqlValue.GetDate, "now()");
                case DataBaseType.MsSql:
                case DataBaseType.Sybase:
                    return Replace(text, SqlValue.GetDate, "getdate()");
                case DataBaseType.Oracle:
                    return Replace(text, SqlValue.GetDate, "current_date");
                case DataBaseType.DB2:
                    return Replace(text, SqlValue.GetDate, "CURRENT TIMESTAMP");
                case DataBaseType.SQLite:
                    return Replace(text, SqlValue.GetDate, "datetime('now','localtime')");
                case DataBaseType.Txt:
                case DataBaseType.Xml:
                    return Replace(text, SqlValue.GetDate, "'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'");
            }
            return text;
        }
        private static string FormatCharIndex(string text, DataBaseType dalType)
        {
            string key = SqlValue.CharIndex;
            //select [#charindex]('ok',xxx) from xxx where [#charindex]('ok',xx)>0
            int index = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (index > -1)//存在charIndex函数
            {
                switch (dalType)
                {
                    case DataBaseType.Access:
                    case DataBaseType.Oracle:
                        int found = 0;
                        string func = string.Empty;
                        do
                        {
                            int start = index + key.Length;
                            text = text.Insert(index + 2, "_");//select [#_charindex]('ok',xxx) from xxx where [#charindex]('ok',xx)>0
                            found = text.IndexOf(')', index + 4);
                            func = text.Substring(start + 2, found - start - 2);
                            string[] funs = func.Split(',');
                            text = text.Remove(start + 2, found - start - 2);//移除//select [#_charindex]() from xxx where [#charindex]('ok',xx)>0
                            text = text.Insert(start + 2, funs[1] + "," + funs[0]);
                            index = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                        }
                        while (index > -1);
                        text = text.Replace("#_", "#");
                        return Replace(text, key, "instr");
                    case DataBaseType.MySql:
                    case DataBaseType.DB2:
                        return Replace(text, key, "locate");
                    case DataBaseType.MsSql:
                    case DataBaseType.Sybase:
                    case DataBaseType.SQLite:
                        return Replace(text, key, "charindex");
                    case DataBaseType.PostgreSQL:
                        found = 0;
                        func = string.Empty;
                        do
                        {
                            int start = index + key.Length;
                            text = text.Insert(index + 2, "_");//select [#_charindex]('ok',xxx) from xxx where [#charindex]('ok',xx)>0
                            found = text.IndexOf(')', index + 4);
                            func = text.Substring(start + 2, found - start - 2);//('ok',xxx)
                            string[] funs = func.Split(',');
                            text = text.Remove(start + 2, found - start - 2);//移除//select [#_charindex]() from xxx where [#charindex]('ok',xx)>0
                            text = text.Insert(start + 2, funs[0] + " in " + funs[1]);
                            index = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                        }
                        while (index > -1);
                        text = text.Replace("#_", "#");
                        return Replace(text, key, "position");


                }
            }
            return text;
        }
        private static string FormatDateDiff(string text, DataBaseType dalType)
        {
            string key = SqlValue.DateDiff;
            //select [#DATEDIFF](aa,'bb','cc') from xxx where [#DATEDIFF](aa,'bb','cc')>0
            int index = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (index > -1)//'yyyy','q','m','y','d','ww','hh/h','n','s'
            {
                string[] keys = new string[] { "yyyy", "m", "d", "h", "n", "s" };//"hh/h"
                switch (dalType)
                {
                    case DataBaseType.Access:
                    case DataBaseType.Oracle:
                        foreach (string key1 in keys)
                        {
                            text = text.Replace("[#" + key1 + "]", "'" + key1 + "'");
                        }
                        break;
                    case DataBaseType.MsSql:
                    case DataBaseType.Sybase:
                        text = text.Replace("[#h]", "hh");
                        foreach (string key2 in keys)
                        {
                            text = text.Replace("[#" + key2 + "]", key2);
                        }
                        break;
                    case DataBaseType.MySql://和mssql/access参数相反
                        foreach (string key2 in keys)
                        {
                            text = text.Replace("[#" + key2 + "],", string.Empty);
                        }
                        text = text.Replace("()", AppConst.SplitChar);
                        int found = 0;
                        string func = string.Empty;
                        do
                        {
                            int start = index + key.Length;
                            text = text.Insert(index + 2, "_");//select [#_DateDiff](time1,time2()) from xxx where [#DateDiff](time1,time2())>0
                            found = text.IndexOf(')', index + 4);
                            func = text.Substring(start + 2, found - start - 2);
                            string[] funs = func.Split(',');
                            text = text.Remove(start + 2, found - start - 2);//移除//select [#_DateDiff() from xxx where [#DateDiff](time1,time2)>0
                            text = text.Insert(start + 2, funs[1] + "," + funs[0]);
                            index = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                        }
                        while (index > -1);
                        text = text.Replace("#_", "#").Replace(AppConst.SplitChar, "()");
                        break;
                    case DataBaseType.SQLite:
                        found = 0;
                        func = string.Empty;
                        do
                        {
                            int start = index + key.Length;
                            text = text.Insert(index + 2, "_");//[#_DateDiff]([#d],startTime',endTime)
                            found = text.IndexOf(')', index + 4);
                            func = text.Substring(start + 2, found - start - 2);//[#d],startTime',endTime
                            string[] funs = func.Split(',');
                            text = text.Remove(start + 2, found - start - 2);//移除[#_DateDiff]()
                            text = text.Insert(start + 2, "julianday(" + funs[2] + ")-julianday(" + funs[1] + ")");
                            index = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);//寻找还有没有第二次出现的函数字段
                        }
                        while (index > -1);
                        text = text.Replace("#_", "#");
                        text = Replace(text, key, string.Empty);
                        break;
                    case DataBaseType.PostgreSQL:
                        found = 0;
                        func = string.Empty;
                        string para = "", ageFun = "";
                        do
                        {
                            int start = index + key.Length;
                            text = text.Insert(index + 2, "_");//[#_DateDiff]([#d],startTime',endTime)
                            found = text.IndexOf(')', index + 4);
                            func = text.Substring(start + 2, found - start - 2);//[#d],startTime',endTime
                            string[] funs = func.Split(',');
                            text = text.Remove(start + 2, found - start - 2);//移除[#_DateDiff]()
                            ageFun = " from age(" + funs[2] + "," + funs[1] + "))";
                            switch (funs[0])
                            {
                                case "[#yyyy]":
                                    para = "year" + ageFun;
                                    break;
                                case "[#m]":
                                    para = string.Format("year{0}*12 + extract(month{0}", ageFun);
                                    break;
                                case "[#d]":
                                    para = "epoch" + ageFun + "/86400";
                                    break;
                                case "[#h]":
                                    para = "epoch" + ageFun + "/3600";
                                    break;
                                case "[#n]":
                                    para = "epoch" + ageFun + "/60";
                                    break;
                                case "[#s]":
                                    para = "epoch" + ageFun;
                                    break;

                            }
                            //floor(EXTRACT(   )
                            text = text.Insert(start + 2, para);
                            index = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);//寻找还有没有第二次出现的函数字段
                        }
                        while (index > -1);
                        text = text.Replace("#_", "#");
                        return Replace(text, key, "floor(extract");
                }
            }
            return Replace(text, key, "DateDiff");
        }
        private static string FormatCaseWhen(string text, DataBaseType dalType)
        {
            //CASE when languageid=1 THEN 1000 ELSE 10 End

            switch (dalType)
            {
                case DataBaseType.MsSql:
                case DataBaseType.Oracle:
                case DataBaseType.MySql:
                case DataBaseType.SQLite:
                case DataBaseType.Sybase:
                case DataBaseType.PostgreSQL:
                case DataBaseType.DB2:
                    if (text.IndexOf(SqlValue.Case, StringComparison.OrdinalIgnoreCase) > -1 || text.IndexOf(SqlValue.CaseWhen, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        text = Replace(text, SqlValue.Case, "Case");
                        text = Replace(text, SqlValue.CaseWhen, "Case When");
                        text = Replace(text, "[#WHEN]", "when");
                        text = Replace(text, "[#THEN]", "then");
                        text = Replace(text, "[#ELSE]", "else");
                        text = Replace(text, "[#END]", "end");
                    }
                    break;
                case DataBaseType.Access:
                    if (text.IndexOf(SqlValue.Case, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        text = Replace(text, SqlValue.Case, string.Empty);
                        text = Replace(text, " [#WHEN] ", "iif(");
                        text = Replace(text, " [#THEN] ", ",");
                        text = Replace(text, " [#ELSE] ", ",");
                        text = Replace(text, " [#END]", ")");
                    }
                    else if (text.IndexOf(SqlValue.CaseWhen, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        text = Replace(text, SqlValue.CaseWhen, "SWITCH(");
                        text = Replace(text, "[#THEN]", ",");
                        text = Replace(text, "[#ELSE]", "TRUE,");
                        text = Replace(text, "[#END]", ")");
                    }
                    break;
            }

            return text;
        }
        #endregion

        //忽略大小写的替换。
        private static string Replace(string text, string oldValue, string newValue)
        {
            oldValue = oldValue.Replace("[", "\\[").Replace("]", "\\]").Replace("(", "\\(");
            return Regex.Replace(text, oldValue, newValue, RegexOptions.IgnoreCase);
        }
    }
}
