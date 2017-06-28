using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data;
using CYQ.Data.Table;
using CYQ.Data.SQL;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;


namespace CYQ.Data.Tool
{
    /// <summary>
    /// 类型转换（支持json转实体）
    /// </summary>
    public static class ConvertTool
    {
        /// <summary>
        /// 类型转换(精准强大)
        /// </summary>
        /// <param name="value">值处理</param>
        /// <param name="t">类型</param>
        /// <returns></returns>
        public static object ChangeType(object value, Type t)
        {
            return StaticTool.ChangeType(value, t);
        }
    }
    /// <summary>
    /// 反射工具（带缓存）
    /// </summary>
    public static class ReflectTool
    {
        /// <summary>
        /// 获取泛型参数的长度
        /// </summary>
        public static int GetGenericArgumentLength(ref Type t)
        {
            return StaticTool.GetArgumentLength(ref t);
        }
        /// <summary>
        /// 获取泛型参数的长度（和类型）
        /// </summary>
        public static int GetGenericArgumentLength(ref Type t, out Type[] argTypes)
        {
            return StaticTool.GetArgumentLength(ref t,out argTypes);
        }
        /// <summary>
        /// 获得反射属性（内部有缓存）
        /// </summary>
        public static List<PropertyInfo> GetPropertys(Type t)
        {
            return StaticTool.GetPropertyInfo(t);
        }
        /// <summary>
        /// 获取系统类型，若是Nullable类型，则转为基础类型。
        ///  </summary>
        public static SysType GetSystemType(ref Type t)
        {
            return StaticTool.GetSystemType(ref t);
        }
    }
    /// <summary>
    /// 静态方法工具类
    /// </summary>
    internal static class StaticTool
    {
        /// <summary>
        /// 将PropertyInfo[] 改成PropertyInfo List，是因为.NET的CLR会引发内存读写异常（启用IntelliTrace时）
        /// </summary>
        static MDictionary<string, List<PropertyInfo>> propCache = new MDictionary<string, List<PropertyInfo>>();
        /// <summary>
        /// 获取属性列表
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static List<PropertyInfo> GetPropertyInfo(Type t)
        {
            string key = t.GUID.ToString();
            if (propCache.ContainsKey(key))
            {
                return propCache[key];
            }
            else
            {
                bool isInheritOrm = t.BaseType.Name == "OrmBase" || t.BaseType.Name == "SimpleOrmBase";
                PropertyInfo[] pInfo = isInheritOrm ? t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) : t.GetProperties();
                List<PropertyInfo> list = new List<PropertyInfo>(pInfo.Length);
                try
                {

                    list.AddRange(pInfo);
                    propCache.Set(key, list);
                }
                catch (Exception err)
                {
                    Log.WriteLogToTxt(err);
                }
                return list;
            }
        }

        static Dictionary<string, Type[]> argumentCache = new Dictionary<string, Type[]>();
        /// <summary>
        ///  获取泛型的参数长度（非泛型按默认方法计算）
        /// </summary>
        public static int GetArgumentLength(ref Type t)
        {
            Type[] argTypes;
            return GetArgumentLength(ref t, out argTypes);
        }
        /// <summary>
        /// 获取泛型的参数长度（非泛型按默认方法计算）
        /// </summary>
        public static int GetArgumentLength(ref Type t, out Type[] argTypes)
        {
            if (argumentCache.ContainsKey(t.FullName))
            {
                argTypes = argumentCache[t.FullName];
                return argTypes.Length;
            }
            else
            {
                int len = 0;
                if (t.IsGenericType)
                {
                    argTypes = t.GetGenericArguments();
                    len = argTypes.Length;
                    for (int i = 0; i < argTypes.Length; i++)
                    {
                        if (argTypes[i].IsGenericType && argTypes[i].Name.StartsWith("Nullable"))
                        {
                            argTypes[i] = Nullable.GetUnderlyingType(argTypes[i]);
                        }
                    }
                    if (t.Name.StartsWith("Nullable"))
                    {
                        t = Nullable.GetUnderlyingType(t);
                    }
                }
                else
                {
                    if (t.Name.EndsWith("[]") || t.Name=="MDataRowCollection")
                    {
                        len = 1;
                    }
                    else if (t.Name == "NameValueCollection" || (t.BaseType != null && t.BaseType.Name == "NameValueCollection"))
                    {
                        len = 2;
                    }
                    else
                    {
                        System.Reflection.MethodInfo mi = t.GetMethod("Add");
                        if (mi != null)
                        {
                            len = mi.GetParameters().Length;
                        }
                    }
                    argTypes = new Type[len];
                    for (int i = 0; i < argTypes.Length; i++)
                    {
                        argTypes[i] = typeof(object);
                    }
                }
                try
                {
                    argumentCache.Add(t.FullName, argTypes);
                }
                catch
                {

                }
                return len;
            }
        }

        /// <summary>
        /// 获取系统类型，若是Nullable类型，则转为基础类型。
        ///  </summary>
        public static SysType GetSystemType(ref Type t)
        {
            if (t.IsEnum)
            {
                return SysType.Enum;
            }
            if (t.FullName.StartsWith("System.")) // 系统类型
            {
                if (t.IsGenericType)
                {
                    if (t.Name.StartsWith("Nullable"))//int? id
                    {
                        t = Nullable.GetUnderlyingType(t);
                        return SysType.Base;
                    }
                    return SysType.Generic;
                }
                else if (t.FullName.StartsWith("System.Collections."))
                {
                    return SysType.Collection;
                }
                else if (t.Name.EndsWith("[]"))
                {
                    return SysType.Array;
                }
                if (t.FullName.Split('.').Length > 2)
                {
                    return SysType.Custom;
                }
                return SysType.Base;
            }
            else
            {
                return SysType.Custom;
            }
        }

        /// <summary>
        /// 将GUID转成16字节字符串
        /// </summary>
        /// <returns></returns>
        internal static string ToGuidByteString(string guid)
        {
            return BitConverter.ToString(new Guid(guid).ToByteArray()).Replace("-", "");
        }

        /// <summary>
        /// 获取约定枚举的数据库名称
        /// </summary>
        /// <param name="tableNamesEnum">表枚举或表名</param>
        /// <returns></returns>
        internal static string GetDbName(ref object tableNamesEnum)
        {
            string dbName = string.Empty;
            if (tableNamesEnum is Enum)
            {
                Type t = tableNamesEnum.GetType();
                string enumName = t.Name;
                if (enumName != "TableNames" && enumName != "ViewNames")
                {
                    if (enumName.Length > 1 && enumName[1] == '_')
                    {
                        dbName = enumName.Substring(2, enumName.Length - 6);//.Replace("Enum", "Conn");
                    }
                    else
                    {
                        string[] items = t.FullName.Split('.');
                        if (items.Length > 1)
                        {
                            dbName = items[items.Length - 2];// +"Conn";
                            items = null;
                        }
                    }
                }
                t = null;
            }
            else if (tableNamesEnum is string)
            {
                string tName = tableNamesEnum.ToString();
                int index = tName.LastIndexOf(')');
                if (index > 0) // 视图
                {
                    string viewSQL = tName;
                    string a = tName.Substring(0, index + 1);//a部分
                    tName = tName.Substring(index + 1).Trim();//b部分。ddd.v_xxx
                    //修改原对像

                    if (tName.Contains("."))
                    {
                        tableNamesEnum = a + " " + tName.Substring(tName.LastIndexOf('.') + 1);
                    }
                }
                if (tName.Contains(".") && !tName.Trim().Contains(" "))
                {
                    dbName = tName.Split('.')[0];
                }

            }
            return dbName;
        }

        /// <summary>
        /// 类型转换(精准强大)
        /// </summary>
        /// <param name="value">值处理</param>
        /// <param name="t">类型</param>
        /// <returns></returns>
        public static object ChangeType(object value, Type t)
        {
            if (t == null)
            {
                return null;
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
            if (t.Name == "String")
            {
                return strValue;
            }
            if (strValue == "")
            {
                return Activator.CreateInstance(t);
            }
            else if (t.IsValueType)
            {
                if (t.Name == "Guid")
                {
                    return new Guid(strValue);
                }
                else if (t.Name.StartsWith("Int") && strValue.IndexOf('.') > -1)
                {
                    strValue = strValue.Split('.')[0];
                }
                return Convert.ChangeType(strValue, t);
            }
            else
            {
                switch(GetSystemType(ref t))
                {
                    case SysType.Custom:
                        return MDataRow.CreateFrom(strValue).ToEntity(t);
                    case SysType.Generic:
                        return MDataTable.CreateFrom(strValue).ToList(t);
                    case SysType.Array:
                        if (t.Name == "Byte[]")
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                new BinaryFormatter().Serialize(ms, value);
                                return ms.ToArray();
                            }
                        }
                        break;
                }
                return Convert.ChangeType(value, t);
            }
        }

        #region 将字符串变HashKey
        static Dictionary<string, string> hashKeyCache = new Dictionary<string, string>(32);
        internal static string GetHashKey(string sourceString)
        {
            try
            {
                if (hashKeyCache.ContainsKey(sourceString))
                {
                    return hashKeyCache[sourceString];
                }
                else
                {
                    if (hashKeyCache.Count > 512)
                    {
                        hashKeyCache.Clear();
                        hashKeyCache = new Dictionary<string, string>(64);
                    }
                    string value = "K" + Math.Abs(sourceString.GetHashCode()) + sourceString.Length;
                    hashKeyCache.Add(sourceString, value);
                    return value;
                }
            }
            catch
            {
                return sourceString;
            }
        }
        #endregion

    }
}
