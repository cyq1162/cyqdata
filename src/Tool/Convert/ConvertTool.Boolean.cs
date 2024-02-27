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
        internal static object ToBoolean(object value, bool isGenericType)
        {
            if (value is Boolean) { return value; }
            string strValue = Convert.ToString(value).Trim('\r', '\n', '\t', ' ');

            switch (strValue.ToLower())
            {
                case "yes":
                case "true":
                case "success":
                case "1":
                case "on":
                case "ok":
                case "是":
                case "√":
                    return true;
                case "no":
                case "false":
                case "fail":
                case "0":
                case "off":
                case "not":
                case "否":
                case "×":
                default:
                    if (isGenericType) { return null; }
                    return false;
            }
        }
    }
}
