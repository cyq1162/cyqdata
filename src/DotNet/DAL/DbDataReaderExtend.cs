using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace CYQ.Data
{
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

        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static DataTable GetSchemaTableSync(this DbDataReader reader)
        {
            return reader.GetSchemaTable();
        }
    }
}
