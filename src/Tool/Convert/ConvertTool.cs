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
            if (value == null)
            {
                return t.IsValueType ? Activator.CreateInstance(t) : null;
            }
            //对基础类型进行单独处理，提升转化性能。
            switch (t.Name)
            {
                case "SByte":
                    return ToSByte(value);
                case "Byte":
                    return ToByte(value);
                case "UInt16":
                    return ToUInt16(value);
                case "Int16":
                    return ToInt16(value);
                case "UInt32":
                    return ToUInt32(value);
                case "Int32":
                    return ToInt32(value);
                case "UInt64":
                    return ToUInt64(value);
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
            if (t.FullName.StartsWith("System."))
            {
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
                if (t.FullName == "System.Text.StringBuilder")
                {
                    return value as StringBuilder;
                }
                if (t.FullName == "System.Text.Encoding")
                {
                    return value as Encoding;
                }
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

                if (valueType.FullName != t.FullName)
                {
                    if ((strValue.StartsWith("{") || strValue.StartsWith("[")) && (strValue.EndsWith("}") || strValue.EndsWith("]")))
                    {
                        return JsonHelper.ToEntity(t, strValue, EscapeOp.No);
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

        internal static object GetObj(Type toType, object objValue)
        {
            if (objValue == null) { return null; }
            Type returnType = toType;
            string value = Convert.ToString(objValue);
            object returnObj = null;
            SysType sysType = ReflectTool.GetSystemType(ref returnType);
            switch (sysType)
            {
                case SysType.Enum:
                    returnObj = ConvertTool.ChangeType(objValue, toType);// Enum.Parse(propType, value);
                    break;
                case SysType.Base:
                    #region 基础类型处理
                    if (returnType.Name == "String")
                    {
                        //去掉转义符号
                        if (value.IndexOf("\\\"") > -1)
                        {
                            returnObj = value.Replace("\\\"", "\"");
                        }
                        else
                        {
                            returnObj = value;
                        }
                    }
                    else
                    {
                        returnObj = ConvertTool.ChangeType(value, returnType);
                    }
                    #endregion
                    break;
                case SysType.Array:
                case SysType.Collection:
                case SysType.Generic:
                    #region 数组处理
                    if (objValue.GetType() == returnType)
                    {
                        returnObj = objValue;
                    }
                    else
                    {
                        Type[] argTypes = null;
                        int len = ReflectTool.GetArgumentLength(ref returnType, out argTypes);
                        if (len == 1) // Table
                        {

                            if (value.Contains(":") && value.Contains("{"))
                            {
                                #region Json 嵌套处理，复杂数组处理。
                                //returnObj = JsonHelper.ToEntity(returnType, value, EscapeOp.No);
                                MDataTable dt = MDataTable.CreateFrom(value);//, SchemaCreate.GetColumns(argTypes[0])
                                returnObj = dt.ToList(returnType);
                                dt = null;
                                #endregion
                            }
                            else
                            {
                                #region 单纯的基础类型数组处理["xxx","xxx2","xx3"]
                                List<string> items = JsonSplit.SplitEscapeArray(value);//内部去掉转义符号
                                if (items == null) { return null; }
                                returnObj = Activator.CreateInstance(returnType, items.Count);//创建实例
                                //Type objListType = returnObj.GetType();
                                bool isArray = sysType == SysType.Array;
                                MethodInfo method;
                                if (isArray)
                                {
                                    method = returnType.GetMethod("Set");
                                    if (method != null)
                                    {
                                        for (int i = 0; i < items.Count; i++)
                                        {
                                            Object item = ConvertTool.ChangeType(items[i], returnType.GetElementType());//Type.GetType(propType.FullName.Replace("[]", "")
                                            method.Invoke(returnObj, new object[] { i, item });
                                        }
                                    }
                                }
                                else
                                {
                                    method = returnType.GetMethod("Add");
                                    if (method == null)
                                    {
                                        method = returnType.GetMethod("Push");
                                    }
                                    if (method != null)
                                    {
                                        for (int i = 0; i < items.Count; i++)
                                        {
                                            Object item = ConvertTool.ChangeType(items[i], argTypes[0]);
                                            method.Invoke(returnObj, new object[] { item });
                                        }

                                    }
                                }


                                #endregion
                            }
                        }
                        else if (len == 2) // row
                        {
                            MDataRow mRow = MDataRow.CreateFrom(objValue, argTypes[1]);
                            returnObj = returnType.Name.Contains("Dictionary") ? Activator.CreateInstance(returnType, StringComparer.OrdinalIgnoreCase) : Activator.CreateInstance(returnType);
                            //Activator.CreateInstance(propType, mRow.Columns.Count);//创建实例
                            Type objListType = returnObj.GetType();
                            MethodInfo mi = objListType.GetMethod("Add");
                            foreach (MDataCell mCell in mRow)
                            {
                                object mObj = GetValue(mCell.ToRow(), argTypes[1]);
                                mi.Invoke(returnObj, new object[] { mCell.ColumnName, mObj });
                            }
                            mRow = null;
                        }
                    }
                    #endregion
                    break;
                case SysType.Custom://继续递归
                    MDataRow mr = null;
                    if (objValue is MDataRow)
                    {
                        mr = objValue as MDataRow;
                    }
                    else
                    {
                        mr = new MDataRow(TableSchema.GetColumnByType(returnType));
                        mr.LoadFrom(objValue);
                    }
                    returnObj = mr.ToEntity(returnType);
                    //returnObj = Activator.CreateInstance(returnType);
                    //SetToEntity(ref returnObj, mr);
                    //mr = null;
                    break;

            }
            return returnObj;
        }

        private static object GetValue(MDataRow row, Type type)
        {
            switch (ReflectTool.GetSystemType(ref type))
            {
                case SysType.Base:
                    return ConvertTool.ChangeType(row[0].Value, type);
                case SysType.Enum:
                    return Enum.Parse(type, row[0].ToString());
                default:
                    return row.ToEntity(type);
                //object o = Activator.CreateInstance(type);
                //SetToEntity(ref o, row);
                //return o;
            }
        }
    }
}
