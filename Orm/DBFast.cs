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
    /// ���ٲ��������ࡣ
    /// </summary>
    public static class DBFast
    {
        /// <summary>
        /// ��ǰ �û� �Ƿ�����ȫ������
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
        /// �浵����ı�ʶ
        /// </summary>
        public static MDictionary<string, IsolationLevel> TransationKeys = new MDictionary<string, IsolationLevel>();
        /// <summary>
        /// �������� (Web ״̬���ԣ�Session+�߳�ID��Ϊ��λ������״̬�½����߳�IDΪ��λ)
        /// ����Ѵ��������򷵻�false��
        /// </summary>
        public static bool BeginTransation(string conn)
        {
            return BeginTransation(conn, IsolationLevel.ReadCommitted);
        }
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
        /// �ύ����
        /// </summary>
        public static bool EndTransation(string conn)
        {
            string key = StaticTool.GetTransationKey(conn);
            TransationKeys.Remove(key);
            DalBase dal = DalCreate.Get(key);
            if (dal != null && dal.EndTransaction())//�������ع��ˣ�
            {
                dal.Dispose();
                return DalCreate.Remove(key);
            }
            return false;
        }
        /// <summary>
        /// ����ع�
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
        /// ���ҵ�����¼
        /// </summary>
        /// <typeparam name="T">ʵ������</typeparam>
        /// <param name="where">����</param>
        /// <param name="columns">ָ����ѯ���У���ѡ��</param>
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
        /// �б��ѯ
        /// </summary>
        /// <param name="where">��ѯ����[�ɸ��� order by ���]</param>
        /// <returns></returns>
        public static List<T> Select<T>(string where, params string[] columns) where T : class
        {
            int count;
            return Select<T>(0, 0, where, out count, columns);
        }
        /// <summary>
        /// �б��ѯ
        /// </summary>
        /// <param name="topN">��ѯ����</param>
        /// <param name="where">��ѯ����[�ɸ��� order by ���]</param>
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
        /// ���Ҷ�����¼
        /// </summary>
        /// <typeparam name="T">ʵ������</typeparam>
        /// <param name="pageIndex">��Nҳ</param>
        /// <param name="pageSize">ÿҳN��</param>
        /// <param name="where">����</param>
        /// <param name="count">���ؼ�¼����</param>
        /// <param name="columns">ָ����ѯ���У���ѡ��</param>
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
                return action.Select<T>(pageIndex, pageSize, where, out count);
                //dt = action.Select(pageIndex, pageSize, where, out count);
            }
           // return dt.ToList<T>();
        }

        /// <summary>
        /// ɾ����¼
        /// </summary>
        /// <typeparam name="T">ʵ������</typeparam>
        /// <param name="where">����</param>
        /// <returns></returns>
        public static bool Delete<T>(object where) where T : class
        {
            return Delete<T>(where, false);
        }
        public static bool Delete<T>(object where, bool isIgnoreDeleteField) where T : class
        {
            if (where == null) { return false; }
            using (MAction action = GetMAction<T>())
            {

                if (typeof(T).FullName == where.GetType().FullName)//����ʵ����
                {
                    action.Data.LoadFrom(where);
                    return action.Delete(null, isIgnoreDeleteField);
                }
                else
                {
                    return action.Delete(where, isIgnoreDeleteField);
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
        /// ���һ����¼
        /// </summary>
        /// <typeparam name="T">ʵ������</typeparam>
        /// <param name="t">ʵ�����</param>
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
        /// ���¼�¼
        /// </summary>
        /// <typeparam name="T">ʵ������</typeparam>
        /// <param name="t">ʵ�����</param>
        /// <param name="where">����</param>
        /// <param name="updateColumns">ָ��Ҫ������</param>
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
                return action.Update(where);
            }
        }
        /// <summary>
        /// �Ƿ����ָ��������
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
            if (string.IsNullOrEmpty(tName))
            {
                t = null;
                if (tName.EndsWith(AppConfig.EntitySuffix))
                {
                    tName = tName.Substring(0, tName.Length - AppConfig.EntitySuffix.Length);
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
