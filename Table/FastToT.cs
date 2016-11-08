using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using CYQ.Data.Table;

namespace CYQ.Data.Tool
{
    /// <summary>
    /// 快速转换类[数据量越大[500条起],性能越高]
    /// </summary>
    internal class FastToT<T>
    {
        public delegate T EmitHandle(MDataRow row);
        /// <summary>
        /// 构建一个ORM实体转换器
        /// </summary>
        /// <typeparam name="T">转换的目标类型</typeparam>
        /// <param name="schema">表数据架构</param>
        public static EmitHandle Create(MDataTable schema)
        {
            Type tType = typeof(T);
            Type rowType = typeof(MDataRow);
            DynamicMethod method = new DynamicMethod("RowToT", tType, new Type[] { rowType }, tType);


            MethodInfo getValue = rowType.GetMethod("GetItemValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(int) }, null);


            ILGenerator gen = method.GetILGenerator();//开始编写IL方法。

            gen.DeclareLocal(tType);
            gen.DeclareLocal(typeof(object));
            gen.DeclareLocal(typeof(bool)); //分别定义了一个Type t,object o,bool b;

            gen.Emit(OpCodes.Newobj, tType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null));
            gen.Emit(OpCodes.Stloc_0);//t= new T();
            int ordinal = -1;

            List<FieldInfo> fileds = new List<FieldInfo>();
            fileds.AddRange(tType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
            if (tType.BaseType.Name != "Object" && tType.BaseType.Name != "OrmBase")
            {
                FieldInfo[] items = tType.BaseType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (FieldInfo item in items)
                {
                    bool isAdd = true;
                    foreach (FieldInfo f in fileds)//这里检测去重复。
                    {
                        if (item.Name == f.Name)
                        {
                            isAdd = false;
                            break;
                        }
                    }
                    
                    if (isAdd)
                    {
                        fileds.Add(item);
                    }
                }
            }
            foreach (FieldInfo field in fileds)
            {
                string fieldName = field.Name;
                if(fieldName[0]=='<')
                {
                    fieldName=fieldName.Substring(1,fieldName.IndexOf('>')-1);
                }
                ordinal = schema.Columns.GetIndex(fieldName.TrimStart('_'));
                if (ordinal == -1)
                {
                    ordinal = schema.Columns.GetIndex(fieldName);
                }
                if (ordinal > -1)
                {
                    Label retFalse = gen.DefineLabel();//定义标签；goto;
                    gen.Emit(OpCodes.Ldarg_0);
                    gen.Emit(OpCodes.Ldc_I4, ordinal);
                    gen.Emit(OpCodes.Call, getValue);//Call GetItemValue(ordinal);
                    gen.Emit(OpCodes.Stloc_1); // o=GetItemValue(ordinal);

                    gen.Emit(OpCodes.Ldloc_1);
                    gen.Emit(OpCodes.Ldnull);
                    gen.Emit(OpCodes.Ceq);// if (o==null)
                    gen.Emit(OpCodes.Stloc_2); //b=o==null;
                    gen.Emit(OpCodes.Ldloc_2);

                    gen.Emit(OpCodes.Brtrue_S, retFalse);//为null值，跳过

                    gen.Emit(OpCodes.Ldloc_0);//实体对象
                    gen.Emit(OpCodes.Ldloc_1);//属性的值
                    EmitCastObj(gen, field.FieldType);//类型转换
                    gen.Emit(OpCodes.Stfld, field);//对实体赋值 System.Object.FieldSetter(String typeName, String fieldName, Object val)

                    gen.MarkLabel(retFalse);//继续下一个循环
                }
            }

            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ret);

            return method.CreateDelegate(typeof(EmitHandle)) as EmitHandle;
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
