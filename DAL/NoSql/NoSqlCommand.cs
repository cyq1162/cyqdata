using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;
using CYQ.Data.SQL;
using System.Data.Common;

namespace CYQ.Data
{
    internal class NoSqlCommand : IDisposable // DbCommand
    {
        SqlSyntax ss;
        string sourceSql = string.Empty;//传过来的SQL语句
        public string CommandText
        {
            get
            {
                return sourceSql;
            }
            set
            {
                sourceSql = value;
                if (ss == null)
                {
                    ss = SqlSyntax.Analyze(sourceSql);
                }
            }
        }

        NoSqlAction action = null;
        public NoSqlCommand(string sqlText, DbBase dbBase)
        {
            try
            {
                if (string.IsNullOrEmpty(sqlText))
                {
                    return;
                }
                sourceSql = sqlText;
                ss = SqlSyntax.Analyze(sqlText);
            }
            catch (Exception err)
            {
                Log.WriteLogToTxt(err);
            }
            if (ss.IsSelect || ss.IsUpdate || ss.IsDelete || ss.IsInsert)
            {
                MDataRow row = new MDataRow();
                if (TableSchema.FillTableSchema(ref row, ref dbBase, ss.TableName, ss.TableName))
                {
                    row.Conn = dbBase.conn;
                    action = new NoSqlAction(ref row, ss.TableName, dbBase.Con.DataSource, dbBase.dalType);
                }
            }
            else
            {
                Log.WriteLogToTxt("NoSql Grammar Error Or No Support : " + sqlText);
            }
        }
        public MDataTable ExeMDataTable()
        {
            int count = 0;
            MDataTable dt = action.Select(1, ss.TopN, ss.Where, out count);
            if (ss.FieldItems.Count > 0)
            {
                //处理 a as B 的列。
                Dictionary<string, string> dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string field in ss.FieldItems)
                {
                    string[] items = field.Trim().Split(' ');
                    dic.Add(items[0], items.Length > 1 ? items[items.Length - 1] : "");
                }
                for (int i = dt.Columns.Count - 1; i >= 0; i--)
                {
                    string columnName = dt.Columns[i].ColumnName;
                    if (!dic.ContainsKey(columnName))
                    {
                        dt.Columns.RemoveAt(i);
                    }
                    else if (dic[columnName] != "")//处理 a as B 的列。
                    {
                        dt.Columns[i].ColumnName = dic[columnName];
                    }
                }
            }
            if (ss.IsDistinct)
            {
                dt.Distinct();
            }
            return dt;

        }
        public int ExeNonQuery()
        {
            int count = -1;
            if (ss.IsInsert || ss.IsUpdate)
            {
                int index = 0;
                foreach (string item in ss.FieldItems)
                {
                    index = item.IndexOf('=');//处理，号。
                    if (index > -1)
                    {
                        string key = item.Substring(0, index);
                        string value = item.Substring(index + 1);
                        if (value.Length > 0 && value[0] == '\'')
                        {
                            value = value.Substring(1, value.Length - 2).Replace("''", "'");
                        }
                        action._Row.Set(key, value);
                    }
                }
                if (ss.IsUpdate)
                {
                    action.Update(ss.Where, out count);
                }
                else if (ss.IsInsert && action.Insert(false))
                {
                    count = 1;
                }
            }
            else if (ss.IsDelete)
            {
                action.Delete(ss.Where, out count);
            }
            return count;
        }
        public object ExeScalar()
        {
            if (ss.IsSelect)
            {
                if (ss.IsGetCount)
                {
                    return action.GetCount(ss.Where);
                }
                else if (ss.FieldItems.Count > 0 && action.Fill(ss.Where) && action._Row.Columns.Contains(ss.FieldItems[0]))
                {
                    return action._Row[ss.FieldItems[0]].Value;
                }
                return null;
            }
            else
            {
                return ExeNonQuery();
            }
        }

        #region IDisposable 成员

        public void Dispose()
        {
            if (action != null)
            {
                action.Dispose();
            }
        }

        #endregion

    }
}
