using System;
using System.Collections.Generic;
using System.Text;

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
            }
        }
        private Dictionary<string, TableInfo> _Views = new Dictionary<string, TableInfo>();
        public Dictionary<string, TableInfo> Views
        {
            get
            {
                if (!isGetViews && _Views.Count == 0)
                {
                    isGetViews = true;
                    GetViews();////延迟加载
                }
                return _Views;
            }
            internal set
            {
                _Views = value;
            }
        }
        private Dictionary<string, TableInfo> _Procs = new Dictionary<string, TableInfo>();
        public Dictionary<string, TableInfo> Procs
        {
            get
            {
                if (!isGetProcs && _Procs.Count == 0)
                {
                    isGetProcs = true;
                    GetProcs();//延迟加载
                }
                return _Procs;
            }
            internal set
            {
                _Procs = value;
            }
        }


        internal TableInfo GetTableInfo(string tableHash)
        {
            return GetTableInfo(tableHash, null);
        }
        internal TableInfo GetTableInfo(string tableHash, string type)
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
        internal bool Remove(string tableHash, string type)
        {
            if (Tables != null && (type == null || type == "U") && Tables.ContainsKey(tableHash))
            {
                return Tables.Remove(tableHash);
            }
            if (Views != null && (type == null || type == "V") && Views.ContainsKey(tableHash))
            {
                return Views.Remove(tableHash);
            }
            if (Procs != null && (type == null || type == "P") && Procs.ContainsKey(tableHash))
            {
                return Procs.Remove(tableHash);
            }
            return false;
        }
        internal bool Add(string tableHash, string type, string name)
        {
            try
            {
                if (Tables != null && (type == null || type == "U") && !Tables.ContainsKey(tableHash))
                {
                    Tables.Add(tableHash, new TableInfo(name, type, null, this));
                }
                if (Views != null && (type == null || type == "V") && !Views.ContainsKey(tableHash))
                {
                    Views.Add(tableHash, new TableInfo(name, type, null, this));
                }
                if (Procs != null && (type == null || type == "P") && !Procs.ContainsKey(tableHash))
                {
                    Procs.Add(tableHash, new TableInfo(name, type, null, this));
                }
                return true;
            }
            catch
            {
                return false;
            }
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

        #region 延迟加载
        internal bool isGetViews = false, isGetProcs = false, isGetVersion = false;
        private readonly object lockViewObj = new object();
        private void GetViews()
        {
            if (_Views.Count == 0)
            {
                lock (lockViewObj)
                {
                    if (_Views.Count == 0)
                    {
                        using (DalBase dal = DalCreate.CreateDal(ConnString))
                        {
                            Dictionary<string, string> views = dal.GetViews();
                            if (views != null && views.Count > 0)
                            {
                                Dictionary<string, TableInfo> dic = new Dictionary<string, TableInfo>();
                                foreach (KeyValuePair<string, string> item in views)
                                {
                                    string hash = TableInfo.GetHashKey(item.Key);
                                    if (!dic.ContainsKey(hash))
                                    {
                                        dic.Add(hash, new TableInfo(item.Key, "V", item.Value, this));
                                    }
                                }
                                _Views = dic;
                            }
                        }
                    }
                }
            }
        }
        private readonly object lockProcObj = new object();
        private void GetProcs()
        {
            if (_Procs.Count == 0)
            {
                lock (lockProcObj)
                {
                    if (_Procs.Count == 0)
                    {
                        using (DalBase dal = DalCreate.CreateDal(ConnString))
                        {
                            Dictionary<string, string> procs = dal.GetProcs();
                            if (procs != null && procs.Count > 0)
                            {
                                Dictionary<string, TableInfo> dic = new Dictionary<string, TableInfo>();
                                foreach (KeyValuePair<string, string> item in procs)
                                {
                                    string hash = TableInfo.GetHashKey(item.Key);
                                    if (!dic.ContainsKey(hash))
                                    {
                                        dic.Add(hash, new TableInfo(item.Key, "P", item.Value, this));
                                    }
                                }
                                _Procs = dic;
                            }
                        }
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
