using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.SQL;
using CYQ.Data.Extension;

namespace CYQ.Data.SyntaxExtended
{
    /// <summary>
    /// 通用型Sql函数,能解析到各数据库执行
    /// </summary>
    public class SqlFunc
    {
        #region 函数方法
        public static MField Len(object filed)
        {
            return new MField(SqlValue.Len + "(" + filed + ")", true);
        }
        public static MField Substring(object filed, int start, int length)
        {
            return new MField(SqlValue.Substring + "(" + filed+ "," + start + "," + length + ")", false);
        }
        public static MField Year(object filed)
        {
            return new MField(SqlValue.Year + "(" + filed+ ")", true);
        }
        public static MField Month(object filed)
        {
            return new MField(SqlValue.Month + "(" + filed + ")", true);
        }
        public static MField Day(object filed)
        {
            return new MField(SqlValue.Day + "(" + filed+ ")", true);
        }
        public static MField CharIndex(string findChars, object inField)
        {
            return new MField(SqlValue.CharIndex + "(" + findChars + "," + inField + ")", true);
        }
        public static MField DateDiff(string dateOp, object fromTime, object endTime)
        {
            return new MField(SqlValue.DateDiff + "(" + dateOp + "," + fromTime + "," + endTime + ")", true);
        }
        #endregion


        #region SQL语句
        public static string OrderBy(object filed, string ascOrdesc, params object[] moreOrder)
        {
            string orderby = " order by [" + ((filed is IField) ? ((IField)filed).Name : Convert.ToString(filed)) + "] " + ascOrdesc;
            if (moreOrder.Length > 0)
            {
                for (int i = 0; i < moreOrder.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        orderby += ",[" + ((moreOrder[i] is IField) ? ((IField)moreOrder[i]).Name : Convert.ToString(moreOrder[i])) + "] ";
                    }
                    else
                    {
                        orderby += Convert.ToString(moreOrder[i]);
                    }
                }
            }
            return orderby;
        }
        #endregion
    }
  
    /// <summary>
    /// DateDiff函数的常量值
    /// </summary>
    public class DateOp
    {
        public const string year = "[#yyyy]";
        public const string quarter = "[#q]";
        public const string month = "[#m]";
        public const string dayofyear = "[#y]";
        public const string day = "[#d]";
        public const string hour = "[#h]";
        public const string week = "[#ww]";
        public const string minute = "[#n]";
        public const string second = "[#s]";
    }
}
