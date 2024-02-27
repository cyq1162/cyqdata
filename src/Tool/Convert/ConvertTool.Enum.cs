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
        internal static object ToEnum(object value, Type t, bool isGenericType)
        {
            if (value is Enum) { return value; }
            string strValue = Convert.ToString(value);
            if (strValue != "")
            {
                if (Enum.IsDefined(t, strValue))
                {
                    return Enum.Parse(t, strValue);
                }
                int v = 0;
                if (int.TryParse(strValue, out v))
                {
                    object v1 = Enum.Parse(t, strValue);
                    if (v1.ToString() != strValue)
                    {
                        return v1;
                    }
                }
                string[] names = Enum.GetNames(t);
                string lower = strValue.ToLower();
                foreach (string name in names)
                {
                    if (name.ToLower() == lower)
                    {
                        return Enum.Parse(t, name);
                    }
                }

            }
            if (isGenericType) { return null; }
            //取第一个值。
            string firstKey = Enum.GetName(t, -1);
            if (!string.IsNullOrEmpty(firstKey))
            {
                return Enum.Parse(t, firstKey);
            }
            return Enum.Parse(t, Enum.GetNames(t)[0]);
        }
    }
}
