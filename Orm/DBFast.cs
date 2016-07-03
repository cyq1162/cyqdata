using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;


namespace CYQ.Data.Orm
{
    /// <summary>
    /// 快速操作操作类。
    /// </summary>
    public static class DBFast
    {
        //public static MDataRow FindRow<T>(object where)
        //{
        //    MDataRow row = new MDataRow();
        //    using (MAction action = new MAction(GetTableName<T>()))
        //    {
        //        action.SetNoAop();
        //        if (action.Fill(where))
        //        {
        //            row = action.Data;
        //        }
        //    }
        //    return row;
        //}
        /// <summary>
        /// 查找单条记录
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="where">条件</param>
        /// <param name="columns">指定查询的列（可选）</param>
        /// <returns></returns>
        public static T Find<T>(object where, params string[] columns)
        {
            T result = default(T);
            MDataRow row = null;
            using (MAction action = GetMAction<T>())
            {
                if (columns != null && columns.Length > 0)
                {
                    action.SetSelectColumns(columns);
                }
                if (action.Fill(where))
                {
                    row = action.Data;
                }
            }
            if (row != null)
            {
                result = row.ToEntity<T>();
            }
            return result;
        }
        public static List<T> Select<T>()
        {
            int count;
            return Select<T>(0, 0, null, out count, null);
        }
        /// <summary>
        /// 列表查询
        /// </summary>
        /// <param name="where">查询条件[可附带 order by 语句]</param>
        /// <returns></returns>
        public static List<T> Select<T>(string where, params string[] columns)
        {
            int count;
            return Select<T>(0, 0, where, out count, columns);
        }
        /// <summary>
        /// 列表查询
        /// </summary>
        /// <param name="topN">查询几条</param>
        /// <param name="where">查询条件[可附带 order by 语句]</param>
        /// <returns></returns>
        public static List<T> Select<T>(int topN, string where, params string[] columns)
        {
            int count;
            return Select<T>(1, topN, where, out count, columns);
        }
        public static List<T> Select<T>(int pageIndex, int pageSize, params string[] columns)
        {
            int count;
            return Select<T>(pageIndex, pageSize, null, out count, columns);
        }
        public static List<T> Select<T>(int pageIndex, int pageSize, string where, params string[] columns)
        {
            int count;
            return Select<T>(pageIndex, pageSize, where, out count, columns);
        }
        /// <summary>
        /// 查找多条记录
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="pageIndex">第N页</param>
        /// <param name="pageSize">每页N条</param>
        /// <param name="where">条件</param>
        /// <param name="count">返回记录总数</param>
        /// <param name="columns">指定查询的列（可选）</param>
        /// <returns></returns>
        public static List<T> Select<T>(int pageIndex, int pageSize, object where, out int count, params string[] columns)
        {
            MDataTable dt = null;
            using (MAction action = GetMAction<T>())
            {
                if (columns != null && columns.Length > 0)
                {
                    action.SetSelectColumns(columns);
                }
                dt = action.Select(pageIndex, pageSize, where, out count);
            }
            return dt.ToList<T>();
        }

        /// <summary>
        /// 删除记录
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="where">条件</param>
        /// <returns></returns>
        public static bool Delete<T>(object where)
        {
            bool result = false;
            using (MAction action = GetMAction<T>())
            {
                result = action.Delete(where);
            }
            return result;
        }
        public static bool Insert<T>(T t)
        {
            return Insert<T>(t, InsertOp.ID);
        }
        /// <summary>
        /// 添加一条记录
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="t">实体对象</param>
        /// <returns></returns>
        public static bool Insert<T>(T t, InsertOp op)
        {
            bool result = false;
            MDataRow row = null;
            using (MAction action = GetMAction<T>())
            {
                action.Data.LoadFrom(t);
                result = action.Insert(op);
                if (result && op != InsertOp.None)
                {
                    row = action.Data;
                }
            }
            if (row != null)
            {
                row.SetToEntity(t);
            }
            return result;
        }
        public static bool Update<T>(T t)
        {
            return Update<T>(t, null);
        }
        /// <summary>
        /// 更新记录
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="t">实体对象</param>
        /// <param name="where">条件</param>
        /// <returns></returns>
        public static bool Update<T>(T t, object where)
        {
            bool result = false;
            using (MAction action = GetMAction<T>())
            {
                action.Data.LoadFrom(t);
                result = action.Update(where);
            }
            return result;
        }
        private static MAction GetMAction<T>()
        {
            string conn = string.Empty;
            MAction action = new MAction(GetTableName<T>(out conn), conn);
            action.SetAopOff();
            return action;
        }
        private static string GetTableName<T>(out string conn)
        {
            conn = string.Empty;
            Type t = typeof(T);

            string[] items = t.FullName.Split('.');
            if (items.Length > 1)
            {
                conn = items[items.Length - 2] + "Conn";
                items = null;
            }
            string tName = t.Name;
            t = null;
            if (tName.EndsWith(AppConfig.EntitySuffix))
            {
                tName = tName.Substring(0, tName.Length - AppConfig.EntitySuffix.Length);
            }
            return tName;
        }
    }
}
