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
        /// <summary>
        /// 类型转换(精准强大)
        /// </summary>
        public static T ChangeType<T>(object value)
        {
            return (T)ChangeType(value, typeof(T));
        }

        /// <summary>
        /// 类型转换(精准强大)
        /// </summary>
        /// <param name="value">值处理</param>
        /// <param name="t">类型</param>
        /// <returns></returns>
        public static object ChangeType(object value, Type t)
        {
            if (t == null) { return null; }
            //对基础类型进行单独处理，提升转化性能。
            switch (t.Name)
            {
                case "Byte":
                    return ToByte(value);
                case "Int16":
                    return ToInt16(value);
                case "Int32":
                    return ToInt32(value);
                case "Int64":
                    return ToInt64(value);
                case "String":
                    return ToString(value);
                case "DateTime":
                    return ToDateTime(value);
                case "Boolean":
                    return ToBoolean(value);
                case "Guid":
                    return ToGuid(value);
                case "Single":
                    return ToSingle(value);
                case "Double":
                    return ToDouble(value);
                case "Decimal":
                    return ToDecimal(value);
            }



            if (t.IsEnum)
            {
                return ToEnum(value, t);
            }
            if (value == null)
            {
                return t.IsValueType ? Activator.CreateInstance(t) : null;
            }
            if (t.FullName == "System.Object")
            {
                return value;
            }
            if (t.FullName == "System.Type")
            {
                return (Type)value;
            }
            if (t.FullName == "System.IO.Stream" && value is HttpPostedFile)
            {
                return ((HttpPostedFile)value).InputStream;
            }

            string strValue = Convert.ToString(value);
            if (t.IsGenericType && t.Name.StartsWith("Nullable"))
            {
                t = Nullable.GetUnderlyingType(t);
                if (strValue == "")
                {
                    return null;
                }
            }

            if (t.FullName == "System.Text.StringBuilder")
            {
                return value as StringBuilder;
            }
            if (t.FullName == "System.Text.Encoding")
            {
                return value as Encoding;
            }
            if (strValue.Trim() == "")
            {
                if (t.Name.EndsWith("[]")) { return null; }
                return Activator.CreateInstance(t);
            }

            if (t.IsValueType)
            {
                strValue = strValue.Trim('\r', '\n', '\t', ' ');
                return Convert.ChangeType(strValue, t);
            }
            else
            {
                Type valueType = value.GetType();
                //if(valueType.IsEnum && t.is)

                if (valueType.FullName != t.FullName)
                {
                    if ((strValue.StartsWith("{") || strValue.StartsWith("[")) && (strValue.EndsWith("}") || strValue.EndsWith("]")))
                    {
                        return JsonHelper.ToEntity(t, strValue, EscapeOp.Default);
                    }
                    switch (ReflectTool.GetSystemType(ref t))
                    {
                        case SysType.Custom:
                            return MDataRow.CreateFrom(value).ToEntity(t);
                        case SysType.Collection:
                            return MDataTable.CreateFrom(value).ToList(t);
                        case SysType.Generic:
                            if (t.Name.StartsWith("List") || t.Name.StartsWith("IList") || t.Name.StartsWith("MList"))
                            {
                                return MDataTable.CreateFrom(value).ToList(t);
                            }
                            return MDataRow.CreateFrom(value).ToEntity(t);
                        case SysType.Array:
                            if (t.Name == "Byte[]")
                            {
                                if (valueType.Name == "String")
                                {
                                    return Convert.FromBase64String(strValue);
                                }
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    new BinaryFormatter().Serialize(ms, value);
                                    return ms.ToArray();
                                }
                            }
                            break;
                    }
                }
                return Convert.ChangeType(value, t);
            }
        }


    }
}
