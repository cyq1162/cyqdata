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
        private Dictionary<int, TableInfo> _Tables;
        public Dictionary<int, TableInfo> Tables
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
        private Dictionary<int, TableInfo> _Views;
        public Dictionary<int, TableInfo> Views
        {
            get
            {
                return _Views;
            }
            internal set
            {
                _Views = value;
            }
        }
        private Dictionary<int, TableInfo> _Procs;
        public Dictionary<int, TableInfo> Procs
        {
            get
            {
                return _Procs;
            }
            internal set
            {
                _Procs = value;
            }
        }


        internal TableInfo GetTableInfo(int tableHash)
        {
            return GetTableInfo(tableHash, null);
        }
        internal TableInfo GetTableInfo(int tableHash, string type)
        {
            if (Tables != null && (type == null || type == "U") && Tables.ContainsKey(tableHash))
            {
                return Tables[tableHash];
            }
            if (Views != null && (type == null || type == "V") && Views.ContainsKey(tableHash))
            {
                return Views[tableHash];
            }
            if (Procs != null && (type == null || type == "P") && Procs.ContainsKey(tableHash))
            {
                return Procs[tableHash];
            }
            return null;
        }
        internal bool Remove(int tableHash, string type)
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
        internal bool Add(int tableHash, string type, string name)
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
        /// 获取指定数据库链接的Hash值
        /// </summary>
        /// <param name="connNameOrString">配置名或链接字符串</param>
        /// <returns></returns>
        public static int GetHashCode(string connNameOrString)
        {
            return ConnBean.Create(connNameOrString).GetHashCode();
        }
    }
}
