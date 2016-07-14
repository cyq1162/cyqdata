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


namespace CYQ.Data.Tool
{
    /// <summary>
    /// json 帮助类
    /// </summary>
    public partial class JsonHelper
    {
        #region 实例属性

        public JsonHelper()
        {
        }
        /// <param name="addHead">是否带输出头</param>
        public JsonHelper(bool addHead)
        {
            _AddHead = addHead;
        }

        /// <param name="addSchema">是否首行带表结构[MDataTable.LoadFromJson可以还原表的数据类型]</param>
        public JsonHelper(bool addHead, bool addSchema)
        {
            _AddHead = addHead;
            _AddSchema = addSchema;
        }
        #region 属性
        ///// <summary>
        ///// 是否转义转义符号（默认true）
        ///// </summary>
        //public bool IsEscapeChar = true;
        /// <summary>
        /// 是否将名称转为小写
        /// </summary>
        public bool IsConvertNameToLower = false;
        /// <summary>
        /// 日期的格式化（默认：yyyy-MM-dd HH:mm:ss）
        /// </summary>
        public string DateTimeFormatter = "yyyy-MM-dd HH:mm:ss";
        RowOp _RowOp = RowOp.IgnoreNull;
        /// <summary>
        ///  Json输出行数据的过滤选项
        /// </summary>
        public RowOp RowOp
        {
            get
            {
                return _RowOp;
            }
            set
            {
                _RowOp = value;
            }
        }

        private bool _AddHead = false;
        private bool _AddSchema = false;
        /// <summary>
        /// 是否成功   
        /// </summary>
        public bool Success
        {
            get
            {
                return rowCount > 0;
            }
        }
        private string errorMsg = "";
        /// <summary>
        /// 错误提示信息   
        /// </summary>
        public string ErrorMsg
        {
            get
            {
                return errorMsg;
            }
            set
            {
                errorMsg = value;
            }
        }
        private int rowCount = 0;
        /// <summary>
        /// 当前返回的行数
        /// </summary>
        public int RowCount
        {
            get
            {
                return rowCount;
            }
            set
            {
                rowCount = value;
            }
        }
        private int total;
        /// <summary>
        /// 所有记录的总数（多数用于分页的记录总数）。
        /// </summary>
        public int Total
        {
            get
            {
                if (total == 0)
                {
                    return rowCount;
                }
                return total;
            }
            set
            {
                total = value;
            }
        }
        #endregion

        private List<string> jsonItems = new List<string>();


        /// <summary>
        /// 添加完一行数据后调用此方法换行
        /// </summary>
        public void AddBr()
        {
            jsonItems.Add("[#<br>]");
            rowCount++;
        }
        string footText = string.Empty;
        /// <summary>
        /// 添加底部数据（只有AddHead为true情况才能添加数据）
        /// </summary>
        public void AddFoot(string name, string value)
        {
            AddFoot(name, value, false);
        }
        /// <summary>
        ///  添加底部数据（只有AddHead为true情况才能添加数据）
        /// </summary>
        public void AddFoot(string name, string value, bool noQuotes)
        {
            if (_AddHead)
            {
                footText += "," + Format(name, value, noQuotes);
            }
        }

        /// <summary>
        /// 添加一个字段的值
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Add(string name, string value)
        {
            jsonItems.Add(Format(name, value, false));
        }
        /// <summary>
        /// 添加一个字段的值
        /// </summary>
        /// <param name="noQuotes">值不带引号</param>
        public void Add(string name, string value, bool noQuotes)
        {
            jsonItems.Add(Format(name, value, noQuotes));
        }
        private string Format(string name, string value, bool children)
        {
            value = value ?? "";
            children = children && !string.IsNullOrEmpty(value);
            if (!children && value.Length > 1 &&
                ((value[0] == '{' && value[value.Length - 1] == '}') || (value[0] == '[' && value[value.Length - 1] == ']')))
            {
                children = IsJson(value);
            }
            if (!children)
            {
                // int index = value.IndexOf('"');
                int len = value.Length;
                for (int i = 0; i < len; i++)
                {
                    switch (value[i])
                    {
                        case '"':
                            if (i == 0 || value[i - 1] != '\\')
                            {
                                value = value.Insert(i, "\\");
                                len++;//新插入了一个字符。
                                i++;//索引往前一个。
                            }
                            break;
                        case '\\':
                            if (i == len - 1 || (value[i + 1] != '"'))// && value[i + 1] != '\\'))
                            {
                                value = value.Insert(i, "\\");
                                len++;//新插入了一个字符。
                                i++;//索引往前一个。
                            }
                            break;
                    }
                }

                //if (children)
                //{
                //    value = value.Replace("\\\"", "\"");
                //}
            }
            return "\"" + name + "\":" + (!children ? "\"" : "") + value + (!children ? "\"" : "");//;//.Replace("\",\"", "\" , \"").Replace("}{", "} {").Replace("},{", "}, {")
        }

        /// <summary>
        /// 输出Json字符串
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (_AddHead)
            {
                sb.Append("{");
                sb.Append("\"rowcount\":" + rowCount + ",");
                sb.Append("\"total\":" + Total + ",");
                sb.Append("\"errorMsg\":\"" + errorMsg + "\",");
                sb.Append("\"success\":" + Success.ToString().ToLower() + ",");
                sb.Append("\"rows\":");
            }
            int index = 0;
            if (jsonItems.Count <= 0)
            {
                if (_AddHead)
                {
                    sb.Append("[]");
                }
            }
            else
            {
                if (jsonItems[jsonItems.Count - 1] != "[#<br>]")
                {
                    AddBr();
                }
                if (_AddHead || rowCount > 1)
                {
                    sb.Append("[");
                }
                sb.Append("{");
                foreach (string val in jsonItems)
                {
                    index++;

                    if (val != "[#<br>]")
                    {
                        sb.Append(val + ",");
                    }
                    else
                    {
                        sb = sb.Replace(",", "", sb.Length - 1, 1);
                        sb.Append("},");
                        if (index < jsonItems.Count)
                        {
                            sb.Append("{");
                        }
                    }
                }
                sb = sb.Replace(",", "", sb.Length - 1, 1);//去除最后一个,号。
                //sb.Append("}");
                if (_AddHead || rowCount > 1)
                {
                    sb.Append("]");
                }
            }
            if (_AddHead)
            {
                sb.Append(footText + "}");
            }
            string json = sb.ToString();
            if (System.Web.HttpContext.Current != null) // Web应用
            {
                json = json.Replace("\n", "<br/>").Replace("\t", " ").Replace("\r", " ");
            }
            return json;

        }
        public string ToString(bool arrayEnd)
        {
            string result = ToString();
            if (arrayEnd && !result.StartsWith("["))
            {
                result = '[' + result + ']';
            }
            return result;
        }

        #endregion

        /// <summary>
        /// 检测是否Json格式的字符串
        /// </summary>
        /// <param name="json">要检测的字符串</param>
        public static bool IsJson(string json)
        {
            return JsonSplit.IsJson(json);
        }
        /// <summary>
        /// 检测是否Json格式的字符串
        /// </summary>
        /// <param name="json">要检测的字符串</param>
        /// <param name="errIndex">错误的字符索引</param>
        /// <returns></returns>
        public static bool IsJson(string json, out int errIndex)
        {
            return JsonSplit.IsJson(json, out errIndex);
        }
        /// <summary>
        /// 获取Json字符串的值
        /// </summary>
        /// <param name="json">Json字符串</param>
        /// <param name="key">键值</param>
        /// <returns></returns>
        public static string GetJosnValue(string json, string key)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(json))
            {
                Dictionary<string, string> jsonDic = Split(json);
                if (jsonDic.ContainsKey(key))
                {
                    result = jsonDic[key];
                }
                else
                {
                    #region 字符串截取
                    key = "\"" + key.Trim('"') + "\"";
                    int index = json.IndexOf(key, StringComparison.OrdinalIgnoreCase) + key.Length + 1;
                    if (index > key.Length + 1)
                    {
                        int end = 0;
                        for (int i = index; i < json.Length; i++)
                        {
                            switch (json[i])
                            {
                                case '{':
                                    end = json.IndexOf('}', i) + 1;
                                    goto endfor;
                                case '[':
                                    end = json.IndexOf(']', i) + 1;
                                    goto endfor;
                                case '"':
                                    end = json.IndexOf('"', i) + 1;
                                    goto endfor;
                                case '\'':
                                    end = json.IndexOf('\'', i) + 1;
                                    goto endfor;
                                case ' ':
                                    continue;
                                default:
                                    end = json.IndexOf(',', i);
                                    if (end == -1)
                                    {
                                        end = json.IndexOf('}', index);
                                    }
                                    goto endfor;

                            }
                        }
                    endfor:
                        if (end > index)
                        {
                            //index = json.IndexOf('"', index + key.Length + 1) + 1;
                            result = json.Substring(index, end - index);
                            //过滤引号或空格
                            result = result.Trim(new char[] { '"', ' ', '\'' });
                        }
                    }
                    #endregion
                }
                jsonDic = null;
            }
            return result;
        }
        /// <summary>
        /// 返回Json格式的结果信息
        /// </summary>
        public static string OutResult(bool result, string msg, params Dictionary<string, object>[] otherKeyValues)
        {
            JsonHelper js = new JsonHelper(false, false);
            js.Add("success", result.ToString().ToLower(), true);
            js.Add("msg", msg);
            if (otherKeyValues.Length > 0)
            {
                string value = string.Empty;
                foreach (KeyValuePair<string, object> item in otherKeyValues[0])
                {
                    if (item.Value != null)
                    {
                        value = item.Value.ToString();
                        Type t = item.Value.GetType();
                        bool isValueType = t.IsValueType;
                        if (isValueType)
                        {
                            if (t.Name == "Boolean")
                            {
                                value = value.ToLower();
                            }
                        }
                        js.Add(item.Key, value, isValueType);
                    }
                }
            }
            return js.ToString();
            /*
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"success\":" + result.ToString().ToLower() + ",");
            sb.Append("\"msg\":\"" + msg + "\"");
            if (otherKeyValues.Length > 0)
            {
                string value = string.Empty;
                foreach (KeyValuePair<string, object> item in otherKeyValues[0])
                {
                    if (item.Value != null)
                    {
                        value = item.Value.ToString();
                        Type t = item.Value.GetType();
                        bool isValueType = t.IsValueType;
                        if (isValueType)
                        {
                            if (t.Name == "Boolean")
                            {
                                value = value.ToLower();
                            }
                        }
                        sb.AppendFormat(",\"{0}\":{1}", item.Key, isValueType ? value : "\"" + value + "\"");
                    }
                }
            }
            sb.Append("}");
            return sb.ToString();
             * */
        }

        /// <summary>
        /// 将Json分隔成键值对。
        /// </summary>
        public static Dictionary<string, string> Split(string json)
        {
            List<Dictionary<string, string>> result = JsonSplit.Split(json);
            if (result != null && result.Count > 0)
            {
                return result[0];
            }
            return null;
        }
        /// <summary>
        /// 将Json 数组分隔成多个键值对。
        /// </summary>
        public static List<Dictionary<string, string>> SplitArray(string jsonArray)
        {
            if (string.IsNullOrEmpty(jsonArray))
            {
                return null;
            }
            jsonArray = jsonArray.Trim();
            return JsonSplit.Split(jsonArray);
        }


    }

    // 扩展交互部分
    public partial class JsonHelper
    {
        /// <summary>
        /// 从数据表中取数据填充,最终可输出json字符串
        /// </summary>
        public void Fill(MDataTable table)
        {
            if (table == null)
            {
                ErrorMsg = "MDataTable object is null";
                return;
            }
            if (_AddSchema)
            {
                Fill(table.Columns, false);
            }
            //RowCount = table.Rows.Count;
            Total = table.RecordsAffected;

            if (table.Rows.Count > 0)
            {
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    Fill(table.Rows[i]);
                }
            }
        }
        /// <summary>
        /// 从数据行中填充，最终可输出json字符串。
        /// </summary>
        /// <param name="row"></param>
        public void Fill(MDataRow row)
        {
            if (row == null)
            {
                ErrorMsg = "MDataRow object is null";
                return;
            }

            for (int i = 0; i < row.Count; i++)
            {
                MDataCell cell = row[i];
                //if (cell.IsNull)
                //{
                //    continue;
                //}
                if (_RowOp == RowOp.None || (!cell.IsNull && (cell.Struct.IsPrimaryKey || cell.cellValue.State >= (int)_RowOp)))
                {
                    #region MyRegion
                    string name = row[i].ColumnName;
                    if (IsConvertNameToLower)
                    {
                        name = name.ToLower();
                    }

                    string value = cell.ToString();
                    int groupID = DataType.GetGroup(cell.Struct.SqlType);
                    bool noQuot = groupID == 1 || groupID == 3;
                    if (cell.IsNull)
                    {
                        value = "null";
                        noQuot = true;
                    }
                    else
                    {

                        if (groupID == 3)
                        {
                            value = value.ToLower();
                        }
                        else if (groupID == 2)
                        {
                            DateTime dt;
                            if (DateTime.TryParse(value, out dt))
                            {
                                value = dt.ToString(DateTimeFormatter);
                            }
                        }
                        else if (groupID == 999)
                        {
                            Type t = cell.Value.GetType();
                            if (!t.FullName.StartsWith("System."))//普通对象。
                            {
                                MDataRow oRow = new MDataRow(TableSchema.GetColumns(t));
                                oRow.LoadFrom(cell.Value);
                                value = oRow.ToJson(_RowOp, IsConvertNameToLower);
                                noQuot = true;
                            }
                            else if (cell.Value is IEnumerable)
                            {
                                int len = StaticTool.GetArgumentLength(ref t);
                                if (len == 1)
                                {
                                    MDataTable dt = MDataTable.CreateFrom(cell.Value);
                                    value = dt.ToJson(false, false, _RowOp, IsConvertNameToLower);
                                    noQuot = true;
                                }
                                else if (len == 2)
                                {
                                    value = MDataRow.CreateFrom(cell.Value).ToJson(_RowOp, IsConvertNameToLower);
                                    noQuot = true;
                                }
                            }
                        }
                    }
                    Add(name, value, noQuot);

                    #endregion
                }

            }
            AddBr();
        }
        /// <summary>
        /// 从数据结构填充，最终可输出json字符串。
        /// </summary>
        /// <param name="column">数据结构</param>
        /// <param name="isFullSchema">false：输出单行的[列名：数据类型]；true：输出多行的完整的数据结构</param>
        public void Fill(MDataColumn column, bool isFullSchema)
        {
            if (column == null)
            {
                ErrorMsg = "MDataColumn object is null";
                return;
            }

            if (isFullSchema)
            {
                foreach (MCellStruct item in column)
                {
                    Add("ColumnName", item.ColumnName);
                    Add("SqlType", item.ValueType.FullName);
                    Add("IsAutoIncrement", item.IsAutoIncrement.ToString().ToLower(), true);
                    Add("IsCanNull", item.IsCanNull.ToString().ToLower(), true);
                    Add("MaxSize", item.MaxSize.ToString(), true);
                    Add("Scale", item.Scale.ToString().ToLower(), true);
                    Add("IsPrimaryKey", item.IsPrimaryKey.ToString().ToLower(), true);
                    Add("DefaultValue", Convert.ToString(item.DefaultValue));
                    Add("Description", item.Description);
                    //新增属性
                    Add("TableName", item.TableName);
                    Add("IsUniqueKey", item.IsUniqueKey.ToString().ToLower(), true);
                    Add("IsForeignKey", item.IsForeignKey.ToString().ToLower(), true);
                    Add("FKTableName", item.FKTableName);

                    AddBr();
                }
            }
            else
            {
                for (int i = 0; i < column.Count; i++)
                {
                    Add(column[i].ColumnName, column[i].ValueType.FullName);
                }
                AddBr();
            }
            rowCount = 0;//重置为0
        }
        /// <summary>
        /// 可从类(对象,泛型List、泛型Dictionary）中填充，最终可输出json字符串。
        /// </summary>
        /// <param name="obj">实体类对象</param>
        public void Fill(object obj)
        {
            if (obj != null)
            {
                if (obj is IEnumerable)
                {
                    Type t = obj.GetType();
                    int len = StaticTool.GetArgumentLength(ref t);
                    if (len == 1)
                    {
                        foreach (object o in obj as IEnumerable)
                        {
                            Fill(MDataRow.CreateFrom(o));
                        }
                    }
                    else if (len == 2)
                    {
                        Fill(MDataRow.CreateFrom(obj));
                    }
                }
                else
                {
                    MDataRow row = new MDataRow();
                    row.LoadFrom(obj);
                    Fill(row);
                }
            }
        }

        private static Dictionary<string, object> lockList = new Dictionary<string, object>();
        /// <summary>
        /// 从Json字符串中反加载成数据表
        /// </summary>
        internal static MDataTable ToMDataTable(string jsonOrFileName, MDataColumn mdc)
        {

            MDataTable table = new MDataTable("SysDefaultLoadFromJson");
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
                        if (result.Count == 1 && result[0].ContainsKey("total") && result[0].ContainsKey("rows"))
                        {
                            int count = 0;
                            if (int.TryParse(result[0]["total"], out count))
                            {
                                table.RecordsAffected = count;//还原记录总数。
                            }
                            result = SplitArray(result[0]["rows"]);
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
                                        cell.Value = item.Value;
                                    }
                                }
                            }
                        }
                    }
                    #endregion
            }
            catch (Exception err)
            {
                Log.WriteLogToTxt(err);
            }

            return table;
        }
        /// <summary>
        /// 将Json转换成集合
        /// </summary>
        /// <typeparam name="T">集合类型</typeparam>
        /// <param name="json">json数据</param>
        /// <returns></returns>
        private static T ToIEnumerator<T>(string json)
            where T : class
        {
            Type t = typeof(T);
            if (t.FullName.StartsWith("System.Collections."))
            {
                Dictionary<string, string> dic = Split(json);
                if (t.FullName.Contains("Dictionary"))
                {
                    Type[] ts;
                    if (StaticTool.GetArgumentLength(ref t, out ts) == 2 && ts[0].Name == "String" && ts[1].Name == "String")
                    {
                        return dic as T;
                    }
                }

                T objT = Activator.CreateInstance<T>();
                Type oT = objT.GetType();
                MethodInfo mi = null;
                try
                {
                    mi = oT.GetMethod("Add");
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
                    foreach (KeyValuePair<string, string> kv in dic)
                    {
                        mi.Invoke(objT, new object[] { kv.Key, kv.Value });
                    }
                    return objT;
                }

            }
            return default(T);
        }
        /// <summary>
        /// 将Json转换为实体
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="json">json数据</param>
        public static T ToEntity<T>(string json) where T : class
        {
            Type t = typeof(T);
            if (t.FullName.StartsWith("System.Collections."))
            {
                return ToIEnumerator<T>(json);
            }
            else
            {
                MDataRow row = new MDataRow(TableSchema.GetColumns(t));
                row.LoadFrom(json);
                return row.ToEntity<T>();
            }
        }
        /// <summary>
        /// 将Json转换为实体列表
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="json">json数据</param>
        public static List<T> ToList<T>(string json) where T : class
        {
            return ToMDataTable(json, TableSchema.GetColumns(typeof(T))).ToList<T>();
        }
        /// <summary>
        /// 将一个对象（实体，泛型List，字典Dictionary）转成Json
        /// </summary>
        public static string ToJson(object obj)
        {
            return ToJson(obj, false, RowOp.IgnoreNull);
        }
        /// <summary>
        /// 将一个对象（实体，泛型List，字典Dictionary）转成Json
        /// </summary>
        public static string ToJson(object obj, bool isConvertNameToLower)
        {
            return ToJson(obj, isConvertNameToLower, RowOp.IgnoreNull);
        }
        /// <summary>
        ///  将一个对象（实体，泛型List，字典Dictionary）转成Json
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="isConvertNameToLower">字段是否转小写</param>
        /// <param name="op">默认值为RowOp.All</param>
        /// <returns></returns>
        public static string ToJson(object obj, bool isConvertNameToLower, RowOp op)
        {
            JsonHelper js = new JsonHelper();
            js.IsConvertNameToLower = isConvertNameToLower;
            js.RowOp = op;
            js.Fill(obj);
            return js.ToString();
        }

        #region Xml 转 Json
        /*
        /// <summary>
        /// 转Json
        /// <param name="xml">xml字符串</param>
        /// <param name="isConvertNameToLower">字段是否转小写</param>
        /// <param name="isWithAttr">是否将属性值也输出</param>
        /// </summary>
        public static string ToJson(string xml, bool isConvertNameToLower, bool isWithAttr)
        {
            using (XHtmlAction action = new XHtmlAction(false, true))
            {
                try
                {
                    action.LoadXml(xml);
                    return ToJson(action.XmlDoc.DocumentElement, isConvertNameToLower, isWithAttr);
                }
                catch (Exception err)
                {
                    Log.WriteLogToTxt(err);
                }

            }
        } 
         */
        #endregion


        #region Json转Xml
        /// <summary>
        /// 将一个Json转成Xml
        /// </summary>
        /// <param name="json">Json字符串</param>
        public static string ToXml(string json)
        {
            return ToXml(json, true);
        }
        /// <summary>
        /// 将一个Json转成Xml
        /// </summary>
        /// <param name="json">Json字符串</param>
        /// <param name="isWithAttr">是否转成属性，默认true</param>
        /// <returns></returns>
        public static string ToXml(string json, bool isWithAttr)
        {
            StringBuilder xml = new StringBuilder();
            xml.Append("<?xml version=\"1.0\"  standalone=\"yes\"?>");
            List<Dictionary<string, string>> dicList = JsonSplit.Split(json);
            if (dicList != null && dicList.Count > 0)
            {
                bool addRoot = dicList.Count > 1 || dicList[0].Count > 1;
                if (addRoot)
                {
                    xml.Append("<root>");//</root>";
                }

                xml.Append(GetXmlList(dicList, isWithAttr));

                if (addRoot)
                {
                    xml.Append("</root>");//</root>";
                }

            }
            return xml.ToString();
        }

        private static string GetXmlList(List<Dictionary<string, string>> dicList, bool isWithAttr)
        {
            if (dicList == null || dicList.Count == 0)
            {
                return string.Empty;
            }
            StringBuilder xml = new StringBuilder();
            for (int i = 0; i < dicList.Count; i++)
            {
                xml.Append(GetXml(dicList[i], isWithAttr));
            }
            return xml.ToString();
        }

        private static string GetXml(Dictionary<string, string> dic, bool isWithAttr)
        {
            StringBuilder xml = new StringBuilder();
            bool isJson = false;
            foreach (KeyValuePair<string, string> item in dic)
            {
                isJson = IsJson(item.Value);
                if (!isJson)
                {
                    xml.AppendFormat("<{0}>{1}</{0}>", item.Key, FormatCDATA(item.Value));
                }
                else
                {
                    List<Dictionary<string, string>> jsonList = JsonSplit.Split(item.Value);
                    if (jsonList != null && jsonList.Count > 0)
                    {
                        if (!isWithAttr)
                        {
                            xml.AppendFormat("<{0}>", item.Key);
                        }
                        for (int j = 0; j < jsonList.Count; j++)
                        {
                            if (isWithAttr)
                            {
                                xml.Append(GetXmlElement(item.Key, jsonList[j]));
                            }
                            else
                            {
                                xml.Append(GetXml(jsonList[j], isWithAttr));
                            }
                        }
                        if (!isWithAttr)
                        {
                            xml.AppendFormat("</{0}>", item.Key);
                        }
                    }
                    else // 空Json {}
                    {
                        xml.AppendFormat("<{0}></{0}>", item.Key);
                    }
                }
            }
            return xml.ToString();
        }

        private static string GetXmlElement(string parentName, Dictionary<string, string> dic)
        {
            StringBuilder xml = new StringBuilder();
            Dictionary<string, string> jsonDic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            xml.Append("<" + parentName);
            foreach (KeyValuePair<string, string> kv in dic)
            {
                if (kv.Value.IndexOf('"') > -1 || IsJson(kv.Value)) // 属性不能带双引号，所以转到元素处理。
                {
                    jsonDic.Add(kv.Key, kv.Value);
                }
            }
            //InnerText 节点存在=》（如果有元素节点，则当属性处理；若无，则当InnerText）
            bool useForInnerText = dic.ContainsKey(parentName) && jsonDic.Count == 0;
            foreach (KeyValuePair<string, string> kv in dic)
            {
                if (!jsonDic.ContainsKey(kv.Key) && (kv.Key != parentName || !useForInnerText))
                {
                    xml.AppendFormat(" {0}=\"{1}\"", kv.Key, kv.Value);//先忽略同名属性，内部InnerText节点，
                }
            }
            xml.Append(">");
            if (useForInnerText)
            {
                xml.Append(FormatCDATA(dic[parentName]));//InnerText。
            }
            else if (jsonDic.Count > 0)
            {
                xml.Append(GetXml(jsonDic, true));//数组，当元素处理。
            }
            xml.Append("</" + parentName + ">");

            return xml.ToString();
        }
        private static string FormatCDATA(string text)
        {
            if (text.LastIndexOfAny(new char[] { '<', '>', '&' }) > -1 && !text.StartsWith("<![CDATA["))
            {
                text = "<![CDATA[" + text.Trim() + "]]>";
            }
            return text;
        }
        #endregion
    }
}
