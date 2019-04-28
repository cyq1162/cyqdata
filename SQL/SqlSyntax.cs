using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.SQL
{
    /// <summary>
    /// SQL语法解析规则
    /// </summary>
    internal class SqlSyntax
    {
        /// <summary>
        /// 分析语法
        /// </summary>
        public static SqlSyntax Analyze(string sql)
        {
            return new SqlSyntax(sql);
        }
        public string SqlText;
        private SqlSyntax(string sql)
        {
            SqlText = sql;
            FormatSqlText(sql);
        }
        public string TableName = string.Empty;
        public string Where = string.Empty;
        public bool IsInsert = false;
        public bool IsInsertInto = false;
        public bool IsSelect = false;
        public bool IsUpdate = false;
        public bool IsDelete = false;
        public bool IsFrom = false;
        public bool IsGetCount = false;
        // bool IsAll = false;
        public bool IsTopN = false;
        public bool IsDistinct = false;
        public bool IsLimit = false;
        public bool IsOffset = false;
        public bool IsWhere = false;
        public bool IsOrder = false;
        public int TopN = -1;
        public int PageIndex = 0;
        public int PageSize = 0;
        public List<string> FieldItems = new List<string>();

        void FormatSqlText(string sqlText)
        {
            string[] items = sqlText.Split(' ');
            foreach (string item in items)
            {
                #region MyRegion


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
                    case "count(0)":
                    case "count(1)":
                        IsGetCount = true;
                        break;
                    case "limit":
                        if (IsSelect && (IsFrom || IsWhere))
                        {
                            IsLimit = true;
                        }
                        break;
                    case "offset":
                        if (IsLimit)
                        {
                            IsLimit = false;
                            IsOffset = true;
                        }
                        break;
                    case "order":
                        if (IsFrom || IsWhere)
                        {
                            IsOrder = true;
                        }
                        break;
                    case "by":
                    case "where":
                        if (IsFrom || IsUpdate)
                        {
                            IsFrom = false;
                            IsWhere = true;

                            int startIndex = sqlText.LastIndexOf(" " + item + " ") + item.Length + 2;
                            int limit = sqlText.IndexOf(" limit ", startIndex, StringComparison.OrdinalIgnoreCase);
                            if (limit == -1)
                            {
                                Where = sqlText.Substring(startIndex);
                            }
                            else
                            {
                                Where = sqlText.Substring(startIndex, limit - startIndex);
                            }
                            if (item.ToLower() == "by")
                            {
                                Where = "order by " + Where;
                            }
                        }
                        break;
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
                        if (IsUpdate && !string.IsNullOrEmpty(TableName) && FieldItems.Count == 0)
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
                                    if (!FieldItems.Contains(keyValue))
                                    {
                                        FieldItems.Add(keyValue.Trim());
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
                                                if (!FieldItems.Contains(keyValue))
                                                {
                                                    FieldItems.Add(keyValue);
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
                        if (IsOffset)
                        {
                            if (!int.TryParse(item, out PageIndex))
                            {
                                PageIndex = 0;
                            }
                        }
                        else if (IsLimit)
                        {
                            if (!int.TryParse(item, out PageSize))
                            {
                                PageSize = 0;
                            }

                        }
                        if (!IsWhere)
                        {
                            if (IsTopN && TopN == -1)
                            {
                                if (int.TryParse(item, out TopN))//查询TopN
                                {
                                    PageSize = TopN;
                                }
                                IsTopN = false;//关闭topN
                            }
                            else if ((IsFrom || IsUpdate || IsInsertInto) && string.IsNullOrEmpty(TableName))
                            {
                                TableName = item.Split('(')[0].Trim();//获取表名。
                            }
                            else if (IsSelect && !IsFrom)//提取查询的中间条件。
                            {
                                #region Select 字段搜集
                                switch (item)
                                {
                                    case "*":
                                    case "count(*)":
                                    case "count(0)":
                                    case "count(1)":
                                    case "distinct":
                                        break;
                                    default:
                                        fieldText.Append(item + " ");
                                        break;
                                }

                                #endregion
                            }
                            else if (IsInsertInto && !string.IsNullOrEmpty(TableName) && FieldItems.Count == 0)
                            {
                                #region 解析Insert Into的字段与值

                                int start = sqlText.IndexOf(TableName) + TableName.Length;
                                int end = sqlText.IndexOf("values", start, StringComparison.OrdinalIgnoreCase);
                                if (end == -1)
                                {
                                    end = sqlText.IndexOf("select", start, StringComparison.OrdinalIgnoreCase);
                                    if (end == -1) { break; }
                                }
                                string keys = sqlText.Substring(start, end - start).Trim();
                                string[] keyItems = keys.Substring(1, keys.Length - 2).Split(',');//去除两边括号再按逗号分隔。

                                string values = sqlText.Substring(end + 6).Trim();
                                if (IsSelect && IsFrom)
                                {
                                    end = values.IndexOf("from");
                                    if (end > 0)
                                    {
                                        //insert into ...select ...from 模式
                                        values = values.Substring(0, end);//去除两边括号
                                    }
                                }
                                else
                                {
                                    // insert into ... values 模式。
                                    values = values.Substring(1, values.Length - 2);//去除两边括号
                                }
                                int quoteCount = 0, commaIndex = 0, valueIndex = 0;

                                #region get values
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
                                #endregion
                                FieldItems.AddRange(keyItems);

                                #endregion
                            }
                        }
                        break;
                }
                #endregion
            }
            #region Select 字段解析
            if (fieldText.Length > 0)
            {
                if (FieldItems.Count == 0)
                {
                    string[] fields = fieldText.ToString().Split(',');
                    FieldItems.AddRange(fields);
                }
                fieldText.Length = 0;
            }
            #endregion
        }
        private StringBuilder fieldText = new StringBuilder();

    }
}
