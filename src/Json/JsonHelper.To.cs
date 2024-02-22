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
        /// <summary>
        /// 将Json转换成集合
        /// </summary>
        /// <typeparam name="T">集合类型</typeparam>
        /// <param name="json">json数据</param>
        /// <returns></returns>
        /// 
        private static T ToIEnumerator<T>(string json, EscapeOp op)
            where T : class
        {
            return ToIEnumerator(typeof(T), json, op) as T;
        }
        private static object ToIEnumerator(Type t, string json, EscapeOp op)
        {
            if (t.FullName.StartsWith("System.Collections.") || t.FullName.Contains("MDictionary") || t.FullName.Contains("MList"))
            {
                Type[] ts;
                int argLength = ReflectTool.GetArgumentLength(ref t, out ts);
                if (argLength == 1)
                {
                    return JsonSplit.ToEntityOrList(t, json, op);
                }
                else
                {
                    #region Dictionary
                    if (t.Name.StartsWith("Dictionary") && ts[0].Name == "String" && ts[1].Name == "String")
                    {
                        //忽略MDictionary
                        return Split(json);
                    }

                    object objT = t.Name.Contains("Dictionary") ? Activator.CreateInstance(t, StringComparer.OrdinalIgnoreCase) : Activator.CreateInstance(t);
                    Type oT = objT.GetType();
                    MethodInfo mi = null;
                    try
                    {
                        if (t.Name == "NameValueCollection")
                        {
                            mi = oT.GetMethod("Add", new Type[] { typeof(string), typeof(string) });
                        }
                        else
                        {
                            mi = oT.GetMethod("Add");
                        }
                    }
                    catch
                    {

                    }
                    if (mi == null)
                    {
                        mi = oT.GetMethod("Add", new Type[] { typeof(string), typeof(string) });
                    }
                    if (mi != null)
                    {
                        Dictionary<string, string> dic = Split(json);
                        if (dic != null && dic.Count > 0)
                        {
                            foreach (KeyValuePair<string, string> kv in dic)
                            {
                                mi.Invoke(objT, new object[] { ConvertTool.ChangeType(kv.Key, ts[0]), ConvertTool.ChangeType(UnEscape(kv.Value, op), ts[1]) });
                            }
                        }
                        return objT;
                    }
                    #endregion
                }


            }
            else if (t.FullName.EndsWith("[]"))
            {
                return ConvertTool.GetObj(t, json);
            }
            return null;
        }
        public static T ToEntity<T>(string json) where T : class
        {
            return ToEntity<T>(json, EscapeOp.No);
        }
        internal static object ToEntity(Type t, string json, EscapeOp op)
        {
            if (t.FullName == "System.Data.DataTable")
            {
                return MDataTable.CreateFrom(json, null, op).ToDataTable();
            }
            if (t.Name == "MDataTable")
            {
                return MDataTable.CreateFrom(json, null, op);
            }
            if (t.FullName.StartsWith("System.Collections.") || t.FullName.EndsWith("[]") || t.FullName.Contains("MDictionary") || t.FullName.Contains("MList"))
            {
                return ToIEnumerator(t, json, op);
            }
            else
            {
                return JsonSplit.ToEntityOrList(t, json, op);
            }
        }
        /// <summary>
        /// Convert json to Entity
        /// <para>将Json转换为实体</para>
        /// </summary>
        /// <typeparam name="T">Type<para>类型</para></typeparam>
        public static T ToEntity<T>(string json, EscapeOp op) where T : class
        {
            return ToEntity(typeof(T), json, op) as T;
        }
        public static List<T> ToList<T>(string json)// where T : class
        {
            return ToList<T>(json, EscapeOp.No);
        }
        /// <summary>
        ///  Convert json to Entity List
        ///  <para>将Json转换为实体列表</para>
        /// </summary>
        /// <typeparam name="T">Type<para>类型</para></typeparam>
        public static List<T> ToList<T>(string json, EscapeOp op)// where T : class
        {
            return JsonSplit.ToList<T>(json, 0, op);//减少中间转换环节。
        }
        /// <summary>
        /// Convert object to json
        /// <para>将一个对象（实体，泛型List，字典Dictionary）转成Json</para>
        /// </summary>
        public static string ToJson(object obj)
        {
            return ToJson(obj, null);
        }

        /// <param name="jsonOp">Json Config</param>

        public static string ToJson(object obj, JsonOp jsonOp)
        {
            if (obj is string)
            {
                string text = Convert.ToString(obj);
                if (text == "")
                {
                    return "{}";
                }
                else if (text[0] == '{' || text[0] == '[')
                {
                    if (IsJson(text))
                    {
                        return text;
                    }
                }
                else if (text[0] == '<' && text[text.Length - 1] == '>')
                {
                    return XmlToJson(text, true);
                }
            }
            if (obj is IEnumerable && obj is ICollection)
            {
                int batchSize = 300;//200条数据为一个批次。
                int count = ((ICollection)obj).Count;
                if (count > batchSize * 2)
                {
                    var objIEnumerable = obj as IEnumerable;
                    StringBuilder sb = new StringBuilder();
                    sb.Append("[");
                    int batchCount = (count % batchSize) == 0 ? count / batchSize : count / batchSize + 1;//页数
                    string[] items = new string[batchCount];
                    int i = -1, batchID = 1;
                    List<object> firstList = new List<object>(batchSize);
                    ThreadEntity entity = null;
                    foreach (var o in objIEnumerable)
                    {
                        i++;

                        //自己处理的条数
                        if (i < batchSize) { firstList.Add(o); continue; }

                        if (i % batchSize == 0)
                        {
                            entity = new ThreadEntity();
                            entity.Items = items;
                            entity.BatchID = batchID;
                            entity.Objs = new List<object>(batchSize);
                            entity.Objs.Add(o);
                            entity.JsonOp = jsonOp;
                        }
                        else
                        {
                            entity.Objs.Add(o);
                        }
                        if (entity.Objs.Count == batchSize || i == count - 1)
                        {
                            // new Thread(new ParameterizedThreadStart(ToInThread)).Start(entity);
                            ThreadPool.QueueUserWorkItem(new WaitCallback(ToInThread), entity);
                            batchID++;
                        }

                    }
                    //自己处理前100条数据。
                    string json = ListToJson(firstList, jsonOp);
                    sb.Append(json);
                    i = 1;
                    while (true)
                    {
                        if (items[i] != null)
                        {
                            json = items[i];
                            sb.Append(',');
                            sb.Append(json);
                            i++;
                        }

                        if (i > items.Length - 1)
                        {
                            break;
                        }
                    }
                    sb.Append(']');
                    return sb.ToString();
                }
            }

            JsonHelper js = new JsonHelper(jsonOp);
            js.Fill(obj);
            return js.ToString(obj is IList || obj is DataTable || obj is MDataTable);

        }

        private static string ListToJson(List<object> obj, JsonOp jsonOp)
        {
            JsonHelper js = new JsonHelper(jsonOp);
            js.isArrayEnd = false;
            js.Fill(obj);
            return js.ToString();
        }
        private static void ToInThread(object entityObj)
        {
            ThreadEntity entity = entityObj as ThreadEntity;
            string json = ListToJson(entity.Objs, entity.JsonOp);
            entity.Items[entity.BatchID] = json;
        }

    }

    internal class ThreadEntity
    {
        public int BatchID { get; set; }
        public List<object> Objs { get; set; }

        public string[] Items { get; set; }
        public JsonOp JsonOp { get; set; }
    }
}
