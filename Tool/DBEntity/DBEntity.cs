using CYQ.Data.SQL;
using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Tool
{
    public class DBInfo
    {
        public string ConnName;
        public string ConnString;
        public string DataBaseName;
        public Dictionary<int, TableInfo> Tables;
        public Dictionary<int, TableInfo> Views;
        public Dictionary<int, TableInfo> Procs;
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
    public class TableInfo
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
        /// <summary>
        /// 获取指定数据库链接的Hash值
        /// </summary>
        /// <param name="connNameOrString">配置名或链接字符串</param>
        /// <returns></returns>
        public static int GetHashCode(string tableName)
        {
            return TableSchema.GetTableHash(tableName);
        }
    }
}
