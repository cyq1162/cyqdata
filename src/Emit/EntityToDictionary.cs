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
    /// <summary>
    /// 实体转字典 Dictionary&lt;string, object&gt;
    /// </summary>
    public static partial class EntityToDictionary
    {
        static Dictionary<Type, Func<object, Dictionary<string, object>>> typeFuncs = new Dictionary<Type, Func<object, Dictionary<string, object>>>();

        private static readonly object lockDicObj = new object();
        internal static Func<object, Dictionary<string, object>> Delegate(Type entityType)
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
                var func = (Func<object, Dictionary<string, object>>)dynamicMethod.CreateDelegate(typeof(Func<object, Dictionary<string, object>>));
                typeFuncs.Add(entityType, func);
                return func;
            }
        }
        private static DynamicMethod CreateDynamicMethod(Type entityType)
        {
            var dynamicMethod = new DynamicMethod("EntityToDictionary", typeof(Dictionary<string, object>), new[] { typeof(object) }, entityType);
            var ilGen = dynamicMethod.GetILGenerator();
            var dictionaryCtor = typeof(Dictionary<string, object>).GetConstructor(Type.EmptyTypes);
            var addMethod = typeof(Dictionary<string, object>).GetMethod("Add");
            ilGen.DeclareLocal(typeof(Dictionary<string, object>));

            ilGen.Emit(OpCodes.Newobj, dictionaryCtor);
            ilGen.Emit(OpCodes.Stloc_0);

            List<PropertyInfo> properties = ReflectTool.GetPropertyList(entityType);
            if (properties.Count > 0)
            {
                foreach (var property in properties)
                {
                    if (property.CanRead)
                    {
                        if (property.GetCustomAttributes(typeof(JsonIgnoreAttribute), true).Length > 0)
                        {
                            continue;
                        }
                        ilGen.Emit(OpCodes.Ldloc_0);//load Dicationary 
                        ilGen.Emit(OpCodes.Ldstr, property.Name);//load name
                                                                 //----------------------------------------------------
                        ilGen.Emit(OpCodes.Ldarg_0);// load Entity object
                        ilGen.Emit(OpCodes.Castclass, entityType);// object as Entity
                        ilGen.Emit(OpCodes.Callvirt, property.GetGetMethod());// xxx.Name get value.
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
                        ilGen.Emit(OpCodes.Callvirt, addMethod);//dic.Add(string,object)
                    }
                }
            }

            List<FieldInfo> fields = ReflectTool.GetFieldList(entityType);
            if (fields.Count > 0)
            {
                foreach (var field in fields)
                {
                    if (field.GetCustomAttributes(typeof(JsonIgnoreAttribute), true).Length > 0)
                    {
                        continue;
                    }
                    ilGen.Emit(OpCodes.Ldloc_0);
                    ilGen.Emit(OpCodes.Ldstr, field.Name);
                    //----------------------------------------------------
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Castclass, entityType);
                    ilGen.Emit(OpCodes.Ldfld, field);
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
                    ilGen.Emit(OpCodes.Callvirt, addMethod);
                }
            }

            ilGen.Emit(OpCodes.Ldloc_0);
            ilGen.Emit(OpCodes.Ret);
            return dynamicMethod;
        }
    }

    public static partial class EntityToDictionary
    {
        /// <summary>
        /// 调用返回字典数据。
        /// </summary>
        public static Dictionary<string, object> Invoke(object entity)
        {
            if (entity == null) { return null; }
            var type = entity.GetType();
            if (type.IsArray || type.IsValueType || type.IsGenericType) { return null; }
            var func = Delegate(type);
            if (func == null) { return null; };
            return func(entity);
        }
    }
}
