using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using CYQ.Data.Table;
using CYQ.Data.Tool;
using CYQ.Data.SQL;

namespace CYQ.Data.Emit
{
    /// <summary>
    /// 实体从数据行加载数据
    /// </summary>
    internal class MDataRowSetToEntity
    {
        private static MDictionary<Type, Action<MDataRow, object>> typeFuncs = new MDictionary<Type, Action<MDataRow, object>>();
        private static readonly object lockObj = new object();
        public static Action<MDataRow, object> Delegate(Type t)
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
                var func = method.CreateDelegate(typeof(Action<MDataRow, object>)) as Action<MDataRow, object>;
                typeFuncs.Add(t, func);
                return func;
            }
        }

        private static DynamicMethod CreateMethod(Type entityType)
        {

            #region 创建动态方法

            Type rowType = typeof(MDataRow);
            Type toolType = typeof(ConvertTool);
            DynamicMethod method = new DynamicMethod("MDataRowSetToEntity", typeof(void), new Type[] { rowType, typeof(object) }, entityType);
            MethodInfo getValue = rowType.GetMethod("GetItemValue", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string) }, null);
            MethodInfo changeType = toolType.GetMethod("ChangeType", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(object), typeof(Type) }, null);
            MethodInfo getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");
            ILGenerator gen = method.GetILGenerator();//开始编写IL方法。

            gen.DeclareLocal(entityType);//0
            gen.DeclareLocal(typeof(object));//1
            gen.DeclareLocal(typeof(Type));//2
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stloc_0);//t= new T();

            List<PropertyInfo> properties = ReflectTool.GetPropertyList(entityType);
            if (properties.Count > 0)
            {
                foreach (var property in properties)
                {
                    SetValueByRow(gen, getValue, changeType, getTypeFromHandle, property, null);
                }
            }
            List<FieldInfo> fields = ReflectTool.GetFieldList(entityType);
            if (fields.Count > 0)
            {
                foreach (var field in fields)
                {
                    SetValueByRow(gen, getValue, changeType, getTypeFromHandle, null, field);
                }
            }
            gen.Emit(OpCodes.Ret);

            #endregion

            return method;
        }
        private static void SetValueByRow(ILGenerator gen, MethodInfo getValue, MethodInfo changeType, MethodInfo getTypeFromHandle, PropertyInfo pi, FieldInfo fi)
        {
            Type valueType = pi != null ? pi.PropertyType : fi.FieldType;
            string fieldName = pi != null ? pi.Name : fi.Name;

            Label labelContinue = gen.DefineLabel();//定义标签；goto;
            gen.Emit(OpCodes.Ldarg_0);//设置参数0 ：row 将索引为 0 的自变量加载到计算堆栈上。

            gen.Emit(OpCodes.Ldstr, fieldName);//设置参数值：string 推送对元数据中存储的字符串的新对象引用。

            gen.Emit(OpCodes.Call, getValue);//Call GetItemValue(ordinal);=> invoke(row,1)
            gen.Emit(OpCodes.Stloc_1); // o=GetItemValue(ordinal); 从计算堆栈的顶部弹出当前值并将其存储到索引 1 处的局部变量列表中。

            gen.Emit(OpCodes.Ldloc_1);//将索引 1 处的局部变量加载到计算堆栈上。
            gen.Emit(OpCodes.Brfalse, labelContinue); // break out of switch


            //-------------新增：o=ConvertTool.ChangeType(o, t);
            //gen.Emit(OpCodes.Nop);//如果修补操作码，则填充空间。 尽管可能消耗处理周期，但未执行任何有意义的操作。
            gen.Emit(OpCodes.Ldtoken, valueType);//这个卡我卡的有点久。将元数据标记转换为其运行时表示形式，并将其推送到计算堆栈上。
                                                 //下面这句Call，解决在 .net 中抛的异常：无法获取Type值，尝试读取或写入受保护的内存。这通常指示其他内存已损坏。
            gen.Emit(OpCodes.Call, getTypeFromHandle);
            gen.Emit(OpCodes.Stloc_2);

            gen.Emit(OpCodes.Ldloc_1);//o
            gen.Emit(OpCodes.Ldloc_2);
            gen.Emit(OpCodes.Call, changeType);//Call ChangeType(o,type);=> invoke(o,type) 调用由传递的方法说明符指示的方法。
            gen.Emit(OpCodes.Stloc_1); // o=GetItemValue(ordinal);
                                       //-------------------------------------------
            #region 为属性设置值
            SetValue(gen, pi, fi);
            #endregion

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

    }
}
