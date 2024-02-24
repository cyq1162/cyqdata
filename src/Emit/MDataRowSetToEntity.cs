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
    /// ʵ��������м�������
    /// </summary>
    internal class MDataRowSetToEntity
    {
        private static MDictionary<string, Action<MDataRow, object>> typeFuncs = new MDictionary<string, Action<MDataRow, object>>();
        private static readonly object lockObj = new object();
        public static Action<MDataRow, object> Delegate(Type t, MDataColumn schema)
        {
            string key = t.FullName + (schema == null ? "" : schema.GetHashCode().ToString());
            if (typeFuncs.ContainsKey(key))
            {
                return typeFuncs[key];
            }
            lock (lockObj)
            {
                if (typeFuncs.ContainsKey(key))
                {
                    return typeFuncs[key];
                }
                DynamicMethod method = CreateMethod(t, schema);
                var func = method.CreateDelegate(typeof(Action<MDataRow, object>)) as Action<MDataRow, object>;
                typeFuncs.Add(key, func);
                return func;
            }
        }

        private static DynamicMethod CreateMethod(Type entityType, MDataColumn schema)
        {

            #region ������̬����

            Type rowType = typeof(MDataRow);
            Type toolType = typeof(ConvertTool);
            DynamicMethod method = new DynamicMethod("MDataRowSetToEntity", typeof(void), new Type[] { rowType, typeof(object)}, entityType);
            MethodInfo getValue = null;
            if (schema == null)
            {
                getValue = rowType.GetMethod("GetItemValue", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null);
            }
            else
            {
                getValue = rowType.GetMethod("GetItemValue", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(int) }, null);
            }
            MethodInfo changeType = toolType.GetMethod("ChangeType", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(object), typeof(Type) }, null);

            ILGenerator gen = method.GetILGenerator();//��ʼ��дIL������

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
                    SetValueByRow(gen, schema, getValue, changeType, property, null);
                }
            }
            List<FieldInfo> fields = ReflectTool.GetFieldList(entityType);
            if (fields.Count > 0)
            {
                foreach (var field in fields)
                {
                    SetValueByRow(gen, schema, getValue, changeType, null, field);
                }
            }

            //gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ret);


            //List<FieldInfo> fileds = new List<FieldInfo>();
            //fileds.AddRange(tType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
            //if (tType.BaseType.Name != "Object" && tType.BaseType.Name != "OrmBase")
            //{
            //    FieldInfo[] items = tType.BaseType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            //    foreach (FieldInfo item in items)
            //    {
            //        bool isAdd = true;
            //        foreach (FieldInfo f in fileds)//������ȥ�ظ���
            //        {
            //            if (item.Name == f.Name)
            //            {
            //                isAdd = false;
            //                break;
            //            }
            //        }

            //        if (isAdd)
            //        {
            //            fileds.Add(item);
            //        }
            //    }
            //}
            //foreach (FieldInfo field in fileds)
            //{
            //    string fieldName = field.Name;
            //    if (fieldName[0] == '<')//<ID>k__BackingField
            //    {
            //        fieldName = fieldName.Substring(1, fieldName.IndexOf('>') - 1);
            //    }

            //    Label retFalse = gen.DefineLabel();//�����ǩ��goto;
            //    gen.Emit(OpCodes.Ldarg_0);//���ò���0 ��row ������Ϊ 0 ���Ա������ص������ջ�ϡ�

            //    gen.Emit(OpCodes.Ldstr, fieldName);//���ò���ֵ��string ���Ͷ�Ԫ�����д洢���ַ������¶������á�


            //    gen.Emit(OpCodes.Call, getValue);//Call GetItemValue(ordinal);=> invoke(row,1)
            //    gen.Emit(OpCodes.Stloc_1); // o=GetItemValue(ordinal); �Ӽ����ջ�Ķ���������ǰֵ������洢������ 1 ���ľֲ������б��С�

            //    gen.Emit(OpCodes.Ldloc_1);//������ 1 ���ľֲ��������ص������ջ�ϡ�
            //    gen.Emit(OpCodes.Ldnull);//�������ã�O ���ͣ����͵������ջ�ϡ�
            //    gen.Emit(OpCodes.Ceq);// if (o==null) �Ƚ�����ֵ�� ���������ֵ��ȣ�������ֵ 1 (int32) ���͵������ջ�ϣ����򣬽� 0 (int32) ���͵������ջ�ϡ�
            //    gen.Emit(OpCodes.Stloc_2); //b=(o==null); �Ӽ����ջ�Ķ���������ǰֵ������洢������ 2 ���ľֲ������б��С�
            //    gen.Emit(OpCodes.Ldloc_2);//������ 2 ���ľֲ��������ص������ջ�ϡ�

            //    gen.Emit(OpCodes.Brtrue_S, retFalse);//Ϊnullֵ������  ��� value Ϊ true���ǿջ���㣬�򽫿���ת�Ƶ�Ŀ��ָ��̸�ʽ����

            //    //-------------������o=ConvertTool.ChangeType(o, t);
            //    gen.Emit(OpCodes.Nop);//����޲������룬�����ռ䡣 ���ܿ������Ĵ������ڣ���δִ���κ�������Ĳ�����
            //    gen.Emit(OpCodes.Ldtoken, field.FieldType);//������ҿ����е�á���Ԫ���ݱ��ת��Ϊ������ʱ��ʾ��ʽ�����������͵������ջ�ϡ�
            //    gen.Emit(OpCodes.Stloc_3);

            //    gen.Emit(OpCodes.Ldloc_1);//o
            //    gen.Emit(OpCodes.Ldloc_3);
            //    gen.Emit(OpCodes.Call, changeType);//Call ChangeType(o,type);=> invoke(o,type) �����ɴ��ݵķ���˵����ָʾ�ķ�����
            //    gen.Emit(OpCodes.Stloc_1); // o=GetItemValue(ordinal);
            //                               //-------------------------------------------
            //    gen.Emit(OpCodes.Ldloc_0);//ʵ�����obj
            //    gen.Emit(OpCodes.Ldloc_1);//���Ե�ֵ objvalue
            //    EmitCastObj(gen, field.FieldType);//����ת��
            //    gen.Emit(OpCodes.Stfld, field);//��ʵ�帳ֵ System.Object.FieldSetter(String typeName, String fieldName, Object val)

            //    gen.MarkLabel(retFalse);//������һ��ѭ��

            //}

            //gen.Emit(OpCodes.Ldloc_0);
            //gen.Emit(OpCodes.Ret);
            #endregion

            return method;
        }
        private static void SetValueByRow(ILGenerator gen, MDataColumn schema, MethodInfo getValue, MethodInfo changeType, PropertyInfo pi, FieldInfo fi)
        {
            Type valueType = pi != null ? pi.PropertyType : fi.FieldType;
            string fieldName = pi != null ? pi.Name : fi.Name;
            int ordinal = -1;
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
                Label labelContinue = gen.DefineLabel();//�����ǩ��goto;
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
                gen.Emit(OpCodes.Brfalse, labelContinue); // break out of switch


                //-------------������o=ConvertTool.ChangeType(o, t);
                gen.Emit(OpCodes.Nop);//����޲������룬�����ռ䡣 ���ܿ������Ĵ������ڣ���δִ���κ�������Ĳ�����
                gen.Emit(OpCodes.Ldtoken, valueType);//������ҿ����е�á���Ԫ���ݱ��ת��Ϊ������ʱ��ʾ��ʽ�����������͵������ջ�ϡ�
                gen.Emit(OpCodes.Stloc_2);

                gen.Emit(OpCodes.Ldloc_1);//o
                gen.Emit(OpCodes.Ldloc_2);
                gen.Emit(OpCodes.Call, changeType);//Call ChangeType(o,type);=> invoke(o,type) �����ɴ��ݵķ���˵����ָʾ�ķ�����
                gen.Emit(OpCodes.Stloc_1); // o=GetItemValue(ordinal);
                                           //-------------------------------------------
                #region Ϊ��������ֵ
                SetValue(gen, pi, fi);
                #endregion

                gen.MarkLabel(labelContinue);//������һ��ѭ��

            }
        }
        private static void SetValue(ILGenerator gen, PropertyInfo pi, FieldInfo fi)
        {
            if (pi != null)
            {
                gen.Emit(OpCodes.Ldloc_0);//ʵ�����obj
                gen.Emit(OpCodes.Ldloc_1);//���Ե�ֵ objvalue
                EmitCastObj(gen, pi.PropertyType);//����ת��
                gen.EmitCall(OpCodes.Callvirt, pi.GetSetMethod(), null); // Call the property setter
            }
            if (fi != null)
            {
                gen.Emit(OpCodes.Ldloc_0);//ʵ�����obj
                gen.Emit(OpCodes.Ldloc_1);//���Ե�ֵ objvalue
                EmitCastObj(gen, fi.FieldType);//����ת��
                gen.Emit(OpCodes.Stfld, fi);//��ʵ�帳ֵ System.Object.FieldSetter(String typeName, String fieldName, Object val)
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