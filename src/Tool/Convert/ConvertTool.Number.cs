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
        internal static object ToSByte(object value, bool isGenericType)
        {
            if (value is sbyte) { return value; }
            if (value is Enum) { return (sbyte)(int)value; }
            sbyte result = 0;
            if (value != null)
            {
                string strValue = Convert.ToString(value).Split('.')[0];
                if (sbyte.TryParse(strValue, out result))
                {
                    return result;
                }
            }
            if (isGenericType) { return null; }
            return result;
        }
        internal static object ToByte(object value, bool isGenericType)
        {
            if (value is byte) { return value; }
            if (value is Enum) { return (byte)(int)value; }
            byte result = 0;
            if (value != null)
            {
                string strValue = Convert.ToString(value).Split('.')[0];
                if (byte.TryParse(strValue, out result))
                {
                    return result;
                }
            }
            if (isGenericType) { return null; }
            return result;
        }

        internal static object ToUInt16(object value, bool isGenericType)
        {
            if (value is ushort) { return value; }
            if (value is Enum) { return (ushort)(int)value; }
            ushort result = 0;
            if (value != null)
            {
                string strValue = Convert.ToString(value).Split('.')[0];
                if (ushort.TryParse(strValue, out result))
                {
                    return result;
                }
            }
            if (isGenericType) { return null; }
            return result;
        }
        internal static object ToInt16(object value, bool isGenericType)
        {
            if (value is short) { return value; }
            if (value is Enum) { return (short)(int)value; }
            short result = 0;
            if (value != null)
            {
                string strValue = Convert.ToString(value).Split('.')[0];
                if (short.TryParse(strValue, out result))
                {
                    return result;
                }
            }
            if (isGenericType) { return null; }
            return result;
        }

        internal static object ToUInt32(object value, bool isGenericType)
        {
            if (value is uint) { return value; }
            if (value is Enum) { return (uint)value; }
            uint result = 0;
            if (value != null)
            {
                string strValue = Convert.ToString(value).Split('.')[0];
                if (uint.TryParse(strValue, out result))
                {
                    return result;
                }
            }
            if (isGenericType) { return null; }
            return result;
        }

        internal static object ToInt32(object value, bool isGenericType)
        {
            if (value is int) { return value; }
            if (value is Enum) { return (int)value; }
            int result = 0;
            if (value != null)
            {
                string strValue = Convert.ToString(value).Split('.')[0];
                if (int.TryParse(strValue, out result))
                {
                    return result;
                }
            }
            if (isGenericType) { return null; }
            return result;
        }

        internal static object ToUInt64(object value, bool isGenericType)
        {
            if (value is ulong) { return value; }
            if (value is Enum) { return (ulong)(int)value; }
            ulong result = 0;
            if (value != null)
            {
                string strValue = Convert.ToString(value).Split('.')[0];
                if (ulong.TryParse(strValue, out result))
                {
                    return result;
                }
            }
            if (isGenericType) { return null; }
            return result;
        }
        internal static object ToInt64(object value, bool isGenericType)
        {
            if (value is long) { return value; }
            if (value is Enum) { return (long)(int)value; }
            long result = 0;
            if (value != null)
            {
                string strValue = Convert.ToString(value).Split('.')[0];
                if (long.TryParse(strValue, out result))
                {
                    return result;
                }
            }
            if (isGenericType) { return null; }
            return result;
        }

        internal static object ToSingle(object value, bool isGenericType)
        {
            if (value is float) { return value; }
            if (value is Enum) { return (float)(int)value; }
            float result = 0;
            if (value != null)
            {
                string strValue = Convert.ToString(value);
                if (float.TryParse(strValue, out result))
                {
                    return result;
                }
                switch (strValue)
                {
                    case "infinity":
                    case "正无穷大":
                        return float.PositiveInfinity;
                    case "-infinity":
                    case "负无穷大":
                        return float.NegativeInfinity;
                }
            }
            if (isGenericType) { return null; }
            return result;
        }
        internal static object ToDouble(object value, bool isGenericType)
        {
            if (value is double) { return value; }
            if (value is Enum) { return (double)(int)value; }
            double result = 0;
            if (value != null)
            {
                string strValue = Convert.ToString(value);
                if (double.TryParse(strValue, out result))
                {
                    return result;
                }
                switch (strValue)
                {
                    case "infinity":
                    case "正无穷大":
                        return double.PositiveInfinity;
                    case "-infinity":
                    case "负无穷大":
                        return double.NegativeInfinity;
                }
            }
            if (isGenericType) { return null; }
            return result;
        }

        internal static object ToDecimal(object value, bool isGenericType)
        {
            if (value is decimal) { return value; }
            if (value is Enum) { return (decimal)(int)value; }
            decimal result = 0;
            if (value != null)
            {
                string strValue = Convert.ToString(value);
                if (decimal.TryParse(strValue, out result))
                {
                    return result;
                }
            }
            if (isGenericType) { return null; }
            return result;
        }
    }
}
