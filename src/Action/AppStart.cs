using CYQ.Data.Emit;
using CYQ.Data.SQL;
using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CYQ.Data
{
    /// <summary>
    /// 用于程序运行时进行预热初始化
    /// </summary>
    internal class AppStart
    {
        public static void Run()
        {
            new Thread(new ThreadStart(OnStart)).Start();
        }
        private static void OnStart()
        {
            try
            {
                // 数据库预热
                DBSchema.InitDBSchemasOnStart();
                EmitPreheat.Add(typeof(SysLogs));
                PreheatInitRegex();
            }
            catch
            {

            }
            
        }
        private static void PreheatInitRegex()
        {
            Regex.Matches("", @"@#(\d{1,2})#@", RegexOptions.Compiled);
            Regex.Matches("", @"\$\{([\S\s]*?)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Regex.Matches("", @"<%#([\S\s]*?)%>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }
}
