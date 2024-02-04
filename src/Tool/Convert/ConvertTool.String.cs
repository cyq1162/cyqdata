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
        internal static object ToString(object value)
        {
            if (value is string) { return value; }
            if (value is byte[])
            {
                return Convert.ToBase64String((byte[])value);
            }
            return Convert.ToString(value);
        }
    }
}
