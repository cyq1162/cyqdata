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
            return StaticTool.GetArgumentLength(ref t, out argTypes);
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
        /// <summary>
        /// 获得反射的特性列表
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static object[] GetAttributes(Type t)
        {
            return StaticTool.GetAttributes(t);
        }
    }
}
