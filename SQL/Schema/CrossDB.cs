using CYQ.Data.Tool;
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
        /// <summary>
        /// 获得链接的配置或语句。
        /// </summary>
        /// <param name="nameOrSql">表名、视图名、存储过程名</param>
        /// <returns></returns>
        public static string GetConn(string nameOrSql, out string fixName, string priorityConn)
        {
            string firstTableName = null;
            string conn = null;
            nameOrSql = nameOrSql.Trim();
            fixName = nameOrSql;
            if (nameOrSql.IndexOf(' ') == -1)//单表。
            {
                #region 单表
                if (nameOrSql.IndexOf('.') > -1) //dbname.tablename
                {
                    string[] items = nameOrSql.Split('.');
                    conn = items[0] + "Conn";
                    fixName = items[items.Length - 1];
                }
                else
                {
                    firstTableName = nameOrSql;
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
        /// <param name="formatName"></param>
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
        /// 获得数据库表相关信息
        /// </summary>
        /// <param name="conn">指定时优先寻找。</param>
        /// <param name="name">表名</param>
        /// <returns></returns>
        public static TableInfo GetTableInfoByName(string name, string conn)
        {
            if (!string.IsNullOrEmpty(name))
            {
                name = SqlFormat.NotKeyword(name);
                int tableHash = TableInfo.GetHashCode(name);
                if (!string.IsNullOrEmpty(conn))
                {
                    int dbHash = ConnBean.GetHashCode(conn);
                    if (DBSchema.DBScheams.Count > 0 && DBSchema.DBScheams.ContainsKey(dbHash))
                    {
                        TableInfo info = DBSchema.DBScheams[dbHash].GetTableInfo(tableHash);
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
                            TableInfo info = dbInfo.GetTableInfo(tableHash);
                            if (info != null)
                            {
                                return info;
                            }
                        }
                    }
                }
                foreach (KeyValuePair<int, DBInfo> item in DBSchema.DBScheams)
                {
                    TableInfo info = item.Value.GetTableInfo(tableHash);
                    if (info != null)
                    {
                        return info;
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// 是否存在指定的表名、视图名、存储过程名
        /// </summary>
        /// <param name="name"></param>
        /// <param name="conn"></param>
        /// <returns></returns>
        public static bool Exists(string name, string type, string conn)
        {
            if (DBSchema.DBScheams.Count == 0 && !string.IsNullOrEmpty(conn))
            {
                DBSchema.GetSchema(conn);
            }
            if (!string.IsNullOrEmpty(name) && DBSchema.DBScheams.Count > 0)
            {
                int tableHash = TableInfo.GetHashCode(name);
                if (!string.IsNullOrEmpty(conn))
                {
                    int dbHash = ConnBean.GetHashCode(conn);
                    if (DBSchema.DBScheams.ContainsKey(dbHash))
                    {
                        TableInfo info = DBSchema.DBScheams[dbHash].GetTableInfo(tableHash, type);
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
                            TableInfo info = dbInfo.GetTableInfo(tableHash, type);
                            if (info != null)
                            {
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    foreach (KeyValuePair<int, DBInfo> item in DBSchema.DBScheams)
                    {
                        TableInfo info = item.Value.GetTableInfo(tableHash, type);
                        if (info != null)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public static bool Remove(string name, string type, string conn)
        {
            if (!string.IsNullOrEmpty(name) && DBSchema.DBScheams.Count > 0)
            {
                int tableHash = TableInfo.GetHashCode(name);
                if (!string.IsNullOrEmpty(conn))
                {
                    int dbHash = ConnBean.GetHashCode(conn);
                    if (DBSchema.DBScheams.ContainsKey(dbHash))
                    {

                        return DBSchema.DBScheams[dbHash].Remove(tableHash, type);
                    }
                }
                else
                {
                    foreach (KeyValuePair<int, DBInfo> item in DBSchema.DBScheams)
                    {
                        if (item.Value.Remove(tableHash, type))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public static bool Add(string name, string type, string conn)
        {
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(type) && DBSchema.DBScheams.Count > 0)
            {
                int tableHash = TableInfo.GetHashCode(name);
                int dbHash = ConnBean.GetHashCode(conn);
                if (DBSchema.DBScheams.ContainsKey(dbHash))
                {

                    return DBSchema.DBScheams[dbHash].Add(tableHash, type, name);
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
