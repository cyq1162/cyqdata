using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.UI
{
    /// <summary>
    /// 第三方控件UI注册
    /// </summary>
    public static class RegisterUI
    {
        internal static Dictionary<string, string> UIList = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// 添加要注册的第三方控件名称。
        /// </summary>
        /// <param name="controlClassName">控件类名，如：TextBox</param>
        /// <param name="propertyName">控件的自动取值或赋值属性名称，如：Text</param>
        public static void Add(string controlClassName, string propertyName)
        {
            if (!UIList.ContainsKey(controlClassName))
            {
                UIList.Add(controlClassName, propertyName);
            }
        }
        /// <summary>
        /// 清除UI注册
        /// </summary>
        public static void Clear()
        {
            UIList.Clear();
        }
    }
}
