using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Tool
{
    /// <summary>
    /// 分隔Json字符串为字典集合。
    /// </summary>
    internal partial class JsonSplit
    {
        internal static bool IsJson(string json)
        {
            int errIndex;
            return IsJson(json, out errIndex);
        }
        internal static bool IsJson(string json, out int errIndex)
        {
            errIndex = 0;

            if (string.IsNullOrEmpty(json) || json.Length < 2 ||
                ((json[0] != '{' && json[json.Length - 1] != '}') && (json[0] != '[' && json[json.Length - 1] != ']')))
            {
                return false;
            }
            CharState cs = new CharState();
            char c;
            for (int i = 0; i < json.Length; i++)
            {
                c = json[i];
                if (SetCharState(c, ref cs) && cs.childrenStart)//设置关键符号状态。
                {
                    string item = json.Substring(i);
                    int err;
                    int length = GetValueLength(item, true, out err);
                    cs.childrenStart = false;
                    if (err > 0)
                    {
                        errIndex = i + err;
                        return false;
                    }
                    i = i + length - 1;
                }
                if (cs.isError)
                {
                    errIndex = i;
                    return false;
                }
            }

            return !cs.arrayStart && !cs.jsonStart; //只要不是正常关闭，则失败
        }
        internal static List<Dictionary<string, string>> Split(string json)
        {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

            if (!string.IsNullOrEmpty(json))
            {
                Dictionary<string, string> dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string key = string.Empty;
                StringBuilder value = new StringBuilder();
                CharState cs = new CharState();
                try
                {
                    #region 核心逻辑
                    char c;
                    for (int i = 0; i < json.Length; i++)
                    {
                        c = json[i];
                        if (!SetCharState(c, ref cs))//设置关键符号状态。
                        {
                            if (cs.jsonStart)//Json进行中。。。
                            {
                                if (cs.keyStart > 0)
                                {
                                    key += c;
                                }
                                else if (cs.valueStart > 0)
                                {
                                    value.Append(c);
                                    //value += c;
                                }
                            }
                            else if (!cs.arrayStart)//json结束，又不是数组，则退出。
                            {
                                break;
                            }
                        }
                        else if (cs.childrenStart)//正常字符，值状态下。
                        {
                            string item = json.Substring(i);
                            int temp;
                            int length = GetValueLength(item, false, out temp);
                            //value = item.Substring(0, length);
                            value.Length = 0;
                            value.Append(item.Substring(0, length));
                            cs.childrenStart = false;
                            cs.valueStart = 0;
                            //cs.state = 0;
                            cs.setDicValue = true;
                            i = i + length - 1;
                        }
                        if (cs.setDicValue)//设置键值对。
                        {
                            if (!string.IsNullOrEmpty(key) && !dic.ContainsKey(key))
                            {
                                //if (value != string.Empty)
                                //{
                                bool isNull = json[i - 5] == ':' && json[i] != '"' && value.Length == 4 && value.ToString() == "null";
                                if (isNull)
                                {
                                    value.Length = 0;
                                }
                                dic.Add(key, value.ToString());

                                //}
                            }
                            cs.setDicValue = false;
                            key = string.Empty;
                            value.Length = 0;
                        }

                        if (!cs.jsonStart && dic.Count > 0)
                        {
                            result.Add(dic);
                            if (cs.arrayStart)//处理数组。
                            {
                                dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            }
                        }
                    }
                    #endregion
                }
                catch (Exception err)
                {
                    Log.WriteLogToTxt(err);
                }
                finally
                {
                    key = null;
                    value.Length = 0;
                    value.Capacity = 16;
                    value = null;
                }
            }
            return result;
        }
        /// <summary>
        /// 获取值的长度（当Json值嵌套以"{"或"["开头时）
        /// </summary>
        private static int GetValueLength(string json, bool breakOnErr, out int errIndex)
        {
            errIndex = 0;
            int len = 0;
            if (!string.IsNullOrEmpty(json))
            {
                CharState cs = new CharState();
                char c;
                for (int i = 0; i < json.Length; i++)
                {
                    c = json[i];
                    if (!SetCharState(c, ref cs))//设置关键符号状态。
                    {
                        if (!cs.jsonStart && !cs.arrayStart)//json结束，又不是数组，则退出。
                        {
                            break;
                        }
                    }
                    else if (cs.childrenStart)//正常字符，值状态下。
                    {
                        int length = GetValueLength(json.Substring(i), breakOnErr, out errIndex);//递归子值，返回一个长度。。。
                        cs.childrenStart = false;
                        cs.valueStart = 0;
                        //cs.state = 0;
                        i = i + length - 1;
                    }
                    if (breakOnErr && cs.isError)
                    {
                        errIndex = i;
                        return i;
                    }
                    if (!cs.jsonStart && !cs.arrayStart)//记录当前结束位置。
                    {
                        len = i + 1;//长度比索引+1
                        break;
                    }
                }
            }
            return len;
        }
        /// <summary>
        /// 字符状态
        /// </summary>
        private class CharState
        {
            internal bool jsonStart = false;//以 "{"开始了...
            internal bool setDicValue = false;// 可以设置字典值了。
            internal bool escapeChar = false;//以"\"转义符号开始了
            /// <summary>
            /// 数组开始【仅第一开头才算】，值嵌套的以【childrenStart】来标识。
            /// </summary>
            internal bool arrayStart = false;//以"[" 符号开始了
            internal bool childrenStart = false;//子级嵌套开始了。
            /// <summary>
            /// 【-1 未初始化】【0 取名称中】；【1 取值中】
            /// </summary>
            internal int state = -1;

            /// <summary>
            /// 【-2 已结束】【-1 未初始化】【0 未开始】【1 无引号开始】【2 单引号开始】【3 双引号开始】
            /// </summary>
            internal int keyStart = -1;
            /// <summary>
            /// 【-2 已结束】【-1 未初始化】【0 未开始】【1 无引号开始】【2 单引号开始】【3 双引号开始】
            /// </summary>
            internal int valueStart = -1;

            internal bool isError = false;//是否语法错误。

            internal void CheckIsError(char c)//只当成一级处理（因为GetLength会递归到每一个子项处理）
            {
                switch (c)
                {
                    case '{'://[{ "[{A}]":[{"[{B}]":3,"m":"C"}]}]
                        isError = jsonStart && state == 0;//重复开始错误 同时不是值处理。
                        break;
                    case '}':
                        isError = !jsonStart || (keyStart > 0 && state == 0);//重复结束错误 或者 提前结束。
                        break;
                    case '[':
                        isError = arrayStart && state == 0;//重复开始错误
                        break;
                    case ']':
                        isError = !arrayStart || (state == 1 && valueStart == 0);//重复开始错误[{},]1,0  正常：[111,222] 1,1 [111,"22"] 1,-2 
                        break;
                    case '"':
                        isError = !jsonStart && !arrayStart;//未开始Json，同时也未开始数组。
                        break;
                    case '\'':
                        isError = !jsonStart && !arrayStart;//未开始Json
                        break;
                    case ':':
                        isError = (!jsonStart && !arrayStart) || (jsonStart && keyStart < 2 && valueStart < 2 && state == 1);//未开始Json 同时 只能处理在取值之前。
                        break;
                    case ',':
                        isError = (!jsonStart && !arrayStart)
                            || (!jsonStart && arrayStart && state == -1) //[,111]
                            || (jsonStart && keyStart < 2 && valueStart < 2 && state == 0);//未开始Json 同时 只能处理在取值之后。
                        break;
                    default: //值开头。。
                        isError = (!jsonStart && !arrayStart) || (keyStart == 0 && valueStart == 0 && state == 0);//
                        break;
                }
                //if (isError)
                //{

                //}
            }
        }
        /// <summary>
        /// 设置字符状态(返回true则为关键词，返回false则当为普通字符处理）
        /// </summary>
        private static bool SetCharState(char c, ref CharState cs)
        {
            switch (c)
            {
                case '{'://[{ "[{A}]":[{"[{B}]":3,"m":"C"}]}]
                    #region 大括号
                    if (cs.keyStart <= 0 && cs.valueStart <= 0)
                    {
                        cs.CheckIsError(c);
                        if (cs.jsonStart && cs.state == 1)
                        {
                            cs.valueStart = 0;
                            cs.childrenStart = true;
                        }
                        else
                        {
                            cs.state = 0;
                        }
                        cs.jsonStart = true;//开始。
                        return true;
                    }
                    #endregion
                    break;
                case '}':
                    #region 大括号结束
                    if (cs.keyStart <= 0 && cs.valueStart < 2)
                    {
                        cs.CheckIsError(c);
                        if (cs.jsonStart)
                        {
                            cs.jsonStart = false;//正常结束。
                            cs.valueStart = -1;
                            cs.state = 0;
                            cs.setDicValue = true;
                        }
                        return true;
                    }
                    // cs.isError = !cs.jsonStart && cs.state == 0;
                    #endregion
                    break;
                case '[':
                    #region 中括号开始
                    if (!cs.jsonStart)
                    {
                        cs.CheckIsError(c);
                        cs.arrayStart = true;
                        return true;
                    }
                    else if (cs.jsonStart && cs.state == 1 && cs.valueStart < 2)
                    {
                        cs.CheckIsError(c);
                        //cs.valueStart = 1;
                        cs.childrenStart = true;
                        return true;
                    }
                    #endregion
                    break;
                case ']':
                    #region 中括号结束
                    if (!cs.jsonStart && (cs.keyStart <= 0 && cs.valueStart <= 0) || (cs.keyStart == -1 && cs.valueStart == 1))
                    {
                        cs.CheckIsError(c);
                        if (cs.arrayStart)// && !cs.childrenStart
                        {
                            cs.arrayStart = false;
                        }
                        return true;
                    }
                    #endregion
                    break;
                case '"':
                case '\'':
                    cs.CheckIsError(c);
                    #region 引号
                    if (cs.jsonStart || cs.arrayStart)
                    {
                        if (!cs.jsonStart && cs.arrayStart)
                        {
                            cs.state = 1;//如果是数组，只有取值，没有Key，所以直接跳过0
                        }
                        if (cs.state == 0)//key阶段
                        {
                            cs.keyStart = (cs.keyStart <= 0 ? (c == '"' ? 3 : 2) : -2);
                            return true;
                        }
                        else if (cs.state == 1)//值阶段
                        {
                            if (cs.valueStart <= 0)
                            {
                                cs.valueStart = (c == '"' ? 3 : 2);
                                return true;
                            }
                            else if ((cs.valueStart == 2 && c == '\'') || (cs.valueStart == 3 && c == '"'))
                            {
                                if (!cs.escapeChar)
                                {
                                    cs.valueStart = -2;
                                    return true;
                                }
                                else
                                {
                                    cs.escapeChar = false;
                                }
                            }

                        }
                    }
                    #endregion
                    break;
                case ':':
                    cs.CheckIsError(c);
                    #region 冒号
                    if (cs.jsonStart && cs.keyStart < 2 && cs.valueStart < 2 && cs.state == 0)
                    {
                        cs.keyStart = 0;
                        cs.state = 1;
                        return true;
                    }
                    #endregion
                    break;
                case ',':
                    cs.CheckIsError(c);
                    #region 逗号 {"a": [11,"22", ], "Type": 2}
                    if (cs.jsonStart && cs.keyStart < 2 && cs.valueStart < 2 && cs.state == 1)
                    {
                        cs.state = 0;
                        cs.valueStart = 0;
                        cs.setDicValue = true;
                        return true;
                    }
                    else if (cs.arrayStart && !cs.jsonStart) //[a,b]  [",",33] [{},{}]
                    {
                        if ((cs.state == -1 && cs.valueStart == -1) || (cs.valueStart < 2 && cs.state == 1))
                        {
                            cs.valueStart = 0;
                            return true;
                        }
                    }
                    #endregion
                    break;
                case ' ':
                case '\r':
                case '\n':
                    if (cs.jsonStart && cs.keyStart <= 0 && cs.valueStart <= 0)
                    {
                        return true;//跳过空格。
                    }
                    break;
                default: //值开头。。
                    cs.CheckIsError(c);
                    if (c == '\\') //转义符号
                    {
                        if (cs.escapeChar)
                        {
                            cs.escapeChar = false;
                        }
                        else
                        {
                            cs.escapeChar = true;
                            return true;
                        }
                    }
                    else
                    {
                        cs.escapeChar = false;
                    }
                    if (cs.jsonStart)
                    {
                        if (cs.keyStart <= 0 && cs.state <= 0)
                        {
                            cs.keyStart = 1;//无引号的
                        }
                        else if (cs.valueStart <= 0 && cs.state == 1)
                        {
                            cs.valueStart = 1;//无引号的
                        }
                    }
                    else if (cs.arrayStart)
                    {
                        cs.state = 1;
                        if (cs.valueStart < 1)
                        {
                            cs.valueStart = 1;//无引号的
                        }
                    }
                    break;
            }
            return false;
        }


    }
    internal partial class JsonSplit
    {
        internal static List<string> SplitEscapeArray(string jsonArray)
        {
            if (!string.IsNullOrEmpty(jsonArray))
            {
                jsonArray = jsonArray.Trim(' ', '[', ']');//["a,","bbb,,"]
                if (jsonArray.Length > 0)
                {
                    List<string> list = new List<string>();
                    string[] items = jsonArray.Split(',');
                    string objStr = string.Empty;
                    foreach (string item in items)
                    {
                        if (objStr == string.Empty) { objStr = item; }
                        else { objStr += "," + item; }
                        char firstChar = objStr[0];
                        if (firstChar == '"' || firstChar == '\'')
                        {
                            //检测双引号的数量
                            if (GetCharCount(objStr, firstChar) % 2 == 0)//引号成双
                            {
                                list.Add(objStr.Trim(firstChar).Replace("\\" + firstChar, firstChar.ToString()));
                                objStr = string.Empty;
                            }
                        }
                        else
                        {
                            list.Add(item);
                            objStr = string.Empty;
                        }
                    }
                    return list;
                }


            }
            return null;
        }
        /// <summary>
        /// 获取字符在字符串出现的次数
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static int GetCharCount(string item, char c)
        {
            int num = 0;
            for (int i = 0; i < item.Length; i++)
            {
                if (item[i] == '\\')
                {
                    i++;
                }
                else if (item[i] == c)
                {
                    num++;
                }
            }
            return num;
        }
    }
}
