using CYQ.Data.Emit;
using CYQ.Data.SQL;
using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data
{
    /// <summary>
    /// 用于程序运行时进行预热初始化
    /// </summary>
    internal class AppStart
    {
        public static void Run()
        {
            // 数据库预热
            DBSchema.InitDBSchemasOnStart();
            EmitPreheat.Add(typeof(SysLogs));
        }
    }
}
