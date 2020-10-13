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
            Type toolType = typeof(ConvertTool);
            DynamicMethod method = new DynamicMethod("RowToT", tType, new Type[] { rowType }, tType);


            MethodInfo getValue = rowType.GetMethod("GetItemValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(int) }, null);
            MethodInfo changeType = toolType.GetMethod("ChangeType", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(object),typeof(Type) }, null);

            ILGenerator gen = method.GetILGenerator();//开始编写IL方法。

            gen.DeclareLocal(tType);//0
            gen.DeclareLocal(typeof(object));//1
            gen.DeclareLocal(typeof(bool)); //2   分别定义了一个 实体：Type t,属性值：object o,值是否为Null：bool b，值类型：Type
            gen.DeclareLocal(typeof(Type));//3
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
                if(fieldName[0]=='<')//<ID>k__BackingField
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
                    gen.Emit(OpCodes.Ldarg_0);//设置参数0 ：row
                    gen.Emit(OpCodes.Ldc_I4, ordinal);//设置参数值：1
                    gen.Emit(OpCodes.Call, getValue);//Call GetItemValue(ordinal);=> invoke(row,1)
                    gen.Emit(OpCodes.Stloc_1); // o=GetItemValue(ordinal);

                    gen.Emit(OpCodes.Ldloc_1);
                    gen.Emit(OpCodes.Ldnull);
                    gen.Emit(OpCodes.Ceq);// if (o==null)
                    gen.Emit(OpCodes.Stloc_2); //b=(o==null);
                    gen.Emit(OpCodes.Ldloc_2);

                    gen.Emit(OpCodes.Brtrue_S, retFalse);//为null值，跳过

                    //-------------新增：o=ConvertTool.ChangeType(o, t);
                    gen.Emit(OpCodes.Nop);
                    gen.Emit(OpCodes.Ldtoken, field.FieldType);//这个卡我卡的有点久。
                    gen.Emit(OpCodes.Stloc_3);

                    gen.Emit(OpCodes.Ldloc_1);//o
                    gen.Emit(OpCodes.Ldloc_3);
                    gen.Emit(OpCodes.Call, changeType);//Call ChangeType(o,type);=> invoke(o,type)
                    gen.Emit(OpCodes.Stloc_1); // o=GetItemValue(ordinal);
                    //-------------------------------------------
                    gen.Emit(OpCodes.Ldloc_0);//实体对象obj
                    gen.Emit(OpCodes.Ldloc_1);//属性的值 objvalue
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
}
