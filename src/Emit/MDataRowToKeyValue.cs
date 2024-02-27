using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using CYQ.Data.Table;
using CYQ.Data.Tool;
using CYQ.Data.SQL;
using System.Data;

namespace CYQ.Data.Emit
{
    /// <summary>
    /// 数据行转实体列表
    /// </summary>
    internal class MDataRowToKeyValue
    {
        //public static object Test(MDataRow dt, Type kvType)
        //{
        //    DynamicMethod method = CreateDynamicMethod(kvType);
        //    var func = method.CreateDelegate(typeof(Func<MDataTable, object>)) as Func<MDataTable, object>;
        //    var obj = func(dt);
        //    return obj;
        //}

        static Dictionary<Type, Func<MDataRow, object>> typeFuncs = new Dictionary<Type, Func<MDataRow, object>>();

        private static readonly object lockObj = new object();

        internal static Func<MDataRow, object> Delegate(Type kvType)
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
                var func = method.CreateDelegate(typeof(Func<MDataRow, object>)) as Func<MDataRow, object>;
                typeFuncs.Add(kvType, func);
                return func;
            }
        }

        private static DynamicMethod CreateDynamicMethod(Type kvType)
        {
            Type rowType = typeof(MDataRow);
            Type cellType = typeof(MDataCell);
            Type toolType = typeof(ConvertTool);
            var getCountMethod = rowType.GetProperty("Count").GetGetMethod();
            var getCellMethod = rowType.GetMethod("get_Item", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(int) }, null);
            var getCellKeyMethod = cellType.GetProperty("ColumnName").GetGetMethod();
            var getCellValueMethod = cellType.GetProperty("Value").GetGetMethod();
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

            var method = new DynamicMethod("MDataRowToKeyValue", typeof(object), new[] { rowType }, kvType, true);

            var ilGen = method.GetILGenerator();

            // Define variables
            var countLocal = ilGen.DeclareLocal(typeof(int));
            var kvLocal = ilGen.DeclareLocal(kvType);
            var kLocal = ilGen.DeclareLocal(kType);
            var vLocal = ilGen.DeclareLocal(vType);
            var iLocal = ilGen.DeclareLocal(typeof(int));
            var cellLocal = ilGen.DeclareLocal(cellType);
            var kTypeLocal = ilGen.DeclareLocal(typeof(Type));
            var vTypeLocal = ilGen.DeclareLocal(typeof(Type));


            // Load the count of rows into the countLocal variable
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Callvirt, getCountMethod);
            ilGen.Emit(OpCodes.Stloc, countLocal);

            // Create a new instance of the target list type and store it in the itemsLocal variable
            if (kvType.Name.Contains("Dictionary"))
            {
                var stringComparerType = typeof(StringComparer);
                var ordinalIgnoreCaseField = stringComparerType.GetProperty("OrdinalIgnoreCase", BindingFlags.Public | BindingFlags.Static).GetGetMethod();
                var constructorInfo = kvType.GetConstructor(new[] { typeof(IEqualityComparer<string>) }); // 获取 Dictionary<string, string>(IEqualityComparer<string>) 构造函数
                ilGen.Emit(OpCodes.Call, ordinalIgnoreCaseField);// 加载 StringComparer.OrdinalIgnoreCase 字段
                ilGen.Emit(OpCodes.Newobj, constructorInfo);// 创建 Dictionary<string, string> 实例
            }
            else
            {
                ilGen.Emit(OpCodes.Newobj, kvType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { }, null));
            }

            ilGen.Emit(OpCodes.Stloc, kvLocal);

            // Initialize the iLocal variable to 0
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Stloc, iLocal);

            // Start loop
            var loopStart = ilGen.DefineLabel();
            ilGen.MarkLabel(loopStart);

            // Get Cell
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldloc, iLocal);
            ilGen.Emit(OpCodes.Callvirt, getCellMethod);
            ilGen.Emit(OpCodes.Stloc, cellLocal);

            //Get Cell.ColumnName
            ilGen.Emit(OpCodes.Ldloc, cellLocal);
            ilGen.Emit(OpCodes.Callvirt, getCellKeyMethod); //ilGen.Emit(OpCodes.Ret);
            ilGen.Emit(OpCodes.Stloc, kLocal);

            if (kType.Name != "String")
            {
                //类型转换。
                ilGen.Emit(OpCodes.Ldtoken, kType);
                ilGen.Emit(OpCodes.Call, getTypeFromHandleMethod);
                ilGen.Emit(OpCodes.Stloc, kTypeLocal);

                ilGen.Emit(OpCodes.Ldloc, kLocal);
                ilGen.Emit(OpCodes.Ldloc, kTypeLocal);
                ilGen.Emit(OpCodes.Call, getChangeTypeMethod);
                if (kType.IsValueType)
                {
                    ilGen.Emit(OpCodes.Unbox_Any, kType);
                }

                ilGen.Emit(OpCodes.Stloc, kLocal);
            }


            //Get Cell.Value
            ilGen.Emit(OpCodes.Ldloc, cellLocal);
            ilGen.Emit(OpCodes.Callvirt, getCellValueMethod);// ilGen.Emit(OpCodes.Ret);
            ilGen.Emit(OpCodes.Stloc, vLocal);

            if (vType.Name != "Object")
            {
                //类型转换
                ilGen.Emit(OpCodes.Ldtoken, vType);
                ilGen.Emit(OpCodes.Call, getTypeFromHandleMethod);
                ilGen.Emit(OpCodes.Stloc, vTypeLocal);

                ilGen.Emit(OpCodes.Ldloc, vLocal);
                ilGen.Emit(OpCodes.Ldloc, vTypeLocal);
                ilGen.Emit(OpCodes.Call, getChangeTypeMethod);
                if (vType.IsValueType)
                {
                    ilGen.Emit(OpCodes.Unbox_Any, vType);
                }

                ilGen.Emit(OpCodes.Stloc, vLocal);
            }

            //Load the current row and the target type onto the stack and call the ToEntity method



            // Assign the result to the itemsLocal array at the current index

            ilGen.Emit(OpCodes.Ldloc, kvLocal); //ilGen.Emit(OpCodes.Ret);
            ilGen.Emit(OpCodes.Ldloc, kLocal);
            ilGen.Emit(OpCodes.Ldloc, vLocal);
            ilGen.Emit(OpCodes.Callvirt, getAddMethod);


            // Increment the index variable
            ilGen.Emit(OpCodes.Ldloc, iLocal);
            ilGen.Emit(OpCodes.Ldc_I4_1);
            ilGen.Emit(OpCodes.Add);
            ilGen.Emit(OpCodes.Stloc, iLocal);

            // Check if the loop should continue
            ilGen.Emit(OpCodes.Ldloc, iLocal);
            ilGen.Emit(OpCodes.Ldloc, countLocal);
            ilGen.Emit(OpCodes.Blt, loopStart);

            // Load the result array onto the stack and return it
            ilGen.Emit(OpCodes.Ldloc, kvLocal);
            ilGen.Emit(OpCodes.Ret);

            return method;
        }

    }
}
