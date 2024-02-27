using CYQ.Data.Json;
using CYQ.Data.Orm;
using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace CYQ.Data.Emit
{
    internal class JsonHelperFillEntity
    {
        static Dictionary<Type, Action<JsonHelper, object>> typeFuncs = new Dictionary<Type, Action<JsonHelper, object>>();


        private static readonly object lockDicObj = new object();
        public static Action<JsonHelper, object> Delegate(Type entityType)
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
                var func = (Action<JsonHelper, object>)dynamicMethod.CreateDelegate(typeof(Action<JsonHelper, object>));
                typeFuncs.Add(entityType, func);
                return func;
            }
        }
        private static DynamicMethod CreateDynamicMethod(Type entityType)
        {
            var jsonType = typeof(JsonHelper);
            var addObjMethod = jsonType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string), typeof(object) }, null);
            var addIntMethod = jsonType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string), typeof(int) }, null);
            var addLongMethod = jsonType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string), typeof(long) }, null);
            var addBoolMethod = jsonType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string), typeof(bool) }, null);
            var addDateTimeMethod = jsonType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string), typeof(DateTime) }, null);
            var addGuidMethod = jsonType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string), typeof(Guid) }, null);
            var addStringMethod = jsonType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string), typeof(string) }, null);
            var dynamicMethod = new DynamicMethod("JsonHelperFillEntity", typeof(void), new[] { jsonType, typeof(object) }, entityType);
            var ilGen = dynamicMethod.GetILGenerator();

            List<PropertyInfo> properties = ReflectTool.GetPropertyList(entityType);
            if (properties.Count > 0)
            {
                foreach (var property in properties)
                {
                    ilGen.Emit(OpCodes.Ldarg_0);//load JsonHelper 
                    ilGen.Emit(OpCodes.Ldstr, property.Name);//load name
                    //----------------------------------------------------
                    ilGen.Emit(OpCodes.Ldarg_1);// load Entity object
                    ilGen.Emit(OpCodes.Castclass, entityType);// object as Entity
                    ilGen.Emit(OpCodes.Callvirt, property.GetGetMethod());// xxx.Name get value.
                    SetValue(ilGen, property, null, addObjMethod, addStringMethod, addIntMethod, addLongMethod, addDateTimeMethod, addBoolMethod, addGuidMethod);
                }
            }

            List<FieldInfo> fields = ReflectTool.GetFieldList(entityType);
            if (fields.Count > 0)
            {
                foreach (var field in fields)
                {
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldstr, field.Name);
                    //----------------------------------------------------
                    ilGen.Emit(OpCodes.Ldarg_1);
                    ilGen.Emit(OpCodes.Castclass, entityType);
                    ilGen.Emit(OpCodes.Ldfld, field);
                    SetValue(ilGen, null, field, addObjMethod, addStringMethod, addIntMethod, addLongMethod, addDateTimeMethod, addBoolMethod, addGuidMethod);

                }
            }

            ilGen.Emit(OpCodes.Ret);
            return dynamicMethod;
        }

        private static void SetValue(ILGenerator ilGen, PropertyInfo pi, FieldInfo fi, MethodInfo addObjMethod,
            MethodInfo addStringMethod, MethodInfo addIntMethod, MethodInfo addLongMethod, MethodInfo addDateTimeMethod, MethodInfo addBoolMethod, MethodInfo addGuidMethod)
        {
            Type type = pi != null ? pi.PropertyType : fi.FieldType;
            if (type.IsValueType)
            {
                if (type.IsEnum)
                {
                    if (ReflectTool.GetAttr<JsonEnumToStringAttribute>(pi, fi) != null)
                    {
                        ilGen.Emit(OpCodes.Box, type);
                        ilGen.Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString"));
                        ilGen.Emit(OpCodes.Callvirt, addStringMethod);
                        return;
                    }
                    ilGen.Emit(OpCodes.Callvirt, addIntMethod);
                }
                else
                {

                    switch (type.Name)
                    {
                        case "UInt16":
                        case "Int16":
                        case "UInt32":
                        case "Int32":
                            ilGen.Emit(OpCodes.Callvirt, addIntMethod);
                            break;
                        case "UInt64":
                        case "Int64":
                            ilGen.Emit(OpCodes.Callvirt, addLongMethod);
                            break;
                        case "DateTime":
                            ilGen.Emit(OpCodes.Callvirt, addDateTimeMethod);
                            break;
                        case "Boolean":
                            ilGen.Emit(OpCodes.Callvirt, addBoolMethod);
                            break;
                        case "Guid":
                            ilGen.Emit(OpCodes.Callvirt, addGuidMethod);
                            break;
                        default:
                            ilGen.Emit(OpCodes.Box, type);
                            ilGen.Emit(OpCodes.Callvirt, addObjMethod);
                            break;
                    }
                }

            }
            else if (type.Name == "String")
            {
                ilGen.Emit(OpCodes.Callvirt, addStringMethod);
            }
            else
            {
                ilGen.Emit(OpCodes.Callvirt, addObjMethod);//dic.Add(string,object)
            }
        }
    }
}
