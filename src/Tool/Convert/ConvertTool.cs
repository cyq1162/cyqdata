using CYQ.Data.Emit;
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
                return (t.IsValueType && !t.IsGenericType) ? Activator.CreateInstance(t) : null;
            }
            if (t.IsValueType)
            {
                #region 值类型处理
                bool isGenericType = t.IsGenericType;
                if (isGenericType && t.Name.StartsWith("Nullable"))
                {
                    t = Nullable.GetUnderlyingType(t);
                }
                //对基础类型进行单独处理，提升转化性能。
                switch (t.Name)
                {
                    case "Char":
                        return ToChar(value, isGenericType);
                    case "SByte":
                        return ToSByte(value, isGenericType);
                    case "Byte":
                        return ToByte(value, isGenericType);
                    case "UInt16":
                        return ToUInt16(value, isGenericType);
                    case "Int16":
                        return ToInt16(value, isGenericType);
                    case "UInt32":
                        return ToUInt32(value, isGenericType);
                    case "Int32":
                        return ToInt32(value, isGenericType);
                    case "UInt64":
                        return ToUInt64(value, isGenericType);
                    case "Int64":
                        return ToInt64(value, isGenericType);
                    case "DateTime":
                        return ToDateTime(value, isGenericType);
                    case "Boolean":
                        return ToBoolean(value, isGenericType);
                    case "Guid":
                        return ToGuid(value, isGenericType);
                    case "Single":
                        return ToSingle(value, isGenericType);
                    case "Double":
                        return ToDouble(value, isGenericType);
                    case "Decimal":
                        return ToDecimal(value, isGenericType);
                    default:
                        if (t.IsEnum)
                        {
                            return ToEnum(value, t, isGenericType);
                        }
                        //值类型：只剩下结构体
                        if (t == value.GetType()) { return value; }
                        if (isGenericType && t.Name.StartsWith("Nullable"))
                        {
                            return null;
                        }
                        return Activator.CreateInstance(t);
                }
                #endregion
            }
            #region 引用类型处理
            if (t.FullName.StartsWith("System."))
            {
                switch (t.Name)
                {
                    case "String":
                        return ToString(value);
                    case "Object":
                        return value;
                    case "Type":
                        return value as Type;
                    case "StringBuilder":
                        if (value is StringBuilder)
                        {
                            return value as StringBuilder;
                        }
                        return new StringBuilder(Convert.ToString(value));
                    case "Encoding":
                        return value as Encoding;
                    case "Stream":
                        if (value is HttpPostedFile)
                        {
                            return ((HttpPostedFile)value).InputStream;
                        }
                        return value as Stream;
                    case "Byte[]":
                        if (value is Byte[]) { return value; }
                        if (value is string)
                        {
                            return Convert.FromBase64String(value.ToString());
                        }
                        using (MemoryStream ms = new MemoryStream())
                        {
                            new BinaryFormatter().Serialize(ms, value);
                            return ms.ToArray();
                        }
                }
            }
            if (value is ValueType) { return null; }
            if (value is string)
            {
                string strValue = value as string;
                bool isJson = (strValue.StartsWith("{") && strValue.EndsWith("}")) || (strValue.StartsWith("[") && strValue.EndsWith("]"));
                if (isJson)
                {
                    return JsonHelper.ToEntity(t, strValue, EscapeOp.No);
                }
                else if (strValue.Contains("="))
                {
                    return MDataRow.CreateFrom(value).ToEntity(t);
                }
                return null;
            }
            if (value is MDataRow)
            {
                return (value as MDataRow).ToEntity(t);
            }

            Type valueType = value.GetType();
            if (valueType == t) { return value; }

            switch (ReflectTool.GetSystemType(ref t))
            {
                case SysType.Custom:
                    return MDataRow.CreateFrom(value).ToEntity(t);
                case SysType.Collection:
                case SysType.Generic:
                case SysType.Array:
                    int len = ReflectTool.GetArgumentLength(ref t);
                    if (len == 1) // Table
                    {
                        return MDataTable.CreateFrom(value).ToList(t);
                    }
                    return MDataRow.CreateFrom(value).ToEntity(t);
            }

            return null;
            #endregion
        }
        /* 终于把这个给消灭了。
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
                    returnObj = ChangeType(objValue, toType);// Enum.Parse(propType, value);
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
                        returnObj = ChangeType(value, returnType);
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
                                var func = ListStringToList.Delegate(returnType, argTypes[0]);
                                returnObj = func(items);
                                //returnObj = Activator.CreateInstance(returnType, items.Count);//创建实例
                                ////Type objListType = returnObj.GetType();
                                //bool isArray = sysType == SysType.Array;
                                //MethodInfo method;
                                //if (isArray)
                                //{
                                //    method = returnType.GetMethod("Set");
                                //    if (method != null)
                                //    {
                                //        for (int i = 0; i < items.Count; i++)
                                //        {
                                //            Object item = ChangeType(items[i], returnType.GetElementType());//Type.GetType(propType.FullName.Replace("[]", "")
                                //            method.Invoke(returnObj, new object[] { i, item });
                                //        }
                                //    }
                                //}
                                //else
                                //{
                                //    method = returnType.GetMethod("Add");
                                //    if (method == null)
                                //    {
                                //        method = returnType.GetMethod("Push");
                                //    }
                                //    if (method != null)
                                //    {
                                //        for (int i = 0; i < items.Count; i++)
                                //        {
                                //            Object item = ChangeType(items[i], argTypes[0]);
                                //            method.Invoke(returnObj, new object[] { item });
                                //        }

                                //    }
                                //}


                                #endregion
                            }
                        }
                        else if (len == 2) // row
                        {
                            Type argType = argTypes[1];
                            // bool isObjectType = argType.Name == "Object";
                            MDataRow mRow;
                            if (objValue is MDataRow)
                            {
                                mRow = objValue as MDataRow;
                            }
                            else
                            {
                                mRow = MDataRow.CreateFrom(objValue, argType);
                            }
                            var func = MDataRowToKeyValue.Delegate(returnType);
                            var obj = func(mRow);
                            return obj;

                            //returnObj = returnType.Name.Contains("Dictionary") ? Activator.CreateInstance(returnType, StringComparer.OrdinalIgnoreCase) : Activator.CreateInstance(returnType);
                            ////Activator.CreateInstance(propType, mRow.Columns.Count);//创建实例
                            //Type objListType = returnObj.GetType();
                            //MethodInfo mi = objListType.GetMethod("Add");
                            //foreach (MDataCell mCell in mRow)
                            //{
                            //    object mObj = ChangeType(mCell.Value, argType);//  isObjectType ? mCell.Value : GetValue(mCell.ToRow(), argType);
                            //    mi.Invoke(returnObj, new object[] { mCell.ColumnName, mObj });
                            //}
                            //mRow = null;
                        }
                    }
                    #endregion
                    break;
                case SysType.Custom://继续递归
                    returnObj = MDataRow.CreateFrom(objValue).ToEntity(returnType);
                    break;

            }
            return returnObj;
        }

        //private static object GetValue(MDataRow row, Type type)
        //{
        //    switch (ReflectTool.GetSystemType(ref type))
        //    {
        //        case SysType.Base:
        //            return ConvertTool.ChangeType(row[0].Value, type);
        //        case SysType.Enum:
        //            return Enum.Parse(type, row[0].ToString());
        //        default:
        //            return row.ToEntity(type);
        //        //object o = Activator.CreateInstance(type);
        //        //SetToEntity(ref o, row);
        //        //return o;
        //    }
        //}
        */
    }
}
