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
using System.Linq;

namespace CYQ.Data.Tool
{
    /// <summary>
    /// Escape json char options
    /// <para>JsonHelper 的符号转义选项</para>
    /// </summary>
    public enum EscapeOp
    {
        /// <summary>
        /// 过滤ascii小于32的特殊值、并对\n "（双引号）进行转义，对\转义符 （仅\\"或\\n时不转义，其它情况转义）
        /// </summary>
        Default,
        /// <summary>
        ///  不进行任何转义，只用于保留原如数据（注意：存在双引号时，[或ascii小于32的值都会破坏json格式]，从而json数据无法被解析）
        /// </summary>
        No,
        /// <summary>
        ///  过滤ascii小于32的特殊值、并对 ：\r \n \t "（双引号） \(转义符号) 直接进行转义
        /// </summary>
        Yes,
        /// <summary>
        /// 系统内部使用： ascii小于32（包括\n \t \r）、"(双引号)，\(转义符号) 进行编码（规则为：@#{0}#@ {0}为asciii值，系统转的时候会自动解码）
        /// </summary>
        Encode
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
        private const string brFlag = "[#<br>]";
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
        //internal BreakOp BreakOp
        //{
        //    get
        //    {
        //        switch (_RowOp)
        //        {
        //            case Table.RowOp.IgnoreNull:
        //                return Table.BreakOp.Null;
        //            default:
        //                return Table.BreakOp.None;
        //        }
        //    }
        //}
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
        StringBuilder headText = new StringBuilder();
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
        public void Add(string name, object value)
        {
            if (value != null)
            {
                string v = null;
                Type t = value.GetType();
                if (t.IsEnum)
                {
                    value = (int)value;
                    Add(name, value.ToString(), true);
                }
                else
                {
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
                if (headText.Length == 0)
                {
                    sb.Append("{");
                    sb.Append("\"rowcount\":" + rowCount + ",");
                    sb.Append("\"total\":" + Total + ",");
                    sb.Append("\"errorMsg\":\"" + errorMsg + "\",");
                    sb.Append("\"success\":" + Success.ToString().ToLower() + ",");
                    sb.Append("\"rows\":");
                }
                else
                {
                    sb.Append(headText.ToString());
                }
            }
            if (jsonItems.Count == 0)
            {
                if (_AddHead)
                {
                    sb.Append("[]");
                }
            }
            else
            {
                if (jsonItems[jsonItems.Count - 1] != brFlag)
                {
                    AddBr();
                }
                if (_AddHead || rowCount > 1)
                {
                    sb.Append("[");
                }
                char left = '{', right = '}';
                if (jsonItems[0] != brFlag && !jsonItems[0].Contains(":"))
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
                        if (sb[sb.Length - 1] == ',')
                        {
                            sb.Remove(sb.Length - 1, 1);//性能优化（内部时，必须多了一个“，”号）。
                            // sb = sb.Replace(",", "", sb.Length - 1, 1);
                        }
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
        public static string GetValue(string json, string key)
        {
            return GetValue(json, key, DefaultEscape);
        }
        /// <summary>
        /// Get json value
        /// <para>获取Json字符串的值</para>
        /// </summary>
        /// <param name="key">the name or key of json
        /// <para>键值(有层级时用：XXX.YYY.ZZZ)</para></param>
        /// <returns></returns>
        public static string GetValue(string json, string key, EscapeOp op)
        {
            string value = GetSourceValue(json, key);
            return UnEscape(value, op);
        }

        private static string GetSourceValue(string json, string key)
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
                                return GetSourceValue(ToJson(numJson), key.Substring(fi + 1));
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
                        else if (jsonDic.ContainsKey(fKey)) // 取子集
                        {

                            return GetSourceValue(jsonDic[fKey], key.Substring(fi + 1));
                        }
                    }
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
            return OutResult(result, msgObj, null, null);
        }
        public static string OutResult(bool result, object msgObj, string name, object value, params object[] nameValues)
        {

            //JsonHelper js = new JsonHelper(false, false);
            //js.Add("success", result.ToString().ToLower(), true);
            //if (msgObj is string)
            //{
            //    js.Add("msg", Convert.ToString(msgObj));
            //}
            //else
            //{
            //    js.Add("msg", ToJson(msgObj), true);
            //}
            int num = name == null ? 2 : 4;
            object[] nvs = new object[nameValues.Length + num];
            nvs[0] = "msg";
            nvs[1] = msgObj;
            if (num == 4)
            {
                nvs[2] = name;
                nvs[3] = value;
            }
            if (nameValues.Length > 0)
            {
                nameValues.CopyTo(nvs, num);
            }
            return OutResult("success", result, nvs);

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
            if (!string.IsNullOrEmpty(json))
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
            }
            return null;
        }
        //public static List<Dictionary<string, string>> SplitArray(string jsonArray)
        //{
        //    return SplitArray(jsonArray, DefaultEscape);
        //}
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
        private void SetEscape(ref string value)
        {
            if (Escape == EscapeOp.No) { return; }
            bool isInsert = false;
            int len = value.Length;
            StringBuilder sb = new StringBuilder(len + 10);
            for (int i = 0; i < len; i++)
            {
                char c = value[i];
                if (Escape == EscapeOp.Encode)
                {
                    if (c < 32 || c == '"' || c == '\\')
                    {
                        sb.AppendFormat("@#{0}#@", (int)c);
                        isInsert = true;
                    }
                    else { sb.Append(c); }
                    continue;
                }

                if (c < 32)
                {

                    #region 十六进制符号处理
                    switch (c)
                    {
                        case '\n':
                            sb.Append("\\n");//直接替换追加。
                            break;
                        case '\t':
                            if (Escape == EscapeOp.Yes)
                            {
                                sb.Append("\\t");//直接替换追加
                            }
                            break;
                        case '\r':
                            if (Escape == EscapeOp.Yes)
                            {
                                sb.Append("\\r");//直接替换追加
                            }
                            break;
                        default:
                            break;
                    }
                    #endregion
                    // '\n'=10  '\r'=13 '\t'=9 都会被过滤。
                    isInsert = true;
                    continue;
                }
                #region 双引号和转义符号处理
                switch (c)
                {
                    case '"':
                        isInsert = true;
                        sb.Append("\\");
                        //if (i == 0 || value[i - 1] != '\\')//这个是强制转，不然整体格式有问题。
                        //{
                        //    isInsert = true;
                        //    sb.Append("\\");
                        //}
                        break;
                    case '\\':
                        //if (i < len - 1)
                        //{
                        //    switch (value[i + 1])
                        //    {
                        //      //  case '"':
                        //        case 'n':
                        //            case 'r'://r和t要转义，不过出事。
                        //            case 't':
                        //                isInsert = true;
                        //                sb.Append("\\");
                        //            break;
                        //        default:
                        //            //isOK = true;
                        //            break;
                        //    }
                        //}
                        bool isOK = Escape == EscapeOp.Yes;// || (i != 0 && i == len - 1 && value[i - 1] != '\\');// 如果是以\结尾,（非\\结尾时, 这部分是强制转，不会然影响整体格式）
                        if (!isOK && Escape == EscapeOp.Default && len > 1 && i < len - 1)//中间
                        {
                            switch (value[i + 1])
                            {
                                // case '"':
                                case 'n':
                                    //case 'r'://r和t要转义，不过出事。
                                    //case 't':
                                    break;
                                default:
                                    isOK = true;
                                    break;
                            }
                        }
                        if (isOK)
                        {
                            isInsert = true;
                            sb.Append("\\");
                        }
                        break;
                }
                #endregion
                sb.Append(c);
            }

            if (isInsert)
            {
                value = null;
                value = sb.ToString();
            }
            else { sb = null; }
        }
        /// <summary>
        /// 解码替换数据转义符
        /// </summary>
        public static string UnEscape(string result, EscapeOp op)
        {
            if (op == EscapeOp.No) { return result; }
            if (op == EscapeOp.Encode)
            {
                if (result.IndexOf("@#") > -1 && result.IndexOf("#@") > -1) // 先解系统编码
                {
                    MatchCollection matchs = Regex.Matches(result, @"@#(\d{1,2})#@", RegexOptions.Compiled);
                    if (matchs != null && matchs.Count > 0)
                    {
                        List<string> keys = new List<string>(matchs.Count);
                        foreach (Match match in matchs)
                        {
                            if (match.Groups.Count > 1)
                            {
                                int code = int.Parse(match.Groups[1].Value);
                                string charText = ((char)code).ToString();
                                result = result.Replace(match.Groups[0].Value, charText);
                            }
                        }
                    }
                }
                return result;
            }
            if (result.IndexOf("\\") > -1)
            {
                result = result.Replace("\\\\", "#&#=#");
                if (op == EscapeOp.Yes)
                {
                    result = result.Replace("\\t", "\t").Replace("\\r", "\r");
                }
                result = result.Replace("\\\"", "\"").Replace("\\n", "\n");//.Replace("\\\\","\\");
                result = result.Replace("#&#=#", "\\");
            }
            return result;
        }
    }

    // 扩展交互部分
    public partial class JsonHelper
    {
        /// <summary>
        /// 用于控制自循环的层级判断。
        /// </summary>
        internal int Level = 1;
        /// <summary>
        /// 用于自循环检测列表。
        /// </summary>
        internal MDictionary<int, int> LoopCheckList = new MDictionary<int, int>();
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
                if (cell.IsJsonIgnore)
                {
                    continue;
                }
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
                            int hash = cell.Value.GetHashCode();
                            //检测是否循环引用
                            if (LoopCheckList.ContainsKey(hash))
                            {
                                //continue;
                                int level = LoopCheckList[hash];
                                if (level < Level)
                                {
                                    continue;
                                }
                                else
                                {
                                    LoopCheckList[hash] = Level;//更新级别
                                }
                            }
                            else
                            {
                                LoopCheckList.Add(hash, Level);
                            }
                            Type t = cell.Struct.ValueType;
                            if (t.FullName == "System.Object")
                            {
                                t = cell.Value.GetType();
                            }
                            if (t.Name == "Byte[]")
                            {
                                value = Convert.ToBase64String(cell.Value as byte[]);
                            }
                            else if (t.Name == "String")
                            {
                                value = cell.StringValue;
                            }
                            else
                            {
                                if (cell.Value is IEnumerable)
                                {
                                    int len = ReflectTool.GetArgumentLength(ref t);
                                    if (len <= 1)//List<T>
                                    {
                                        JsonHelper js = new JsonHelper(false, false);
                                        js.Level = Level + 1;
                                        js.LoopCheckList = LoopCheckList;
                                        js.Escape = Escape;
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
                                    else if (len == 2)//Dictionary<T,K>
                                    {
                                        MDataRow dicRow = MDataRow.CreateFrom(cell.Value);
                                        dicRow.DynamicData = LoopCheckList;
                                        value = dicRow.ToJson(RowOp, IsConvertNameToLower, Escape);
                                        noQuot = true;
                                    }
                                }
                                else
                                {
                                    if (!t.FullName.StartsWith("System."))//普通对象。
                                    {
                                        MDataRow oRow = new MDataRow(TableSchema.GetColumnByType(t));
                                        oRow.DynamicData = LoopCheckList;
                                        oRow.LoadFrom(cell.Value);
                                        value = oRow.ToJson(RowOp, IsConvertNameToLower, Escape);
                                        noQuot = true;
                                    }
                                    else if (t.FullName == "System.Data.DataTable")
                                    {
                                        MDataTable dt = cell.Value as DataTable;
                                        dt.DynamicData = LoopCheckList;
                                        value = dt.ToJson(false, false, RowOp, IsConvertNameToLower, Escape);
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
                if (!string.IsNullOrEmpty(column.TableName))
                {
                    _AddHead = true;
                    headText.Append("{");
                    headText.Append("\"TableName\":\"" + column.TableName + "\",");
                    headText.Append("\"Description\":\"" + column.Description + "\",");
                    headText.Append("\"Columns\":");
                }
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
                    int len = ReflectTool.GetArgumentLength(ref t);
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
        internal static MDataTable ToMDataTable(string jsonOrFileName, MDataColumn mdc, EscapeOp op)
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
        private static T ToIEnumerator<T>(string json, EscapeOp op)
            where T : class
        {
            Type t = typeof(T);
            if (t.FullName.StartsWith("System.Collections."))
            {
                Type[] ts;
                int argLength = ReflectTool.GetArgumentLength(ref t, out ts);
                #region Dictionary
                if (t.FullName.Contains("Dictionary") && argLength == 2 && ts[0].Name == "String" && ts[1].Name == "String")
                {
                    Dictionary<string, string> dic = Split(json);
                    return dic as T;
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
                    if (argLength == 1)
                    {
                        List<Dictionary<string, string>> items = JsonSplit.Split(json);
                        if (items != null && items.Count > 0)
                        {
                            foreach (Dictionary<string, string> item in items)
                            {
                                mi.Invoke(objT, new object[] { MDataRow.CreateFrom(item).ToEntity(ts[0]) });
                            }
                        }
                    }
                    else if (argLength == 2)
                    {
                        Dictionary<string, string> dic = Split(json);
                        if (dic != null && dic.Count > 0)
                        {
                            foreach (KeyValuePair<string, string> kv in dic)
                            {
                                mi.Invoke(objT, new object[] { Convert.ChangeType(kv.Key, ts[0]), Convert.ChangeType(UnEscape(kv.Value, op), ts[1]) });
                            }
                        }
                    }
                    return objT;
                }
                #endregion

            }
            else if (t.FullName.EndsWith("[]"))
            {
                Object o = new MDataRow().GetObj(t, json);
                if (o != null)
                {
                    return (T)o;
                }
            }
            return default(T);
        }
        public static T ToEntity<T>(string json) where T : class
        {
            return ToEntity<T>(json, DefaultEscape);
        }
        /// <summary>
        /// Convert json to Entity
        /// <para>将Json转换为实体</para>
        /// </summary>
        /// <typeparam name="T">Type<para>类型</para></typeparam>
        public static T ToEntity<T>(string json, EscapeOp op) where T : class
        {
            Type t = typeof(T);
            if (t.FullName.StartsWith("System.Collections.") || t.FullName.EndsWith("[]"))
            {
                return ToIEnumerator<T>(json, op);
            }
            else
            {
                MDataRow row = new MDataRow(TableSchema.GetColumnByType(t));
                row.LoadFrom(json, op);
                return row.ToEntity<T>();
            }
        }
        public static List<T> ToList<T>(string json) where T : class
        {
            return ToList<T>(json, DefaultEscape);
        }
        /// <summary>
        ///  Convert json to Entity List
        ///  <para>将Json转换为实体列表</para>
        /// </summary>
        /// <typeparam name="T">Type<para>类型</para></typeparam>
        public static List<T> ToList<T>(string json, EscapeOp op) where T : class
        {
            return ToMDataTable(json, TableSchema.GetColumnByType(typeof(T)), op).ToList<T>();
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
        public static string ToJson(object obj, bool isConvertNameToLower, RowOp rowOp)
        {
            return ToJson(obj, isConvertNameToLower, rowOp, DefaultEscape);
        }

        /// <param name="op">default value is RowOp.All
        /// <para>默认值为RowOp.All</para></param>
        public static string ToJson(object obj, bool isConvertNameToLower, RowOp rowOp, EscapeOp escapeOp)
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

            JsonHelper js = new JsonHelper();
            js.LoopCheckList.Add(obj.GetHashCode(), 0);
            js.Escape = escapeOp;
            js.IsConvertNameToLower = isConvertNameToLower;
            js.RowOp = rowOp;
            js.Fill(obj);
            return js.ToString(obj is IList || obj is MDataTable || obj is DataTable);
        }



        #region Xml 转 Json

        /// <summary>
        /// 转Json
        /// <param name="xml">xml字符串</param>
        /// <param name="isConvertNameToLower">字段是否转小写</param>
        /// <param name="isWithAttr">是否将属性值也输出</param>
        /// </summary>
        private static string XmlToJson(string xml, bool isWithAttr)
        {
            using (XHtmlAction action = new XHtmlAction(false, true))
            {
                try
                {
                    action.LoadXml(xml);
                    return action.ToJson(action.XmlDoc.DocumentElement, isWithAttr);
                }
                catch (Exception err)
                {
                    Log.WriteLogToTxt(err, LogType.Error);
                    return string.Empty;
                }

            }
        }

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

        public static string ToXml(string json, bool isWithAttr)
        {
            return ToXml(json, isWithAttr, DefaultEscape);
        }
        public static string ToXml(string json, bool isWithAttr, EscapeOp op)
        {
            return ToXml(json, isWithAttr, op, null);
        }
        /// <param name="isWithAttr">default value is true
        /// <para>是否转成属性，默认true</para></param>
        public static string ToXml(string json, bool isWithAttr, EscapeOp op, string rootName)
        {
            if (!string.IsNullOrEmpty(rootName))
            {
                json = string.Format("{{\"{0}\":{1}}}", rootName, json);
            }
            StringBuilder xml = new StringBuilder();
            xml.Append("<?xml version=\"1.0\"  standalone=\"yes\"?>");
            List<Dictionary<string, string>> dicList = JsonSplit.Split(json);
            if (dicList != null && dicList.Count > 0)
            {
                bool addRoot = dicList.Count > 1 || dicList[0].Count > 1;
                if (addRoot)
                {
                    xml.Append(string.Format("<{0}>", rootName ?? "root"));//</root>";
                }

                xml.Append(GetXmlList(dicList, isWithAttr, op));

                if (addRoot)
                {
                    xml.Append(string.Format("</{0}>", rootName ?? "root"));//</root>";
                }

            }
            return xml.ToString();
        }

        private static string GetXmlList(List<Dictionary<string, string>> dicList, bool isWithAttr, EscapeOp op)
        {
            if (dicList == null || dicList.Count == 0)
            {
                return string.Empty;
            }
            StringBuilder xml = new StringBuilder();
            for (int i = 0; i < dicList.Count; i++)
            {
                xml.Append(GetXml(dicList[i], isWithAttr, op));
            }
            return xml.ToString();
        }

        private static string GetXml(Dictionary<string, string> dic, bool isWithAttr, EscapeOp op)
        {
            StringBuilder xml = new StringBuilder();
            bool isJson = false;
            foreach (KeyValuePair<string, string> item in dic)
            {
                isJson = IsJson(item.Value);
                if (!isJson)
                {
                    xml.AppendFormat("<{0}>{1}</{0}>", item.Key, FormatCDATA(UnEscape(item.Value, op)));
                }
                else
                {
                    string key = item.Key;
                    if (key.EndsWith("List") && isWithAttr)
                    {
                        xml.AppendFormat("<{0}>", key);
                        key = key.Substring(0, key.Length - 4);
                    }
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
                                xml.Append(GetXmlElement(key, jsonList[j], op));
                            }
                            else
                            {
                                xml.Append(GetXml(jsonList[j], isWithAttr, op));
                            }
                        }
                        if (!isWithAttr)
                        {
                            xml.AppendFormat("</{0}>", key);
                        }
                    }
                    else // 空Json {}
                    {
                        xml.AppendFormat("<{0}></{0}>", key);
                    }

                    if (item.Key.EndsWith("List") && isWithAttr)
                    {
                        xml.AppendFormat("</{0}>", item.Key);
                    }
                }
            }
            return xml.ToString();
        }

        private static string GetXmlElement(string parentName, Dictionary<string, string> dic, EscapeOp op)
        {
            StringBuilder xml = new StringBuilder();
            Dictionary<string, string> jsonDic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            xml.Append("<" + parentName);
            foreach (KeyValuePair<string, string> kv in dic)
            {
                if (kv.Value.IndexOf('"') > -1 || kv.Value.Length > 50
                    || kv.Key.Contains("Remark") || kv.Key.Contains("Description") || kv.Key.Contains("Rule")
                    || IsJson(kv.Value)) // 属性不能带双引号，所以转到元素处理。
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
                xml.Append(FormatCDATA(UnEscape(dic[parentName], op)));//InnerText。
            }
            else if (jsonDic.Count > 0)
            {
                xml.Append(GetXml(jsonDic, true, op));//数组，当元素处理。
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

    public partial class JsonHelper
    {
        /// <summary>
        /// 读取文本中的Json（并去掉注释）
        /// </summary>
        /// <returns></returns>
        internal static string ReadJson(string filePath)
        {
            string json = string.Empty;
            if (System.IO.File.Exists(filePath))
            {
                #region Read from path
                json = IOHelper.ReadAllText(filePath);
                if (!string.IsNullOrEmpty(json))
                {
                    int index = json.LastIndexOf("/*");
                    if (index > -1)//去掉注释
                    {
                        json = Regex.Replace(json, @"/\*[.\s\S]*?\*/", string.Empty, RegexOptions.IgnoreCase);
                    }
                    char splitChar = '\n';
                    if (json.IndexOf(splitChar) > -1)
                    {
                        string[] items = json.Split(splitChar);
                        StringBuilder sb = new StringBuilder();
                        foreach (string item in items)
                        {
                            if (!item.TrimStart(' ', '\r', '\t').StartsWith("//"))
                            {
                                sb.Append(item.Trim(' ', '\r', '\t'));
                            }
                        }
                        json = sb.ToString();
                    }
                    if (json.IndexOf("\\\\") > -1)
                    {
                        json = json.Replace("\\\\", "\\");
                    }
                }
                #endregion
            }
            return json;
        }
    }
}
