using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace CYQ.Data
{
    internal static class DbConnectionExtend
    {
        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static void OpenSync(this DbConnection connection)
        {
            connection.Open();
        }
        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static void CloseSync(this DbConnection connection)
        {
            connection.Close();
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
            return connection.BeginTransaction(isolationLevel);
        }
        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static DataTable GetSchemaSync(this DbConnection connection, string collectionName, string[] restrictionValues)
        {
            return connection.GetSchema(collectionName, restrictionValues);
        }
    }
}
