using CYQ.Data.Tool;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;

namespace CYQ.Data.SQL
{
    /// <summary>
    /// 管理数据库（N个库）
    /// </summary>
    internal partial class DBSchema
    {
        /// <summary>
        /// 首次初始化的数据库。
        /// </summary>
        private static MDictionary<string, DBInfo> _DBScheams = new MDictionary<string, DBInfo>();

        /// <summary>
        /// 存档数据库对应的keys，用于遍历。
        /// </summary>
        public static List<string> DBScheamKeys = new List<string>();

        public static Dictionary<string, DBInfo> DBScheams
        {
            get
            {
                if (!IsInitDBCompleted)
                {
                    InitDBSchemasAgain();
                }
                return _DBScheams;
            }
        }

        /// <summary>
        /// 获取(并缓存)数据库的“表、视图、存储过程”名称列表。
        /// </summary>
        public static DBInfo GetSchema(string conn)
        {
            ConnBean cb = ConnBean.Create(conn);
            if (cb != null)
            {
                string hash = cb.GetHashKey();
                if (!_DBScheams.ContainsKey(hash))
                {
                    lock (cb)
                    {
                        if (!_DBScheams.ContainsKey(hash))
                        {
                            DBInfo dbSchema = GetSchemaDic(cb.ConnName, false);
                            if (dbSchema != null)
                            {
                                if (!_DBScheams.ContainsKey(hash))
                                {
                                    _DBScheams.Add(hash, dbSchema);
                                    DBScheamKeys.Add(hash);
                                }
                            }
                            return dbSchema;
                        }
                    }

                }
                if (_DBScheams.ContainsKey(hash))
                {
                    return _DBScheams[hash];
                }
            }
            return null;
        }
        private static DBInfo GetSchemaDic(string conn, bool isIgnoreCache)
        {
            DBInfo info = new DBInfo();
            using (DalBase dal = DalCreate.CreateDal(conn))
            {
                info.ConnName = dal.ConnObj.Master.ConnName;
                info.ConnString = dal.ConnObj.Master.ConnString;
                info.DataBaseName = dal.DataBaseName;
                info.DataBaseType = dal.DataBaseType;

                Dictionary<string, string> tables = dal.GetTables(isIgnoreCache);
                if (tables != null && tables.Count > 0)
                {
                    Dictionary<string, TableInfo> dic = new Dictionary<string, TableInfo>();
                    foreach (KeyValuePair<string, string> item in tables)
                    {
                        string hash = TableInfo.GetHashKey(item.Key);
                        if (!dic.ContainsKey(hash))
                        {
                            dic.Add(hash, new TableInfo(item.Key, "U", item.Value, info));
                        }
                    }
                    info.Tables = dic;
                }
                if (isIgnoreCache)//延迟加载。
                {
                    info.isGetVersion = true;
                    info.DataBaseVersion = dal.Version;
                    Dictionary<string, string> views = dal.GetViews(isIgnoreCache);
                    if (views != null && views.Count > 0)
                    {
                        Dictionary<string, TableInfo> dic = new Dictionary<string, TableInfo>();
                        foreach (KeyValuePair<string, string> item in views)
                        {
                            string hash = TableInfo.GetHashKey(item.Key);
                            if (!dic.ContainsKey(hash))
                            {
                                dic.Add(hash, new TableInfo(item.Key, "V", item.Value, info));
                            }
                        }
                        info.isGetViews = true;
                        info.Views = dic;
                    }
                    Dictionary<string, string> procs = dal.GetProcs(isIgnoreCache);
                    if (procs != null && procs.Count > 0)
                    {
                        Dictionary<string, TableInfo> dic = new Dictionary<string, TableInfo>();
                        foreach (KeyValuePair<string, string> item in procs)
                        {
                            string hash = TableInfo.GetHashKey(item.Key);
                            if (!dic.ContainsKey(hash))
                            {
                                dic.Add(hash, new TableInfo(item.Key, "P", item.Value, info));
                            }
                        }
                        info.isGetProcs = true;
                        info.Procs = dic;
                    }
                }
            }
            return info;

        }

        /// <summary>
        /// 移除数据库缓存
        /// </summary>
        /// <param name="key"></param>
        public static void Remove(string key)
        {
            _DBScheams.Remove(key);
            DBScheamKeys.Remove(key);
        }

        /// <summary>
        /// 清空所有数据库缓存
        /// </summary>
        public static void Clear()
        {
            _DBScheams.Clear();
            DBScheamKeys.Clear();
        }


    }


    /// <summary>
    /// 初始化。
    /// </summary>
    internal partial class DBSchema
    {
        internal static bool IsInitDBCompleted
        {
            get
            {
                return _InitFlag <= 0;
            }
        }
        private static int _InitFlag = 1;
        /// <summary>
        /// 初始化 基础表结构，只运行1次
        /// </summary>
        public static void InitDBSchemasOnStart()
        {
            List<string> connNames = new List<string>();
            foreach (ConnectionStringSettings item in ConfigurationManager.ConnectionStrings)
            {
                if (!string.IsNullOrEmpty(item.Name) && item.Name.ToLower().EndsWith("conn"))
                {
                    connNames.Add(item.Name);
                }
            }
            _InitFlag = connNames.Count;
            if (connNames.Count > 0)
            {
                foreach (string item in connNames)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(InitDBSchemaByThreadWork), item);
                }
            }
        }

        private static void InitDBSchemaByThreadWork(object conn)
        {
            ConnBean cb = ConnBean.Create(conn.ToString());
            if (cb == null) { return; }
            string key = cb.GetHashKey();
            DBInfo info = GetSchemaDic(cb.ConnName, true);

            if (!_DBScheams.ContainsKey(key))
            {
                _DBScheams.Add(key, info);
                DBScheamKeys.Add(key);
            }
            _InitFlag--;
            //初始化表结构
            if (info.Tables != null && info.Tables.Count > 0)
            {
                foreach (var tableKey in info.TableKeys)
                {
                    if (info.Tables.ContainsKey(tableKey))
                    {
                        var table = info.Tables[tableKey];
                        if (table != null)
                        {
                            table.Reflesh();
                        }
                    }
                }
            }
            if (info.Views != null && info.Views.Count > 0)
            {
                foreach (var viewKey in info.ViewKeys)
                {
                    if (info.Views.ContainsKey(viewKey))
                    {
                        var view = info.Views[viewKey];
                        if (view != null)
                        {
                            view.Reflesh();
                        }
                    }
                }
            }
            if (info.Procs != null && info.Procs.Count > 0)
            {
                foreach (var procKey in info.ProcKeys)
                {
                    if (info.Procs.ContainsKey(procKey))
                    {
                        var proc = info.Procs[procKey];
                        if (proc != null)
                        {
                            proc.Reflesh();
                        }
                    }
                }
            }
        }

        private static readonly object oo = new object();
        public static void InitDBSchemasAgain()
        {
            lock (oo)
            {
                if (!IsInitDBCompleted)
                {
                    List<string> connNames = new List<string>();
                    foreach (ConnectionStringSettings item in ConfigurationManager.ConnectionStrings)
                    {
                        if (!string.IsNullOrEmpty(item.Name) && item.Name.ToLower().EndsWith("conn"))
                        {
                            connNames.Add(item.Name);
                        }
                    }
                    if (connNames.Count > 0)
                    {
                        foreach (string item in connNames)
                        {
                            if (IsInitDBCompleted)
                            {
                                break;
                            }
                            GetSchema(item);
                        }
                    }
                    _InitFlag = 0;
                }
            }

        }
    }
}
