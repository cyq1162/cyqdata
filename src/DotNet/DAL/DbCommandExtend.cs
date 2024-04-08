using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace CYQ.Data
{
    internal static class DbCommandExtend
    {
        ///// <summary>
        ///// 在异步中同步
        ///// </summary>
        //public static void DisposeSync(this DbCommand cmd)
        //{
        //    cmd.Dispose();
        //}

        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static DbDataReader ExecuteReaderSync(this DbCommand cmd, CommandBehavior behavior)
        {
            return cmd.ExecuteReader(behavior);
        }

        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static int ExecuteNonQuerySync(this DbCommand cmd)
        {
            return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 在异步中同步
        /// </summary>
        public static object ExecuteScalarSync(this DbCommand cmd)
        {
            return cmd.ExecuteScalar();
        }
    }
}
