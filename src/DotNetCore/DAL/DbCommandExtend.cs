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
    internal static class DbCommandExtend
    {
        ///// <summary>
        ///// 在异步中同步
        ///// </summary>
        //public static async void DisposeSync(this DbCommand cmd)
        //{
        //    await cmd.DisposeAsync();
        //}

        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static DbDataReader ExecuteReaderSync(this DbCommand cmd, CommandBehavior behavior)
        {
            return ExecuteReaderSync2(cmd, behavior).GetAwaiter().GetResult();


        }
        private static async Task<DbDataReader> ExecuteReaderSync2(DbCommand cmd, CommandBehavior behavior)
        {
            return await cmd.ExecuteReaderAsync(behavior);
        }


        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static int ExecuteNonQuerySync(this DbCommand cmd)
        {
            return ExecuteNonQuerySync2(cmd).GetAwaiter().GetResult();
        }
        private static async Task<int> ExecuteNonQuerySync2(DbCommand cmd)
        {
            return await cmd.ExecuteNonQueryAsync();
        }


        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static object ExecuteScalarSync(this DbCommand cmd)
        {
            return ExecuteScalarSync2(cmd).GetAwaiter().GetResult();
        }
        private static async Task<object> ExecuteScalarSync2(DbCommand cmd)
        {
            return await cmd.ExecuteScalarAsync();
        }

    }
}
