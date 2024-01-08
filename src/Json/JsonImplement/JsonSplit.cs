using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using CYQ.Data.Table;
using CYQ.Data.Tool;

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
                    int length = GetValueLength(isStrictMode, json, i, true, out err);
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
            return Split(json, 0);
        }
        ///// <summary>
        ///// 解析Json
        ///// </summary>
        ///// <returns></returns>
        //internal static List<Dictionary<string, string>> Split(string json, int topN)
        //{
        //    List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

        //    if (!string.IsNullOrEmpty(json))
        //    {
        //        Dictionary<string, string> dic = new Dictionary<string, string>(16, StringComparer.OrdinalIgnoreCase);
        //        int keyIndex = -1, valueIndex = -1;
        //        int keyLength = 0, valueLength = 0;
        //        CharState cs = new CharState(false);
        //        try
        //        {
        //            #region 核心逻辑
        //            for (int i = 0; i < json.Length; i++)
        //            {
        //                char c = json[i];
        //                if (!cs.IsKeyword(c))//设置关键符号状态。
        //                {
        //                    if (cs.jsonStart)//Json进行中。。。
        //                    {
        //                        if (cs.keyStart > 0)
        //                        {
        //                            if (keyIndex == -1)
        //                            {
        //                                keyIndex = i;
        //                            }
        //                            keyLength++;
        //                        }
        //                        else if (cs.valueStart > 0)
        //                        {
        //                            if (valueIndex == -1)
        //                            {
        //                                valueIndex = i;
        //                            }
        //                            valueLength++;
        //                        }
        //                    }
        //                    else if (!cs.arrayStart)//json结束，又不是数组，则退出。
        //                    {
        //                        break;
        //                    }
        //                }
        //                else if (cs.childrenStart)//正常字符，值状态下。
        //                {
        //                    int temp;
        //                    valueLength = GetValueLength(false, json, i, false, out temp);//优化后，速度快了10倍
        //                    valueIndex = i;
        //                    cs.childrenStart = false;
        //                    cs.valueStart = 0;
        //                    cs.setDicValue = true;
        //                    i = i + valueLength - 1;
        //                }
        //                if (cs.setDicValue)//设置键值对。
        //                {
        //                    if (keyLength > 0)
        //                    {
        //                        string k = json.Substring(keyIndex, keyLength);// key.ToString();
        //                        if (!dic.ContainsKey(k))
        //                        {
        //                            string val = valueLength > 0 ? json.Substring(valueIndex, valueLength) : ""; //value.ToString();
        //                            bool isNull = json[i - 5] == ':' && json[i] != '"' && valueLength == 4 && val == "null";
        //                            if (isNull)
        //                            {
        //                                val = "";
        //                            }
        //                            dic.Add(k, val);
        //                        }
        //                    }
        //                    cs.setDicValue = false;
        //                    keyIndex = valueIndex = -1;
        //                    keyLength = valueLength = 0;
        //                }

        //                if (!cs.jsonStart && dic.Count > 0)
        //                {
        //                    result.Add(dic);
        //                    if (topN > 0 && result.Count >= topN)
        //                    {
        //                        return result;
        //                    }
        //                    if (cs.arrayStart)//处理数组。
        //                    {
        //                        dic = new Dictionary<string, string>(16, StringComparer.OrdinalIgnoreCase);
        //                    }
        //                }
        //            }
        //            #endregion
        //        }
        //        catch (Exception err)
        //        {
        //            Log.Write(err, LogType.Error);
        //        }
        //    }
        //    return result;
        //}
        /// <summary>
        /// 解析Json
        /// </summary>
        /// <returns></returns>
        internal static List<Dictionary<string, string>> Split(string json, int topN)
        {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

            if (!string.IsNullOrEmpty(json))
            {
                Dictionary<string, string> dic = new Dictionary<string, string>(16, StringComparer.OrdinalIgnoreCase);
                //string key = string.Empty;
                StringBuilder key = new StringBuilder(32);
                StringBuilder value = new StringBuilder();
                CharState cs = new CharState(false);
                try
                {
                    #region 核心逻辑
                    for (int i = 0; i < json.Length; i++)
                    {
                        char c = json[i];
                        if (!cs.IsKeyword(c))//设置关键符号状态。
                        {
                            if (cs.jsonStart)//Json进行中。。。
                            {
                                if (cs.keyStart > 0)
                                {
                                    key.Append(c);
                                }
                                else if (cs.valueStart > 0)
                                {
                                    value.Append(c);
                                }
                            }
                            else if (!cs.arrayStart)//json结束，又不是数组，则退出。
                            {
                                break;
                            }
                        }
                        else if (cs.childrenStart)//正常字符，值状态下。
                        {
                            //string item = json.Substring(i);
                            int temp;
                            // int length = GetValueLength(false, json.Substring(i), false, out temp);//这里应该有优化的空间，传json和i，不重新生成string
                            int length = GetValueLength(false, json, i, false, out temp);//优化后，速度快了10倍

                            value.Length = 0;
                            value.Append(json.Substring(i, length));
                            cs.childrenStart = false;
                            cs.valueStart = 0;
                            //cs.state = 0;
                            cs.setDicValue = true;
                            i = i + length - 1;
                        }
                        if (cs.setDicValue)//设置键值对。
                        {
                            if (key.Length > 0)
                            {
                                string k = key.ToString();
                                if (!dic.ContainsKey(k))
                                {
                                    string val = value.ToString();
                                    bool isNull = i > 4 && json[i - 5] == ':' && json[i] != '"' && value.Length == 4 && val == "null";
                                    if (isNull)
                                    {
                                        val = "";
                                    }
                                    dic.Add(k, val);
                                }
                            }
                            cs.setDicValue = false;
                            key.Length = 0;
                            value.Length = 0;
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
                                dic = new Dictionary<string, string>(16, StringComparer.OrdinalIgnoreCase);
                            }
                        }
                    }
                    #endregion
                }
                catch (Exception err)
                {
                    Log.Write(err, LogType.Error);
                }
                finally
                {
                    key = null;
                    value = null;
                }
            }
            return result;
        }
        /*
        /// <summary>
        /// 获取值的长度（当Json值嵌套以"{"或"["开头时）
        /// </summary>
        private static int GetValueLength(bool isStrictMode, string json, bool breakOnErr, out int errIndex)
        {
            errIndex = 0;
            int len = json.Length - 1;
            if (!string.IsNullOrEmpty(json))
            {
                CharState cs = new CharState(isStrictMode);
                char c;
                for (int i = 0; i < json.Length; i++)
                {
                    c = json[i];
                    if (!cs.IsKeyword(c))//设置关键符号状态。
                    {
                        if (!cs.jsonStart && !cs.arrayStart)//json结束，又不是数组，则退出。
                        {
                            break;
                        }
                    }
                    else if (cs.childrenStart)//正常字符，值状态下。
                    {
                        int length = GetValueLength(isStrictMode, json.Substring(i), breakOnErr, out errIndex);//递归子值，返回一个长度。。。
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
        */
        /// <summary>
        /// 获取值的长度（当Json值嵌套以"{"或"["开头时），【优化后】
        /// </summary>
        private static int GetValueLength(bool isStrictMode, string json, int startIndex, bool breakOnErr, out int errIndex)
        {
            errIndex = 0;
            int len = json.Length - 1 - startIndex;
            if (!string.IsNullOrEmpty(json))
            {
                CharState cs = new CharState(isStrictMode);
                char c;
                for (int i = startIndex; i < json.Length; i++)
                {
                    c = json[i];
                    if (!cs.IsKeyword(c))//设置关键符号状态。
                    {
                        if (!cs.jsonStart && !cs.arrayStart)//json结束，又不是数组，则退出。
                        {
                            break;
                        }
                    }
                    else if (cs.childrenStart)//正常字符，值状态下。
                    {
                        int length = GetValueLength(isStrictMode, json, i, breakOnErr, out errIndex);//递归子值，返回一个长度。。。
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


        #region 扩展转实体T

        internal static T ToEntity<T>(string json, EscapeOp op)
        {
            List<T> t = ToList<T>(json, 0, op);
            if (t.Count > 0)
            {
                return t[0];
            }
            return default(T);
        }
        internal static List<T> ToList<T>(string json, int topN, EscapeOp op)
        {
            Type t = typeof(T);
            if (t.IsValueType || t.Name == "String")
            {
                return new MDataRow().GetObj(typeof(List<T>), json) as List<T>;
            }

            List<T> result = new List<T>();

            if (!string.IsNullOrEmpty(json) && json.Length > 4)
            {
                // object entity = Activator.CreateInstance(t);
                T entity = Activator.CreateInstance<T>();
                bool hasSetValue = false;
                List<PropertyInfo> pInfoList = ReflectTool.GetPropertyList(t);
                List<FieldInfo> fInfoList = ReflectTool.GetFieldList(t);
                //string key = string.Empty;
                StringBuilder key = new StringBuilder(32);
                StringBuilder value = new StringBuilder();
                CharState cs = new CharState(false);
                try
                {
                    #region 核心逻辑
                    char c;
                    for (int i = 0; i < json.Length; i++)
                    {
                        c = json[i];
                        if (!cs.IsKeyword(c))//设置关键符号状态。
                        {
                            if (cs.jsonStart)//Json进行中。。。
                            {
                                if (cs.keyStart > 0)
                                {
                                    key.Append(c);
                                }
                                else if (cs.valueStart > 0)
                                {
                                    value.Append(c);
                                }
                            }
                            else if (!cs.arrayStart)//json结束，又不是数组，则退出。
                            {
                                break;
                            }
                        }
                        else if (cs.childrenStart)//正常字符，值状态下。
                        {
                            int temp;
                            int length = GetValueLength(false, json, i, false, out temp);//优化后，速度快了10倍
                            value.Length = 0;
                            value.Append(json.Substring(i, length));
                            cs.childrenStart = false;
                            cs.valueStart = 0;
                            cs.setDicValue = true;
                            i = i + length - 1;
                        }
                        if (cs.setDicValue)//设置键值对。
                        {
                            if (key.Length > 0)
                            {
                                string k = key.ToString();
                                string val = value.ToString();//.TrimEnd('\r', '\n', '\t');
                                bool isNull = i > 4 && json[i - 5] == ':' && json[i] != '"' && val.Length == 4 && val == "null";
                                if (isNull)
                                {
                                    val = "";
                                }
                                else
                                {
                                    val = JsonHelper.UnEscape(val, op);
                                }
                                bool hasProperty = false;
                                object o = val;
                                foreach (PropertyInfo p in pInfoList)
                                {
                                    if (String.Compare(p.Name, k, StringComparison.OrdinalIgnoreCase) == 0)
                                    {
                                        hasProperty = true;
                                        if (p.CanWrite)
                                        {
                                            if (p.PropertyType.Name != "String")
                                            {
                                                o = ConvertTool.ChangeType(val, p.PropertyType);
                                            }
                                            p.SetValue(entity, o, null);
                                            hasSetValue = true;
                                        }
                                        break;
                                    }
                                }
                                if (!hasProperty && fInfoList.Count > 0)
                                {
                                    foreach (FieldInfo f in fInfoList)
                                    {
                                        if (String.Compare(f.Name, k, StringComparison.OrdinalIgnoreCase) == 0)
                                        {
                                            if (f.FieldType.Name != "String")
                                            {
                                                o = ConvertTool.ChangeType(val, f.FieldType);
                                            }
                                            f.SetValue(entity, o);
                                            hasSetValue = true;
                                            break;
                                        }
                                    }
                                }


                            }
                            cs.setDicValue = false;
                            key.Length = 0;
                            value.Length = 0;
                        }

                        if (!cs.jsonStart && hasSetValue)
                        {
                            result.Add(entity);
                            if (topN > 0 && result.Count >= topN)
                            {
                                return result;
                            }
                            if (cs.arrayStart)//处理数组。
                            {
                                entity = Activator.CreateInstance<T>();
                                hasSetValue = false;
                            }
                        }
                    }
                    #endregion
                }
                catch (Exception err)
                {
                    Log.Write(err, LogType.Error);
                }
                finally
                {
                    key = null;
                    value = null;
                }
            }
            return result;
        }

        /// <summary>
        /// 支持转换：实体或实体列表二合一
        /// </summary>
        internal static object ToEntityOrList(Type t, string json, EscapeOp op)
        {
            Type toType = t;
            object listObj = null;
            MethodInfo method = null;
            if (t.IsGenericType && (t.Name.StartsWith("List") || t.Name.StartsWith("IList") || t.Name.StartsWith("MList")))
            {
                Type[] paraTypeList = null;
                ReflectTool.GetArgumentLength(ref t, out paraTypeList);
                toType = paraTypeList[0];
                if (toType.IsValueType || toType.Name == "String")
                {
                    return new MDataRow().GetObj(t, json);
                }
                listObj = Activator.CreateInstance(t);//创建实例

                method = t.GetMethod("Add");
            }

            if (!string.IsNullOrEmpty(json))
            {
                object entity = Activator.CreateInstance(toType);
                bool hasSetValue = false;
                List<PropertyInfo> pInfoList = ReflectTool.GetPropertyList(toType);
                List<FieldInfo> fInfoList = ReflectTool.GetFieldList(toType);
                //string key = string.Empty;
                StringBuilder key = new StringBuilder(32);
                StringBuilder value = new StringBuilder();
                CharState cs = new CharState(false);
                try
                {
                    #region 核心逻辑
                    char c;
                    for (int i = 0; i < json.Length; i++)
                    {
                        c = json[i];
                        if (!cs.IsKeyword(c))//设置关键符号状态。
                        {
                            if (cs.jsonStart)//Json进行中。。。
                            {
                                if (cs.keyStart > 0)
                                {
                                    key.Append(c);
                                }
                                else if (cs.valueStart > 0)
                                {
                                    value.Append(c);
                                }
                            }
                            else if (!cs.arrayStart)//json结束，又不是数组，则退出。
                            {
                                break;
                            }
                        }
                        else if (cs.childrenStart)//正常字符，值状态下。
                        {
                            int temp;
                            int length = GetValueLength(false, json, i, false, out temp);//优化后，速度快了10倍
                            value.Length = 0;
                            value.Append(json.Substring(i, length));
                            cs.childrenStart = false;
                            cs.valueStart = 0;
                            cs.setDicValue = true;
                            i = i + length - 1;
                        }
                        if (cs.setDicValue)//设置键值对。
                        {
                            if (key.Length > 0)
                            {
                                string k = key.ToString();
                                string val = value.ToString();//.TrimEnd('\r', '\n', '\t');
                                bool isNull = i > 4 && json[i - 5] == ':' && json[i] != '"' && val.Length == 4 && val == "null";
                                if (isNull)
                                {
                                    val = "";
                                }
                                else
                                {
                                    val = JsonHelper.UnEscape(val, op);
                                }
                                bool hasProperty = false;
                                object o = val;
                                foreach (PropertyInfo p in pInfoList)
                                {
                                    if (String.Compare(p.Name, k, StringComparison.OrdinalIgnoreCase) == 0)
                                    {
                                        hasProperty = true;
                                        if (p.CanWrite)
                                        {
                                            if (p.PropertyType.Name != "String")
                                            {
                                                o = ConvertTool.ChangeType(val, p.PropertyType);
                                            }
                                            p.SetValue(entity, o, null);
                                            hasSetValue = true;
                                        }
                                        break;
                                    }
                                }
                                if (!hasProperty && fInfoList.Count > 0)
                                {
                                    foreach (FieldInfo f in fInfoList)
                                    {
                                        if (String.Compare(f.Name, k, StringComparison.OrdinalIgnoreCase) == 0)
                                        {
                                            if (f.FieldType.Name != "String")
                                            {
                                                o = ConvertTool.ChangeType(val, f.FieldType);
                                            }
                                            f.SetValue(entity, o);
                                            hasSetValue = true;
                                            break;
                                        }
                                    }
                                }


                            }
                            cs.setDicValue = false;
                            key.Length = 0;
                            value.Length = 0;
                        }

                        if (!cs.jsonStart && hasSetValue)
                        {
                            if (method != null)
                            {
                                method.Invoke(listObj, new object[] { entity });
                            }
                            else
                            {
                                return entity;
                            }
                            if (cs.arrayStart)//处理数组。
                            {
                                entity = Activator.CreateInstance(toType);
                                hasSetValue = false;
                            }
                        }
                    }
                    #endregion
                }
                catch (Exception err)
                {
                    Log.Write(err, LogType.Error);
                }
                finally
                {
                    key = null;
                    value = null;
                }
            }
            return listObj;
        }
        #endregion
    }
    internal partial class JsonSplit
    {
        /// <summary>
        /// 将json数组分成字符串List
        /// </summary>
        /// <param name="jsonArray">["a,","bbb,,"]</param>
        /// <returns></returns>
        internal static List<string> SplitEscapeArray(string jsonArray)
        {
            if (!string.IsNullOrEmpty(jsonArray))
            {
                jsonArray = jsonArray.Trim(' ', '[', ']');//["a,","bbb,,"]
                List<string> list = new List<string>();
                if (jsonArray.Length > 0)
                {
                    string[] items = jsonArray.Split(',');
                    string objStr = string.Empty;
                    foreach (string value in items)
                    {
                        string item = value.Trim('\r', '\n', '\t', ' ');
                        if (objStr == string.Empty)
                        {
                            objStr = item;
                        }
                        else
                        {
                            objStr += "," + item;
                        }
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
                }
                return list;

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
