using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using CYQ.Data.Table;

namespace CYQ.Data.Tool
{
    internal class FastToT<T>
    {
        public delegate T EmitHandle(MDataRow row);
        private static EmitHandle emit;
        public static EmitHandle Create()
        {
            if (emit == null)
            {
                emit = Create(null);
            }
            return emit;
        }
        public static EmitHandle Create(MDataColumn schema)
        {
            DynamicMethod method = FastToT.CreateMethod(typeof(T), schema);
            return method.CreateDelegate(typeof(EmitHandle)) as EmitHandle;
        }
    }
    /// <summary>
    /// 快速转换类[数据量越大[500条起],性能越高]
    /// </summary>
    internal class FastToT
    {
        public delegate object EmitHandle(MDataRow row);
        private static EmitHandle emit;
        public static EmitHandle Create(Type tType)
        {
            if (emit == null)
            {
                emit = Create(tType, null);
            }
            return emit;
        }
        public static EmitHandle Create(Type tType, MDataColumn schema)
        {
            DynamicMethod method = CreateMethod(tType, schema);
            return method.CreateDelegate(typeof(EmitHandle)) as EmitHandle;
        }
        /// <summary>
        /// 构建一个ORM实体转换器（第1次构建有一定开销时间）
        /// </summary>
        /// <param name="tType">转换的目标类型</param>
        /// <param name="schema">表数据架构</param>
        internal static DynamicMethod CreateMethod(Type tType, MDataColumn schema)
        {
            //Type tType = typeof(T);
            Type rowType = typeof(MDataRow);
            Type toolType = typeof(ConvertTool);
            DynamicMethod method = new DynamicMethod("RowToT", tType, new Type[] { rowType }, tType);
            MethodInfo getValue = null;
            if (schema == null)
            {
                getValue = rowType.GetMethod("GetItemValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null);
            }
            else
            {
                getValue = rowType.GetMethod("GetItemValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(int) }, null);
            }

            MethodInfo changeType = toolType.GetMethod("ChangeType", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(object), typeof(Type) }, null);

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
                if (fieldName[0] == '<')//<ID>k__BackingField
                {
                    fieldName = fieldName.Substring(1, fieldName.IndexOf('>') - 1);
                }
                if (schema != null)
                {
                    ordinal = schema.GetIndex(fieldName.TrimStart('_'));
                    if (ordinal == -1)
                    {
                        ordinal = schema.GetIndex(fieldName);
                    }
                }
                if (schema == null || ordinal > -1)
                {
                    Label retFalse = gen.DefineLabel();//定义标签；goto;
                    gen.Emit(OpCodes.Ldarg_0);//设置参数0 ：row 将索引为 0 的自变量加载到计算堆栈上。
                    if (schema == null)
                    {
                        gen.Emit(OpCodes.Ldstr, fieldName);//设置参数值：string 推送对元数据中存储的字符串的新对象引用。
                    }
                    else
                    {
                        gen.Emit(OpCodes.Ldc_I4, ordinal);//设置参数值：1 int 将所提供的 int32 类型的值作为 int32 推送到计算堆栈上。
                    }

                    gen.Emit(OpCodes.Call, getValue);//Call GetItemValue(ordinal);=> invoke(row,1)
                    gen.Emit(OpCodes.Stloc_1); // o=GetItemValue(ordinal); 从计算堆栈的顶部弹出当前值并将其存储到索引 1 处的局部变量列表中。

                    gen.Emit(OpCodes.Ldloc_1);//将索引 1 处的局部变量加载到计算堆栈上。
                    gen.Emit(OpCodes.Ldnull);//将空引用（O 类型）推送到计算堆栈上。
                    gen.Emit(OpCodes.Ceq);// if (o==null) 比较两个值。 如果这两个值相等，则将整数值 1 (int32) 推送到计算堆栈上；否则，将 0 (int32) 推送到计算堆栈上。
                    gen.Emit(OpCodes.Stloc_2); //b=(o==null); 从计算堆栈的顶部弹出当前值并将其存储到索引 2 处的局部变量列表中。
                    gen.Emit(OpCodes.Ldloc_2);//将索引 2 处的局部变量加载到计算堆栈上。

                    gen.Emit(OpCodes.Brtrue_S, retFalse);//为null值，跳过  如果 value 为 true、非空或非零，则将控制转移到目标指令（短格式）。

                    //-------------新增：o=ConvertTool.ChangeType(o, t);
                    gen.Emit(OpCodes.Nop);//如果修补操作码，则填充空间。 尽管可能消耗处理周期，但未执行任何有意义的操作。
                    gen.Emit(OpCodes.Ldtoken, field.FieldType);//这个卡我卡的有点久。将元数据标记转换为其运行时表示形式，并将其推送到计算堆栈上。
                    gen.Emit(OpCodes.Stloc_3);

                    gen.Emit(OpCodes.Ldloc_1);//o
                    gen.Emit(OpCodes.Ldloc_3);
                    gen.Emit(OpCodes.Call, changeType);//Call ChangeType(o,type);=> invoke(o,type) 调用由传递的方法说明符指示的方法。
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
            return method;
            //return method.CreateDelegate(typeof(EmitHandle)) as EmitHandle;
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
