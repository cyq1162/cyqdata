﻿using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.SQL
{
    /// <summary>
    /// 处理跨库访问
    /// </summary>
    internal class CrossDB
    {
        public static string GetConnByEnum(Enum tableNameObj)
        {
            if (tableNameObj == null) { return null; }
            string connName = string.Empty;
            Type t = tableNameObj.GetType();
            string enumName = t.Name;
            if (enumName != "TableNames" && enumName != "ViewNames" && enumName != "ProcNames")
            {
                if (enumName.Length > 1 && enumName[1] == '_')
                {
                    connName = enumName.Substring(2, enumName.Length - 6) + "Conn";
                }
                else
                {
                    string[] items = t.FullName.Split('.');
                    if (items.Length > 1)
                    {
                        connName = items[items.Length - 2] + "Conn";
                        items = null;
                    }
                }
            }
            if (!string.IsNullOrEmpty(connName) && !string.IsNullOrEmpty(AppConfig.GetConn(connName)))
            {
                return connName;
            }
            return null;
        }
        public static string GetConn(string nameOrSql, out string fixName, string priorityConn)
        {
            string dbName;
            return GetConn(nameOrSql, out fixName, priorityConn, out dbName);
        }
        /// <summary>
        /// 获得最优（可能会切换数据库）链接的配置或语句。
        /// </summary>
        /// <param name="nameOrSql">表名、视图名、存储过程名</param>
        /// <returns></returns>
        public static string GetConn(string nameOrSql, out string fixName, string priorityConn, out string dbName)
        {
            string firstTableName = null;
            string conn = null;
            nameOrSql = nameOrSql.Trim();
            fixName = nameOrSql;
            dbName = string.Empty;
            if (nameOrSql.IndexOf(' ') == -1)//单表。
            {
                #region 单表
                if (nameOrSql.IndexOf('.') > -1) //dbname.tablename
                {
                    string[] items = nameOrSql.Split('.');
                    dbName = items[0];
                    conn = dbName + "Conn";
                    fixName = SqlFormat.NotKeyword(items[items.Length - 1]);
                }
                else
                {
                    firstTableName = SqlFormat.NotKeyword(nameOrSql);
                }
                #endregion
            }
            else
            {
                if (nameOrSql[0] == '(')  // 视图
                {
                    int index = nameOrSql.LastIndexOf(')');
                    string viewSQL = nameOrSql;
                    string startSql = viewSQL.Substring(0, index + 1);//a部分
                    viewSQL = viewSQL.Substring(index + 1).Trim();//b部分。ddd.v_xxx
                    if (viewSQL.Contains(".") && !viewSQL.Contains(" "))//修改原对像
                    {
                        string[] items = viewSQL.Split('.');
                        fixName = startSql + " " + items[items.Length - 1];
                        conn = items[0] + "Conn";
                    }
                    else
                    {
                        firstTableName = GetFirstTableNameFromSql(startSql);
                    }
                }
                else
                {
                    //sql 语句
                    firstTableName = GetFirstTableNameFromSql(nameOrSql);
                    fixName = SqlCreate.SqlToViewSql(nameOrSql);//Sql修正为视图
                }
            }
            if (!string.IsNullOrEmpty(firstTableName))
            {
                TableInfo info = GetTableInfoByName(firstTableName, priorityConn);
                if (info != null && info.DBInfo != null)
                {
                    if (nameOrSql == firstTableName)
                    {
                        fixName = info.Name;
                    }
                    conn = info.DBInfo.ConnName;
                }
            }
            return string.IsNullOrEmpty(conn) ? priorityConn : conn;
        }

        /// <summary>
        /// 获得数据库名称。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetDBName(string name)
        {
            TableInfo info = GetTableInfoByName(name);
            if (info != null && info.DBInfo != null)
            {
                return info.DBInfo.DataBaseName;
            }
            return "";
        }

        public static string GetFixName(string name, string conn)
        {
            TableInfo info = GetTableInfoByName(name, conn);
            if (info != null)
            {
                return info.Name;
            }
            return name;
        }

        public static TableInfo GetTableInfoByName(string name)
        {
            return GetTableInfoByName(name, null);
        }
        /// <summary>
        /// 获得数据库表相关信息（会搜寻最新数据库内容）
        /// </summary>
        /// <param name="conn">指定时优先寻找。</param>
        /// <param name="name">表名</param>
        /// <returns></returns>
        public static TableInfo GetTableInfoByName(string name, string conn)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var dbScheams = DBSchema.DBScheams;
                name = SqlFormat.NotKeyword(name);
                string tableHash = TableInfo.GetHashKey(name);
                if (!string.IsNullOrEmpty(conn))
                {
                    string dbHash = ConnBean.GetHashKey(conn);
                    if (dbScheams.Count > 0 && dbScheams.ContainsKey(dbHash))
                    {
                        TableInfo info = dbScheams[dbHash].GetTableInfoByHash(tableHash);
                        if (info != null)
                        {
                            return info;
                        }
                    }
                    else
                    {
                        DBInfo dbInfo = DBSchema.GetSchema(conn);
                        if (dbInfo != null)
                        {
                            TableInfo info = dbInfo.GetTableInfoByHash(tableHash);
                            if (info != null)
                            {
                                return info;
                            }
                        }
                    }
                }
                else
                {
                    DBInfo dbInfo = DBSchema.GetSchema(AppConfig.DB.DefaultConn);//优先取默认链接
                    if (dbInfo != null)
                    {
                        TableInfo info = dbInfo.GetTableInfoByHash(tableHash);
                        if (info != null)
                        {
                            return info;
                        }
                    }
                }
                List<string> keys = dbScheams.GetKeys();
                foreach (string key in keys)
                {
                    if (dbScheams.ContainsKey(key))
                    {
                        TableInfo info = dbScheams[key].GetTableInfoByHash(tableHash);
                        if (info != null)
                        {
                            return info;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 是否存在指定的表名、视图名、存储过程名
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static bool Exists(string name, string type, string conn)
        {
            string newName = name;
            string newConn = GetConn(newName, out newName, conn);
            if (string.IsNullOrEmpty(conn) || (name.Contains("..") && newConn.StartsWith(name.Split('.')[0])))//已指定链接，则不切换链接
            {
                conn = newConn;
            }
            //if (DBSchema.DBScheams.Count == 0 && !string.IsNullOrEmpty(conn))
            //{
            //    DBSchema.GetSchema(conn);
            //}
            if (!string.IsNullOrEmpty(newName))// && DBSchema.DBScheams.Count > 0
            {
                var dbScheams = DBSchema.DBScheams;
                string tableHash = TableInfo.GetHashKey(newName);
                if (!string.IsNullOrEmpty(conn))
                {
                    string dbHash = ConnBean.GetHashKey(conn);
                    if (dbScheams.ContainsKey(dbHash))
                    {
                        var db = dbScheams[dbHash];
                        TableInfo info = db.GetTableInfoByHash(tableHash, type);
                        if (info == null && type == "U")
                        {
                            db.Reflesh(type);//刷新缓存，重新获取
                            info = db.GetTableInfoByHash(tableHash, type);
                        }
                        if (info != null)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        DBInfo dbInfo = DBSchema.GetSchema(conn);
                        if (dbInfo != null)
                        {
                            TableInfo info = dbInfo.GetTableInfoByHash(tableHash, type);
                            if (info != null)
                            {
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    List<string> keys = dbScheams.GetKeys();
                    foreach (string key in keys)
                    {
                        if (dbScheams.ContainsKey(key))
                        {
                            TableInfo info = dbScheams[key].GetTableInfoByHash(tableHash, type);
                            if (info != null)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        public static bool Remove(string name, string type, string conn)
        {
            var dbScheams = DBSchema.DBScheams;
            if (!string.IsNullOrEmpty(name) && dbScheams.Count > 0)
            {
                string newName = name;
                string newConn = GetConn(newName, out newName, conn);//可能移到别的库去？
                if (string.IsNullOrEmpty(conn) || (name.Contains("..") && newConn.StartsWith(name.Split('.')[0])))//已指定链接，则不切换链接
                {
                    conn = newConn;
                }
                string tableHash = TableInfo.GetHashKey(newName);
                if (!string.IsNullOrEmpty(conn))
                {
                    string dbHash = ConnBean.GetHashKey(conn);
                    if (dbScheams.ContainsKey(dbHash))
                    {
                        if (dbScheams[dbHash].Remove(tableHash, type))
                        {
                            dbScheams[dbHash].Reflesh(type);
                            return true;
                        }
                    }
                }
                else
                {
                    List<string> keys = dbScheams.GetKeys();
                    foreach (string key in keys)
                    {
                        if (dbScheams[key].Remove(tableHash, type))
                        {
                            dbScheams[key].Reflesh(type);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public static bool Add(string name, string type, string conn)
        {
            var dbScheams = DBSchema.DBScheams;
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(type) && dbScheams.Count > 0)
            {
                string newName = name;
                string newConn = GetConn(newName, out newName, conn);
                if (string.IsNullOrEmpty(conn) || (name.Contains("..") && newConn.StartsWith(name.Split('.')[0])))//已指定链接，则不切换链接
                {
                    conn = newConn;
                }
                string tableHash = TableInfo.GetHashKey(newName);
                string dbHash = ConnBean.GetHashKey(conn);
                if (dbScheams.ContainsKey(dbHash))
                {
                    dbScheams[dbHash].Reflesh(type);
                    return dbScheams[dbHash].Add(tableHash, type, newName);
                }

            }
            return false;
        }
        private static string GetFirstTableNameFromSql(string sql)
        {
            //获取原始表名
            string[] items = sql.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Split(' ');
            if (items.Length == 1) { return sql; }//单表名
            if (items.Length > 3) // 总是包含空格的select * from xxx
            {
                bool startFrom = false;
                foreach (string item in items)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        if (item.ToLower() == "from")
                        {
                            startFrom = true;
                        }
                        else if (startFrom)
                        {
                            if (item[0] == '(')
                            {
                                startFrom = false;
                            }
                            else
                            {
                                string name = item.Split(')')[0];
                                if (name.IndexOf('.') > -1)
                                {
                                    if (name.ToLower().StartsWith("dbo."))
                                    {
                                        return name.Substring(4);
                                    }
                                    startFrom = false;
                                }
                                else
                                {
                                    return name;
                                }
                            }
                        }
                    }
                }
                return "";
            }
            return sql;
        }
    }




}
