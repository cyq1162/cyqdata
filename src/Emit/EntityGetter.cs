using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace CYQ.Data.Emit
{
    /// <summary>
    /// （未使用）
    /// </summary>
    internal class EntityGetter
    {
        static Dictionary<PropertyInfo, Func<object, object>> piFuncs = new Dictionary<PropertyInfo, Func<object, object>>();
        static Dictionary<FieldInfo, Func<object, object>> fiFuncs = new Dictionary<FieldInfo, Func<object, object>>();
        public static Func<object, object> GetterFunc(PropertyInfo pi, FieldInfo fi)
        {
            if (pi != null)
            {
                return GetterFunc(pi);
            }
            if (fi != null)
            {
                return GetterFunc(fi);
            }
            return null;
        }

        private static readonly object lockPIObj = new object();
        public static Func<object, object> GetterFunc(PropertyInfo pi)
        {
            if (piFuncs.ContainsKey(pi))
            {
                return piFuncs[pi];
            }
            lock (lockPIObj)
            {
                if (piFuncs.ContainsKey(pi))
                {
                    return piFuncs[pi];
                }
                var method = new DynamicMethod("GetterFunc", typeof(object), new[] { typeof(object) }, pi.DeclaringType, true);
                var il = method.GetILGenerator();
                var getMethod = pi.GetGetMethod();

                il.Emit(OpCodes.Ldarg_0); // Load the input object onto the stack
                il.EmitCall(OpCodes.Callvirt, getMethod, null); // Call the property getter
                if (pi.PropertyType.IsValueType)
                {
                    il.Emit(OpCodes.Box, pi.PropertyType); // Box the value type
                }

                il.Emit(OpCodes.Ret); // Return the value

                var func = (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));
                piFuncs.Add(pi, func);
                return func;
            }
        }
        private static readonly object lockFIObj = new object();
        public static Func<object, object> GetterFunc(FieldInfo fi)
        {
            if (fiFuncs.ContainsKey(fi))
            {
                return fiFuncs[fi];
            }
            lock (lockFIObj)
            {
                if (fiFuncs.ContainsKey(fi))
                {
                    return fiFuncs[fi];
                }
                var method = new DynamicMethod("GetterFunc", typeof(object), new[] { typeof(object) }, fi.DeclaringType, true);
                var il = method.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0); // Load the input object onto the stack
                il.Emit(OpCodes.Ldfld, fi); // 加载 Id 成员变量的值到堆栈
                if (fi.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Box, fi.FieldType); // Box the value type
                }

                il.Emit(OpCodes.Ret); // Return the value

                var func = (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));
                fiFuncs.Add(fi, func);
                return func;
            }
        }

    }
}
