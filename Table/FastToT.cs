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
    /// ����ת����[������Խ��[500����],����Խ��]
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
        /// ����һ��ORMʵ��ת��������1�ι�����һ������ʱ�䣩
        /// </summary>
        /// <param name="tType">ת����Ŀ������</param>
        /// <param name="schema">�����ݼܹ�</param>
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

            ILGenerator gen = method.GetILGenerator();//��ʼ��дIL������

            gen.DeclareLocal(tType);//0
            gen.DeclareLocal(typeof(object));//1
            gen.DeclareLocal(typeof(bool)); //2   �ֱ�����һ�� ʵ�壺Type t,����ֵ��object o,ֵ�Ƿ�ΪNull��bool b��ֵ���ͣ�Type
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
                    foreach (FieldInfo f in fileds)//������ȥ�ظ���
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
                    Label retFalse = gen.DefineLabel();//�����ǩ��goto;
                    gen.Emit(OpCodes.Ldarg_0);//���ò���0 ��row ������Ϊ 0 ���Ա������ص������ջ�ϡ�
                    if (schema == null)
                    {
                        gen.Emit(OpCodes.Ldstr, fieldName);//���ò���ֵ��string ���Ͷ�Ԫ�����д洢���ַ������¶������á�
                    }
                    else
                    {
                        gen.Emit(OpCodes.Ldc_I4, ordinal);//���ò���ֵ��1 int �����ṩ�� int32 ���͵�ֵ��Ϊ int32 ���͵������ջ�ϡ�
                    }

                    gen.Emit(OpCodes.Call, getValue);//Call GetItemValue(ordinal);=> invoke(row,1)
                    gen.Emit(OpCodes.Stloc_1); // o=GetItemValue(ordinal); �Ӽ����ջ�Ķ���������ǰֵ������洢������ 1 ���ľֲ������б��С�

                    gen.Emit(OpCodes.Ldloc_1);//������ 1 ���ľֲ��������ص������ջ�ϡ�
                    gen.Emit(OpCodes.Ldnull);//�������ã�O ���ͣ����͵������ջ�ϡ�
                    gen.Emit(OpCodes.Ceq);// if (o==null) �Ƚ�����ֵ�� ���������ֵ��ȣ�������ֵ 1 (int32) ���͵������ջ�ϣ����򣬽� 0 (int32) ���͵������ջ�ϡ�
                    gen.Emit(OpCodes.Stloc_2); //b=(o==null); �Ӽ����ջ�Ķ���������ǰֵ������洢������ 2 ���ľֲ������б��С�
                    gen.Emit(OpCodes.Ldloc_2);//������ 2 ���ľֲ��������ص������ջ�ϡ�

                    gen.Emit(OpCodes.Brtrue_S, retFalse);//Ϊnullֵ������  ��� value Ϊ true���ǿջ���㣬�򽫿���ת�Ƶ�Ŀ��ָ��̸�ʽ����

                    //-------------������o=ConvertTool.ChangeType(o, t);
                    gen.Emit(OpCodes.Nop);//����޲������룬�����ռ䡣 ���ܿ������Ĵ������ڣ���δִ���κ�������Ĳ�����
                    gen.Emit(OpCodes.Ldtoken, field.FieldType);//������ҿ����е�á���Ԫ���ݱ��ת��Ϊ������ʱ��ʾ��ʽ�����������͵������ջ�ϡ�
                    gen.Emit(OpCodes.Stloc_3);

                    gen.Emit(OpCodes.Ldloc_1);//o
                    gen.Emit(OpCodes.Ldloc_3);
                    gen.Emit(OpCodes.Call, changeType);//Call ChangeType(o,type);=> invoke(o,type) �����ɴ��ݵķ���˵����ָʾ�ķ�����
                    gen.Emit(OpCodes.Stloc_1); // o=GetItemValue(ordinal);
                    //-------------------------------------------
                    gen.Emit(OpCodes.Ldloc_0);//ʵ�����obj
                    gen.Emit(OpCodes.Ldloc_1);//���Ե�ֵ objvalue
                    EmitCastObj(gen, field.FieldType);//����ת��
                    gen.Emit(OpCodes.Stfld, field);//��ʵ�帳ֵ System.Object.FieldSetter(String typeName, String fieldName, Object val)

                    gen.MarkLabel(retFalse);//������һ��ѭ��
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
        /* Emit ����ԭ�ͣ������棩
         public ETable RowToT(MDataRow row)
        {
            Type ttt = typeof(Int32);
            Type t = ttt;
            ETable et;
            object o;
            bool b;
            et = new ETable();
            label:
         //ѭ���������ɡ�
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
