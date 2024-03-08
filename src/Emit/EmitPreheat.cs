using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Emit
{
    /// <summary>
    /// Emit 预热
    /// </summary>
    public static class EmitPreheat
    {
        /// <summary>
        /// 添加对需要预热的实体类型对象。
        /// </summary>
        /// <param name="type">预热的实体类型对象</param>
        public static void Add(Type type)
        {
            if (type == null || type.IsValueType) return;
            if (type.FullName.StartsWith("System.")) { return; }
            var sysType = ReflectTool.GetSystemType(ref type);
            if (sysType == SysType.Custom)
            {
                DbDataReaderToEntity.Delegate(type);
                DictionaryToEntity.Delegate(type);
                JsonHelperFillEntity.Delegate(type);
                MDataRowLoadEntity.Delegate(type);
                MDataRowToEntity.Delegate(type);
                MDataRowSetToEntity.Delegate(type);
            }

        }
    }
}
