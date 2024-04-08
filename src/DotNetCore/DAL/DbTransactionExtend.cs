using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace CYQ.Data
{
    /// <summary>
    /// 未已启用异步（不确认各组件对异步支持是否稳定，暂不启用。）
    /// </summary>
    internal static class DbTransactionExtend
    {
        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static async void CommitSync(this DbTransaction tran)
        {
            await tran.CommitAsync();
        }

        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static async void RollbackSync(this DbTransaction tran)
        {
            await tran.RollbackAsync();
        }
    }
}
