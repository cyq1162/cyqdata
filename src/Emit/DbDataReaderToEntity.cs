using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using CYQ.Data.Table;
using CYQ.Data.Tool;
using CYQ.Data.SQL;
using System.Data.Common;

namespace CYQ.Data.Emit
{
    /// <summary>
    /// DbDataReader 转实体
    /// </summary>
    internal static partial class DbDataReaderToEntity
    {

        static Dictionary<Type, Func<DbDataReader, object>> typeFuncs = new Dictionary<Type, Func<DbDataReader, object>>();

        private static readonly object lockObj = new object();

        internal static Func<DbDataReader, object> Delegate(Type t)
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
                DynamicMethod method = CreateDynamicMethod(t);
                var func = method.CreateDelegate(typeof(Func<DbDataReader, object>)) as Func<DbDataReader, object>;
                typeFuncs.Add(t, func);
                return func;
            }
        }

        /// <summary>
        /// 构建一个ORM实体转换器（第1次构建有一定开销时间）
        /// </summary>
        /// <param name="entityType">转换的目标类型</param>
        private static DynamicMethod CreateDynamicMethod(Type entityType)
        {


            #region 创建动态方法

            var readerType = typeof(DbDataReader);
            Type convertToolType = typeof(ConvertTool);
            MethodInfo getValue = readerType.GetMethod("get_Item", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string) }, null);
            MethodInfo changeType = convertToolType.GetMethod("ChangeType", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(object), typeof(Type) }, null);
            MethodInfo getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");

            DynamicMethod method = new DynamicMethod("DbDataReaderToEntity", typeof(object), new Type[] { readerType }, entityType);
            var constructor = entityType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);

            ILGenerator gen = method.GetILGenerator();//开始编写IL方法。
            if (constructor == null)
            {
                gen.Emit(OpCodes.Ret);
                return method;
            }
            var instance = gen.DeclareLocal(entityType);//0 ： Entity t0;
            gen.DeclareLocal(typeof(object));//1 string s1;
            gen.DeclareLocal(typeof(Type));//2   Type t2;
            gen.Emit(OpCodes.Newobj, constructor);
            gen.Emit(OpCodes.Stloc_0, instance);//t0= new T();

            List<PropertyInfo> properties = ReflectTool.GetPropertyList(entityType);
            if (properties != null && properties.Count > 0)
            {
                foreach (var property in properties)
                {
                    SetValueByRow(gen, getValue, changeType, getTypeFromHandle, property, null);
                }
            }
            List<FieldInfo> fields = ReflectTool.GetFieldList(entityType);
            if (fields != null && fields.Count > 0)
            {
                foreach (var field in fields)
                {
                    SetValueByRow(gen, getValue, changeType, getTypeFromHandle, null, field);
                }
            }

            gen.Emit(OpCodes.Ldloc_0, instance);//t0 加载，准备返回
            gen.Emit(OpCodes.Ret);
            #endregion

            return method;
        }
        private static void SetValueByRow(ILGenerator gen, MethodInfo getValue, MethodInfo changeType, MethodInfo getTypeFromHandle, PropertyInfo pi, FieldInfo fi)
        {
            Type valueType = pi != null ? pi.PropertyType : fi.FieldType;
            string fieldName = pi != null ? pi.Name : fi.Name;

            Label labelContinue = gen.DefineLabel();//定义循环标签；goto;


            gen.Emit(OpCodes.Ldarg_0);//加载 reader 对象
            gen.Emit(OpCodes.Ldstr, fieldName);//设置字段名。
            gen.Emit(OpCodes.Callvirt, getValue);//bool a=dic.tryGetValue(...,out value)
            gen.Emit(OpCodes.Stloc_1);//将索引 1 处的局部变量加载到计算堆栈上。

            gen.Emit(OpCodes.Ldloc_1);//将索引 1 处的局部变量加载到计算堆栈上。
            gen.Emit(OpCodes.Brfalse_S, labelContinue);//if(!a){continue;}




            //-------------新增：o=ConvertTool.ChangeType(o, t);
            if (valueType.Name != "Object")
            {
                //gen.Emit(OpCodes.Nop);//如果修补操作码，则填充空间。 尽管可能消耗处理周期，但未执行任何有意义的操作。
                gen.Emit(OpCodes.Ldtoken, valueType);//这个卡我卡的有点久。将元数据标记转换为其运行时表示形式，并将其推送到计算堆栈上。
                //下面这句Call，解决在 .net 中无法获取Type值，抛的异常：尝试读取或写入受保护的内存。这通常指示其他内存已损坏。
                gen.Emit(OpCodes.Call, getTypeFromHandle);
                gen.Emit(OpCodes.Stloc_2);

                gen.Emit(OpCodes.Ldloc_1);//o
                gen.Emit(OpCodes.Ldloc_2);
                gen.Emit(OpCodes.Call, changeType);//Call ChangeType(o,type);=> invoke(o,type) 调用由传递的方法说明符指示的方法。
                gen.Emit(OpCodes.Stloc_1); // o=GetItemValue(ordinal);
            }
            //-------------------------------------------
            SetValue(gen, pi, fi);
            gen.MarkLabel(labelContinue);//继续下一个循环
        }

        private static void SetValue(ILGenerator gen, PropertyInfo pi, FieldInfo fi)
        {
            if (pi != null && pi.CanWrite)
            {
                gen.Emit(OpCodes.Ldloc_0);//实体对象obj
                gen.Emit(OpCodes.Ldloc_1);//属性的值 objvalue
                EmitCastObj(gen, pi.PropertyType);//类型转换
                gen.EmitCall(OpCodes.Callvirt, pi.GetSetMethod(), null); // Call the property setter
            }
            if (fi != null)
            {
                gen.Emit(OpCodes.Ldloc_0);//实体对象obj
                gen.Emit(OpCodes.Ldloc_1);//属性的值 objvalue
                EmitCastObj(gen, fi.FieldType);//类型转换
                gen.Emit(OpCodes.Stfld, fi);//对实体赋值 System.Object.FieldSetter(String typeName, String fieldName, Object val)
            }
        }
        private static void EmitCastObj(ILGenerator il, Type targetType)
        {
            if (targetType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, targetType);
            }
            else
            {
                il.Emit(OpCodes.Castclass, targetType);
            }
        }
        /* Emit 方法原型（升级版）
         public ETable RowToT(MDataRow row)
        {
            Type ttt = typeof(Int32);
            Type t = ttt;
            ETable et;
            object o;
            bool b;
            et = new ETable();
            label:
         //循环属性生成。
            o = row.GetItemValue(0);
            if (o == null)
            {
                goto label;
            }

            o = ConvertTool.ChangeType(o, t);
            et.ID = (int)o;
            return et;
        }
         */

    }
    //internal static partial class DbDataReaderToEntity
    //{
    //    /// <summary>
    //    /// 调用返回实体类。
    //    /// </summary>
    //    public static T Invoke<T>(DbDataReader reader)
    //    {
    //        if (reader == null) { return default(T); }
    //        var func = Delegate(typeof(T));
    //        if (func == null) { return default(T); };
    //        return (T)func(reader);
    //    }
    //}
}
