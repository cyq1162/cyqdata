using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.UI
{
    /// <summary>
    /// �������ؼ�UIע��
    /// </summary>
    public static class RegisterUI
    {
        internal static Dictionary<string, string> UIList = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// ���Ҫע��ĵ������ؼ����ơ�
        /// </summary>
        /// <param name="controlClassName">�ؼ��������磺TextBox</param>
        /// <param name="propertyName">�ؼ����Զ�ȡֵ��ֵ�������ƣ��磺Text</param>
        public static void Add(string controlClassName, string propertyName)
        {
            if (!UIList.ContainsKey(controlClassName))
            {
                UIList.Add(controlClassName, propertyName);
            }
        }
        /// <summary>
        /// ���UIע��
        /// </summary>
        public static void Clear()
        {
            UIList.Clear();
        }
    }
}
