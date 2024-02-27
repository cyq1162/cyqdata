using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using CYQ.Data.Table;
using CYQ.Data.Tool;
using CYQ.Data.SQL;
using System.Data;
using System.Collections;

namespace CYQ.Data.Emit
{
    /// <summary>
    /// 字典转KeyValue
    /// 说明：Dictionary 中无法用 Foreach 功能，距坑，最后转数组处理。
    /// </summary>
    internal class DictionaryToKeyValue
    {
        //public static object Test(Dictionary<string, string> dt, Type kvType)
        //{
        //    DynamicMethod method = CreateDynamicMethod(kvType);
        //    var func = method.CreateDelegate(typeof(Func<Dictionary<string, string>, object>)) as Func<Dictionary<string, string>, object>;
        //    var obj = func(dt);
        //    return obj;
        //}

        static Dictionary<Type, Func<Dictionary<string, string>, object>> typeFuncs = new Dictionary<Type, Func<Dictionary<string, string>, object>>();

        private static readonly object lockObj = new object();

        internal static Func<Dictionary<string, string>, object> Delegate(Type kvType)
        {
            if (typeFuncs.ContainsKey(kvType))
            {
                return typeFuncs[kvType];
            }
            lock (lockObj)
            {
                if (typeFuncs.ContainsKey(kvType))
                {
                    return typeFuncs[kvType];
                }
                DynamicMethod method = CreateDynamicMethod(kvType);
                var func = method.CreateDelegate(typeof(Func<Dictionary<string, string>, object>)) as Func<Dictionary<string, string>, object>;
                typeFuncs.Add(kvType, func);
                return func;
            }
        }

        private static DynamicMethod CreateDynamicMethod(Type kvType)
        {
            Type dicType = typeof(Dictionary<string, string>);
            Type entryType = typeof(KeyValuePair<string, string>);
            Type toolType = typeof(ConvertTool);


            var getChangeTypeMethod = toolType.GetMethod("ChangeType", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(object), typeof(Type) }, null);
            var getTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle");


            MethodInfo getAddMethod = null;
            Type kType = null;
            Type vType = null;
            foreach (var item in kvType.GetMethods())
            {
                if (item.Name == "Add")
                {
                    var paras = item.GetParameters();
                    if (paras.Length == 2)
                    {
                        kType = paras[0].ParameterType;
                        vType = paras[1].ParameterType;
                        getAddMethod = item;
                        break;
                    }
                }
            }

            var method = new DynamicMethod("MDataRowToKeyValue", typeof(object), new[] { dicType }, kvType, true);

            var ilGenerator = method.GetILGenerator();

            // Define variables
            var kvLocal = ilGenerator.DeclareLocal(kvType);
            var kLocal = ilGenerator.DeclareLocal(kType);
            var vLocal = ilGenerator.DeclareLocal(vType);

            var entryLocal = ilGenerator.DeclareLocal(entryType);
            var kTypeLocal = ilGenerator.DeclareLocal(typeof(Type));
            var vTypeLocal = ilGenerator.DeclareLocal(typeof(Type));


            #region New HashTable、KeyValue
            // Create a new instance of the target list type and store it in the itemsLocal variable
            if (kvType.Name.Contains("Dictionary"))
            {
                var stringComparerType = typeof(StringComparer);
                var ordinalIgnoreCaseField = stringComparerType.GetProperty("OrdinalIgnoreCase", BindingFlags.Public | BindingFlags.Static).GetGetMethod();
                var constructorInfo = kvType.GetConstructor(new[] { typeof(IEqualityComparer<string>) }); // 获取 Dictionary<string, string>(IEqualityComparer<string>) 构造函数
                ilGenerator.Emit(OpCodes.Call, ordinalIgnoreCaseField);// 加载 StringComparer.OrdinalIgnoreCase 字段
                ilGenerator.Emit(OpCodes.Newobj, constructorInfo);// 创建 Dictionary<string, string> 实例
            }
            else
            {
                ilGenerator.Emit(OpCodes.Newobj, kvType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null));
            }
            ilGenerator.Emit(OpCodes.Stloc, kvLocal);
            #endregion

            #region 将Keys Copy 到新数组中。
            var keysProperty = typeof(Dictionary<string, string>).GetProperty("Keys");
            var countProperty = typeof(ICollection<string>).GetProperty("Count");
            var copyToMethod = typeof(ICollection<string>).GetMethod("CopyTo");
            var keysLocal = ilGenerator.DeclareLocal(typeof(string[])); // Local variable for keys array

            ilGenerator.Emit(OpCodes.Ldarg_0); // Load dictionary argument
            ilGenerator.Emit(OpCodes.Callvirt, keysProperty.GetGetMethod()); // Call get_Keys method
            ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()); // Get keys count
            ilGenerator.Emit(OpCodes.Newarr, typeof(string)); // Create new string array
            ilGenerator.Emit(OpCodes.Stloc, keysLocal); // Store keys array in local variable

            ilGenerator.Emit(OpCodes.Ldarg_0); // Load dictionary argument
            ilGenerator.Emit(OpCodes.Callvirt, keysProperty.GetGetMethod()); // Call get_Keys method
            ilGenerator.Emit(OpCodes.Ldloc, keysLocal); // Load keys array
            ilGenerator.Emit(OpCodes.Ldc_I4_0); // Load 0
            ilGenerator.Emit(OpCodes.Callvirt, copyToMethod); // Copy keys to array

            #endregion

            #region For(....) Start
            var index = ilGenerator.DeclareLocal(typeof(int)); // Local variable for index
            ilGenerator.Emit(OpCodes.Ldc_I4_0); // Load 0 onto the stack
            ilGenerator.Emit(OpCodes.Stloc, index); // Store 0 in local variable (index)

            var loopStart = ilGenerator.DefineLabel();
            var loopEnd = ilGenerator.DefineLabel();

            ilGenerator.Emit(OpCodes.Br, loopEnd); // Jump to loop end

            ilGenerator.MarkLabel(loopStart);
            #endregion


            //var getRowMethod = rowsType.GetMethod("get_Item");


            #region Get Key

            ilGenerator.Emit(OpCodes.Ldloc, keysLocal); // Load keys array
            ilGenerator.Emit(OpCodes.Ldloc, index); // Load index
            ilGenerator.Emit(OpCodes.Ldelem, typeof(string)); // Load key at index
            ilGenerator.Emit(OpCodes.Stloc, kLocal);

            if (kType.Name != "String")
            {
                //类型转换。
                ilGenerator.Emit(OpCodes.Ldtoken, kType);
                ilGenerator.Emit(OpCodes.Call, getTypeFromHandleMethod);
                ilGenerator.Emit(OpCodes.Stloc, kTypeLocal);

                ilGenerator.Emit(OpCodes.Ldloc, kLocal);
                ilGenerator.Emit(OpCodes.Ldloc, kTypeLocal);
                ilGenerator.Emit(OpCodes.Call, getChangeTypeMethod);
                if (kType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, kType);
                }

                ilGenerator.Emit(OpCodes.Stloc, kLocal);
            }
            #endregion

            #region Get Value
            var getItemMethod = typeof(Dictionary<string, string>).GetMethod("get_Item");
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldloc, kLocal);
            ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);// ilGen.Emit(OpCodes.Ret);
            ilGenerator.Emit(OpCodes.Stloc, vLocal);

            if (vType.Name != "Object")
            {
                //类型转换
                ilGenerator.Emit(OpCodes.Ldtoken, vType);
                ilGenerator.Emit(OpCodes.Call, getTypeFromHandleMethod);
                ilGenerator.Emit(OpCodes.Stloc, vTypeLocal);

                ilGenerator.Emit(OpCodes.Ldloc, vLocal);
                ilGenerator.Emit(OpCodes.Ldloc, vTypeLocal);
                ilGenerator.Emit(OpCodes.Call, getChangeTypeMethod);
                if (vType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, vType);
                }

                ilGenerator.Emit(OpCodes.Stloc, vLocal);
            }
            #endregion

            #region Add To New Key Value

            // Assign the result to the itemsLocal array at the current index

            ilGenerator.Emit(OpCodes.Ldloc, kvLocal); //ilGen.Emit(OpCodes.Ret);
            ilGenerator.Emit(OpCodes.Ldloc, kLocal);
            ilGenerator.Emit(OpCodes.Ldloc, vLocal);
            ilGenerator.Emit(OpCodes.Callvirt, getAddMethod);
            #endregion

            #region For(...) End

            ilGenerator.Emit(OpCodes.Ldloc, index); // Load index
            ilGenerator.Emit(OpCodes.Ldc_I4_1); // Load 1
            ilGenerator.Emit(OpCodes.Add); // Add 1 to index
            ilGenerator.Emit(OpCodes.Stloc, index); // Store updated index

            ilGenerator.MarkLabel(loopEnd);
            ilGenerator.Emit(OpCodes.Ldloc, index); // Load index
            ilGenerator.Emit(OpCodes.Ldloc, keysLocal); // Load keys array
            ilGenerator.Emit(OpCodes.Ldlen); // Get length of keys array
            ilGenerator.Emit(OpCodes.Conv_I4); // Convert length to int
            ilGenerator.Emit(OpCodes.Blt, loopStart); // If index < length, jump to loop start
            #endregion

            // Load the result array onto the stack and return it
            ilGenerator.Emit(OpCodes.Ldloc, kvLocal);
            ilGenerator.Emit(OpCodes.Ret);

            return method;
        }

    }
}
