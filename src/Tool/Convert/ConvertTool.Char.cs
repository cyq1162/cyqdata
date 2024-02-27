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
        internal static object ToChar(object value, bool isGenericType)
        {
            if (value is Char) { return value; }
            if (value != null)
            {
                if (value is Enum) { value = (int)value; }
                char result;
                string strValue = Convert.ToString(value);
                if (char.TryParse(strValue, out result))
                {
                    return result;
                }
            }
            if (isGenericType) { return null; }
            return '\0';
        }
    }
}
