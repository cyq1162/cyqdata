using CYQ.Data.Json;
using CYQ.Data.SQL;
using CYQ.Data.Table;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Web;

namespace CYQ.Data.Tool
{
    /// <summary>
    /// 类型转换（支持json转实体）
    /// </summary>
    public static partial class ConvertTool
    {
        internal static object ToDateTime(object value)
        {
            if (value is DateTime) { return value; }
            if (value != null)
            {
                string strValue = Convert.ToString(value).Trim('\r', '\n', '\t', ' ');
                DateTime dt;
                if (DateTime.TryParse(strValue, out dt))
                {
                    return dt;
                }
                switch (strValue.ToLower().TrimEnd(')', '('))
                {
                    case "now":
                    case "getdate":
                    case "current_timestamp":
                        return DateTime.Now;
                }
            }
            return (DateTime)SqlDateTime.MinValue;
        }
    }
}
