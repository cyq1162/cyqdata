using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;
using CYQ.Data.SQL;

namespace CYQ.Data
{
    internal class NoSqlCommand : IDisposable
    {
        string tableName = string.Empty;
        string whereSql = string.Empty;
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
                FormatSqlText(sourceSql);
            }
        }

        NoSqlAction action = null;
        public NoSqlCommand(string sqlText, DbBase dbBase)
        {
            try
            {
                sourceSql = sqlText;
                FormatSqlText(sqlText);
            }
            catch (Exception err)
            {
                Log.WriteLogToTxt(err);
            }
            if (IsSelect || IsUpdate || IsDelete || IsInsert)
            {
                MDataRow row = new MDataRow();
                if (TableSchema.FillTableSchema(ref row, ref dbBase, tableName, tableName))
                {
                    row.Conn = dbBase.conn;
                    action = new NoSqlAction(ref row, tableName, dbBase.Con.DataSource, dbBase.dalType);
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
            MDataTable dt = action.Select(1, topN, whereSql, out count);
            if (fieldItems.Count > 0)
            {
                for (int i = dt.Columns.Count - 1; i >= 0; i--)
                {
                    if (!fieldItems.Contains(dt.Columns[i].ColumnName.ToLower()))
                    {
                        dt.Columns.RemoveAt(i);
                    }
                }
            }
            if (IsDistinct)
            {
                dt.Distinct();
            }
            return dt;

        }
        public int ExeNonQuery()
        {
            int count = -1;
            if (IsInsert || IsUpdate)
            {
                int index = 0;
                foreach (string item in fieldItems)
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
                if (IsUpdate)
                {
                    action.Update(whereSql, out count);
                }
                else if (IsInsert && action.Insert(false))
                {
                    count = 1;
                }
            }
            else if (IsDelete)
            {
                action.Delete(whereSql, out count);
            }
            return count;
        }
        public object ExeScalar()
        {
            if (IsSelect)
            {
                if (IsGetCount)
                {
                    return action.GetCount(whereSql);
                }
                else if (fieldItems.Count > 0 && action.Fill(whereSql) && action._Row.Columns.Contains(fieldItems[0]))
                {
                    return action._Row[fieldItems[0]].Value;
                }
                return null;
            }
            else
            {
                return ExeNonQuery();
            }
        }
        void FormatSqlText(string sqlText)
        {
            string[] items = sqlText.Split(' ');
            foreach (string item in items)
            {
                switch (item.ToLower())
                {
                    case "insert":
                        IsInsert = true;
                        break;
                    case "into":
                        if (IsInsert)
                        {
                            IsInsertInto = true;
                        }
                        break;
                    case "select":
                        IsSelect = true;
                        break;
                    case "update":
                        IsUpdate = true;
                        break;
                    case "delete":
                        IsDelete = true;
                        break;
                    case "from":
                        IsFrom = true;
                        break;
                    case "count(*)":
                        IsGetCount = true;
                        break;
                    case "where":
                        whereSql = sqlText.Substring(sqlText.IndexOf(item) + item.Length + 1);
                        //该结束语句了。
                        return;
                    case "top":
                        if (IsSelect && !IsFrom)
                        {
                            IsTopN = true;
                        }
                        break;
                    case "distinct":
                        if (IsSelect && !IsFrom)
                        {
                            IsDistinct = true;
                        }
                        break;
                    case "set":
                        if (IsUpdate && !string.IsNullOrEmpty(tableName) && fieldItems.Count == 0)
                        {
                            #region 解析Update的字段与值

                            int start = sqlText.IndexOf(item) + item.Length;
                            int end = sqlText.ToLower().IndexOf("where");
                            string itemText = sqlText.Substring(start, end == -1 ? sqlText.Length - start : end - start);
                            int quoteCount = 0, commaIndex = 0;

                            for (int i = 0; i < itemText.Length; i++)
                            {
                                if (i == itemText.Length - 1)
                                {
                                    string keyValue = itemText.Substring(commaIndex).Trim();
                                    if (!fieldItems.Contains(keyValue))
                                    {
                                        fieldItems.Add(keyValue);
                                    }
                                }
                                else
                                {
                                    switch (itemText[i])
                                    {
                                        case '\'':
                                            quoteCount++;
                                            break;
                                        case ',':
                                            if (quoteCount % 2 == 0)//双数，则允许分隔。
                                            {
                                                string keyValue = itemText.Substring(commaIndex, i - commaIndex).Trim();
                                                if (!fieldItems.Contains(keyValue))
                                                {
                                                    fieldItems.Add(keyValue);
                                                }
                                                commaIndex = i + 1;
                                            }
                                            break;

                                    }
                                }
                            }
                            #endregion
                        }
                        break;
                    default:
                        if (IsTopN && topN == -1)
                        {
                            int.TryParse(item, out topN);//查询TopN
                        }
                        else if ((IsFrom || IsUpdate || IsInsertInto) && string.IsNullOrEmpty(tableName))
                        {
                            tableName = item.Split('(')[0].Trim();//获取表名。
                        }
                        else if (IsSelect && !IsFrom)//提取查询的中间条件。
                        {
                            #region Select语法解析
                            string[] temps = item.ToLower().Trim(',').Split(',');
                            foreach (string temp in temps)
                            {
                                switch (temp)
                                {
                                    case "*":
                                    case "count(*)":
                                    case "top":
                                    case "distinct":
                                        break;
                                    default:
                                        if (IsTopN && topN.ToString() == temp)
                                        {
                                            break;
                                        }

                                        if (!fieldItems.Contains(temp))
                                        {
                                            fieldItems.Add(temp);
                                        }
                                        break;
                                }
                            }
                            #endregion
                        }
                        else if (IsInsertInto && !string.IsNullOrEmpty(tableName) && fieldItems.Count == 0)
                        {
                            #region 解析Insert Into的字段与值

                            int start = sqlText.IndexOf(tableName) + tableName.Length;
                            int end = sqlText.IndexOf("values", start, StringComparison.OrdinalIgnoreCase);
                            string keys = sqlText.Substring(start, end - start).Trim();
                            string[] keyItems = keys.Substring(1, keys.Length - 2).Split(',');//去除两边括号再按逗号分隔。

                            string values = sqlText.Substring(end + 6).Trim();
                            values = values.Substring(1, values.Length - 2);//去除两边括号
                            int quoteCount = 0, commaIndex = 0, valueIndex = 0;

                            for (int i = 0; i < values.Length; i++)
                            {
                                if (valueIndex >= keyItems.Length)
                                {
                                    break;
                                }
                                if (i == values.Length - 1)
                                {
                                    string value = values.Substring(commaIndex).Trim();
                                    keyItems[valueIndex] += "=" + value;
                                }
                                else
                                {
                                    switch (values[i])
                                    {
                                        case '\'':
                                            quoteCount++;
                                            break;
                                        case ',':
                                            if (quoteCount % 2 == 0)//双数，则允许分隔。
                                            {
                                                string value = values.Substring(commaIndex, i - commaIndex).Trim();
                                                keyItems[valueIndex] += "=" + value;
                                                commaIndex = i + 1;
                                                valueIndex++;
                                            }
                                            break;

                                    }
                                }
                            }
                            fieldItems.AddRange(keyItems);

                            #endregion
                        }
                        break;
                }
            }
        }
        int topN = -1;
        bool IsInsert = false;
        bool IsInsertInto = false;
        bool IsSelect = false;
        bool IsUpdate = false;
        bool IsDelete = false;
        bool IsFrom = false;
        bool IsGetCount = false;
        // bool IsAll = false;
        bool IsTopN = false;
        bool IsDistinct = false;
        List<string> fieldItems = new List<string>();


        #region IDisposable 成员

        public void Dispose()
        {
            action.Dispose();
        }

        #endregion
    }
}
