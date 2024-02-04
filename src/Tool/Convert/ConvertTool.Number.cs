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
        internal static object ToByte(object value)
        {
            if (value is byte) { return value; }
            if (value is Enum) { return (byte)(int)value; }
            if (value != null)
            {
                byte result = 0;
                string strValue = Convert.ToString(value);
                if (byte.TryParse(strValue, out result))
                {
                    return result;
                }
            }
            return 0;
        }
        internal static object ToInt16(object value)
        {
            if (value is short) { return value; }
            if (value is Enum) { return (short)(int)value; }
            if (value != null)
            {
                short result = 0;
                string strValue = Convert.ToString(value).Split('.')[0];
                if (short.TryParse(strValue, out result))
                {
                    return result;
                }
            }
            return 0;
        }
        internal static object ToInt32(object value)
        {
            if (value is int) { return value; }
            if (value is Enum) { return (int)value; }
            if (value != null)
            {
                int result = 0;
                string strValue = Convert.ToString(value).Split('.')[0];
                if (int.TryParse(strValue, out result))
                {
                    return result;
                }
            }
            return 0;
        }
        internal static object ToInt64(object value)
        {
            if (value is long) { return value; }
            if (value is Enum) { return (long)(int)value; }
            if (value != null)
            {
                long result = 0;
                string strValue = Convert.ToString(value).Split('.')[0];
                if (long.TryParse(strValue, out result))
                {
                    return result;
                }
            }
            return 0;
        }

        internal static object ToSingle(object value)
        {
            if (value is float) { return value; }
            if (value is Enum) { return (float)(int)value; }
            if (value != null)
            {
                float result = 0;
                string strValue = Convert.ToString(value);
                if (float.TryParse(strValue, out result))
                {
                    return result;
                }
                switch(strValue)
                {
                    case "infinity":
                    case "正无穷大":
                        return float.PositiveInfinity;
                    case "-infinity":
                    case "负无穷大":
                        return float.NegativeInfinity;
                }
            }
            return 0;
        }
        internal static object ToDouble(object value)
        {
            if (value is double) { return value; }
            if (value is Enum) { return (double)(int)value; }
            if (value != null)
            {
                double result = 0;
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
            return 0;
        }

        internal static object ToDecimal(object value)
        {
            if (value is decimal) { return value; }
            if (value is Enum) { return (decimal)(int)value; }
            if (value != null)
            {
                decimal result = 0;
                string strValue = Convert.ToString(value);
                if (decimal.TryParse(strValue, out result))
                {
                    return result;
                }
            }
            return 0;
        }
    }
}
