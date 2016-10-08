using System;




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
        internal static string Format(string text, DalType dalType)
        {
            if (!string.IsNullOrEmpty(text))
            {
                text = FormatPara(text, dalType);
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
            return text;
        }
        #region 过滤与多数据库标签解析
        internal static string FormatLeft(string text, DalType dalType)
        {
            switch (dalType)
            {
                //substr(MAX(SheetId),1,4)) IS NULL THEN 0 ELSE substr(MAX(SheetId)length(MAX(SheetId))-4,4) 
                case DalType.Oracle:
                    int index = text.IndexOf(SqlValue.Left);//left(a,4) =>to_char(substr(a,1,4))
                    if (index > -1)
                    {
                        do
                        {
                            index = text.IndexOf('(', index);
                            int end = text.IndexOf(',', index);
                            int end2 = text.IndexOf(')', end + 1);
                            text = text.Insert(end2, ")");
                            text = text.Insert(end + 1, "1,");
                            index = text.IndexOf(SqlValue.Left, end);//寻找还有没有第二次出现的函数字段
                        }
                        while (index > -1);
                        return text.Replace(SqlValue.Left, "to_char(substr");
                    }
                    return text;
                default:
                    return text.Replace(SqlValue.Left, "Left");
            }
        }
        internal static string FormatRight(string text, DalType dalType)
        {
            switch (dalType)
            {
                case DalType.Oracle:
                    int index = text.IndexOf(SqlValue.Right);//right(a,4) => to_char(substr(a,length(a)-4,4))
                    if (index > -1)
                    {
                        do
                        {
                            ////substr(MAX(SheetId),1,4)) IS NULL THEN 0 ELSE substr(MAX(SheetId)length(MAX(SheetId))-4,4) 
                            index = text.IndexOf('(', index);
                            int end = text.IndexOf(',', index);
                            string key = text.Substring(index + 1, end - index - 1);//找到 a
                            int end2 = text.IndexOf(')', end + 1);
                            string key2 = text.Substring(end + 1, end2 - end - 1);//找到b
                            text = text.Insert(end2, ")");
                            text = text.Insert(end + 1, "length(" + key + ")+1-" + key2 + ",");//
                            index = text.IndexOf(SqlValue.Right, end);//寻找还有没有第二次出现的函数字段
                        }
                        while (index > -1);
                        return text.Replace(SqlValue.Right, "to_char(substr");
                    }
                    return text;
                default:
                    return text.Replace(SqlValue.Right, "Right");
            }
        }
        internal static string FormatContact(string text, DalType dalType)
        {
            switch (dalType)
            {
                case DalType.Oracle:
                    return text.Replace(SqlValue.Contact, "||");
                default:
                    return text.Replace(SqlValue.Contact, "+");
            }
        }
        internal static string FormatIsNull(string text, DalType dalType)
        {
            switch (dalType)
            {
                case DalType.Access:
                    int index = text.IndexOf(SqlValue.IsNull);//isnull  (isnull(aaa),'3,3')   iff(isnull   (aaa),333,aaa)
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
                            index = text.IndexOf(SqlValue.IsNull, end);//寻找还有没有第二次出现的函数字段
                        }
                        while (index > -1);
                        return text.Replace(SqlValue.IsNull, "iff(isnull");
                    }
                    break;
                case DalType.SQLite:
                case DalType.MySql:
                    return text.Replace(SqlValue.IsNull, "IfNull");
                case DalType.MsSql:
                case DalType.Sybase:
                    return text.Replace(SqlValue.IsNull, "IsNull");
                case DalType.Oracle:
                    return text.Replace(SqlValue.IsNull, "NVL");
            }
            return text;
        }
        internal static string FormatGUID(string text, DalType dalType)
        {
            switch (dalType)
            {
                case DalType.Access:
                    return text.Replace(SqlValue.Guid, "GenGUID()");
                case DalType.MySql:
                    return text.Replace(SqlValue.Guid, "UUID()");
                case DalType.MsSql:
                case DalType.Sybase:
                    return text.Replace(SqlValue.Guid, "newid()");
                case DalType.Oracle:
                    return text.Replace(SqlValue.Guid, "SYS_GUID()");
                case DalType.SQLite:
                    return text.Replace(SqlValue.Guid, "");
            }
            return text;
        }

        private static string FormatPara(string text, DalType dalType)
        {
            switch (dalType)
            {
                case DalType.MySql:
                    return text.Replace("=:?", "=?");
                case DalType.Oracle:
                    return text.Replace("=:?", "=:");
                default:
                    return text.Replace("=:?", "=@");
            }
        }

        private static string FormatTrueFalseAscDesc(string text, DalType dalType)
        {
            switch (dalType)
            {
                case DalType.Access:
                    return text.Replace(SqlValue.True, "true").Replace(SqlValue.False, "false").Replace(SqlValue.Desc, "asc").Replace(SqlValue.Asc, "desc");
                default:
                    return text.Replace(SqlValue.True, "1").Replace(SqlValue.False, "0").Replace(SqlValue.Desc, "desc").Replace(SqlValue.Asc, "asc");
            }
        }

        private static string FormatLen(string text, DalType dalType)
        {
            switch (dalType)//处理函数替换
            {
                case DalType.Access:
                case DalType.MsSql:
                    return text.Replace(SqlValue.Len, "len").Replace(SqlValue.Substring, "substring");
                case DalType.Oracle:
                case DalType.SQLite:
                    return text.Replace(SqlValue.Len, "length").Replace(SqlValue.Substring, "substr");
                case DalType.MySql:
                    return text.Replace(SqlValue.Len, "char_length").Replace(SqlValue.Substring, "substring");
                case DalType.Sybase:
                    return text.Replace(SqlValue.Len, "datalength").Replace(SqlValue.Substring, "substring");
            }
            return text;
        }
        private static string GetFormatDateKey(DalType dalType, string key)
        {
            switch (dalType)
            {
                case DalType.SQLite:
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
                case DalType.Sybase:
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
        private static string FormatDate(string text, DalType dalType, string key, string func)
        {
            int index = text.IndexOf(key);//[#year](字段)
            if (index > -1)//存在[#year]函数
            {
                string format = GetFormatDateKey(dalType, key);
                int found = 0;
                switch (dalType)
                {
                    case DalType.Oracle:
                        do
                        {
                            text = text.Insert(index + 2, "_");//[#_year](字段)
                            found = text.IndexOf(')', index + 4);//从[#_year(字段)]找到 ')'的位置
                            text = text.Insert(found, format);//->[#_year](字段,'yyyy')
                            index = text.IndexOf(key);//寻找还有没有第二次出现的函数字段
                        }
                        while (index > -1);
                        text = text.Replace("#_", "#").Replace(key, "to_char");//[#year](字段,'yyyy')
                        break;
                    case DalType.SQLite:
                        do
                        {
                            text = text.Insert(index + 2, "_");//[#_year](字段)
                            found = text.IndexOf('(', index + 4);//从[#_year(字段)]找到 '('的位置
                            text = text.Insert(found + 1, format);//->[#_year]('%Y',字段)
                            found = text.IndexOf(')', found + 1);
                            text = text.Insert(found + 1, " as int)");
                            index = text.IndexOf(key);//寻找还有没有第二次出现的函数字段
                        }
                        while (index > -1);
                        text = text.Replace("#_", "#").Replace(key, "cast(strftime");//cast(strftime('%Y', UpdateTime) as int) [%Y,%m,%d]
                        break;
                    case DalType.Sybase:
                        text = text.Replace(key + "(", "datepart(" + format);
                    //// [#YEAR](getdate())  datepart(mm,getdate()) datepart(mm,getdate()) datepart(mm,getdate())
                        break;
                    default:
                        text = text.Replace(key, func);
                        break;
                }
            }
            return text;
        }
        internal static string FormatGetDate(string text, DalType dalType)
        {
            switch (dalType)
            {
                case DalType.Access:
                case DalType.MySql:
                    return text.Replace(SqlValue.GetDate, "now()");
                case DalType.MsSql:
                case DalType.Sybase:
                    return text.Replace(SqlValue.GetDate, "getdate()");
                case DalType.Oracle:
                    return text.Replace(SqlValue.GetDate, "current_date");
                case DalType.SQLite:
                    return text.Replace(SqlValue.GetDate, "datetime('now','localtime')");
            }
            return text;
        }
        private static string FormatCharIndex(string text, DalType dalType)
        {
            string key = SqlValue.CharIndex;
            //select [#charindex]('ok',xxx) from xxx where [#charindex]('ok',xx)>0
            int index = text.IndexOf(key);
            if (index > -1)//存在charIndex函数
            {
                switch (dalType)
                {
                    case DalType.Access:
                    case DalType.Oracle:
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
                            index = text.IndexOf(key);
                        }
                        while (index > -1);
                        text = text.Replace("#_", "#");
                        return text.Replace(key, "instr");
                    case DalType.MySql:
                        return text.Replace(key, "locate");
                    case DalType.MsSql:
                    case DalType.Sybase:
                        return text.Replace(key, "charindex");
                    case DalType.SQLite:
                        //int found = 0;
                        //string func = string.Empty;
                        //do
                        //{
                        //    int start = index + key.Length;
                        //    text = text.Insert(index + 2, "_");//select [#_charindex]('ok',xxx) from xxx where [#charindex]('ok',xx)>0
                        //    found = text.IndexOf(')', index + 4);
                        //    func = text.Substring(start + 2, found - start - 2);
                        //    string[] funs = func.Split(',');
                        //    text = text.Remove(start + 2, found - start - 2);//移除//select [#_charindex]() from xxx where [#charindex]('ok',xx)>0
                        //    text = text.Insert(start + 2, string.Format("lower({0}),lower({1})", funs[0], funs[1]));
                        //    index = text.IndexOf(key);
                        //}
                        //while (index > -1);
                        //text = text.Replace("#_", "#");
                        return text.Replace(key, "charindex");


                }
            }
            return text;
        }
        private static string FormatDateDiff(string text, DalType dalType)
        {
            string key = SqlValue.DateDiff;
            //select [#DATEDIFF](aa,'bb','cc') from xxx where [#DATEDIFF](aa,'bb','cc')>0
            int index = text.IndexOf(key);
            if (index > -1)//'yyyy','q','m','y','d','ww','hh/h','n','s'
            {
                string[] keys = new string[] { "yyyy", "q", "m", "y", "d", "h", "ww", "n", "s" };//"hh/h"
                switch (dalType)
                {
                    case DalType.Access:
                    case DalType.Oracle:
                        foreach (string key1 in keys)
                        {
                            text = text.Replace("[#" + key1 + "]", "'" + key1 + "'");
                        }
                        break;
                    case DalType.MsSql:
                    case DalType.Sybase:
                        text = text.Replace("[#h]", "hh");
                        foreach (string key2 in keys)
                        {
                            text = text.Replace("[#" + key2 + "]", key2);
                        }
                        break;
                    case DalType.MySql://和mssql/access参数相反
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
                            index = text.IndexOf(key);
                        }
                        while (index > -1);
                        text = text.Replace("#_", "#").Replace(AppConst.SplitChar, "()");
                        break;
                    case DalType.SQLite:
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
                            index = text.IndexOf(key);//寻找还有没有第二次出现的函数字段
                        }
                        while (index > -1);
                        text = text.Replace("#_", "#").Replace(key, string.Empty);
                        break;
                }
            }
            return text.Replace(key, "DateDiff");
        }
        private static string FormatCaseWhen(string text, DalType dalType)
        {
            //CASE when languageID=1 THEN 1000 ELSE 10 End

            switch (dalType)
            {
                case DalType.MsSql:
                case DalType.Oracle:
                case DalType.MySql:
                case DalType.SQLite:
                case DalType.Sybase:
                    if (text.IndexOf(SqlValue.Case) > -1 || text.IndexOf(SqlValue.CaseWhen) > -1)
                    {
                        text = text.Replace(SqlValue.Case, "Case").Replace(SqlValue.CaseWhen, "Case When").Replace("[#WHEN]", "when").Replace("[#THEN]", "then").Replace("[#ELSE]", "else").Replace("[#END]", "end");
                    }
                    break;
                case DalType.Access:
                    if (text.IndexOf(SqlValue.Case) > -1)
                    {
                        text = text.Replace(SqlValue.Case, string.Empty).Replace(" [#WHEN] ", "iif(").Replace(" [#THEN] ", ",").Replace(" [#ELSE] ", ",").Replace(" [#END]", ")");
                    }
                    else if (text.IndexOf(SqlValue.CaseWhen) > -1)
                    {
                        text = text.Replace(SqlValue.CaseWhen, "SWITCH(").Replace("[#THEN]", ",").Replace("[#ELSE]", "TRUE,").Replace("[#END]", ")");
                    }
                    break;
            }

            return text;
        }
        #endregion
    }
}
