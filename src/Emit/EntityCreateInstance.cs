using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;

namespace CYQ.Data.Emit
{
    //public class EntityCreate
    //{
    //    public static object Create(Type type)
    //    {
    //        return EntityCreateInstance.Delegate(type);
    //    }
    //}
    /// <summary>
    /// Emit 实现动态委托创建实例
    /// </summary>
    internal class EntityCreateInstance
    {
        static Dictionary<Type, Func<object>> typeFuncs = new Dictionary<Type, Func<object>>();

        private static readonly object lockObj = new object();

        public static Func<object> Delegate(Type t)
        {
            if (typeFuncs.ContainsKey(t))
            {
                return typeFuncs[t];
            }
            lock (lockObj)
            {
                if (typeFuncs.ContainsKey(t))
                {
                    return typeFuncs[t];
                }
                DynamicMethod method = CreateMethod(t);
                var func = method.CreateDelegate(typeof(Func<object>)) as Func<object>;
                typeFuncs.Add(t, func);
                return func;
            }
        }

        private static DynamicMethod CreateMethod(Type returnType)
        {
            DynamicMethod method = new DynamicMethod("EntityCreateInstance", typeof(object), null, returnType);
            ILGenerator gen = method.GetILGenerator();//开始编写IL方法。
            gen.DeclareLocal(returnType);//0
            gen.Emit(OpCodes.Newobj, returnType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null));
            gen.Emit(OpCodes.Stloc_0);//t= new T();
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ret);
            return method;
        }



    }
}
