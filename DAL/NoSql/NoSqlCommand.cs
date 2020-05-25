using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;
using CYQ.Data.SQL;
using System.Data.Common;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace CYQ.Data
{


    internal partial class NoSqlCommand : DbCommand
    {
        NoSqlParameterCollection list = new NoSqlParameterCollection();
        public override void Cancel()
        {

        }

        public override int CommandTimeout
        {
            get
            {
                return 1000;
            }
            set
            {

            }
        }

        public override CommandType CommandType
        {
            get
            {
                return CommandType.Text;
            }
            set
            {

            }
        }

        protected override DbParameter CreateDbParameter()
        {
            return new NoSqlParameter();
        }

        protected override DbConnection DbConnection
        {
            get
            {
                return con;
            }
            set
            {
                con = value as NoSqlConnection;
            }
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get
            {
                return list as DbParameterCollection;
            }
        }

        protected override DbTransaction DbTransaction
        {
            get
            {
                return con.Transaction;
            }
            set
            {

            }
        }

        public override bool DesignTimeVisible
        {
            get
            {
                return true;
            }
            set
            {

            }
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return new NoSqlDataReader(ExeMDataTable());
        }


        public override void Prepare()
        {

        }

        public override UpdateRowSource UpdatedRowSource
        {
            get
            {
                return UpdateRowSource.None;
            }
            set
            {

            }
        }
    }
    internal partial class NoSqlCommand  // DbCommand
    {
        string _CommandText;
        public override string CommandText
        {
            get
            {
                return _CommandText;
            }
            set
            {
                _CommandText = value;
            }
        }

        NoSqlAction action = null;
        SqlSyntax ss;
        NoSqlConnection con;
        public NoSqlCommand(string sqlText, NoSqlConnection con)
        {
            try
            {
                this.con = con;
                if (string.IsNullOrEmpty(sqlText))
                {
                    return;
                }
                CommandText = sqlText;
            }
            catch (Exception err)
            {
                Log.Write(err, LogType.DataBase);
            }

        }
        private void InitAction()
        {
            ss = SqlSyntax.Analyze(CommandText);
            if (ss != null)
            {
                if (action == null || action.TableName != ss.TableName)
                {
                    if (action != null)
                    {
                        action.Dispose();
                    }
                    if (ss.IsSelect || ss.IsUpdate || ss.IsDelete || ss.IsInsert)
                    {
                        MDataRow row = new MDataRow();
                        row.Conn = con.ConnectionString;
                        if (TableSchema.FillTableSchema(ref row, ss.TableName, ss.TableName))
                        {
                            action = new NoSqlAction(ref row, ss.TableName, con.DataSource, con.DatabaseType);
                        }

                    }
                    else
                    {
                        Log.Write("NoSql Grammar Error Or No Support : " + ss.SqlText, LogType.DataBase);
                    }
                }
            }
        }
        public MDataTable ExeMDataTable()
        {
            InitAction();
            string where = FormatParas(ss.Where);
            int count = 0;
            MDataTable dt = action.Select(ss.PageIndex, ss.PageSize, where, out count);
            if (ss.FieldItems.Count > 0)
            {
                //处理 a as B 的列。
                Dictionary<string, string> dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string name, asName;
                foreach (string field in ss.FieldItems)
                {
                    string[] items = field.Trim().Split(' ');
                    name = items[0];
                    asName = items[items.Length - 1];
                    if (!dic.ContainsKey(name))
                    {
                        dic.Add(items[0], items.Length > 1 ? asName : "");
                    }
                    else if (items.Length > 1 && dt.Columns.Contains(name))//同一个字段as多次
                    {
                        MCellStruct newCell = dt.Columns[name].Clone();
                        newCell.ColumnName = asName;
                        dt.Columns.Add(newCell);
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            dt.Rows[i].Set(asName, dt.Rows[i][name].Value);
                        }
                        dic.Add(asName, "");
                    }
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
        public override int ExecuteNonQuery()
        {
            InitAction();
            string where = FormatParas(ss.Where);
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

                        if (value.Length > 0)
                        {
                            if (value[0] == '@' && list.Contains(value))
                            {
                                value = list[value].Value.ToString();
                            }
                            else if (value[0] == '\'')
                            {
                                value = value.Substring(1, value.Length - 2).Replace("''", "'");
                            }
                        }
                        action._Row.Set(key, value);
                    }
                }
                if (ss.IsUpdate)
                {
                    action.Update(where, out count);
                }
                else if (ss.IsInsert && action.Insert(Transaction != null))
                {
                    count = 1;
                }
            }
            else if (ss.IsDelete)
            {
                action.Delete(where, out count);
            }
            return count;
        }
        public override object ExecuteScalar()
        {
            InitAction();
            if (ss.IsSelect)
            {
                string where = FormatParas(ss.Where);
                if (ss.IsGetCount)
                {
                    return action.GetCount(where);
                }
                else if (ss.FieldItems.Count > 0 && action.Fill(where))
                {
                    if (action._Row.Columns.Contains(ss.FieldItems[0]))
                    {
                        return action._Row[ss.FieldItems[0]].Value;
                    }
                    return ss.FieldItems[0];
                }
                return null;
            }
            else
            {
                int result = ExecuteNonQuery();
                if (result > 0 && ss.IsInsert)
                {
                    return action._Row[action._Row.PrimaryCell.ColumnName].Value;
                }
                return result;
            }
        }
        /// <summary>
        /// 处理参数化替换
        /// </summary>
        /// <returns></returns>
        private string FormatParas(string where)
        {
            if (list.Count > 0 && !string.IsNullOrEmpty(where) && where.IndexOf('@') > -1)
            {
                foreach (KeyValuePair<string, NoSqlParameter> item in list)
                {
                    //aa=@aa and bb=@aab
                    if (where.IndexOf(item.Value.ParameterName + " ", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        where = Regex.Replace(where, item.Value.ParameterName + " ", "'" + item.Value.Value + "' ", RegexOptions.IgnoreCase);
                    }
                    //aa=@aa,bb=@aab
                    else if (where.IndexOf(item.Value.ParameterName + ",", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        where = Regex.Replace(where, item.Value.ParameterName + ",", "'" + item.Value.Value + "',", RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        where = Regex.Replace(where, item.Value.ParameterName, "'" + item.Value.Value + "'", RegexOptions.IgnoreCase);
                    }
                }
            }
            return where;
        }
        #region IDisposable 成员
        public new void Dispose(bool disposing)
        {
            if (action != null)
            {
                action.Dispose();
            }
        }
        #endregion

    }
}
