using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CYQ.Data.Tool
{
    /// <summary>
    /// 反射工具（带缓存）
    /// </summary>
    public static class ReflectTool
    {
        static readonly object[] lockObjs = new object[5];
        static ReflectTool()
        {
            for (int i = 0; i < lockObjs.Length; i++)
            {
                lockObjs[i] = new object();
            }
        }
        /// <summary>
        /// 将PropertyInfo[] 改成PropertyInfo List，是因为.NET的CLR会引发内存读写异常（启用IntelliTrace时）
        /// </summary>
        static MDictionary<string, List<PropertyInfo>> propCache = new MDictionary<string, List<PropertyInfo>>();
        static MDictionary<string, List<FieldInfo>> fieldCache = new MDictionary<string, List<FieldInfo>>();
        static MDictionary<string, object[]> attrCache = new MDictionary<string, object[]>();
        static MDictionary<string, Type[]> argumentCache = new MDictionary<string, Type[]>();
        static MDictionary<int, bool> attrExistsCache = new MDictionary<int, bool>();



        /// <summary>
        /// 获取属性列表
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static List<PropertyInfo> GetPropertyList(Type t)
        {
            bool isAnonymousType = t.Name.Contains("f__AnonymousType");//忽略匿名类型
            string key = t.FullName;// t.GUID.ToString();由泛型 XX<T> 引起的如： Ge<A> 和 Ge<B> ,Guid名相同,所以用FullName
            if (!isAnonymousType && propCache.ContainsKey(key))
            {
                return propCache[key];
            }

            bool isInheritOrm = t.BaseType != null && t.BaseType.FullName.StartsWith("CYQ.Data.Orm");
            PropertyInfo[] pInfo = isInheritOrm ? t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) : t.GetProperties();
            List<PropertyInfo> list = new List<PropertyInfo>(pInfo.Length);
            try
            {

                list.AddRange(pInfo);
                if (!isAnonymousType)
                {
                    propCache.Add(key, list);
                }
            }
            catch (Exception err)
            {
                Log.Write(err, LogType.Error);
            }
            return list;

        }
        /// <summary>
        /// 获取Field列表
        /// </summary>
        public static List<FieldInfo> GetFieldList(Type t)
        {
            string key = t.GUID.ToString();
            if (fieldCache.ContainsKey(key))
            {
                return fieldCache[key];
            }
            else
            {
                bool isInheritOrm = t.BaseType != null && t.BaseType.FullName.StartsWith("CYQ.Data.Orm");
                FieldInfo[] pInfo = isInheritOrm ? t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) : t.GetFields();
                List<FieldInfo> list = new List<FieldInfo>(pInfo.Length);
                try
                {

                    list.AddRange(pInfo);
                    fieldCache.Set(key, list);
                }
                catch (Exception err)
                {
                    Log.Write(err, LogType.Error);
                }
                return list;
            }
        }

        /// <summary>
        ///  获取泛型的参数长度（非泛型按默认方法计算）
        /// </summary>
        public static int GetArgumentLength(ref Type t)
        {
            Type[] argTypes;
            return GetArgumentLength(ref t, out argTypes);
        }
        /// <summary>
        /// 获取泛型的参数长度，同时类型修改为普通类型（非泛型按默认方法计算）
        /// </summary>
        public static int GetArgumentLength(ref Type t, out Type[] argTypes)
        {
            argTypes = null;
            string key = t.FullName;
            if (argumentCache.ContainsKey(key))
            {
                argTypes = argumentCache[key];
            }
            else
            {
                int len = 0;

                if (t.IsGenericType)
                {
                    #region MyRegion
                    argTypes = t.GetGenericArguments();
                    len = argTypes.Length;
                    for (int i = 0; i < argTypes.Length; i++)
                    {
                        if (argTypes[i].IsGenericType && argTypes[i].IsValueType)
                        {
                            argTypes[i] = Nullable.GetUnderlyingType(argTypes[i]);
                        }
                    }
                    #endregion
                }
                else
                {
                    #region 非泛型

                    if (t.Name.EndsWith("[]"))
                    {
                        len = 1;
                        argTypes = new Type[1] { t.GetElementType() };
                    }
                    else if (t.Name == "MDataRowCollection" || t.Name == "MDataColumn")
                    {
                        len = 1;
                    }
                    else if (t.Name == "NameValueCollection" || (t.BaseType != null && t.BaseType.Name == "NameValueCollection"))
                    {
                        len = 2;
                    }
                    else if (t.BaseType != null && t.BaseType.Name == "DbParameterCollection")
                    {
                        len = 1;
                    }
                    else
                    {
                        System.Reflection.MethodInfo[] mis = t.GetMethods();
                        if (mis != null)
                        {
                            foreach (System.Reflection.MethodInfo mi in mis)
                            {
                                if (mi.Name == "Add")
                                {
                                    len = mi.GetParameters().Length;
                                    break;
                                }
                            }
                        }
                        //System.Reflection.MethodInfo mi = t.GetMethod("Add");
                        //if (mi != null)
                        //{
                        //    len = mi.GetParameters().Length;
                        //}
                    }
                    if (argTypes == null)
                    {
                        argTypes = new Type[len];
                        for (int i = 0; i < argTypes.Length; i++)
                        {
                            argTypes[i] = typeof(object);
                        }
                    }
                    #endregion
                }

                argumentCache.Set(key, argTypes);
            }
            if (t.IsGenericType)
            {
                if (t.IsValueType)
                {
                    t = Nullable.GetUnderlyingType(t);
                }
                else if (t.Name == "IList`1")
                {
                    //List<int> a;typeof(List<int>);
                    t = typeof(List<>).MakeGenericType(argTypes[0]);
                }
                else if (t.Name == "IDictionary`2")
                {
                    t = typeof(Dictionary<,>).MakeGenericType(argTypes[0], argTypes[1]);
                }
            }
            return argTypes.Length;
        }

        public static object[] GetAttributes(FieldInfo fi, Type searchType)
        {
            return GetAttributes(fi.DeclaringType, searchType, null, fi);
        }
        public static object[] GetAttributes(PropertyInfo pi, Type searchType)
        {
            return GetAttributes(pi.DeclaringType, searchType, pi, null);
        }
        public static object[] GetAttributes(Type t, Type searchType)
        {
            return GetAttributes(t, searchType, null, null);
        }

        /// <summary>
        /// 获取特性列表
        /// </summary>
        private static object[] GetAttributes(Type t, Type searchType, PropertyInfo pi, FieldInfo fi)
        {
            string key = t.GUID.ToString();
            if (searchType != null)
            {
                key += searchType.Name;
            }
            if (pi != null)
            {
                key += pi.Name;
            }
            else if (fi != null)
            {
                key += fi.Name;
            }
            //key = key.GetHashCode().ToString();
            if (attrCache.ContainsKey(key))
            {
                return attrCache[key];
            }
            else
            {
                try
                {
                    object[] items = null;
                    if (pi != null)
                    {
                        items = searchType == null ? pi.GetCustomAttributes(false) : pi.GetCustomAttributes(searchType, true);
                    }
                    else if (fi != null)
                    {
                        items = searchType == null ? fi.GetCustomAttributes(false) : fi.GetCustomAttributes(searchType, true);
                    }
                    else
                    {
                        items = searchType == null ? t.GetCustomAttributes(false) : t.GetCustomAttributes(searchType, true);
                    }
                    attrCache.Add(key, items);
                    return items;
                }
                catch (Exception err)
                {
                    Log.Write(err, LogType.Error);
                }
                return null;
            }
        }
        internal static T GetAttr<T>(PropertyInfo pi, FieldInfo fi)
        {
            Type type = typeof(T);
            object[] attr = null;
            if (pi != null)
            {
                attr = GetAttributes(pi, type);
            }
            else
            {
                attr = GetAttributes(fi, type);
            }

            if (attr != null && attr.Length == 1)
            {
                return (T)attr[0];
            }
            return default(T);
        }

        /// <summary>
        /// 判断是否存在指定的属性
        /// </summary>
        /// <param name="searchType"></param>
        /// <param name="pi"></param>
        /// <returns></returns>
        internal static bool ExistsAttr(Type searchType, PropertyInfo pi, FieldInfo fi)
        {
            string key = (pi != null ? pi.DeclaringType.FullName + pi.Name : fi.DeclaringType.FullName + fi.Name) + searchType.Name;
            int code = key.GetHashCode();
            if (attrExistsCache.ContainsKey(code))
            {
                return attrExistsCache[code];
            }
            object[] items = pi != null ? pi.GetCustomAttributes(searchType, true) : fi.GetCustomAttributes(searchType, true);
            if (items != null && items.Length > 0)
            {
                attrExistsCache.Add(code, true);
                return true;
            }
            attrExistsCache.Add(code, false);
            return false;
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
            if (t.IsValueType || t.Name == "String")//int? id
            {
                if (t.IsGenericType)
                {
                    t = Nullable.GetUnderlyingType(t);
                }
                return SysType.Base;
            }
            if (t.IsGenericType)
            {
                return SysType.Generic;
            }
            if (t.FullName.EndsWith("[]"))
            {
                return SysType.Array;
            }
            if (t.FullName.StartsWith("System.Collections."))
            {
                return SysType.Collection;
            }

            //if (t.FullName.StartsWith("System.")) // 系统类型
            //{
            //    if (t.FullName.Split('.').Length > 2)
            //    {
            //        return SysType.Custom;
            //    }
            //    return SysType.Base;
            //}

            return SysType.Custom;

        }

    }
}
