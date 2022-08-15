using System;
using System.Collections.Generic;
using System.Text;

namespace System.Collections.Generic
{
    /// <summary>
    /// For HttpContext.Items
    /// </summary>
    public static class IDictionaryExtent
    {
        public static bool Contains(this IDictionary<object, object> dic, string key)
        {
            return dic.ContainsKey(key);
        }
    }
}
