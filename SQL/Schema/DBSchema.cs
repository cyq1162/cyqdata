using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Text;

namespace CYQ.Data.SQL
{
    internal partial class DBSchema
    {
        private static Dictionary<int, DBInfo> _DBScheams = new Dictionary<int, DBInfo>();
        public static Dictionary<int, DBInfo> DBScheams
        {
            get
            {
                if (_DBScheams.Count == 0)
                {
                    InitDBSchemasForCache(null);
                }
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
                        DBInfo dbSchema = GetSchemaDic(cb.ConnName);
                        if (dbSchema != null)
                        {
                            _DBScheams.Add(hash, dbSchema);
                        }
                        return dbSchema;
                    }
                }
            }
            if (_DBScheams.ContainsKey(hash))
            {
                return _DBScheams[hash];
            }
            return null;
        }
        private static DBInfo GetSchemaDic(string conn)
        {
            DBInfo info = new DBInfo();
            using (DalBase dal = DalCreate.CreateDal(conn))
            {
                info.ConnName = dal.ConnObj.Master.ConnName;
                info.ConnString = dal.ConnObj.Master.ConnString;
                info.DataBaseName = dal.DataBaseName;
                info.DataBaseType = dal.DataBaseType;
                info.DataBaseVersion = dal.Version;
                Dictionary<string, string> tables = dal.GetTables();
                if (tables != null && tables.Count > 0)
                {
                    Dictionary<int, TableInfo> dic = new Dictionary<int, TableInfo>();
                    foreach (KeyValuePair<string, string> item in tables)
                    {
                       int hash=TableInfo.GetHashCode(item.Key);
                       if (!dic.ContainsKey(hash))
                       {
                           dic.Add(hash, new TableInfo(item.Key, "U", item.Value, info));
                       }
                    }
                    info.Tables = dic;
                }

                Dictionary<string, string> views = dal.GetViews();
                if (views != null && views.Count > 0)
                {
                    Dictionary<int, TableInfo> dic = new Dictionary<int, TableInfo>();
                    foreach (KeyValuePair<string, string> item in views)
                    {
                         int hash=TableInfo.GetHashCode(item.Key);
                         if (!dic.ContainsKey(hash))
                         {
                             dic.Add(hash, new TableInfo(item.Key, "V", item.Value, info));
                         }
                    }
                    info.Views = dic;
                }
                Dictionary<string, string> procs = dal.GetProcs();
                if (procs != null && procs.Count > 0)
                {
                    Dictionary<int, TableInfo> dic = new Dictionary<int, TableInfo>();
                    foreach (KeyValuePair<string, string> item in procs)
                    {
                         int hash=TableInfo.GetHashCode(item.Key);
                         if (!dic.ContainsKey(hash))
                         {
                             dic.Add(hash, new TableInfo(item.Key, "P", item.Value, info));
                         }
                    }
                    info.Procs = dic;
                }
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
            if (_DBScheams.Count == 0)
            {
                lock (oo)
                {
                    if (_DBScheams.Count == 0)
                    {
                        List<string> connNames = new List<string>();
                        foreach (ConnectionStringSettings item in ConfigurationManager.ConnectionStrings)
                        {
                            if (!string.IsNullOrEmpty(item.Name) && item.Name.ToLower().EndsWith("conn"))
                            {
                                connNames.Add(item.Name);
                            }
                        }
                        //if (connNames.Count == 0 && AppConfig.DB.DefaultConn!="Conn") // 加上之后调试会卡, 先不加。
                        //{
                        //    connNames.Add(AppConfig.DB.DefaultConn);
                        //}
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

}
