using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using CYQ.Data.Table;
using System.Data;
using System.Text.RegularExpressions;
using CYQ.Data.SQL;
using System.IO;
using System.Reflection;
using CYQ.Data.Xml;
using System.ComponentModel;
using CYQ.Data.Tool;
using System.Threading;
namespace CYQ.Data.Json
{

    // 扩展交互部分
    public partial class JsonHelper
    {
        /// <summary>
        /// 从Json字符串中反加载成数据表
        /// </summary>
        internal static MDataTable ToMDataTable(string jsonOrFileName, MDataColumn mdc, EscapeOp op)
        {

            MDataTable table = new MDataTable();
            if (mdc != null)
            {
                table.Columns = mdc;
            }
            if (string.IsNullOrEmpty(jsonOrFileName))
            {
                return table;
            }
            else
            {
                jsonOrFileName = jsonOrFileName.Trim();
            }
            try
            {
                #region 读取Json


                string json = string.Empty;
                #region 获取Json字符串
                if (!jsonOrFileName.StartsWith("{") && !jsonOrFileName.StartsWith("["))//读取文件。
                {
                    if (System.IO.File.Exists(jsonOrFileName))
                    {
                        table.TableName = Path.GetFileNameWithoutExtension(jsonOrFileName);
                        if (table.Columns.Count == 0)
                        {
                            table.Columns = MDataColumn.CreateFrom(jsonOrFileName, false);
                        }
                        json = IOHelper.ReadAllText(jsonOrFileName).Trim(',', ' ', '\r', '\n');
                    }
                }
                else
                {
                    json = jsonOrFileName;
                }
                if (json.StartsWith("{"))
                {
                    json = '[' + json + ']';
                }
                #endregion
                List<Dictionary<string, string>> result = SplitArray(json);
                if (result != null && result.Count > 0)
                {
                    #region 加载数据
                    if (result.Count == 1)
                    {
                        #region 自定义输出头判断
                        Dictionary<string, string> dic = result[0];
                        if (dic.ContainsKey("total") && dic.ContainsKey("rows"))
                        {
                            int count = 0;
                            if (int.TryParse(dic["total"], out count))
                            {
                                table.RecordsAffected = count;//还原记录总数。
                            }
                            result = SplitArray(dic["rows"]);
                        }
                        else if (dic.ContainsKey("TableName") && dic.ContainsKey("Columns"))
                        {
                            table.TableName = dic["TableName"];
                            if (dic.ContainsKey("Description"))
                            {
                                table.Description = dic["Description"];
                            }
                            if (dic.ContainsKey("RelationTables"))
                            {
                                table.Columns.AddRelateionTableName(dic["RelationTables"]);
                            }
                            result = SplitArray(dic["Columns"]);
                        }
                        #endregion
                    }
                    if (result != null && result.Count > 0)
                    {
                        Dictionary<string, string> keyValueDic = null;
                        for (int i = 0; i < result.Count; i++)
                        {
                            keyValueDic = result[i];
                            if (i == 0)
                            {
                                #region 首行列头检测
                                bool addColumn = table.Columns.Count == 0;
                                bool isContinue = false;
                                int k = 0;
                                foreach (KeyValuePair<string, string> item in keyValueDic)
                                {
                                    if (k == 0 && item.Value.StartsWith("System."))
                                    {
                                        isContinue = true;
                                    }
                                    if (!addColumn)
                                    {
                                        break;
                                    }
                                    if (!table.Columns.Contains(item.Key))
                                    {
                                        SqlDbType type = SqlDbType.NVarChar;
                                        if (isContinue && item.Value.StartsWith("System."))//首行是表结构
                                        {
                                            type = DataType.GetSqlType(item.Value.Replace("System.", string.Empty));
                                        }
                                        table.Columns.Add(item.Key, type, (k == 0 && type == SqlDbType.Int));
                                        if (k > keyValueDic.Count - 3 && type == SqlDbType.DateTime)
                                        {
                                            table.Columns[k].DefaultValue = SqlValue.GetDate;
                                        }
                                    }
                                    k++;
                                }
                                if (isContinue)
                                {
                                    continue;
                                }
                                #endregion
                            }


                            bool isKeyValue = table.Columns.Count == 2 && table.Columns[1].ColumnName == "Value" && (table.Columns[0].ColumnName == "Key" || table.Columns[0].ColumnName == "Name");

                            if (isKeyValue)
                            {
                                foreach (KeyValuePair<string, string> item in keyValueDic)
                                {
                                    MDataRow row = table.NewRow(true);
                                    row.Set(0, item.Key);
                                    row.Set(1, item.Value);
                                }
                            }
                            else
                            {
                                MDataRow row = table.NewRow(true);
                                MDataCell cell = null;
                                foreach (KeyValuePair<string, string> item in keyValueDic)
                                {

                                    cell = row[item.Key];
                                    if (cell == null && mdc == null)
                                    {
                                        table.Columns.Add(item.Key, SqlDbType.NVarChar);
                                        cell = row[item.Key];
                                    }
                                    if (cell != null)
                                    {
                                        string val = UnEscape(item.Value, op);
                                        cell.Value = val;
                                        cell.State = 1;
                                    }

                                }
                            }

                        }
                    }
                    #endregion
                }
                else
                {
                    List<string> items = JsonSplit.SplitEscapeArray(json);
                    if (items != null && items.Count > 0)
                    {
                        if (mdc == null)
                        {
                            table.Columns.Add("Key");
                        }
                        foreach (string item in items)
                        {
                            table.NewRow(true).Set(0, item.Trim('"', '\''));
                        }
                    }
                }
                #endregion
            }
            catch (Exception err)
            {
                Log.Write(err, LogType.Error);
            }

            return table;
        }
       
    }

}
