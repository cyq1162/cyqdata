using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using CYQ.Data.Table;
using CYQ.Data.Tool;
using CYQ.Data.Emit;
using System.Threading;

namespace CYQ.Data.Json
{
    /// <summary>
    /// 分隔Json字符串为字典集合。
    /// </summary>
    internal partial class JsonSplit
    {
        /// <summary>
        /// 解析Json
        /// </summary>
        /// <returns></returns>
        internal static List<Dictionary<string, string>> Split(string json, int topN, EscapeOp op)
        {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

            if (!string.IsNullOrEmpty(json))
            {
                Dictionary<string, string> dic = new Dictionary<string, string>(16, StringComparer.OrdinalIgnoreCase);

                int keyStart = 0, keyEnd = 0;
                int valueStart = 0, valueEnd = 0;

                CharState cs = new CharState(false);
                try
                {
                    int jsonLength = json.Length;
                    #region 核心逻辑
                    for (int i = 0; i < jsonLength; i++)
                    {
                        char c = json[i];
                        if (!cs.IsKeyword(c))//设置关键符号状态。
                        {
                            if (cs.jsonStart)//Json进行中。。。
                            {
                                if (cs.keyStart > 0)
                                {
                                    if (keyStart == 0) { keyStart = i; }
                                    else { keyEnd = i; }
                                }
                                else if (cs.valueStart > 0)
                                {
                                    if (valueStart == 0) { valueStart = i; }
                                    else { valueEnd = i; }
                                }
                            }
                            else if (!cs.arrayStart)//json结束，又不是数组，则退出。
                            {
                                break;
                            }
                        }
                        else if (cs.childrenStart)//正常字符，值状态下。
                        {
                            int errIndex;
                            int length = GetValueLength(false, ref json, i, false, out errIndex);//优化后，速度快了10倍

                            valueStart = i;
                            valueEnd = i + length - 1;


                            cs.childrenStart = false;
                            cs.valueStart = 0;
                            cs.setDicValue = true;
                            i = i + length - 1;
                        }
                        if (cs.setDicValue)//设置键值对。
                        {
                            if (keyStart > 0)
                            {
                                string key = json.Substring(keyStart, Math.Max(keyStart, keyEnd) - keyStart + 1);
                                if (!dic.ContainsKey(key))
                                {
                                    string val = string.Empty;
                                    if (valueStart > 0)
                                    {
                                        val = json.Substring(valueStart, Math.Max(valueStart, valueEnd) - valueStart + 1);
                                    }
                                    bool isNull = val.Length == 4 && val == "null" && i > 4 && json[i - 5] == ':' && json[i] != '"';
                                    if (isNull)
                                    {
                                        val = null;
                                    }
                                    else if (op != EscapeOp.No)
                                    {
                                        val = JsonHelper.UnEscape(val, op);
                                    }
                                    dic.Add(key, val);
                                }

                            }
                            cs.setDicValue = false;
                            keyStart = keyEnd = 0;
                            valueStart = valueEnd = 0;
                        }

                        if (!cs.jsonStart && dic.Count > 0)
                        {
                            result.Add(dic);
                            if (topN > 0 && result.Count >= topN)
                            {
                                return result;
                            }
                            if (cs.arrayStart)//处理数组。
                            {
                                dic = new Dictionary<string, string>(dic.Count, StringComparer.OrdinalIgnoreCase);
                            }
                        }

                    }
                    #endregion
                }
                catch (Exception err)
                {
                    Log.Write(err, LogType.Error);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取值的长度（当Json值嵌套以"{"或"["开头时），【优化后】
        /// </summary>
        private static int GetValueLength(bool isStrictMode, ref string json, int startIndex, bool breakOnErr, out int errIndex)
        {

            errIndex = 0;
            int jsonLength = json.Length;
            int len = jsonLength - 1 - startIndex;
            if (!string.IsNullOrEmpty(json))
            {
                CharState cs = new CharState(isStrictMode);
                char c;
                for (int i = startIndex; i < jsonLength; i++)
                {
                    c = json[i];
                    if (!cs.IsKeyword(c))//设置关键符号状态。
                    {
                        //非正常关键字不可能结束，这里应该不会被调用到。
                        if (!cs.jsonStart && !cs.arrayStart)//json结束，又不是数组，则退出。
                        {
                            break;
                        }
                    }
                    else if (cs.childrenStart)//正常字符，值状态下。
                    {
                        int length = GetValueLength(isStrictMode, ref json, i, breakOnErr, out errIndex);//递归子值，返回一个长度。。。
                        cs.childrenStart = false;
                        cs.valueStart = 0;
                        i = i + length - 1;
                    }
                    if (breakOnErr && cs.isError)
                    {
                        errIndex = i;
                        return i - startIndex;
                    }
                    if (!cs.jsonStart && !cs.arrayStart)//记录当前结束位置。
                    {
                        len = i + 1;//长度比索引+1
                        len = len - startIndex;
                        break;
                    }
                }
            }
            return len;
        }

    }
   
}
