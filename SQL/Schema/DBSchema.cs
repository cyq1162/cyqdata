using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Text;

namespace CYQ.Data.SQL
{
    internal partial class DBSchema
    {
        private static Dictionary<int, DBInfo> _DBScheams;
        public static Dictionary<int, DBInfo> DBScheams
        {
            get
            {
                InitDBSchemasForCache(null);
                return _DBScheams;
            }
        }
        private static readonly object o = new object();
        /// <summary>
        /// 获取(并缓存)数据库的“表、视图、存储过程”名称列表。
        /// </summary>
        public static DBInfo GetSchema(string conn)
        {
            ConnBean cb = ConnBean.Create(conn);
            int hash = cb.GetHashCode();
            if (!_DBScheams.ContainsKey(hash))
            {
                lock (o)
                {
                    if (!_DBScheams.ContainsKey(hash))
                    {
                        DBInfo dbSchema = GetSchemaDic(cb.ConnString);
                        if (dbSchema != null)
                        {
                            _DBScheams.Add(hash, dbSchema);
                        }
                        return dbSchema;
                    }
                }
            }
            return null;
        }
        private static DBInfo GetSchemaDic(string conn)
        {
            DalBase dal = DalCreate.CreateDal(conn);

            DBInfo info = new DBInfo();
            info.ConnName = dal.ConnObj.Master.ConnName;
            info.ConnString = dal.ConnObj.Master.ConnString;
            info.DataBaseName = dal.DataBase;
            Dictionary<string, string> tables = TableSchema.GetTables(conn, false);
            if (tables != null && tables.Count > 0)
            {
                Dictionary<int, TableInfo> dic = new Dictionary<int, TableInfo>();
                foreach (KeyValuePair<string, string> item in tables)
                {
                    dic.Add(TableSchema.GetTableHash(item.Key), new TableInfo(item.Key, "U", item.Value, info));
                }
                info.Tables = dic;
            }

            Dictionary<string, string> views = TableSchema.GetViews(conn, false);
            if (views != null && views.Count > 0)
            {
                Dictionary<int, TableInfo> dic = new Dictionary<int, TableInfo>();
                foreach (KeyValuePair<string, string> item in views)
                {
                    dic.Add(TableSchema.GetTableHash(item.Key), new TableInfo(item.Key, "V", item.Value, info));
                }
                info.Views = dic;
            }
            Dictionary<string, string> procs = TableSchema.GetProcs(conn, false);
            if (procs != null && procs.Count > 0)
            {
                Dictionary<int, TableInfo> dic = new Dictionary<int, TableInfo>();
                foreach (KeyValuePair<string, string> item in procs)
                {
                    dic.Add(TableSchema.GetTableHash(item.Key), new TableInfo(item.Key, "P", item.Value, info));
                }
                info.Procs = dic;
            }
            return info;

        }

        public static void Clear()
        {
            _DBScheams.Clear();
        }
        private static readonly object oo = new object();
        /// <summary>
        /// 预先把结构缓存起来。
        /// </summary>
        /// <param name="para"></param>
        public static void InitDBSchemasForCache(object para)
        {
            if (_DBScheams == null)
            {
                lock (oo)
                {
                    if (_DBScheams == null)
                    {
                        List<string> connNames = new List<string>();
                        foreach (ConnectionStringSettings item in ConfigurationManager.ConnectionStrings)
                        {
                            if (!string.IsNullOrEmpty(item.Name) && item.Name.ToLower().EndsWith("conn"))
                            {
                                connNames.Add(item.Name);
                            }
                        }
                        if (_DBScheams == null)
                        {
                            _DBScheams = new Dictionary<int, DBInfo>(connNames.Count);
                        }
                        if (connNames.Count > 0)
                        {
                            foreach (string item in connNames)
                            {
                                GetSchema(item);
                            }
                        }
                    }
                }
            }
        }
    }

    internal class SchemaPara
    {
        public SchemaPara(string conn, bool isGetColumn)
        {
            Conn = conn;
            IsGetColumn = isGetColumn;
        }
        public string Conn;
        public bool IsGetColumn;
    }
    internal class DBInfo
    {
        public string ConnName;
        public string ConnString;
        public string DataBaseName;
        public Dictionary<int, TableInfo> Tables;
        public Dictionary<int, TableInfo> Views;
        public Dictionary<int, TableInfo> Procs;
        public TableInfo GetTableInfo(int tableHash)
        {
            return GetTableInfo(tableHash, null);
        }
        public TableInfo GetTableInfo(int tableHash, string type)
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
        public bool Remove(int tableHash, string type)
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
        public bool Add(int tableHash, string type, string name)
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
    }
    internal class TableInfo
    {
        public TableInfo(string name, string type, string description, DBInfo parent)
        {
            this.Name = name;
            this.Type = type;
            this.Description = description;
            this.Parent = parent;
        }
        public string Name;
        public string Type;
        public string Description;
        public DBInfo Parent;
    }
}
