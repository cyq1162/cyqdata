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
    /// Escape json char options
    /// <para>JsonHelper 的符号转义选项</para>
    /// </summary>
    public enum EscapeOp
    {
        /// <summary>
        /// Web 默认转义，Win不转义
        /// </summary>
        Default,
        No,
        Yes
    }
    /// <summary>
    /// Json class for you easy to operate json
    /// <para>功能全面的json帮助类</para>
    /// </summary>
    public partial class JsonHelper
    {

        internal static EscapeOp DefaultEscape
        {
            get
            {
                return (EscapeOp)Enum.Parse(typeof(EscapeOp), AppConfig.JsonEscape);
            }
        }
        #region 实例属性

        public JsonHelper()
        {

        }
        /// <param name="addHead">with easyui header ?<para>是否带输出头</para></param>
        public JsonHelper(bool addHead)
        {
            _AddHead = addHead;
        }

        /// <param name="addSchema">first row with table schema ?
        /// <para>是否首行带表结构[MDataTable.LoadFromJson可以还原表的数据类型]</para></param>
        public JsonHelper(bool addHead, bool addSchema)
        {
            _AddHead = addHead;
            _AddSchema = addSchema;
        }
        #region 属性
        /// <summary>
        /// Escape options
        /// <para>转义符号</para>
        /// </summary>
        public EscapeOp Escape = JsonHelper.DefaultEscape;
        /// <summary>
        /// convert filed to lower
        /// <para>是否将名称转为小写</para>
        /// </summary>
        public bool IsConvertNameToLower = false;
        /// <summary>
        /// formate datetime
        /// <para>日期的格式化（默认：yyyy-MM-dd HH:mm:ss）</para>
        /// </summary>
        public string DateTimeFormatter = "yyyy-MM-dd HH:mm:ss";
        private string brFlag = "[#<br>]";
        RowOp _RowOp = RowOp.IgnoreNull;
        /// <summary>
        ///  filter json data
        /// <para>Json输出行数据的过滤选项</para>
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
        ///  is success
        /// <para>是否成功</para>
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
        /// Error message
        /// <para>错误提示信息  </para> 
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
        /// data rows count
        /// <para>当前返回的行数</para>
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
        /// totla count
        /// <para>所有记录的总数（多数用于分页的记录总数）。</para>
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
        /// flag a json is end and start a new json
        /// <para> 添加完一个Json数据后调用此方法换行</para>
        /// </summary>
        public void AddBr()
        {
            jsonItems.Add(brFlag);
            rowCount++;
        }
        StringBuilder footText = new StringBuilder();
        /// <summary>
        /// attach json data (AddHead must be true)
        /// <para>添加底部数据（只有AddHead为true情况才能添加数据）</para>
        /// </summary>
        public void AddFoot(string name, string value)
        {
            AddFoot(name, value, false);
        }

        public void AddFoot(string name, string value, bool noQuotes)
        {
            if (_AddHead)
            {
                footText.Append("," + Format(name, value, noQuotes));
            }
        }

        /// <summary>
        /// add json key value
        /// <para>添加一个字段的值</para>
        /// </summary>
        public void Add(string name, string value)
        {
            jsonItems.Add(Format(name, value, false));
        }

        /// <param name="noQuotes">value is no quotes
        /// <para>值不带引号</para></param>
        public void Add(string name, string value, bool noQuotes)
        {
            jsonItems.Add(Format(name, value, noQuotes));
        }
        private void Add(string name, object value)
        {
            if (value != null)
            {
                string v = null;
                Type t = value.GetType();
                int groupID = DataType.GetGroup(DataType.GetSqlType(t));
                bool noQuotes = groupID == 1 || groupID == 3;
                if (groupID == 999)
                {
                    v = ToJson(value);
                }
                else
                {
                    v = Convert.ToString(value);
                    if (groupID == 3)
                    {
                        v = v.ToLower();
                    }
                }
                Add(name, v, noQuotes);
            }
            else
            {
                Add(name, "null", true);
            }
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
                SetEscape(ref value);
            }
            return "\"" + name + "\":" + (!children ? "\"" : "") + value + (!children ? "\"" : "");//;//.Replace("\",\"", "\" , \"").Replace("}{", "} {").Replace("},{", "}, {")
        }
        private void SetEscape(ref string value)
        {
            if (Escape == EscapeOp.No) { return; }
            //if (value.IndexOfAny(new char[] { '"', '\\', '\n' }) > -1)//easyui 输出时需要处理\\符号
            //{
            bool isInsert = false;
            int len = value.Length;
            StringBuilder sb = new StringBuilder(len + 10);
            for (int i = 0; i < len; i++)
            {
                char c = value[i];
                if (Escape == EscapeOp.Yes && c < 32)
                {
                    isInsert = true;
                    sb.Append(" ");//对于特殊符号，直接替换成空。
                    continue;
                }
                switch (c)
                {
                    case '"':
                        if (i == 0 || value[i - 1] != '\\')
                        {
                            isInsert = true;
                            sb.Append("\\");
                        }
                        break;
                    case '\n':
                         isInsert = true;
                        sb.Append("\\n");//直接替换追加
                        continue;
                    case '\t':
                    case '\r':
                        isInsert = true;
                        sb.Append(" ");//直接替换追加
                        continue;
                    case '\\':
                        if (i == len - 1 || ((value[i + 1] != '"') && value[i + 1] != 'n' && value[i + 1] != 't' && value[i + 1] != 'r'))
                        {
                            isInsert = true;
                            sb.Append("\\");
                        }
                        break;
                }
                sb.Append(c);
            }
            if (isInsert)
            {
                value = null;
                value = sb.ToString();
            }
            else { sb = null; }
            // }
        }
        /// <summary>
        /// out json result
        /// <para>输出Json字符串</para>
        /// </summary>
        public override string ToString()
        {
            int capacity = 100;
            if (jsonItems.Count > 0)
            {
                capacity = jsonItems.Count * jsonItems[0].Length;
            }
            StringBuilder sb = new StringBuilder(capacity);

            if (_AddHead)
            {
                sb.Append("{");
                sb.Append("\"rowcount\":" + rowCount + ",");
                sb.Append("\"total\":" + Total + ",");
                sb.Append("\"errorMsg\":\"" + errorMsg + "\",");
                sb.Append("\"success\":" + Success.ToString().ToLower() + ",");
                sb.Append("\"rows\":");
            }
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
                char left = '{', right = '}';
                if (!jsonItems[0].Contains(":") && !jsonItems[rowCount - 1].Contains(":"))
                {
                    //说明为数组
                    left = '[';
                    right = ']';
                }
                sb.Append(left);
                int index = 0;
                foreach (string val in jsonItems)
                {
                    index++;

                    if (val != brFlag)
                    {
                        sb.Append(val);
                        sb.Append(",");
                    }
                    else
                    {
                        sb.Remove(sb.Length - 1, 1);//性能优化（内部时，必须多了一个“，”号）。
                        // sb = sb.Replace(",", "", sb.Length - 1, 1);
                        sb.Append(right + ",");
                        if (index < jsonItems.Count)
                        {
                            sb.Append(left);
                        }
                    }
                }
                if (sb[sb.Length - 1] == ',')
                {
                    sb.Remove(sb.Length - 1, 1);
                }
                //sb = sb.Replace(",", "", sb.Length - 1, 1);//去除最后一个,号。
                //sb.Append("}");
                if (_AddHead || rowCount > 1)
                {
                    sb.Append("]");
                }
            }
            if (_AddHead)
            {
                sb.Append(footText.ToString() + "}");
            }
            return sb.ToString();
            //string json = sb.ToString();
            //if (AppConfig.IsWeb && Escape == EscapeOp.Yes)
            //{
            //    json = json.Replace("\n", "<br/>");
            //}
            //if (Escape != EscapeOp.No) // Web应用
            //{
            //    json = json.Replace("\t", " ").Replace("\r", " ");
            //}
            //return json;

        }

        /// <param name="arrayEnd">end with [] ?</param>
        /// <returns></returns>
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
        /// check string is json
        /// <para>检测是否Json格式的字符串</para>
        /// </summary>
        public static bool IsJson(string json)
        {
            return JsonSplit.IsJson(json);
        }


        /// <param name="errIndex">the index of the error char
        /// <para>错误的字符索引</para></param>
        public static bool IsJson(string json, out int errIndex)
        {
            return JsonSplit.IsJson(json, out errIndex);
        }
        /// <summary>
        /// Get json value
        /// <para>获取Json字符串的值</para>
        /// </summary>
        /// <param name="key">the name or key of json
        /// <para>键值(有层级时用：XXX.YYY.ZZZ)</para></param>
        /// <returns></returns>
        public static string GetValue(string json, string key)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(json))
            {
                List<Dictionary<string, string>> jsonList = JsonSplit.Split(json);
                if (jsonList.Count > 0)
                {
                    string[] items = key.Split('.');
                    string fKey = items[0];
                    int i = -1;
                    int fi = key.IndexOf('.');
                    if (int.TryParse(fKey, out i))//数字
                    {
                        if (i < jsonList.Count)
                        {
                            Dictionary<string, string> numJson = jsonList[i];
                            if (items.Length == 1)
                            {
                                result = ToJson(numJson);
                            }
                            else if (items.Length == 2) // 0.xxx
                            {
                                string sKey = items[1];
                                if (numJson.ContainsKey(sKey))
                                {
                                    result = numJson[sKey];
                                }
                            }
                            else
                            {
                                return GetValue(ToJson(numJson), key.Substring(fi + 1));
                            }
                        }
                    }
                    else // 非数字
                    {
                        Dictionary<string, string> jsonDic = jsonList[0];
                        if (items.Length == 1)
                        {
                            if (jsonDic.ContainsKey(fKey))
                            {
                                result = jsonDic[fKey];
                            }
                        }
                        else  // 取子集
                        {

                            return GetValue(jsonDic[fKey], key.Substring(fi + 1));
                        }
                    }




                    //int fi = key.IndexOf('.');
                    //if (jsonDic.ContainsKey(key))
                    //{
                    //    result = jsonDic[key];
                    //}
                    //else
                    //{

                    //    if (fi > -1)
                    //    {
                    //        string fKey = key.Substring(0, fi);//0.abc
                    //        if (jsonDic.ContainsKey(fKey))//0
                    //        {
                    //            return GetValue(jsonDic[fKey], key.Substring(fi + 1));
                    //        }
                    //        else
                    //        {
                    //            int index = -1;
                    //            if (int.TryParse(fKey, out index))//数字,走索引
                    //            {

                    //            }
                    //        }
                    //    }
                    //}
                    //jsonDic = null;
                    //jsonList = null;
                }
            }
            return result;
        }
        /// <summary>
        /// a easy method for you to return a json
        /// <para>返回Json格式的结果信息</para>
        /// </summary>
        public static string OutResult(bool result, string msg)
        {
            return OutResult(result, msg, false);
        }

        /// <param name="noQuates">no ""</param>
        public static string OutResult(bool result, string msg, bool noQuates)
        {
            JsonHelper js = new JsonHelper(false, false);
            js.Add("success", result.ToString().ToLower(), true);
            js.Add("msg", msg, noQuates);
            return js.ToString();
        }
        public static string OutResult(bool result, object msgObj)
        {
            return OutResult(result, ToJson(msgObj), true);
        }
        public static string OutResult(string name, object value, params object[] nameValues)
        {
            JsonHelper js = new JsonHelper();
            js.Add(name, value);
            for (int i = 0; i < nameValues.Length; i++) // 1
            {
                if (i % 2 == 0)
                {
                    string k = Convert.ToString(nameValues[i]);
                    i++;
                    object v = i == nameValues.Length ? null : nameValues[i];
                    js.Add(k, v);
                }
            }
            return js.ToString();
        }
        /// <summary>
        ///  split json to dicationary
        /// <para>将Json分隔成键值对。</para>
        /// </summary>
        public static Dictionary<string, string> Split(string json)
        {
            json = json.Trim();
            if (json[0] != '{' && json[0] != '[')
            {
                json = ToJson(json);
            }
            List<Dictionary<string, string>> result = JsonSplit.Split(json);
            if (result != null && result.Count > 0)
            {
                return result[0];
            }
            return null;
        }
        /// <summary>
        ///  split json to dicationary array
        /// <para>将Json 数组分隔成多个键值对。</para>
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
        /// Fill obj and get json from  ToString() method
        /// <para>从数据表中取数据填充,最终可输出json字符串</para>
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
        /// Fill obj and get json from  ToString() method
        /// <para>从数据行中取数据填充,最终可输出json字符串</para>
        /// </summary>
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
                if (_RowOp == RowOp.None || (!cell.IsNull && (cell.Struct.IsPrimaryKey || cell.State >= (int)_RowOp)))
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

                        if (groupID == 3 || (cell.Struct.MaxSize == 1 && groupID == 1)) // oracle 下的number 1会处理成bool类型
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
                            Type t = cell.Struct.ValueType;
                            if (t.FullName == "System.Object")
                            {
                                t = cell.Value.GetType();
                            }
                            if (t.Name == "Byte[]")
                            {
                                value = Convert.ToBase64String(cell.Value as byte[]);
                            }
                            else
                            {
                                if (cell.Value is IEnumerable)
                                {
                                    int len = StaticTool.GetArgumentLength(ref t);
                                    if (len <= 1)
                                    {
                                        JsonHelper js = new JsonHelper(false, false);
                                        js._RowOp = _RowOp;
                                        js.DateTimeFormatter = DateTimeFormatter;
                                        js.IsConvertNameToLower = IsConvertNameToLower;
                                        if (cell.Value is MDataRowCollection)
                                        {
                                            MDataTable dtx = (MDataRowCollection)cell.Value;
                                            js.Fill(dtx);
                                        }
                                        else
                                        {
                                            js.Fill(cell.Value);
                                        }
                                        value = js.ToString(true);
                                        noQuot = true;
                                    }
                                    else if (len == 2)
                                    {
                                        value = MDataRow.CreateFrom(cell.Value).ToJson(_RowOp, IsConvertNameToLower);
                                        noQuot = true;
                                    }
                                }
                                else
                                {
                                    if (!t.FullName.StartsWith("System."))//普通对象。
                                    {
                                        MDataRow oRow = new MDataRow(TableSchema.GetColumns(t));
                                        oRow.LoadFrom(cell.Value);
                                        value = oRow.ToJson(_RowOp, IsConvertNameToLower);
                                        noQuot = true;
                                    }
                                    else if (t.FullName == "System.Data.DataTable")
                                    {
                                        MDataTable dt = cell.Value as DataTable;
                                        value = dt.ToJson(false, false);
                                        noQuot = true;
                                    }
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
        ///  Fill obj and get json from  ToString() method
        /// <para>可从类(对象,泛型List、泛型Dictionary）中填充，最终可输出json字符串。</para>
        /// </summary>
        /// <param name="obj">实体类对象</param>
        public void Fill(object obj)
        {
            if (obj != null)
            {
                if (obj is String || obj is ValueType)
                {
                    Fill(Convert.ToString(obj));
                }
                else if (obj is MDataTable)
                {
                    Fill(obj as MDataTable);
                }
                else if (obj is MDataRow)
                {
                    Fill(obj as MDataRow);
                }
                else if (obj is IEnumerable)
                {
                    #region IEnumerable
                    Type t = obj.GetType();
                    int len = StaticTool.GetArgumentLength(ref t);
                    if (len == 1)
                    {
                        foreach (object o in obj as IEnumerable)
                        {
                            if (o is MDataTable)
                            {
                                Fill(o as MDataTable);
                            }
                            else if (o is DataTable)
                            {
                                MDataTable dt = o as DataTable;
                                Fill(dt);
                            }
                            else if (o is String || o is DateTime || o is Enum || o is Guid)
                            {
                                string str = o.ToString();
                                if ((str[0] == '{' || str[0] == '[') && JsonSplit.IsJson(str))
                                {
                                    Fill(MDataRow.CreateFrom(o));
                                }
                                else
                                {
                                    if (o is String) { SetEscape(ref str); }
                                    jsonItems.Add("\"" + str + "\"");
                                }
                            }
                            else if (o is ValueType)
                            {
                                jsonItems.Add(o.ToString());
                            }
                            else
                            {
                                Fill(MDataRow.CreateFrom(o));
                            }
                        }
                    }
                    else if (len == 2)
                    {
                        Fill(MDataRow.CreateFrom(obj));
                    }
                    #endregion
                }
                else if (obj is DataTable)
                {
                    MDataTable dt = obj as DataTable;
                    Fill(dt);
                }
                else if (obj is DataRow)
                {
                    MDataRow row = obj as DataRow;
                    Fill(row);
                }
                else if (obj is DataColumnCollection)
                {
                    MDataColumn mdc = obj as DataColumnCollection;
                    Fill(mdc, true);
                }
                else
                {
                    MDataRow row = new MDataRow();
                    row.LoadFrom(obj);
                    Fill(row);
                }
            }
        }

        public void Fill(string query)
        {
            if (!string.IsNullOrEmpty(query))
            {
                query = query.Trim('?');
                string[] items = query.Split('&');
                foreach (string item in items)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        int index = item.IndexOf('=');
                        if (index > -1)
                        {
                            Add(item.Substring(0, index), item.Substring(index + 1, item.Length - index - 1));
                        }
                        else
                        {
                            Add(item, "");
                        }
                    }
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
                    #region 加载数据
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
                                    cell.State = 1;
                                }
                            }
                        }
                    }
                    #endregion
                }
                else if (mdc != null && mdc.Count == 1)
                {
                    string[] items = json.Trim('[', ']').Split(',');
                    foreach (string item in items)
                    {
                        table.NewRow(true).Set(0, item.Trim('"', '\''));
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
        /// Convert json to Entity
        /// <para>将Json转换为实体</para>
        /// </summary>
        /// <typeparam name="T">Type<para>类型</para></typeparam>
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
        ///  Convert json to Entity List
        ///  <para>将Json转换为实体列表</para>
        /// </summary>
        /// <typeparam name="T">Type<para>类型</para></typeparam>
        public static List<T> ToList<T>(string json) where T : class
        {
            return ToMDataTable(json, TableSchema.GetColumns(typeof(T))).ToList<T>();
        }
        /// <summary>
        /// Convert object to json
        /// <para>将一个对象（实体，泛型List，字典Dictionary）转成Json</para>
        /// </summary>
        public static string ToJson(object obj)
        {
            return ToJson(obj, false, RowOp.IgnoreNull);
        }


        public static string ToJson(object obj, bool isConvertNameToLower)
        {
            return ToJson(obj, isConvertNameToLower, RowOp.IgnoreNull);
        }


        /// <param name="op">default value is RowOp.All
        /// <para>默认值为RowOp.All</para></param>
        public static string ToJson(object obj, bool isConvertNameToLower, RowOp op)
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
            JsonHelper js = new JsonHelper();
            js.IsConvertNameToLower = isConvertNameToLower;
            js.RowOp = op;
            js.Fill(obj);
            return js.ToString(obj is IList);
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
        /// Convert json to Xml
        /// <para>将一个Json转成Xml</para>
        /// </summary>
        public static string ToXml(string json)
        {
            return ToXml(json, true);
        }


        /// <param name="isWithAttr">default value is true
        /// <para>是否转成属性，默认true</para></param>
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
