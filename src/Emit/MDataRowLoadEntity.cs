using CYQ.Data.Json;
using CYQ.Data.Table;
using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace CYQ.Data.Emit
{
    /*
     * 问题总结：
     * 1、使用的方法，返回值不能是自身：MDataRow的Set方法返回this自身，卡在这个里很久。
     * 2、压入栈的数据，不用时需要Pop弹出，以免影响下一个循环。
     * 3、定义的Lable，需要被使用到。
     * 
     */
    /// <summary>
    /// 数据行从实体加载数据：(先取 PropertyInfo，再取：FieldInfo)
    /// </summary>
    internal class MDataRowLoadEntity
    {
        //BreakOp op, int initState
        static Dictionary<Type, Action<MDataRow, object, BreakOp, int>> typeFuncs = new Dictionary<Type, Action<MDataRow, object, BreakOp, int>>();


        private static readonly object lockDicObj = new object();
        internal static Action<MDataRow, object, BreakOp, int> Delegate(Type entityType)
        {
            if (typeFuncs.ContainsKey(entityType))
            {
                return typeFuncs[entityType];
            }
            lock (lockDicObj)
            {
                if (typeFuncs.ContainsKey(entityType))
                {
                    return typeFuncs[entityType];
                }
                var dynamicMethod = CreateDynamicMethod(entityType);
                var func = (Action<MDataRow, object, BreakOp, int>)dynamicMethod.CreateDelegate(typeof(Action<MDataRow, object, BreakOp, int>));
                typeFuncs.Add(entityType, func);
                return func;
            }
        }

        private static DynamicMethod CreateDynamicMethod(Type entityType)
        {
            var dynamicMethod = new DynamicMethod("MDataRowLoadEntity", typeof(void), new[] { typeof(MDataRow), typeof(object), typeof(BreakOp), typeof(int) }, entityType);
            var ilGen = dynamicMethod.GetILGenerator();

            var setMethod = typeof(MDataRow).GetMethod("SetItemValue", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string), typeof(object), typeof(int) }, null);
            var toStringMethod = typeof(Convert).GetMethod("ToString", new Type[] { typeof(object) });
            List<PropertyInfo> properties = ReflectTool.GetPropertyList(entityType);
            if (properties != null && properties.Count > 0)
            {
                foreach (var property in properties)
                {
                    if (property.CanRead)
                    {
                        ilGen.Emit(OpCodes.Ldarg_1);
                        //ilGen.Emit(OpCodes.Castclass, entityType);
                        ilGen.Emit(OpCodes.Callvirt, property.GetGetMethod());//Load object value

                        if (property.PropertyType.IsValueType)
                        {
                            ilGen.Emit(OpCodes.Box, property.PropertyType);
                            if (property.PropertyType.IsEnum)
                            {
                                if (ReflectTool.GetAttr<JsonEnumToStringAttribute>(property, null) != null)
                                {
                                    ilGen.Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString"));
                                }
                            }
                        }
                        SetValue(ilGen, toStringMethod, setMethod, property.Name);
                    }
                }
            }

            List<FieldInfo> fields = ReflectTool.GetFieldList(entityType);
            if (fields != null && fields.Count > 0)
            {
                foreach (var field in fields)
                {
                    ilGen.Emit(OpCodes.Ldarg_1);
                    ilGen.Emit(OpCodes.Ldfld, field);
                    //-----------------------------------------
                    if (field.FieldType.IsValueType)
                    {
                        ilGen.Emit(OpCodes.Box, field.FieldType);
                        if (field.FieldType.IsEnum)
                        {
                            if (ReflectTool.GetAttr<JsonEnumToStringAttribute>(null, field) != null)
                            {
                                ilGen.Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString"));
                            }
                        }
                    }
                    SetValue(ilGen, toStringMethod, setMethod, field.Name);
                }
            }

            ilGen.Emit(OpCodes.Ret);
            return dynamicMethod;
        }


        private static void SetValue(ILGenerator ilGen, MethodInfo toStringMethod, MethodInfo setMethod, string name)
        {
            var obj = ilGen.DeclareLocal(typeof(object));//0
            ilGen.Emit(OpCodes.Stloc, obj);//把值存起来到临时变量
            Label labelContinue = ilGen.DefineLabel();
            Label labelGoOn = ilGen.DefineLabel();
            #region 进行BreakOp 判断


            Label labelNull = ilGen.DefineLabel();
            Label labelEmpty = ilGen.DefineLabel();
            Label labelNullOrEmpty = ilGen.DefineLabel();
            //BreakOp None=-1，Null=0，Empty=1，NullOrEmpty=2
            var lables = new Label[] { labelNull, labelEmpty, labelNullOrEmpty };//0、1、2
            ilGen.Emit(OpCodes.Ldarg_2);  // 将传入的枚举值加载到堆栈上
            ilGen.Emit(OpCodes.Switch, lables);  // 使用Switch指令根据枚举值进行跳转

            //default
            ilGen.Emit(OpCodes.Br, labelGoOn);

            //// 处理 Null
            ilGen.MarkLabel(labelNull);
            ilGen.Emit(OpCodes.Ldloc, obj);
            ilGen.Emit(OpCodes.Brfalse, labelContinue); // break out of switch
            ilGen.Emit(OpCodes.Br, labelGoOn);


            //// 处理 Empty
            ilGen.MarkLabel(labelEmpty);
            ilGen.Emit(OpCodes.Ldloc, obj);
            ilGen.Emit(OpCodes.Brfalse, labelGoOn); // break out of switch
            ilGen.Emit(OpCodes.Ldloc, obj);
            ilGen.Emit(OpCodes.Call, toStringMethod);
            ilGen.Emit(OpCodes.Ldstr, "");
            ilGen.Emit(OpCodes.Ceq);
            ilGen.Emit(OpCodes.Brtrue, labelContinue); // break out of switch
            ilGen.Emit(OpCodes.Br, labelGoOn);

            ////// 处理 NullOrEmpty
            ilGen.MarkLabel(labelNullOrEmpty);
            ilGen.Emit(OpCodes.Ldloc, obj);
            ilGen.Emit(OpCodes.Brfalse, labelContinue); // break out of switch
            ilGen.Emit(OpCodes.Ldloc, obj);
            ilGen.Emit(OpCodes.Call, toStringMethod);
            ilGen.Emit(OpCodes.Ldstr, "");
            ilGen.Emit(OpCodes.Ceq);
            ilGen.Emit(OpCodes.Brtrue, labelContinue); // break out of switch
            ilGen.Emit(OpCodes.Br, labelGoOn);


            #endregion

            //-----------------------------------------
            ilGen.MarkLabel(labelGoOn);
            ilGen.Emit(OpCodes.Ldarg_0);// load MDataRow。
            ilGen.Emit(OpCodes.Ldstr, name);//Load name
            ilGen.Emit(OpCodes.Ldloc, obj);//Load ObjValue
            ilGen.Emit(OpCodes.Ldarg_3);  // 将传入的枚举值加载到堆栈上
            ilGen.Emit(OpCodes.Callvirt, setMethod);//row.SetItemValue(string,object,int)
            ilGen.MarkLabel(labelContinue);
        }

    }
}
