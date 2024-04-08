using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace CYQ.Data
{
    /// <summary>
    /// 已启用异步
    /// </summary>
    internal static class DbDataReaderExtend
    {
        ///// <summary>
        ///// 在异步中同步
        ///// </summary>
        //public static async void CloseSync(this DbDataReader reader)
        //{
        //    await reader.CloseAsync();
        //}
        ///// <summary>
        ///// 在异步中同步
        ///// </summary>
        //public static async void DisposeSync(this DbDataReader reader)
        //{
        //    await reader.DisposeAsync();
        //}


#if NET6_0_OR_GREATER
        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static DataTable GetSchemaTableSync(this DbDataReader reader)
        {
            return GetSchemaTableSync2(reader).GetAwaiter().GetResult();
        }
        private static async Task<DataTable> GetSchemaTableSync2(DbDataReader reader)
        {
            return await reader.GetSchemaTableAsync();
        }
#else
        public static DataTable GetSchemaTableSync(this DbDataReader reader)
        {
            return reader.GetSchemaTable();
        }
#endif
    }
}
