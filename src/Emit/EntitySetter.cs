using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using System.Reflection;

namespace CYQ.Data.Emit
{
    internal class EntitySetter
    {
        static Dictionary<PropertyInfo, Action<object, object>> piActions = new Dictionary<PropertyInfo, Action<object, object>>();
        static Dictionary<FieldInfo, Action<object, object>> fiActions = new Dictionary<FieldInfo, Action<object, object>>();

        public static Action<object, object> SetterAction(PropertyInfo pi, FieldInfo fi)
        {
            if (pi != null)
            {
                SetterAction(pi);
            }
            if (fi != null)
            {
                SetterAction(fi);
            }
            return null;
        }

        /// <summary>
        /// 获取属性的委托调用
        /// </summary>
        /// <param name="pi"></param>
        /// <returns></returns>
        public static Action<object, object> SetterAction(PropertyInfo pi)
        {
            if (piActions.ContainsKey(pi))
            {
                return piActions[pi];
            }
            var method = new DynamicMethod("SetterAction", typeof(void), new[] { typeof(object), typeof(object) }, pi.DeclaringType, true);
            var il = method.GetILGenerator();

            var setMethod = pi.GetSetMethod();

            il.Emit(OpCodes.Ldarg_0); // Load the input object onto the stack
            il.Emit(OpCodes.Ldarg_1); 
            if (pi.PropertyType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, pi.PropertyType); // Unbox the value type
            }
            else
            {
                il.Emit(OpCodes.Castclass, pi.PropertyType); // Cast the value to the property type
            }
            il.EmitCall(OpCodes.Callvirt, setMethod, null); // Call the property setter
            il.Emit(OpCodes.Ret); // Return the value

            var action = (Action<object, object>)method.CreateDelegate(typeof(Action<object, object>));
            piActions.Add(pi, action);
            return action;
        }

        public static Action<object, object> SetterAction(FieldInfo fi)
        {
            if (fiActions.ContainsKey(fi))
            {
                return fiActions[fi];
            }
            var method = new DynamicMethod("SetterAction", typeof(void), new[] { typeof(object), typeof(object) }, fi.DeclaringType, true);
            var il = method.GetILGenerator();


            il.Emit(OpCodes.Ldarg_0); // Load the input object onto the stack
            il.Emit(OpCodes.Ldarg_1); // 将 int 类型参数加载到堆栈
            if (fi.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, fi.FieldType); // Unbox the value type
            }
            else
            {
                il.Emit(OpCodes.Castclass, fi.FieldType); // Cast the value to the property type
            }
            il.Emit(OpCodes.Stfld, fi); // 将 int 类型值存储到 Id 成员变量
            il.Emit(OpCodes.Ret); // Return the value

            var action = (Action<object, object>)method.CreateDelegate(typeof(Action<object, object>));
            fiActions.Add(fi, action);
            return action;
        }
    }
}
