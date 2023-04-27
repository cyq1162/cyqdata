
using CYQ.Data.SQL;
using System.Collections.Generic;

namespace CYQ.Data.Tool
{
    /// <summary>
    /// 数据库信息
    /// </summary>
    public class DBInfo
    {
        internal DBInfo()
        {

        }
        private string _ConnName;
        public string ConnName
        {
            get
            {
                return _ConnName;
            }
            internal set
            {
                _ConnName = value;
            }
        }
        private string _ConnString;
        public string ConnString
        {
            get
            {
                return _ConnString;
            }
            internal set
            {
                _ConnString = value;
            }
        }
        private string _DataBaseName;
        public string DataBaseName
        {
            get
            {
                return _DataBaseName;
            }
            internal set
            {
                _DataBaseName = value;
            }
        }
        private string _DataBaseVersion;
        public string DataBaseVersion
        {
            get
            {
                if (!isGetVersion && string.IsNullOrEmpty(_DataBaseVersion))
                {
                    GetVersion();
                }
                return _DataBaseVersion;
            }
            internal set
            {
                _DataBaseVersion = value;
            }
        }
        private DataBaseType _DataBaseType;
        public DataBaseType DataBaseType
        {
            get
            {
                return _DataBaseType;
            }
            internal set
            {
                _DataBaseType = value;
            }
        }
        /// <summary>
        /// 用于遍历使用（多线程下）
        /// </summary>
        public List<string> TableKeys = new List<string>();
        private Dictionary<string, TableInfo> _Tables = new Dictionary<string, TableInfo>();
        public Dictionary<string, TableInfo> Tables
        {
            get
            {
                return _Tables;
            }
            internal set
            {
                _Tables = value;
                TableKeys.Clear();
                if (value != null)
                {
                    foreach (var key in value.Keys)
                    {
                        TableKeys.Add(key);
                    }
                }
            }
        }
        /// <summary>
        /// 用于遍历使用（多线程下）
        /// </summary>
        public List<string> ViewKeys = new List<string>();
        private Dictionary<string, TableInfo> _Views = new Dictionary<string, TableInfo>();
        public Dictionary<string, TableInfo> Views
        {
            get
            {
                if (!isGetViews && _Views.Count == 0)
                {
                    isGetViews = true;
                    InitInfos("V", false);//延迟加载
                }
                return _Views;
            }
            internal set
            {
                _Views = value;
                ViewKeys.Clear();
                if (value != null)
                {
                    foreach (var key in value.Keys)
                    {
                        ViewKeys.Add(key);
                    }
                }
            }
        }
        /// <summary>
        /// 用于遍历使用（多线程下）
        /// </summary>
        public List<string> ProcKeys = new List<string>();
        private Dictionary<string, TableInfo> _Procs = new Dictionary<string, TableInfo>();
        public Dictionary<string, TableInfo> Procs
        {
            get
            {
                if (!isGetProcs && _Procs.Count == 0)
                {
                    isGetProcs = true;
                    InitInfos("P", false);//延迟加载
                }
                return _Procs;
            }
            internal set
            {
                _Procs = value;
                ProcKeys.Clear();
                if (value != null)
                {
                    foreach (var key in value.Keys)
                    {
                        ProcKeys.Add(key);
                    }
                }
            }
        }
        /// <summary>
        /// 根据名称（按顺序）获取表、视图、存储过程信息。
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public TableInfo GetTableInfo(string name)
        {
            string key = TableInfo.GetHashKey(name);
            return GetTableInfoByHash(key, null);
        }
        /// <summary>
        /// 根据名称获取指定类型（表、视图、存储过程）的信息。
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="type">指定类型：U、V或P</param>
        /// <returns></returns>
        public TableInfo GetTableInfo(string name, string type)
        {
            string key = TableInfo.GetHashKey(name);
            return GetTableInfoByHash(key, type);
        }
        internal TableInfo GetTableInfoByHash(string tableHash)
        {
            return GetTableInfoByHash(tableHash, null);
        }
        internal TableInfo GetTableInfoByHash(string tableHash, string type)
        {
            if ((type == null || type == "U") && Tables != null && Tables.ContainsKey(tableHash))
            {
                return Tables[tableHash];
            }
            if ((type == null || type == "V") && Views != null && Views.ContainsKey(tableHash))
            {
                return Views[tableHash];
            }
            if ((type == null || type == "P") && Procs != null && Procs.ContainsKey(tableHash))
            {
                return Procs[tableHash];
            }
            return null;
        }
        /// <summary>
        /// DBTool Drop Table 时调用
        /// </summary>
        /// <param name="tableHash"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal bool Remove(string tableHash, string type)
        {
            if (Tables != null && (type == null || type == "U") && Tables.ContainsKey(tableHash))
            {
                RefleshKeys(ref TableKeys, tableHash, false);
                Tables[tableHash].RemoveCache();
                return Tables.Remove(tableHash);
            }
            if (Views != null && (type == null || type == "V") && Views.ContainsKey(tableHash))
            {
                RefleshKeys(ref ViewKeys, tableHash, false);
                Views[tableHash].RemoveCache();
                return Views.Remove(tableHash);
            }
            if (Procs != null && (type == null || type == "P") && Procs.ContainsKey(tableHash))
            {
                RefleshKeys(ref ProcKeys, tableHash, false);
                Procs[tableHash].RemoveCache();
                return Procs.Remove(tableHash);
            }
            return false;
        }

        /// <summary>
        /// DBTool Add Table 时调用
        /// </summary>
        /// <param name="tableHash"></param>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal bool Add(string tableHash, string type, string name)
        {
            try
            {
                if (Tables != null && (type == null || type == "U") && !Tables.ContainsKey(tableHash))
                {
                    RefleshKeys(ref TableKeys, tableHash, true);
                    Tables.Add(tableHash, new TableInfo(name, type, null, this));
                }
                if (Views != null && (type == null || type == "V") && !Views.ContainsKey(tableHash))
                {
                    Views.Add(tableHash, new TableInfo(name, type, null, this));
                    RefleshKeys(ref ViewKeys, tableHash, true);
                }
                if (Procs != null && (type == null || type == "P") && !Procs.ContainsKey(tableHash))
                {
                    Procs.Add(tableHash, new TableInfo(name, type, null, this));
                    RefleshKeys(ref ProcKeys, tableHash, true);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 多线程下，不能直接操作Keys的添加或移除
        /// </summary>
        private void RefleshKeys(ref List<string> keys, string hash, bool isAdd)
        {
            List<string> newKeys = new List<string>();

            foreach (string key in keys)
            {
                if (isAdd || key != hash)
                {
                    newKeys.Add(key);
                }
            }
            if (isAdd)
            {
                newKeys.Add(hash);
            }
            keys = newKeys;
        }

        /// <summary>
        /// 获取指定数据库链接的HashKey
        /// </summary>
        /// <param name="connNameOrString">配置名或链接字符串</param>
        /// <returns></returns>
        public static string GetHashKey(string connNameOrString)
        {
            ConnBean connBean = ConnBean.Create(connNameOrString);
            if (connBean == null)
            {
                string err = "DBInfo.GetHashCode ConnBean can't create by " + connNameOrString;
                Log.Write(err, LogType.DataBase);
                Error.Throw(err);
            }
            return connBean.GetHashKey();
        }

        /// <summary>
        /// 刷新：表、视图、存储过程 缓存。
        /// </summary>
        public void Reflesh()
        {
            Reflesh("All");
        }
        /// <summary>
        /// 刷新：表、视图或存储过程列表 缓存。
        /// <para name="type">指定类型：U、V或P</para>
        /// </summary>
        public void Reflesh(string type)
        {
            if ((type == "All" || type == "U") && _Tables != null && _Tables.Count > 0)
            {
                InitInfos("U", true);
            }
            if ((type == "All" || type == "V") && _Views != null && _Views.Count > 0)
            {
                InitInfos("V", true);
            }
            if ((type == "All" || type == "P") && _Procs != null && _Procs.Count > 0)
            {
                InitInfos("P", true);
            }
        }

        #region 延迟加载
        internal bool isGetViews = false, isGetProcs = false, isGetVersion = false;

        private void InitInfos(string type, bool isIgnoreCache)
        {
            using (DalBase dal = DalCreate.CreateDal(ConnString))
            {
                Dictionary<string, string> infoDic = null;
                switch (type)
                {
                    case "U":
                        infoDic = dal.GetTables(isIgnoreCache);
                        break;
                    case "V":
                        infoDic = dal.GetViews(isIgnoreCache);
                        break;
                    case "P":
                        infoDic = dal.GetProcs(isIgnoreCache);
                        break;
                }
                if (infoDic != null && infoDic.Count > 0)
                {
                    Dictionary<string, TableInfo> dic = new Dictionary<string, TableInfo>();
                    foreach (KeyValuePair<string, string> item in infoDic)
                    {
                        string hash = TableInfo.GetHashKey(item.Key);
                        if (!dic.ContainsKey(hash))
                        {
                            dic.Add(hash, new TableInfo(item.Key, type, item.Value, this));
                        }
                    }
                    switch (type)
                    {
                        case "U":
                            _Tables = dic;
                            break;
                        case "V":
                            _Views = dic;
                            break;
                        case "P":
                            _Procs = dic;
                            break;
                    }
                }
            }

        }
        private void GetVersion()
        {
            if (string.IsNullOrEmpty(_DataBaseVersion))
            {
                using (DalBase dal = DalCreate.CreateDal(ConnString))
                {
                    _DataBaseVersion = dal.Version;
                }
            }
        }
        #endregion
    }
}
