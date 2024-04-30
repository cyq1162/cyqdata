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
    /// ������תʵ�� ������ʵ�����г�Ա������FieldInfo��
    /// </summary>
    internal class MDataRowToEntity
    {
        /*
         ȥ����MDataColumn schema ����˵����
             1�������Ļ������Ը��ݸò�������ת������ͨ������ȡֵ��������ܸ��ţ������ǲ�ѯ�����ṹ�Ŀ��ܶ�����������ÿ�ζ�Ҫ�����µ�ί�С�
             2���������Ļ���ͨ���Ը�ǿ������ί�е�
         
         */

        private static Dictionary<Type, Func<MDataRow, object>> emitHandleDic = new Dictionary<Type, Func<MDataRow, object>>();
        private static readonly object lockObj = new object();

        public static Func<MDataRow, object> Delegate(Type t)
        {
            if (emitHandleDic.ContainsKey(t))
            {
                return emitHandleDic[t];
            }
            lock (lockObj)
            {
                if (emitHandleDic.ContainsKey(t))
                {
                    return emitHandleDic[t];
                }
                DynamicMethod method = CreateMethod(t);
                var handle = method.CreateDelegate(typeof(Func<MDataRow, object>)) as Func<MDataRow, object>;
                emitHandleDic.Add(t, handle);
                return handle;
            }
        }

        /// <summary>
        /// ����һ��ORMʵ��ת��������1�ι�����һ������ʱ�䣩
        /// </summary>
        /// <param name="entityType">ת����Ŀ������</param>
        private static DynamicMethod CreateMethod(Type entityType)
        {

            Type rowType = typeof(MDataRow);
            Type toolType = typeof(ConvertTool);
            DynamicMethod method = new DynamicMethod("MDataRowToEntity", entityType, new Type[] { rowType }, entityType);
            MethodInfo getValue = rowType.GetMethod("GetItemValue", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string) }, null);
            MethodInfo changeType = toolType.GetMethod("ChangeType", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(object), typeof(Type) }, null);
            MethodInfo getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");

            var constructor = entityType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);

            ILGenerator gen = method.GetILGenerator();//��ʼ��дIL������
            if (constructor == null)
            {
                gen.Emit(OpCodes.Ret);
                return method;
            }

            gen.DeclareLocal(entityType);//0
            gen.DeclareLocal(typeof(object));//1
            gen.DeclareLocal(typeof(Type));//2
            gen.Emit(OpCodes.Newobj, constructor);
            gen.Emit(OpCodes.Stloc_0);//t= new T();

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

            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ret);


            return method;
        }

        private static void SetValueByRow(ILGenerator gen, MethodInfo getValue, MethodInfo changeType, MethodInfo getTypeFromHandle, PropertyInfo pi, FieldInfo fi)
        {
            Type valueType = pi != null ? pi.PropertyType : fi.FieldType;
            string fieldName = pi != null ? pi.Name : fi.Name;

            Label labelContinue = gen.DefineLabel();//�����ǩ��goto;
            gen.Emit(OpCodes.Ldarg_0);//���ò���0 ��row ������Ϊ 0 ���Ա������ص������ջ�ϡ�

            gen.Emit(OpCodes.Ldstr, fieldName);//���ò���ֵ��string ���Ͷ�Ԫ�����д洢���ַ������¶������á�

            gen.Emit(OpCodes.Call, getValue);//Call GetItemValue(ordinal);=> invoke(row,1)
            gen.Emit(OpCodes.Stloc_1); // o=GetItemValue(ordinal); �Ӽ����ջ�Ķ���������ǰֵ������洢������ 1 ���ľֲ������б��С�

            gen.Emit(OpCodes.Ldloc_1);//������ 1 ���ľֲ��������ص������ջ�ϡ�
            gen.Emit(OpCodes.Brfalse, labelContinue); // break out of switch

            if (valueType.Name != "Object")
            {
                //-------------������o=ConvertTool.ChangeType(o, t);
                //gen.Emit(OpCodes.Nop);//����޲������룬�����ռ䡣 ���ܿ������Ĵ������ڣ���δִ���κ�������Ĳ�����
                gen.Emit(OpCodes.Ldtoken, valueType);//������ҿ����е�á���Ԫ���ݱ��ת��Ϊ������ʱ��ʾ��ʽ�����������͵������ջ�ϡ�
                                                     //�������Call������� .net ���׵��쳣���޷���ȡTypeֵ�����Զ�ȡ��д���ܱ������ڴ档��ͨ��ָʾ�����ڴ����𻵡�
                gen.Emit(OpCodes.Call, getTypeFromHandle);
                gen.Emit(OpCodes.Stloc_2);

                gen.Emit(OpCodes.Ldloc_1);//o
                gen.Emit(OpCodes.Ldloc_2);
                gen.Emit(OpCodes.Call, changeType);//Call ChangeType(o,type);=> invoke(o,type) �����ɴ��ݵķ���˵����ָʾ�ķ�����
                gen.Emit(OpCodes.Stloc_1); // o=GetItemValue(ordinal);
                                           //-------------------------------------------
            }
            #region Ϊ��������ֵ
            SetValue(gen, pi, fi);
            #endregion

            gen.MarkLabel(labelContinue);//������һ��ѭ��


        }

        private static void SetValue(ILGenerator gen, PropertyInfo pi, FieldInfo fi)
        {
            if (pi != null && pi.CanWrite)
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
