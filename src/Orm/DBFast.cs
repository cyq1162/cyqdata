using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;
using CYQ.Data.SQL;
using CYQ.Data.Tool;
using System.Web;
using System.Threading;
using System.Data;


namespace CYQ.Data.Orm
{
    /// <summary>
    /// 快速操作操作类。
    /// </summary>
    public static class DBFast
    {
        /// <summary>
        /// 当前 用户 是否开启了全局事务
        /// </summary>
        /// <returns></returns>
        internal static bool HasTransation(string key)
        {
            return TransationKeys.ContainsKey(key);
        }
        internal static IsolationLevel GetTransationLevel(string key)
        {
            if (TransationKeys.ContainsKey(key))
            {
                return TransationKeys[key];
            }
            return IsolationLevel.ReadCommitted;
        }
        /// <summary>
        /// 存档事务的标识
        /// </summary>
        public static MDictionary<string, IsolationLevel> TransationKeys = new MDictionary<string, IsolationLevel>();
        
        /// <summary>
        /// 开启事务：如果已存在事务（则返回false）
        /// </summary>
        public static bool BeginTransation(string conn)
        {
            return BeginTransation(conn, IsolationLevel.ReadCommitted);
        }
        /// <summary>
        /// 开启事务：如果已存在事务（则返回false）
        /// </summary>
        /// <param name="conn">链接配置项名称或链接字符串</param>
        /// <param name="level">事务等级</param>
        public static bool BeginTransation(string conn, IsolationLevel level)
        {
            string key = StaticTool.GetTransationKey(conn);
            if (!TransationKeys.ContainsKey(key))
            {
                TransationKeys.Add(key, level);
                return true;
            }
            return false;
        }
        /// <summary>
        /// 提交事务
        /// </summary>
        public static bool EndTransation(string conn)
        {
            string key = StaticTool.GetTransationKey(conn);
            TransationKeys.Remove(key);
            DalBase dal = DalCreate.Get(key);
            if (dal != null && dal.EndTransaction())//如果事务回滚了，
            {
                dal.Dispose();
                return DalCreate.Remove(key);
            }
            return false;
        }
        /// <summary>
        /// 事务回滚
        /// </summary>
        public static bool RollBack(string conn)
        {
            string key = StaticTool.GetTransationKey(conn);
            TransationKeys.Remove(key);
            DalBase dal = DalCreate.Get(key);
            if (dal != null && dal.RollBack())
            {
                dal.Dispose();
                return DalCreate.Remove(key);
            }
            return false;
        }
        /// <summary>
        /// 查找单条记录
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="where">条件</param>
        /// <param name="columns">指定查询的列（可选）</param>
        /// <returns></returns>
        public static T Find<T>(object where, params string[] columns) where T : class
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
        public static List<T> Select<T>() where T : class
        {
            int count;
            return Select<T>(0, 0, null, out count, null);
        }
        /// <summary>
        /// 列表查询
        /// </summary>
        /// <param name="where">查询条件[可附带 order by 语句]</param>
        /// <returns></returns>
        public static List<T> Select<T>(string where, params string[] columns) where T : class
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
        public static List<T> Select<T>(int topN, string where, params string[] columns) where T : class
        {
            int count;
            return Select<T>(1, topN, where, out count, columns);
        }
        public static List<T> Select<T>(int pageIndex, int pageSize, params string[] columns) where T : class
        {
            int count;
            return Select<T>(pageIndex, pageSize, null, out count, columns);
        }
        public static List<T> Select<T>(int pageIndex, int pageSize, string where, params string[] columns) where T : class
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
        public static List<T> Select<T>(int pageIndex, int pageSize, object where, out int count, params string[] columns) where T : class
        {
            //MDataTable dt = null;
            using (MAction action = GetMAction<T>())
            {
                if (columns != null && columns.Length > 0)
                {
                    action.SetSelectColumns(columns);
                }
                return action.SelectList<T>(pageIndex, pageSize, where, out count);
                //dt = action.Select(pageIndex, pageSize, where, out count);
            }
            // return dt.ToList<T>();
        }

        /// <summary>
        /// 删除记录(受影响行数>0才为true)
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="where">条件</param>
        /// <returns></returns>
        public static bool Delete<T>(object where) where T : class
        {
            return Delete<T>(where, false);
        }
        /// <summary>
        /// 删除记录(受影响行数>0才为true)
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="where">条件</param>
        /// <param name="isIgnoreDeleteField">是否忽略软删除标识</param>
        /// <returns></returns>
        public static bool Delete<T>(object where, bool isIgnoreDeleteField) where T : class
        {
            if (where == null) { return false; }
            using (MAction action = GetMAction<T>())
            {

                if (typeof(T).FullName == where.GetType().FullName)//传进实体类
                {
                    action.Data.LoadFrom(where);
                    return action.Delete(null, isIgnoreDeleteField) && action.RecordsAffected > 0;
                }
                else
                {
                    return action.Delete(where, isIgnoreDeleteField) && action.RecordsAffected > 0;
                }

            }
        }
        public static bool Insert<T>(T t) where T : class
        {
            return Insert<T>(t, InsertOp.ID, false);
        }
        public static bool Insert<T>(T t, InsertOp op) where T : class
        {
            return Insert<T>(t, op, false);
        }
        /// <summary>
        /// 添加一条记录
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="t">实体对象</param>
        /// <returns></returns>
        public static bool Insert<T>(T t, InsertOp op, bool insertID) where T : class
        {
            bool result = false;
            MDataRow row = null;
            using (MAction action = GetMAction<T>())
            {
                action.AllowInsertID = insertID;
                action.Data.LoadFrom(t, BreakOp.Null);
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
        public static bool Update<T>(T t) where T : class
        {
            return Update<T>(t, null);
        }
        /// <summary>
        /// 更新记录(受影响行数>0才为true)
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="t">实体对象</param>
        /// <param name="where">条件</param>
        /// <param name="updateColumns">指定要更新列</param>
        /// <returns></returns>
        public static bool Update<T>(T t, object where, params string[] updateColumns) where T : class
        {
            using (MAction action = GetMAction<T>())
            {
                action.Data.LoadFrom(t, BreakOp.Null);
                if (updateColumns.Length > 0)
                {
                    action.Data.SetState(0);
                    if (updateColumns.Length == 1)
                    {
                        updateColumns = updateColumns[0].Split(',');
                    }
                    foreach (string item in updateColumns)
                    {
                        MDataCell cell = action.Data[item];
                        if (cell != null)
                        {
                            cell.State = 2;
                        }
                    }
                }
                return action.Update(where) && action.RecordsAffected > 0;
            }
        }
        /// <summary>
        /// 是否存在指定的条件
        /// </summary>
        public static bool Exists<T>(object where) where T : class
        {
            using (MAction action = GetMAction<T>())
            {
                return action.Exists(where);
            }
        }
        public static int GetCount<T>(object where) where T : class
        {
            using (MAction action = GetMAction<T>())
            {
                return action.GetCount(where);
            }
        }
        private static MAction GetMAction<T>()
        {
            string conn = string.Empty;
            MAction action = new MAction(GetTableName<T>(out conn), conn);
            //action.SetAopState(CYQ.Data.Aop.AopOp.CloseAll);
            return action;
        }
        internal static string GetTableName<T>(out string conn)
        {
            Type t = typeof(T);
            return GetTableName(t, out conn);
        }
        internal static string GetTableName(Type t, out string conn)
        {
            conn = string.Empty;
            string[] items = t.FullName.Split('.');
            if (items.Length > 1)
            {
                conn = items[items.Length - 2] + "Conn";
                if (string.IsNullOrEmpty(AppConfig.GetConn(conn)))
                {
                    conn = null;
                }
                items = null;
            }
            string tName = t.Name;
            object[] names = ReflectTool.GetAttributes(t, typeof(TableNameAttribute));
            if (names != null && names.Length > 0)
            {
                foreach (object item in names)
                {
                    if (item is TableNameAttribute)
                    {
                        tName = ((TableNameAttribute)item).TableName;
                        break;
                    }
                }

            }
            if (!string.IsNullOrEmpty(tName))
            {
                t = null;
                if (tName.EndsWith(AppConfig.DB.EntitySuffix))
                {
                    tName = tName.Substring(0, tName.Length - AppConfig.DB.EntitySuffix.Length);
                }

            }

            string fixName;
            conn = CrossDB.GetConn(tName, out fixName, conn);
            if (!string.IsNullOrEmpty(fixName))
            {
                tName = fixName;
            }

            return tName;
        }
    }
}
