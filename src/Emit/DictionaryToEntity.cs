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
    /// �ֵ�תʵ�壨����ʵ�����г�Ա������FieldInfo��
    /// </summary>
    internal class DictionaryToEntity
    {

        static Dictionary<Type, Func<Dictionary<string, string>, object>> typeFuncs = new Dictionary<Type, Func<Dictionary<string, string>, object>>();

        private static readonly object lockObj = new object();

        public static Func<Dictionary<string, string>, object> Delegate(Type t)
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
                var func = method.CreateDelegate(typeof(Func<Dictionary<string, string>, object>)) as Func<Dictionary<string, string>, object>;
                typeFuncs.Add(t, func);
                return func;
            }
        }

        /// <summary>
        /// ����һ��ORMʵ��ת��������1�ι�����һ������ʱ�䣩
        /// </summary>
        /// <param name="entityType">ת����Ŀ������</param>
        private static DynamicMethod CreateDynamicMethod(Type entityType)
        {
            #region ������̬����

            var dicType = typeof(Dictionary<string, string>);
            Type convertToolType = typeof(ConvertTool);
            DynamicMethod method = new DynamicMethod("DictionaryToEntity", typeof(object), new Type[] { dicType }, entityType);
            MethodInfo getValue = dicType.GetMethod("TryGetValue");
            MethodInfo changeType = convertToolType.GetMethod("ChangeType", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(object), typeof(Type) }, null);
            MethodInfo getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");

            var constructor = entityType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null);

            ILGenerator gen = method.GetILGenerator();//��ʼ��дIL������
            if (constructor == null)
            {
                gen.Emit(OpCodes.Ret);
                return method;
            }

            var instance = gen.DeclareLocal(entityType);//0 �� Entity t0;
            gen.DeclareLocal(typeof(string));//1 string s1;
            gen.DeclareLocal(typeof(Type));//2   Type t2;
            gen.Emit(OpCodes.Newobj, constructor);
            gen.Emit(OpCodes.Stloc_0, instance);//t0= new T();

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

            gen.Emit(OpCodes.Ldloc_0, instance);//t0 ���أ�׼������
            gen.Emit(OpCodes.Ret);
            #endregion

            return method;
        }
        private static void SetValueByRow(ILGenerator gen, MethodInfo getValue, MethodInfo changeType, MethodInfo getTypeFromHandle, PropertyInfo pi, FieldInfo fi)
        {
            Type valueType = pi != null ? pi.PropertyType : fi.FieldType;
            string fieldName = pi != null ? pi.Name : fi.Name;

            Label labelContinue = gen.DefineLabel();//����ѭ����ǩ��goto;


            gen.Emit(OpCodes.Ldarg_0);//���� dic ����
            gen.Emit(OpCodes.Ldstr, fieldName);//�����ֶ�����
            gen.Emit(OpCodes.Ldloca_S, 1);// ���� out ֵ
            gen.Emit(OpCodes.Call, getValue);//bool a=dic.tryGetValue(...,out value)
            gen.Emit(OpCodes.Brfalse_S, labelContinue);//if(!a){continue;}


            gen.Emit(OpCodes.Ldloc_1);//������ 1 ���ľֲ��������ص������ջ�ϡ�
            gen.Emit(OpCodes.Brfalse, labelContinue); // break out of switch

            //-------------������o=ConvertTool.ChangeType(o, t);
            if (valueType.Name != "String")
            {
                //gen.Emit(OpCodes.Nop);//����޲������룬�����ռ䡣 ���ܿ������Ĵ������ڣ���δִ���κ�������Ĳ�����
                gen.Emit(OpCodes.Ldtoken, valueType);//������ҿ����е�á���Ԫ���ݱ��ת��Ϊ������ʱ��ʾ��ʽ�����������͵������ջ�ϡ�
                //�������Call������� .net ���޷���ȡTypeֵ���׵��쳣�����Զ�ȡ��д���ܱ������ڴ档��ͨ��ָʾ�����ڴ����𻵡�
                gen.Emit(OpCodes.Call, getTypeFromHandle);
                gen.Emit(OpCodes.Stloc_2);

                gen.Emit(OpCodes.Ldloc_1);//o
                gen.Emit(OpCodes.Ldloc_2);
                gen.Emit(OpCodes.Call, changeType);//Call ChangeType(o,type);=> invoke(o,type) �����ɴ��ݵķ���˵����ָʾ�ķ�����
                gen.Emit(OpCodes.Stloc_1); // o=GetItemValue(ordinal);
            }
            //-------------------------------------------
            SetValue(gen, pi, fi);
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
