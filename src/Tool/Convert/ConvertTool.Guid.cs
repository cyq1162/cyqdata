﻿using CYQ.Data.Json;
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
        internal static object ToGuid(object value, bool isGenericType)
        {
            if (value is Guid) { return value; }
            string strValue = Convert.ToString(value);
            if (strValue.Length == 36)
            {
                return new Guid(strValue);
            }
            if (strValue == SqlValue.Guid || strValue.StartsWith("newid"))
            {
                return Guid.NewGuid();
            }
            if (isGenericType) { return null; }
            return Guid.Empty;
        }
    }
}
