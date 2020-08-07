using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.SQL;
using System.Data;
using System.ComponentModel;
using CYQ.Data.Tool;

namespace CYQ.Data.Table
{
    /// <summary>
    /// 操作符号
    /// </summary>
    internal enum Op
    {
        /// <summary>
        /// 没有操作
        /// </summary>
        None,
        /// <summary>
        /// 操作符号:">"
        /// </summary>
        Big,
        /// <summary>
        /// 操作符号:">="
        /// </summary>
        BigEqual,
        /// <summary>
        /// 操作符号:"="
        /// </summary>
        Equal,
        /// <summary>
        /// 操作符号:"&lt;>"
        /// </summary>
        NotEqual,
        /// <summary>
        /// 操作符号:"&lt;"
        /// </summary>
        Small,
        /// <summary>
        /// 操作符号:"&lt;="
        /// </summary>
        SmallEqual,
        /// <summary>
        /// 操作符号:"like"
        /// </summary>
        Like,
        /// <summary>
        /// 操作符号:"not like"
        /// </summary>
        NotLike,
        /// <summary>
        /// 是否Null值
        /// </summary>
        IsNull,
        /// <summary>
        /// 非Null值
        /// </summary>
        IsNotNull,
        /// <summary>
        /// 操作符号：In
        /// </summary>
        In,
        /// <summary>
        /// 操作符号：Not In
        /// </summary>
        NotIn

    }
    /// <summary>
    /// 查询的基本连接条件
    /// </summary>
    internal enum Ao
    {
        None,
        And,
        Or
    }
    /// <summary>
    /// 条件过滤参数
    /// </summary>
    internal class TFilter
    {
        internal Ao _Ao;
        internal object _valueA;
        internal int _columnAIndex = -1;
        internal Op _Op;
        internal object _valueB;
        internal int _columnBIndex = -1;
        public TFilter(Ao ao, object valueA, int columnAIndex, Op op, object valueB, int columnBIndex)
        {
            _Ao = ao;
            _valueA = valueA;
            _columnAIndex = columnAIndex;
            _Op = op;
            _valueB = valueB;
            _columnBIndex = columnBIndex;
        }
    }
    /// <summary>
    /// MDataTable查询过滤器
    /// </summary>
    internal static class MDataTableFilter
    {
        public static MDataTable[] Split(MDataTable table, object whereObj)
        {
            MDataTable[] mdt2 = new MDataTable[2];
            mdt2[0] = table.GetSchema(false);
            mdt2[1] = table.GetSchema(false);
            if (table.Rows.Count > 0)
            {
                if (Convert.ToString(whereObj).Trim() == "")
                {
                    mdt2[0] = table;
                }
                else
                {
                    List<TFilter> group2 = null;
                    List<TFilter> filters = GetTFilter(whereObj, table.Columns, out group2);
                    if (filters.Count > 0)
                    {
                        for (int i = 0; i < table.Rows.Count; i++)
                        {
                            MDataRow row = table.Rows[i];
                            if (CompareMore(row, filters) && (group2.Count == 0 || CompareMore(row, group2)))
                            {
                                mdt2[0].Rows.Add(row, false);
                            }
                            else
                            {
                                mdt2[1].Rows.Add(row, false);
                            }
                        }
                    }
                }
            }
            return mdt2;
        }
        public static int GetIndex(MDataTable table, object whereObj)
        {
            int index = -1;
            if (table.Rows.Count > 0)
            {
                if (Convert.ToString(whereObj).Trim() == "")
                {
                    return 0;
                }
                List<TFilter> group2 = null;
                List<TFilter> filters = GetTFilter(whereObj, table.Columns, out group2);
                if (filters.Count > 0)
                {
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        if (CompareMore(table.Rows[i], filters) && (group2.Count == 0 || CompareMore(table.Rows[i], group2)))
                        {
                            index = i;
                            break;
                        }
                    }
                }
                filters = null;
            }
            return index;
        }
        public static int GetCount(MDataTable table, object whereObj)
        {
            int count = 0;
            if (table.Rows.Count > 0)
            {
                if (Convert.ToString(whereObj).Trim() == "")
                {
                    return table.Rows.Count;
                }
                List<TFilter> group2 = null;
                List<TFilter> filters = GetTFilter(whereObj, table.Columns, out group2);
                if (filters.Count > 0)
                {
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        if (CompareMore(table.Rows[i], filters) && (group2.Count == 0 || CompareMore(table.Rows[i], group2)))
                        {
                            count++;
                        }
                    }
                }
                filters = null;
            }
            return count;
        }
        public static MDataRowCollection FindAll(MDataTable table, object whereObj)
        {
            if (table != null && table.Rows.Count > 0)
            {
                MDataRowCollection findRows = new MDataRowCollection();
                string where = Convert.ToString(whereObj).Trim();
                if (where == "" || where == "1=1")
                {
                    findRows.AddRange(table.Rows);
                    return findRows;
                }
                string whereStr = SqlFormat.GetIFieldSql(whereObj);
                string orderby;
                SplitWhereOrderby(ref whereStr, out orderby);

                List<TFilter> group2 = null;
                List<TFilter> filters = GetTFilter(whereStr, table.Columns, out group2);
                IList<MDataRow> rows = table.Rows;
                if (filters.Count > 0)
                {

                    rows = table.Rows.FindAll(delegate (MDataRow row)
                     {
                         return CompareMore(row, filters) && (group2.Count == 0 || CompareMore(row, group2));
                     }
                     );
                }
                findRows.AddRange(rows);//添加找到的行。
                filters = null;
                if (!string.IsNullOrEmpty(orderby) && rows.Count > 1)//进行数组排序
                {
                    findRows.Sort(orderby);
                    //MDataRowCollection sortRows = new MDataRowCollection();
                    //sortRows.AddRange(rows);
                    //sortRows.Sort(orderby);
                    //return sortRows;
                }
                return findRows;
            }
            return null;
        }
        public static MDataRow FindRow(MDataTable table, object whereObj)
        {
            if (table.Rows.Count > 0)
            {
                if (Convert.ToString(whereObj).Trim() == "")
                {
                    return table.Rows[0];
                }
                string whereStr = SqlFormat.GetIFieldSql(whereObj);
                string orderby;
                SplitWhereOrderby(ref whereStr, out orderby);
                MDataRowCollection sortRows = null;
                if (!string.IsNullOrEmpty(orderby) && table.Rows.Count > 1)//进行数组排序
                {
                    sortRows = new MDataRowCollection();
                    sortRows.AddRange(table.Rows);
                    sortRows.Sort(orderby);
                }
                List<TFilter> group2 = null;
                List<TFilter> filters = GetTFilter(whereStr, table.Columns, out group2);

                if (filters.Count > 0)
                {
                    if (sortRows == null)
                    {
                        sortRows = table.Rows;
                    }
                    for (int i = 0; i < sortRows.Count; i++)
                    {
                        if (CompareMore(sortRows[i], filters) && (group2.Count == 0 || CompareMore(sortRows[i], group2)))
                        {
                            return sortRows[i];
                        }
                    }
                }

            }
            return null;
        }
        public static MDataTable Select(MDataTable table, int pageIndex, int pageSize, object whereObj)
        {
            return Select(table, pageIndex, pageSize, whereObj);
        }
        public static MDataTable Select(MDataTable table, int pageIndex, int pageSize, object whereObj, params object[] selectColumns)
        {
            if (table == null)
            {
                return null;
            }
            MDataTable sTable = table.GetSchema(true);
            sTable.RecordsAffected = table.Rows.Count;
            if (table.Rows.Count == 0)// 正常情况下，看是否需要处理列移除
            {
                FilterColumns(ref sTable, selectColumns);//列查询过滤
                return sTable;
            }
            MDataRowCollection findRows = FindAll(table, whereObj);
            if (findRows != null)
            {
                sTable.RecordsAffected = findRows.Count;//设置记录总数
                FilterPager(findRows, pageIndex, pageSize);//进行分页筛选，再克隆最后的数据。

                for (int i = 0; i < findRows.Count; i++)
                {
                    if (i < findRows.Count)//内存表时（表有可能在其它线程被清空）
                    {
                        MDataRow row = findRows[i];
                        if (row == null)
                        {
                            break;
                        }
                        sTable.NewRow(true).LoadFrom(row, RowOp.None, false, true);
                    }
                }

                findRows = null;

            }
            if (selectColumns != null && selectColumns.Length > 0)
            {
                FilterColumns(ref sTable, selectColumns);//列查询过滤，由于查询的条件可能包含被移除列，所以只能在最后才过滤
            }
            //进行条件查询
            return sTable;
        }
        private static int IndexOf(string where, string andOrSign, int start)
        {
            //为了处理如下的特殊情况："B like '%'' and ''%' and A='a' or A<2 order by a,b desc"); 第1个 and 是假的
            int index = where.IndexOf(andOrSign, start);
            int lastIndex = start;
            while (index > -1)//前置的引号检查,如果引号为单，则继续取下一个
            {
                if (where.Substring(start, index - start).Split('\'').Length % 2 == 0)//为双的话仅有单个的符号,将继续往后截
                {
                    lastIndex = index + andOrSign.Length;
                    index = where.IndexOf(andOrSign, lastIndex + 1);
                }
                else
                {
                    return index;
                }
            }

            return index;
        }
        /// <summary>
        /// 多个条件
        /// </summary>
        private static List<TFilter> GetTFilter(object whereObj, MDataColumn mdc, out List<TFilter> group2)
        {
            group2 = new List<TFilter>();
            List<TFilter> tFilterList = new List<TFilter>();
            string whereStr = SqlFormat.GetIFieldSql(whereObj);
            whereStr = SqlCreate.FormatWhere(whereStr, mdc, DataBaseType.None, null);
            string lowerWhere = whereStr.ToLower();
            string andSign = " and ";
            string orSign = " or ";
            int andIndex = IndexOf(lowerWhere, andSign, 0);// lowerWhere.IndexOf(andSign);
            int orIndex = IndexOf(lowerWhere, orSign, 0);

            TFilter filter = null;
            if (andIndex == -1 && orIndex == -1)//仅有一个条件
            {
                filter = GetSingleTFilter(whereStr, mdc);
                if (filter != null)
                {
                    tFilterList.Add(filter);
                }
            }
            else if (orIndex == -1) // 只有and条件
            {
                int andStartIndex = 0;
                while (andIndex > -1)
                {
                    filter = GetSingleTFilter(whereStr.Substring(andStartIndex, andIndex - andStartIndex), mdc);
                    if (filter != null)
                    {
                        if (andStartIndex > 0)
                        {
                            filter._Ao = Ao.And;
                        }
                        tFilterList.Add(filter);
                    }
                    andStartIndex = andIndex + andSign.Length;
                    andIndex = IndexOf(lowerWhere, andSign, andStartIndex + 1);
                }
                filter = GetSingleTFilter(whereStr.Substring(andStartIndex), mdc);
                if (filter != null)
                {
                    filter._Ao = Ao.And;
                    tFilterList.Add(filter);
                }

            }
            else if (andIndex == -1) //只有or 条件
            {
                int orStartIndex = 0;
                while (orIndex > -1)
                {
                    filter = GetSingleTFilter(whereStr.Substring(orStartIndex, orIndex - orStartIndex), mdc);
                    if (filter != null)
                    {
                        if (orStartIndex > 0)
                        {
                            filter._Ao = Ao.Or;
                        }
                        tFilterList.Add(filter);
                    }
                    orStartIndex = orIndex + orSign.Length;
                    orIndex = IndexOf(lowerWhere, orSign, orStartIndex + 1);
                }
                filter = GetSingleTFilter(whereStr.Substring(orStartIndex), mdc);
                if (filter != null)
                {
                    filter._Ao = Ao.Or;
                    tFilterList.Add(filter);
                }
            }
            else //有and 又有 or
            {
                bool isAnd = andIndex < orIndex;
                bool lastAnd = isAnd;
                int andOrIndex = isAnd ? andIndex : orIndex;//最小的，前面的先处理

                int andOrStartIndex = 0;
                bool needGroup2 = isAnd;//如果是and开头，则分成两组
                while (andOrIndex > -1)
                {
                    filter = GetSingleTFilter(whereStr.Substring(andOrStartIndex, andOrIndex - andOrStartIndex), mdc);
                    if (filter != null)
                    {
                        if (andOrStartIndex > 0)
                        {
                            filter._Ao = lastAnd ? Ao.And : Ao.Or;
                        }
                        tFilterList.Add(filter);
                    }
                    andOrStartIndex = andOrIndex + (isAnd ? andSign.Length : orSign.Length);
                    if (isAnd)
                    {
                        andIndex = IndexOf(lowerWhere, andSign, andOrStartIndex + 1);
                    }
                    else
                    {
                        orIndex = IndexOf(lowerWhere, orSign, andOrStartIndex + 1);
                    }
                    lastAnd = isAnd;
                    if (andIndex == -1)
                    {
                        isAnd = false;
                        andOrIndex = orIndex;
                    }
                    else if (orIndex == -1)
                    {
                        isAnd = true;
                        andOrIndex = andIndex;
                    }
                    else
                    {
                        isAnd = andIndex < orIndex;
                        andOrIndex = isAnd ? andIndex : orIndex;//最小的，前面的先处理
                    }

                }
                filter = GetSingleTFilter(whereStr.Substring(andOrStartIndex), mdc);
                if (filter != null)
                {
                    filter._Ao = lastAnd ? Ao.And : Ao.Or;
                    tFilterList.Add(filter);
                }
                if (tFilterList.Count > 2 && needGroup2)
                {
                    int okflag = -1;
                    for (int i = 0; i < tFilterList.Count; i++)
                    {
                        if (okflag == -1 && tFilterList[i]._Ao == Ao.Or)
                        {
                            i--;//返回上一个索引1,2,3
                            okflag = i;
                        }
                        if (okflag != -1)
                        {
                            group2.Add(tFilterList[i]);
                        }
                    }
                    tFilterList.RemoveRange(okflag, tFilterList.Count - okflag);
                }
            }
            // string firstFilter=whereStr.su


            return tFilterList;
        }
        /// <summary>
        /// SQL操作符号
        /// </summary>
        private static Dictionary<string, Op> Ops
        {
            get
            {
                Dictionary<string, Op> _ops = new Dictionary<string, Op>(StringComparer.OrdinalIgnoreCase);
                _ops.Add("<>", Op.NotEqual);
                _ops.Add(">=", Op.BigEqual);
                _ops.Add("<=", Op.SmallEqual);
                _ops.Add("=", Op.Equal);
                _ops.Add(">", Op.Big);
                _ops.Add("<", Op.Small);
                _ops.Add(" not like ", Op.NotLike);
                _ops.Add(" like ", Op.Like);
                _ops.Add(" is null", Op.IsNull);
                _ops.Add(" is not null", Op.IsNotNull);
                _ops.Add(" not in", Op.NotIn);//not int 顺序要靠前。
                _ops.Add(" in", Op.In);
                return _ops;
            }
        }
        /// <summary>
        /// 单个条件
        /// </summary>
        private static TFilter GetSingleTFilter(string where, MDataColumn mdc)
        {
            //id like 'a>b=c'
            //id>'a like b'
            where = where.TrimStart('(').TrimEnd(')').Trim();
            int quoteIndex = where.IndexOf('\'');
            quoteIndex = quoteIndex == -1 ? 0 : quoteIndex;
            TFilter tFilter = null;
            foreach (KeyValuePair<string, Op> opItem in Ops)
            {
                if (GetTFilterOk(where, quoteIndex, opItem.Key, opItem.Value, mdc, out tFilter))
                {
                    break;
                }
            }
            return tFilter;
        }
        /// <summary>
        /// 条件比较
        /// </summary>
        private static bool GetTFilterOk(string where, int quoteIndex, string sign, Op op, MDataColumn mdc, out TFilter tFilter)
        {
            bool result = false;
            tFilter = null;
            int index = where.ToLower().IndexOf(sign, 0, quoteIndex > 0 ? quoteIndex : where.Length);
            if (index > 0)
            {
                string columnAName = where.Substring(0, index).Trim();
                int columnAIndex = mdc.GetIndex(columnAName);
                string valueB = where.Substring(index + sign.Length).Trim(' ', '\'').Replace("''", "'");
                if (op == Op.In || op == Op.NotIn)
                {
                    valueB = ',' + valueB.TrimStart('(', ')').Replace("'", "") + ",";//去除单引号。
                }
                int columnBIndex = -1;
                if (quoteIndex == 0 && mdc.Contains(valueB)) //判断右侧的是否列名。
                {
                    columnBIndex = mdc.GetIndex(valueB);
                }
                tFilter = new TFilter(Ao.None, columnAName, columnAIndex, op, valueB, columnBIndex);
                if (columnBIndex == -1 && !string.IsNullOrEmpty(Convert.ToString(valueB)))//右侧是值类型，转换值的类型。
                {
                    if (columnAIndex > -1 && DataType.GetGroup(mdc[columnAIndex].SqlType) == 3)//bool型
                    {
                        switch (Convert.ToString(tFilter._valueB).ToLower())
                        {
                            case "true":
                            case "1":
                            case "on":
                                tFilter._valueB = true;
                                break;
                            case "false":
                            case "0":
                            case "":
                                tFilter._valueB = false;
                                break;
                            default:
                                tFilter._valueB = null;
                                break;
                        }
                    }
                    else
                    {
                        try
                        {
                            tFilter._valueB = ConvertTool.ChangeType(tFilter._valueB, columnAIndex > -1 ? typeof(string) : mdc[columnAIndex].ValueType);
                        }
                        catch
                        {
                        }
                    }
                }
                result = true;

            }
            return result;
        }
        /// <summary>
        /// 多条件下值比较
        /// </summary>
        private static bool CompareMore(MDataRow row, List<TFilter> filters)
        {
            if (row == null) { return false; }
            bool result = false;

            MDataCell cell = null, otherCell = null;
            SqlDbType sqlDbType = SqlDbType.Int;
            //单个条件比较的结果
            bool moreResult = false;
            object valueA = null, valueB = null;
            foreach (TFilter item in filters)
            {
                if (item._Op == Op.None || item._columnAIndex == -1)
                {
                    moreResult = true;//如果条件的列不属于当前表，直接忽略该条件
                }
                else
                {
                    //if (item._columnAIndex == -1)
                    //{
                    //    moreResult = true;
                    //    continue;
                    //    //valueA = item._valueA;
                    //    //sqlDbType = SqlDbType.NVarChar;
                    //}
                    //else
                    //{
                    cell = row[item._columnAIndex];
                    valueA = cell.Value;
                    sqlDbType = cell.Struct.SqlType;
                    //}
                    #region MyRegion
                    if (item._columnBIndex > -1)
                    {
                        otherCell = row[item._columnBIndex];
                        valueB = otherCell.Value;
                        if (DataType.GetGroup(sqlDbType) != DataType.GetGroup(otherCell.Struct.SqlType))
                        {
                            sqlDbType = SqlDbType.NVarChar;//不同类型的比较，转成字符串比较。
                        }
                    }
                    else
                    {
                        valueB = item._valueB;
                    }
                    switch (item._Op)
                    {
                        case Op.IsNull:
                            moreResult = cell != null && cell.IsNull;
                            break;
                        case Op.IsNotNull:
                            moreResult = cell != null && !cell.IsNull;
                            break;
                        default:
                            if (Convert.ToString(valueA) == "" || Convert.ToString(valueB) == "")//空格的问题，稍后回来处理。
                            {
                                #region MyRegion
                                int a = valueA == null ? 1 : (Convert.ToString(valueA) == "" ? 2 : 3);
                                int b = valueB == null ? 1 : (Convert.ToString(valueB) == "" ? 2 : 3);
                                switch (item._Op)
                                {
                                    case Op.Big:
                                        moreResult = a > b;
                                        break;
                                    case Op.Like:
                                    case Op.Equal:
                                        moreResult = a == b;
                                        break;
                                    case Op.SmallEqual:
                                        moreResult = a <= b;
                                        break;
                                    case Op.BigEqual:
                                        moreResult = a >= b;
                                        break;
                                    case Op.Small:
                                        moreResult = a < b;
                                        break;
                                    case Op.NotLike:
                                    case Op.NotEqual:
                                        moreResult = a != b;
                                        break;
                                        //case Op.In:

                                        //    break;
                                        //case Op.NotIn:
                                        //    break;
                                }
                                #endregion
                            }
                            else
                            {
                                moreResult = Compare(sqlDbType, valueA, item._Op, valueB);
                            }
                            break;
                    }
                    #endregion

                }
                switch (item._Ao)
                {
                    case Ao.And:
                        result = result && moreResult;
                        if (!result)
                        {
                            break;
                        }
                        break;
                    case Ao.Or:
                        result = result || moreResult;
                        break;
                    default:
                        result = moreResult;
                        break;
                }

            }

            return result;
        }
        private static bool Compare(SqlDbType sqlType, object valueA, Op op, object valueB)
        {
            try
            {
                switch (op)
                {
                    case Op.Big:
                    case Op.BigEqual:
                        switch (DataType.GetGroup(sqlType))
                        {
                            case 1://int
                                return op == Op.Big ? Convert.ToDecimal(valueA) > Convert.ToDecimal(valueB) : Convert.ToDecimal(valueA) >= Convert.ToDecimal(valueB);
                            case 2://datetime
                                return op == Op.Big ? Convert.ToDateTime(valueA) > Convert.ToDateTime(valueB) : Convert.ToDateTime(valueA) >= Convert.ToDateTime(valueB);
                            default:
                                int value = Convert.ToString(valueA).CompareTo(valueB);
                                return op == Op.Big ? value == 1 : value > -1;
                        }

                    case Op.Equal:
                        return string.Compare(Convert.ToString(valueA).TrimEnd(), Convert.ToString(valueB).TrimEnd(), true) == 0;
                    case Op.NotEqual:
                        return string.Compare(Convert.ToString(valueA).TrimEnd(), Convert.ToString(valueB).TrimEnd(), true) != 0;
                    case Op.Small:
                    case Op.SmallEqual:
                        switch (DataType.GetGroup(sqlType))
                        {
                            case 1://int
                                return op == Op.Small ? Convert.ToDecimal(valueA) < Convert.ToDecimal(valueB) : Convert.ToDecimal(valueA) <= Convert.ToDecimal(valueB);
                            case 2://datetime
                                return op == Op.Small ? Convert.ToDateTime(valueA) < Convert.ToDateTime(valueB) : Convert.ToDateTime(valueA) <= Convert.ToDateTime(valueB);
                            default:
                                int value = Convert.ToString(valueA).CompareTo(valueB);
                                return op == Op.Small ? value == -1 : value <= 0;
                        }
                    case Op.Like:
                    case Op.NotLike:
                        bool result = false;
                        string bValue = Convert.ToString(valueB);
                        if (!bValue.StartsWith("%"))
                        {
                            result = Convert.ToString(valueA).StartsWith(bValue.Trim('%'), StringComparison.OrdinalIgnoreCase);
                        }
                        else if (!bValue.EndsWith("%")) //'123    ' like '123%'
                        {
                            result = Convert.ToString(valueA).TrimEnd().EndsWith(bValue.Trim('%'), StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            result = Convert.ToString(valueA).IndexOf(bValue.Trim('%'), StringComparison.OrdinalIgnoreCase) > -1;
                        }
                        return op == Op.Like ? result : !result;
                    case Op.In:
                        return Convert.ToString(valueB).Contains(',' + Convert.ToString(valueA) + ",");
                    case Op.NotIn:
                        return !Convert.ToString(valueB).Contains(',' + Convert.ToString(valueA) + ",");
                }
                return false;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// 分隔where条件中order by 出来
        /// </summary>
        /// <param name="where"></param>
        private static void SplitWhereOrderby(ref string where, out string orderby)
        {
            orderby = string.Empty;
            if (!string.IsNullOrEmpty(where))
            {
                int orderbyIndex = where.IndexOf("order by", StringComparison.OrdinalIgnoreCase);
                if (orderbyIndex > -1)
                {
                    orderby = where.Substring(orderbyIndex);
                    if (orderbyIndex > 0)
                    {
                        where = where.Substring(0, orderbyIndex - 1);//-1是去掉空格
                    }
                    else
                    {
                        where = string.Empty;
                    }

                }
            }
        }

        private static void FilterPager(MDataRowCollection rows, int pageIndex, int pageSize)
        {
            if (pageIndex > -1 && pageSize > 0) //分页处理返回
            {
                pageIndex = pageIndex == 0 ? 1 : pageIndex;
                int start = (pageIndex - 1) * pageSize;//从第几条开始查(索引)
                int end = start + pageSize;//查到第N条结束(索引)。
                int rowCount = rows.Count;
                if (rowCount > end)//总数>N
                {
                    //rows.re
                    rows.RemoveRange(end, rowCount - end);//移除尾数
                }
                if (start > 0)//总数>起即
                {
                    if (rowCount > start)
                    {
                        rows.RemoveRange(0, start);//移除起始数
                    }
                    else// if (rowCount < start) //查询的超出总数
                    {
                        rows.Clear();//直接清空返回
                    }
                }
            }
        }

        /// <summary>
        /// 只保留查询的列
        /// </summary>
        private static void FilterColumns(ref MDataTable table, params object[] selectColumns)
        {
            if (selectColumns != null && selectColumns.Length > 0)
            {
                #region 列移除
                bool contain = false;
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    contain = false;
                    foreach (object columnName in selectColumns)
                    {
                        string[] items = Convert.ToString(columnName).Split(' ');//a as b
                        if (string.Compare(table.Columns[i].ColumnName, items[0], true) == 0)
                        {
                            contain = true;
                            if (items.Length > 1)
                            {
                                table.Columns[i].ColumnName = items[items.Length - 1];//修改列名
                            }
                            break;
                        }
                    }
                    if (!contain)
                    {
                        table.Columns.RemoveAt(i);
                        i--;
                    }
                }
                #endregion
            }
        }
    }
}
