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
        internal static bool IsJson(string json)
        {
            return IsJson(json, false);
        }
        internal static bool IsJson(string json, bool isStrictMode)
        {
            int errIndex;
            return IsJson(json, isStrictMode, out errIndex);
        }
        internal static bool IsJson(string json, bool isStrictMode, out int errIndex)
        {
            errIndex = 0;

            if (string.IsNullOrEmpty(json) || json.Length < 2 ||
                ((json[0] != '{' && json[json.Length - 1] != '}') && (json[0] != '[' && json[json.Length - 1] != ']')))
            {
                return false;
            }
            CharState cs = new CharState(isStrictMode);
            for (int i = 0; i < json.Length; i++)
            {
                //char c = ;
                if (cs.IsKeyword(json[i]) && cs.childrenStart)//设置关键符号状态。
                {
                    int err;
                    int length = GetValueLength(isStrictMode, ref json, i, true, out err);
                    cs.childrenStart = false;
                    if (err > 0)
                    {
                        errIndex = i + err;
                        return false;
                    }
                    else
                    {
                        //重置状态，避免：{\"a\":[]{}}
                        cs.keyValueState = -1;
                        cs.keyStart = 0;
                        cs.valueStart = 0;
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
    }
}
