using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace CYQ.Data
{
    internal static class DbTransactionExtend
    {
        ///// <summary>
        ///// 在异步中同步
        ///// </summary>
        //public static async void DisposeSync(this DbTransaction tran)
        //{
        //    await tran.DisposeAsync();
        //}
        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static void CommitSync(this DbTransaction tran)
        {
            tran.Commit();
        }
        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static void RollbackSync(this DbTransaction tran)
        {
            tran.Rollback();
        }
    }
}
