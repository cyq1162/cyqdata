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

namespace CYQ.Data.Json
{
    /// <summary>
    /// Json class for you easy to operate json
    /// <para>功能全面的json帮助类</para>
    /// </summary>
    public partial class JsonHelper
    {

        //internal static EscapeOp DefaultEscape
        //{
        //    get
        //    {
        //        return (EscapeOp)Enum.Parse(typeof(EscapeOp), AppConfig.Json.Escape);
        //    }
        //}

        #region 实例属性

        private JsonOp _jsonOp;
        public JsonOp JsonOp
        {
            get
            {
                if (_jsonOp == null) { _jsonOp = new JsonOp() { }; }
                return _jsonOp;
            }
            set
            {
                _jsonOp = value;
            }
        }

        public JsonHelper()
        {
            OnInit(false, false, null);
        }
        public JsonHelper(JsonOp jsonOp)
        {
            OnInit(false, false, jsonOp);
        }
        /// <param name="addHead">with easyui header ?<para>是否带输出头</para></param>
        public JsonHelper(bool addHead)
        {
            OnInit(addHead, false, null);
        }

        /// <param name="addSchema">first row with table schema ?
        /// <para>是否首行带表结构[MDataTable.LoadFromJson可以还原表的数据类型]</para></param>
        public JsonHelper(bool addHead, bool addSchema)
        {
            OnInit(addHead, addSchema, null);
        }
        public JsonHelper(bool addHead, bool addSchema, JsonOp jsonOp)
        {
            OnInit(addHead, addSchema, jsonOp);
        }

        private void OnInit(bool addHead, bool addSchema, JsonOp jsonOp)
        {
            _AddHead = addHead;
            _AddSchema = addSchema;
            if (JsonOp != null)
            {
                this.JsonOp = jsonOp;
            }
            this.JsonOp.Level++;
        }


        #region 属性
        private const string brFlag = "[#<br>]";

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

        private List<string> bodyItems = new List<string>(64);
        private StringBuilder headText = new StringBuilder();
        private StringBuilder footText = new StringBuilder();

        /// <summary>
        /// flag a json is end and start a new json
        /// <para> 添加完一个Json数据后调用此方法换行</para>
        /// </summary>
        public void AddBr()
        {
            bodyItems.Add(brFlag);
            rowCount++;
        }
        ///// <summary>
        ///// 调用后将替换默认的Head (AddHead must be true)
        ///// <para>添加底部数据（只有AddHead为true情况才能添加数据）</para>
        ///// </summary>
        //public void AddHead(string name, string value)
        //{
        //    AddHead(name, value, false);
        //}

        //public void AddHead(string name, string value, bool noQuotes)
        //{
        //    if (_AddHead)
        //    {
        //        headText.Append(",");
        //        headText.Append(Format(name, value, noQuotes));
        //    }
        //}

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
                footText.Append(",");
                if (noQuotes)
                {
                    string item = "\"" + name + "\":" + value;
                    footText.Append(item);
                }
                else
                {
                    string item = "\"" + name + "\":\"" + value + "\"";
                    footText.Append(item);
                }
            }
        }

        /// <summary>
        /// add json key value
        /// <para>添加一个字段的值</para>
        /// </summary>
        public void Add(string name, string value)
        {
            Add(name, value, false);
        }

        /// <param name="noQuotes">value is no quotes
        /// <para>值不带引号</para></param>
        public void Add(string name, string value, bool noQuotes)
        {
            bodyItems.Add(Format(name, value, noQuotes));
        }
        public void Add(string name, int value)
        {
            string item = "\"" + name + "\":" + value;
            bodyItems.Add(item);
        }
        public void Add(string name, long value)
        {
            string item = "\"" + name + "\":" + value;
            bodyItems.Add(item);
        }
        public void Add(string name, bool value)
        {
            string item = "\"" + name + "\":" + (value ? "true" : "false");
            bodyItems.Add(item);
        }
        public void Add(string name, DateTime value)
        {
            string item = "\"" + name + "\":\"" + value.ToString(this.JsonOp.DateTimeFormatter) + "\"";
            bodyItems.Add(item);
        }
        public void Add(string name, Guid value)
        {
            string item = "\"" + name + "\":\"" + value + "\"";
            bodyItems.Add(item);
        }
        internal void Add(string name, ValueType value)
        {
            if (value is bool) { Add(name, (bool)value); return; }
            if (value is DateTime) { Add(name, (DateTime)value); return; }
            if (value is Guid) { Add(name, (Guid)value); return; }
            if (value is Enum) { AddEnum(name, value); return; }

            string item = "\"" + name + "\":" + value;
            bodyItems.Add(item);
        }
        public void Add(string name, object value)
        {
            if (value != null)
            {
                if (value is ValueType)
                {
                    Add(name, (ValueType)value);
                }
                else if (value is string)
                {
                    Add(name, (string)value);
                }
                else
                {
                    Add(name, ToJson(value, this.JsonOp.Clone()), true);
                }
            }
            else
            {
                Add(name, "null", true);
            }
        }

        #region AddValueType
        private void AddEnum(string name, object value)
        {
            bool descriptionNoValue = true;
            if (this.JsonOp.IsConvertEnumToDescription)
            {
                FieldInfo field = value.GetType().GetField(value.ToString());
                if (field != null)
                {
                    DescriptionAttribute da = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (da != null)
                    {
                        Add(name, da.Description, false);
                        descriptionNoValue = false;
                    }
                }

            }
            if (descriptionNoValue)
            {
                if (this.JsonOp.IsConvertEnumToString)
                {
                    Add(name, value.ToString(), false);
                }
                else
                {
                    Add(name, (int)value);
                }
            }
        }
        #endregion

        private string Format(string name, string value, bool isChild)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (isChild)
                {
                    return "\"" + name + "\":null";
                }
                return "\"" + name + "\":\"\"";
            }

            if (!isChild && value.Length > 1)
            {
                //智能检测一下：
                bool isCheck = (value[0] == '{' && value[value.Length - 1] == '}') || (value[0] == '[' && value[value.Length - 1] == ']');
                if (isCheck)
                {
                    isChild = IsJson(value);
                }
            }
            if (!isChild && JsonOp.EscapeOp != EscapeOp.No)
            {
                SetEscape(ref value);
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("\"");
            sb.Append(name);
            sb.Append("\":");
            sb.Append(!isChild ? "\"" : "");
            sb.Append(value);
            sb.Append(!isChild ? "\"" : "");
            return sb.ToString();
            //return "\"" + name + "\":" + (!children ? "\"" : "") + value + (!children ? "\"" : "");//;//.Replace("\",\"", "\" , \"").Replace("}{", "} {").Replace("},{", "}, {")
        }

        /// <summary>
        /// out json result
        /// <para>输出Json字符串</para>
        /// </summary>
        public override string ToString()
        {
            //int capacity = 100;
            //if (bodyItems.Count > 0)
            //{
            //    capacity = bodyItems.Count * bodyItems[0].Length;
            //}
            StringBuilder sb = new StringBuilder();//

            if (_AddHead)
            {
                if (headText.Length == 0)
                {
                    sb.Append("{");
                    sb.Append("\"rowcount\":");
                    sb.Append(rowCount);
                    sb.Append(",");

                    sb.Append("\"total\":");
                    sb.Append(Total);
                    sb.Append(",");

                    sb.Append("\"errormsg\":\"");
                    sb.Append(errorMsg);
                    sb.Append("\",");

                    sb.Append("\"success\":");
                    sb.Append(Success.ToString().ToLower());
                    sb.Append(",");
                    sb.Append("\"rows\":");
                }
                else
                {
                    sb.Append(headText.ToString());
                }
            }
            if (bodyItems.Count == 0)
            {
                if (_AddHead)
                {
                    sb.Append("[]");
                }
            }
            else
            {
                if (bodyItems[bodyItems.Count - 1] != brFlag)
                {
                    AddBr();
                }
                if (_AddHead || (isArrayEnd && rowCount > 1))
                {
                    sb.Append("[");
                }
                char left = '{', right = '}';
                string[] items = bodyItems[0].Split(':');
                if (bodyItems[0] != brFlag && (items.Length == 1 || !bodyItems[0].Trim('"').Contains("\"")))
                {
                    //说明为数组
                    left = '[';
                    right = ']';
                }
                sb.Append(left);
                int index = 0;
                foreach (string val in bodyItems)
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
                        sb.Append(right);
                        sb.Append(",");
                        if (index < bodyItems.Count)
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
                if (_AddHead || (isArrayEnd && rowCount > 1))
                {
                    sb.Append("]");
                }
            }
            if (_AddHead)
            {
                sb.Append(footText.ToString());
                sb.Append("}");
            }
            return sb.ToString();
        }

        internal bool isArrayEnd = true;
        /// <param name="arrayEnd">end with [] ?</param>
        /// <returns></returns>
        public string ToString(bool arrayEnd)
        {
            string result = ToString();
            if (arrayEnd && !result.StartsWith("["))
            {
                result = '[' + result + ']';
            }
            else if (result.Length == 0)
            {
                return "{}";
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
            return JsonSplit.IsJson(json, true);
        }


        /// <param name="errIndex">the index of the error char
        /// <para>错误的字符索引</para></param>
        public static bool IsJson(string json, out int errIndex)
        {
            return JsonSplit.IsJson(json, true, out errIndex);
        }
        public static T GetValue<T>(string json, string key)
        {
            string v = GetValue(json, key);
            if (v == null) { return default(T); }
            return ConvertTool.ChangeType<T>(v);
        }
        public static T GetValue<T>(string json, string key, EscapeOp op)
        {
            string v = GetValue(json, key, op);
            return ConvertTool.ChangeType<T>(v);
        }
        public static string GetValue(string json, string key)
        {
            return GetValue(json, key, EscapeOp.No);
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
            if (value == null) { return null; }
            return UnEscape(value, op);
        }
        /// <summary>
        /// Json 返回值是否为true（包含"success":true）
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static bool IsSuccess(string json)
        {
            return GetValue<bool>(json, "success");
        }
        private static string GetSourceValue(string json, string key)
        {
            string result = null;
            if (!string.IsNullOrEmpty(json))
            {
                Dictionary<string, string> jsonDic = Split(json);//先取top1
                if (jsonDic != null && jsonDic.Count > 0 && jsonDic.ContainsKey(key))
                {
                    return jsonDic[key];
                }

                string[] items = key.Split('.');
                string fKey = items[0];
                int i = -1;
                int fi = key.IndexOf('.');
                if (int.TryParse(fKey, out i))//数字
                {
                    List<Dictionary<string, string>> jsonList = JsonSplit.Split(json, i + 1, EscapeOp.No);
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
                    if (jsonDic != null && jsonDic.Count > 0)
                    {
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
            js.Add("success", result ? "true" : "false", true);
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
            return Split(json, EscapeOp.No);
        }
        /// <summary>
        ///  split json to dicationary
        /// <para>将Json分隔成键值对。</para>
        /// </summary>
        public static Dictionary<string, string> Split(string json, EscapeOp op)

        {
            if (!string.IsNullOrEmpty(json))
            {
                json = json.Trim();
                if (json[0] != '{' && json[0] != '[')
                {
                    json = ToJson(json);
                }
                List<Dictionary<string, string>> result = JsonSplit.Split(json, 1, op);
                if (result != null && result.Count > 0)
                {
                    return result[0];
                }
            }
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        public static List<Dictionary<string, string>> SplitArray(string jsonArray)
        {
            return SplitArray(jsonArray, EscapeOp.No);
        }
        /// <summary>
        ///  split json to dicationary array
        /// <para>将Json 数组分隔成多个键值对。</para>
        /// </summary>
        public static List<Dictionary<string, string>> SplitArray(string jsonArray, EscapeOp op)
        {
            if (string.IsNullOrEmpty(jsonArray))
            {
                return null;
            }
            return JsonSplit.Split(jsonArray, 0, op);
        }
        private void SetEscape(ref string value)
        {
            var escapeOp = JsonOp.EscapeOp;
            if (escapeOp == EscapeOp.No) { return; }
            bool isInsert = false;
            int len = value.Length;
            StringBuilder sb = new StringBuilder(len + 10);
            for (int i = 0; i < len; i++)
            {
                char c = value[i];
                if (escapeOp == EscapeOp.Encode)
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
                            if (escapeOp == EscapeOp.Yes)
                            {
                                sb.Append("\\t");//直接替换追加
                            }
                            break;
                        case '\r':
                            if (escapeOp == EscapeOp.Yes)
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
                        bool isOK = escapeOp == EscapeOp.Yes;// || (i != 0 && i == len - 1 && value[i - 1] != '\\');// 如果是以\结尾,（非\\结尾时, 这部分是强制转，不会然影响整体格式）
                        if (!isOK && escapeOp == EscapeOp.Default && len > 1 && i < len - 1)//中间
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
            if (op == EscapeOp.No || string.IsNullOrEmpty(result)) { return result; }
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
                bool has = result.IndexOf("\\\\") > -1;
                if (has)
                {
                    result = result.Replace("\\\\", "#&#=#");
                }
                if (op == EscapeOp.Yes)
                {
                    result = result.Replace("\\t", "\t").Replace("\\r", "\r");
                }
                result = result.Replace("\\\"", "\"").Replace("\\n", "\n");//.Replace("\\\\","\\");
                if (has)
                {
                    result = result.Replace("#&#=#", "\\");
                }
            }
            return result;
        }
    }
}
