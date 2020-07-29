using System.Text;

using System;
using System.Text.RegularExpressions;

namespace CYQ.Data.SQL
{
    /// <summary>
    /// Sql分页语句类
    /// </summary>
    internal partial class SqlCreateForPager
    {
        public static string GetSql(DataBaseType dalType, string version, int pageIndex, int pageSize, object objWhere, string tableName, int rowCount, string columns, string primaryKey, bool primaryKeyIsidentity)
        {
            if (string.IsNullOrEmpty(columns))
            {
                columns = "*";
            }
            pageIndex = pageIndex == 0 ? 1 : pageIndex;
            string where = SqlFormat.GetIFieldSql(objWhere);
            if (string.IsNullOrEmpty(where))
            {
                where = "1=1";
            }
            if (pageSize == 0)
            {
                return string.Format(top1Pager, columns, tableName, where);
            }
            if (rowCount > 0)//分页查询。
            {
                where = SqlCreate.AddOrderBy(where, primaryKey, dalType);
            }
            int topN = pageIndex * pageSize;//Top N 最大数
            int max = (pageIndex - 1) * pageSize;
            int rowStart = (pageIndex - 1) * pageSize + 1;
            int rowEnd = rowStart + pageSize - 1;
            string orderBy = string.Empty;
            if (pageIndex == 1 && dalType != DataBaseType.Oracle)//第一页（oracle时 rownum 在排序条件为非数字时，和row_number()的不一样，会导致结果差异，所以分页统一用row_number()。）
            {
                switch (dalType)
                {
                    case DataBaseType.Access:
                    case DataBaseType.MsSql:
                    case DataBaseType.Sybase:
                    case DataBaseType.Txt:
                    case DataBaseType.Xml:
                        return string.Format(top1Pager, "top " + pageSize + " " + columns, tableName, where);
                    //case DalType.Oracle:
                    //    return string.Format(top1Pager, columns, tableName, "rownum<=" + pageSize + " and " + where);
                    case DataBaseType.SQLite:
                    case DataBaseType.MySql:
                    case DataBaseType.PostgreSQL:
                        return string.Format(top1Pager, columns, tableName, where + " limit " + pageSize);
                    case DataBaseType.DB2:
                        return string.Format(top1Pager, columns, tableName, where + " fetch first "+ pageSize + " rows only");
                }
            }
            else
            {

                switch (dalType)
                {
                    case DataBaseType.Access:
                    case DataBaseType.MsSql:
                    case DataBaseType.Sybase:
                        int leftNum = rowCount % pageSize;
                        int pageCount = leftNum == 0 ? rowCount / pageSize : rowCount / pageSize + 1;//页数
                        if (pageIndex == pageCount && dalType != DataBaseType.Sybase) // 最后一页Sybase 不支持双Top order by
                        {
                            return string.Format(top2Pager, pageSize + " " + columns, "top " + (leftNum == 0 ? pageSize : leftNum) + " * ", tableName, ReverseOrderBy(where, primaryKey), GetOrderBy(where, false, primaryKey));//反序
                        }
                        if (dalType != DataBaseType.MsSql && (pageCount > 1000 || rowCount > 100000) && pageIndex > pageCount / 2) // 页数过后半段，反转查询
                        {
                            //mssql rownumber 的语句
                            orderBy = GetOrderBy(where, false, primaryKey);
                            where = ReverseOrderBy(where, primaryKey);//事先反转一次。
                            topN = rowCount - max;//取后面的
                            int rowStartTemp = rowCount - rowEnd;
                            rowEnd = rowCount - rowStart + 1;//网友反馈修正（数据行要+1）
                            rowStart = rowStartTemp + 1;//网友反馈修正（数据行要+1）
                        }
                        break;
                    case DataBaseType.Txt:
                    case DataBaseType.Xml:
                        return string.Format(top1Pager, columns, tableName, where + " limit " + pageSize + " offset " + pageIndex);

                }
            }


            switch (dalType)
            {
                case DataBaseType.MsSql:
                case DataBaseType.Oracle:
                case DataBaseType.DB2://
                    if (version.StartsWith("08"))
                    {
                        goto temtable;
                        // goto top3;//sql 2000
                    }
                    int index = tableName.LastIndexOf(')');
                    if (index > 0)
                    {
                        tableName = tableName.Substring(0, index + 1);
                    }
                    string v = dalType == DataBaseType.Oracle ? "" : " v";
                    string onlyWhere = "where " + SqlCreate.RemoveOrderBy(where);
                    onlyWhere = SqlFormat.RemoveWhereOneEqualsOne(onlyWhere);
                    return string.Format(rowNumberPager, GetOrderBy(where, false, primaryKey), (columns == "*" ? "t.*" : columns), tableName, onlyWhere, v, rowStart, rowEnd);
                case DataBaseType.Sybase:
                temtable:
                    if (primaryKeyIsidentity)
                    {
                        bool isOk = columns == "*";
                        if (!isOk)
                        {
                            string kv = SqlFormat.NotKeyword(primaryKey);
                            string[] items = columns.Split(',');
                            foreach (string item in items)
                            {
                                if (string.Compare(SqlFormat.NotKeyword(item), kv, StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    isOk = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            columns = "t.*";
                            index = tableName.LastIndexOf(')');
                            if (index > 0)
                            {
                                tableName = tableName.Substring(0, index + 1);
                            }
                            tableName += " t ";
                        }
                        if (isOk)
                        {

                            return string.Format(tempTablePagerWithidentity, DateTime.Now.Millisecond, topN, primaryKey, tableName, where, pageSize, columns, rowStart, rowEnd, orderBy);
                        }
                    }
                    return string.Format(tempTablePager, DateTime.Now.Millisecond, pageIndex * pageSize + " " + columns, tableName, where, pageSize, rowStart, rowEnd, orderBy);
                case DataBaseType.Access:
                top3:
                    if (!string.IsNullOrEmpty(orderBy)) // 反转查询
                    {
                        return string.Format(top4Pager, columns, (rowCount - max > pageSize ? pageSize : rowCount - max), topN, tableName, where, GetOrderBy(where, true, primaryKey), GetOrderBy(where, false, primaryKey), orderBy);
                    }
                    return string.Format(top3Pager, (rowCount - max > pageSize ? pageSize : rowCount - max), columns, topN, tableName, where, GetOrderBy(where, true, primaryKey), GetOrderBy(where, false, primaryKey));
                case DataBaseType.SQLite:
                case DataBaseType.MySql:
                case DataBaseType.PostgreSQL:
                    if (max > 500000 && primaryKeyIsidentity && Convert.ToString(objWhere) == "" && !tableName.Contains(" "))//单表大数量时的优化成主键访问。
                    {
                        where = string.Format("{0}>=(select {0} from {1} limit {2}, 1) limit {3}", primaryKey, tableName, max, pageSize);
                        return string.Format(top1Pager, columns, tableName, where);
                    }
                    return string.Format(top1Pager, columns, tableName, where + " limit " + pageSize + " offset " + max);
            }
            return (string)Error.Throw("Pager::No Be Support:" + dalType.ToString());
        }

        /// <summary>
        /// 首页查询
        /// </summary>
        private const string top1Pager = "select {0} from {1} where {2}";
        /// <summary>
        /// 最后一页查询
        /// </summary>
        private const string top2Pager = "select top {0} from (select {1} from {2} where {3}) v {4}";
        /// <summary>
        /// 前半段分页查询
        /// </summary>
        private const string top3Pager = @"select top {0} {1} from (select top {0} * from (select top {2} * from {3} where {4}) v {5}) v {6}";
        /// <summary>
        /// 后半段分页查询（即倒过来查询）
        /// </summary>
        private const string top4Pager = @"select {0} from (select top {1} * from (select top {1} * from (select top {2} * from {3} where {4}) v {5}) v {6}) v {7}";
        /// <summary>
        /// MSSQL、Oracle的行号分页
        /// </summary>
        private const string rowNumberPager = "select * from(select row_number() over ({0}) cyqrownum,{1} from {2} t {3}){4} where cyqrownum between {5} and {6}";
        /// <summary>
        /// 临时表分页（不带自增加序列）
        /// </summary>
        private const string tempTablePager = @"select top {1},cyqrownum=identity(int) into #tmp{0} from {2} where {3} select top {4} * from #tmp{0} where cyqrownum between {5} and {6} {7} drop table #tmp{0}";
        /// <summary>
        /// 临时表分页（带自增加序列）
        /// </summary>
        private const string tempTablePagerWithidentity = @"select top {1} cast({2} as int) cyqrowid,cyqrownum=identity(int) into #tmp{0} from {3} where {4} select top {5} {6} from #tmp{0} left join {3} on {2}=cyqrowid where cyqrownum between {7} and {8} {9} drop table #tmp{0}";

        ///// <summary>
        ///// 格式化where，需要有order by
        ///// </summary>
        //private static string GetWhereFixOrderBy(string where)
        //{
        //    if (where.IndexOf("order by", StringComparison.OrdinalIgnoreCase) == -1)
        //    {
        //        where += " order by " + defaultOrderByKey;
        //    }
        //    return where;
        //}
        /// <summary>
        /// 获取order by 语句
        /// </summary>
        /// <param name="reverse">排序是否反转</param>
        internal static string GetOrderBy(string where, bool reverse, string primaryKey)
        {
            string orderby = " order by " + primaryKey + " ";

            int index = where.IndexOf("order by ", StringComparison.OrdinalIgnoreCase);//order by XXasc , Xdesc desc
            if (index > -1)
            {
                if (where.IndexOf("asc", StringComparison.OrdinalIgnoreCase) > -1 || where.IndexOf("desc", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    where = where.Substring(index);//不需要反转，直接取排序内容返回。
                    if (!reverse)
                    {
                        return where;
                    }
                    where = where.Replace(",", " , ");
                    string[] items = where.Split(' ');
                    orderby = string.Empty;
                    foreach (string item in items)
                    {
                        switch (item.ToLower())
                        {
                            case "asc":
                                orderby += "desc ";
                                break;
                            case "desc":
                                orderby += "asc ";
                                break;
                            default:
                                orderby += item + " ";
                                break;
                        }
                    }
                    return orderby.Trim();
                }
                else if (reverse)
                {
                    orderby = where + " desc";
                }
            }
            else
            {
                orderby += reverse ? "desc" : "asc";
            }
            return orderby.ToLower();
        }
        private static string ReverseOrderBy(string where, string primaryKey)
        {
            int index = where.IndexOf("order by ", StringComparison.OrdinalIgnoreCase);
            if (index > -1)
            {
                string orderby = string.Empty;

                string onlyOrderBy = where.Substring(index).Trim();//order by t_sort,t_course_code desc,t_id "

                where = where.Substring(0, index);
                onlyOrderBy = onlyOrderBy.Replace(",", " , ");
                string[] items = onlyOrderBy.Split(' ');
                orderby = string.Empty;
                bool isAscOrDesc = false;
                foreach (string item in items)
                {
                    switch (item.ToLower())
                    {
                        case "asc":
                            orderby += "desc ";
                            isAscOrDesc = true;
                            break;
                        case "desc":
                            orderby += "asc ";
                            isAscOrDesc = true;
                            break;
                        case ","://遇上分号，如果前面没有升降序，默认为升，转为降。
                            if (!isAscOrDesc)
                            {
                                orderby += "desc";
                            }
                            orderby += item;
                            isAscOrDesc = false;
                            break;
                        case "":
                            break;
                        default:
                            orderby += item + " ";
                            isAscOrDesc = false;
                            break;
                    }
                }
                string lastItem = items[items.Length - 1].ToLower();
                if (lastItem != "asc" && lastItem != "desc") // 最后一项为空时，补desc
                {
                    orderby += "desc";
                }
                return where + orderby.Trim();

            }
            else
            {
                return where + " order by " + primaryKey + " desc";
            }

        }
        ///// <summary>
        ///// 是否降序
        ///// </summary>
        //private static bool IsOrderByDesc(string where)
        //{
        //    int index = where.IndexOf("order by", StringComparison.OrdinalIgnoreCase);
        //    if (index > -1 && where.IndexOf("desc", StringComparison.OrdinalIgnoreCase) > -1)
        //    {
        //        return true;
        //    }
        //    return false;
        //}
        //internal static string defaultOrderByKey = "id";

    }
}
