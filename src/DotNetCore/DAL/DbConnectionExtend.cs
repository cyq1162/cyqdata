using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace CYQ.Data
{
    /// <summary>
    /// 未已启用异步（发现各组件对异步支持不太稳定，暂不启用。）
    /// </summary>
    internal static class DbConnectionExtend
    {
        /// <summary>
        /// 在异步中同步
        /// </summary>

        public static async void OpenSync(this DbConnection connection)
        {
            //会抛异常：A task was canceled
            await connection.OpenAsync();
        }
        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static async void CloseSync(this DbConnection connection)
        {
            await connection.CloseAsync();
        }
        ///// <summary>
        ///// 在异步中同步
        ///// </summary>
        //public static async void DisposeSync(this DbConnection connection)
        //{
        //    await connection.DisposeAsync();
        //}
        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static DbTransaction BeginTransactionSync(this DbConnection connection, IsolationLevel isolationLevel)
        {
            return BeginTransactionSync2(connection, isolationLevel).GetAwaiter().GetResult();
        }
        private static async Task<DbTransaction> BeginTransactionSync2(DbConnection connection, IsolationLevel isolationLevel)
        {
            return await connection.BeginTransactionAsync(isolationLevel);
        }


        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static DataTable GetSchemaSync(this DbConnection connection, string collectionName, string[] restrictionValues)
        {
            return GetSchemaSync2(connection, collectionName, restrictionValues).GetAwaiter().GetResult();
        }
        private static async Task<DataTable> GetSchemaSync2(DbConnection connection, string collectionName, string[] restrictionValues)
        {
            return await connection.GetSchemaAsync(collectionName, restrictionValues);
        }
    }
}
