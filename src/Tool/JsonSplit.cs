using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using CYQ.Data.Table;

namespace CYQ.Data.Tool
{
    /// <summary>
    /// �ָ�Json�ַ���Ϊ�ֵ伯�ϡ�
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
                if (cs.IsKeyword(json[i]) && cs.childrenStart)//���ùؼ�����״̬��
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

            return !cs.arrayStart && !cs.jsonStart; //ֻҪ���������رգ���ʧ��
        }
        internal static List<Dictionary<string, string>> Split(string json)
        {
            return Split(json, 0);
        }
        ///// <summary>
        ///// ����Json
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
        //            #region �����߼�
        //            for (int i = 0; i < json.Length; i++)
        //            {
        //                char c = json[i];
        //                if (!cs.IsKeyword(c))//���ùؼ�����״̬��
        //                {
        //                    if (cs.jsonStart)//Json�����С�����
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
        //                    else if (!cs.arrayStart)//json�������ֲ������飬���˳���
        //                    {
        //                        break;
        //                    }
        //                }
        //                else if (cs.childrenStart)//�����ַ���ֵ״̬�¡�
        //                {
        //                    int temp;
        //                    valueLength = GetValueLength(false, json, i, false, out temp);//�Ż����ٶȿ���10��
        //                    valueIndex = i;
        //                    cs.childrenStart = false;
        //                    cs.valueStart = 0;
        //                    cs.setDicValue = true;
        //                    i = i + valueLength - 1;
        //                }
        //                if (cs.setDicValue)//���ü�ֵ�ԡ�
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
        //                    if (cs.arrayStart)//�������顣
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
        /// ����Json
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
                    #region �����߼�
                    for (int i = 0; i < json.Length; i++)
                    {
                        char c = json[i];
                        if (!cs.IsKeyword(c))//���ùؼ�����״̬��
                        {
                            if (cs.jsonStart)//Json�����С�����
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
                            else if (!cs.arrayStart)//json�������ֲ������飬���˳���
                            {
                                break;
                            }
                        }
                        else if (cs.childrenStart)//�����ַ���ֵ״̬�¡�
                        {
                            //string item = json.Substring(i);
                            int temp;
                            // int length = GetValueLength(false, json.Substring(i), false, out temp);//����Ӧ�����Ż��Ŀռ䣬��json��i������������string
                            int length = GetValueLength(false, json, i, false, out temp);//�Ż����ٶȿ���10��

                            value.Length = 0;
                            value.Append(json.Substring(i, length));
                            cs.childrenStart = false;
                            cs.valueStart = 0;
                            //cs.state = 0;
                            cs.setDicValue = true;
                            i = i + length - 1;
                        }
                        if (cs.setDicValue)//���ü�ֵ�ԡ�
                        {
                            if (key.Length > 0)
                            {
                                string k = key.ToString();
                                if (!dic.ContainsKey(k))
                                {
                                    string val = value.ToString();
                                    bool isNull = json[i - 5] == ':' && json[i] != '"' && value.Length == 4 && val == "null";
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
                            if (cs.arrayStart)//�������顣
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
        /// ��ȡֵ�ĳ��ȣ���JsonֵǶ����"{"��"["��ͷʱ��
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
                    if (!cs.IsKeyword(c))//���ùؼ�����״̬��
                    {
                        if (!cs.jsonStart && !cs.arrayStart)//json�������ֲ������飬���˳���
                        {
                            break;
                        }
                    }
                    else if (cs.childrenStart)//�����ַ���ֵ״̬�¡�
                    {
                        int length = GetValueLength(isStrictMode, json.Substring(i), breakOnErr, out errIndex);//�ݹ���ֵ������һ�����ȡ�����
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
                    if (!cs.jsonStart && !cs.arrayStart)//��¼��ǰ����λ�á�
                    {
                        len = i + 1;//���ȱ�����+1
                        break;
                    }
                }
            }
            return len;
        }
        */
        /// <summary>
        /// ��ȡֵ�ĳ��ȣ���JsonֵǶ����"{"��"["��ͷʱ�������Ż���
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
                    if (!cs.IsKeyword(c))//���ùؼ�����״̬��
                    {
                        if (!cs.jsonStart && !cs.arrayStart)//json�������ֲ������飬���˳���
                        {
                            break;
                        }
                    }
                    else if (cs.childrenStart)//�����ַ���ֵ״̬�¡�
                    {
                        int length = GetValueLength(isStrictMode, json, i, breakOnErr, out errIndex);//�ݹ���ֵ������һ�����ȡ�����
                        cs.childrenStart = false;
                        cs.valueStart = 0;
                        i = i + length - 1;
                    }
                    if (breakOnErr && cs.isError)
                    {
                        errIndex = i;
                        return i - startIndex;
                    }
                    if (!cs.jsonStart && !cs.arrayStart)//��¼��ǰ����λ�á�
                    {
                        len = i + 1;//���ȱ�����+1
                        len = len - startIndex;
                        break;
                    }
                }
            }
            return len;
        }


        #region ��չתʵ��T

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
            List<T> result = new List<T>();

            if (!string.IsNullOrEmpty(json))
            {
                Type t = typeof(T);
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
                    #region �����߼�
                    char c;
                    for (int i = 0; i < json.Length; i++)
                    {
                        c = json[i];
                        if (!cs.IsKeyword(c))//���ùؼ�����״̬��
                        {
                            if (cs.jsonStart)//Json�����С�����
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
                            else if (!cs.arrayStart)//json�������ֲ������飬���˳���
                            {
                                break;
                            }
                        }
                        else if (cs.childrenStart)//�����ַ���ֵ״̬�¡�
                        {
                            int temp;
                            int length = GetValueLength(false, json, i, false, out temp);//�Ż����ٶȿ���10��
                            value.Length = 0;
                            value.Append(json.Substring(i, length));
                            cs.childrenStart = false;
                            cs.valueStart = 0;
                            cs.setDicValue = true;
                            i = i + length - 1;
                        }
                        if (cs.setDicValue)//���ü�ֵ�ԡ�
                        {
                            if (key.Length > 0)
                            {
                                string k = key.ToString();
                                string val = value.ToString();//.TrimEnd('\r', '\n', '\t');
                                bool isNull = json[i - 5] == ':' && json[i] != '"' && val.Length == 4 && val == "null";
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
                            if (cs.arrayStart)//�������顣
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
        /// ֧��ת����ʵ���ʵ���б����һ
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
                listObj = Activator.CreateInstance(t);//����ʵ��
                
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
                    #region �����߼�
                    char c;
                    for (int i = 0; i < json.Length; i++)
                    {
                        c = json[i];
                        if (!cs.IsKeyword(c))//���ùؼ�����״̬��
                        {
                            if (cs.jsonStart)//Json�����С�����
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
                            else if (!cs.arrayStart)//json�������ֲ������飬���˳���
                            {
                                break;
                            }
                        }
                        else if (cs.childrenStart)//�����ַ���ֵ״̬�¡�
                        {
                            int temp;
                            int length = GetValueLength(false, json, i, false, out temp);//�Ż����ٶȿ���10��
                            value.Length = 0;
                            value.Append(json.Substring(i, length));
                            cs.childrenStart = false;
                            cs.valueStart = 0;
                            cs.setDicValue = true;
                            i = i + length - 1;
                        }
                        if (cs.setDicValue)//���ü�ֵ�ԡ�
                        {
                            if (key.Length > 0)
                            {
                                string k = key.ToString();
                                string val = value.ToString();//.TrimEnd('\r', '\n', '\t');
                                bool isNull = json[i - 5] == ':' && json[i] != '"' && val.Length == 4 && val == "null";
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
                            if (cs.arrayStart)//�������顣
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
        /// ��json����ֳ��ַ���List
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
                            //���˫���ŵ�����
                            if (GetCharCount(objStr, firstChar) % 2 == 0)//���ų�˫
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
        /// ��ȡ�ַ����ַ������ֵĴ���
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

    /// <summary>
    /// �ַ�״̬
    /// </summary>
    internal class CharState
    {
        internal char lastKeywordChar = ' ';
        internal char lastChar = ' ';
        /// <summary>
        /// �Ƿ��ʽ��ʽ��true���Ա���˫���ţ�false���Կ��Ե����ź������š���
        /// </summary>
        internal bool isStrictMode = false;
        public CharState(bool isStrictMode)
        {
            this.isStrictMode = isStrictMode;
        }
        internal bool jsonStart = false;//�� "{"��ʼ��...
        internal bool setDicValue = false;// ���������ֵ�ֵ�ˡ�
        internal bool escapeChar = false;//��"\"ת����ſ�ʼ��
        /// <summary>
        /// ���鿪ʼ������һ��ͷ���㡿��ֵǶ�׵��ԡ�childrenStart������ʶ��
        /// </summary>
        internal bool arrayStart = false;//��"[" ���ſ�ʼ��
        internal bool childrenStart = false;//�Ӽ�Ƕ�׿�ʼ�ˡ�
        /// <summary>
        /// ��-1 δ��ʼ������0ȡ���׶Ρ���1 ȡֵ�׶Ρ�
        /// </summary>
        internal int keyValueState = -1;

        /// <summary>
        /// ��-2 �ѽ�������-1 δ��ʼ������0 δ��ʼ����1 �����ſ�ʼ����2 �����ſ�ʼ����3 ˫���ſ�ʼ��
        /// </summary>
        internal int keyStart = -1;
        /// <summary>
        /// ��-2 �ѽ�������-1 δ��ʼ������0 δ��ʼ����1 �����ſ�ʼ����2 �����ſ�ʼ����3 ˫���ſ�ʼ��
        /// </summary>
        internal int valueStart = -1;

        internal bool isError = false;//�Ƿ��﷨����

        internal void CheckIsError(char c)//ֻ����һ��������ΪGetLength��ݹ鵽ÿһ�������
        {
            switch (c)
            {
                case '\r':
                case '\n':
                case '\t':
                    return;
                case '{'://[{ "[{A}]":[{"[{B}]":3,"m":"C"}]}]
                    isError = jsonStart && keyValueState == 0;//�ظ���ʼ���� ͬʱ����ֵ����
                    break;
                case '}':
                    isError = !jsonStart || (keyStart > 0 && keyValueState == 0);//�ظ��������� ���� ��ǰ������
                    if (!isError && isStrictMode)
                    {
                        isError = !((keyStart == 3 && keyValueState == 0) || (valueStart != 2 && keyValueState == 1) || valueStart == -2 || (jsonStart && keyStart == -1));
                    }
                    break;
                case '[':
                    isError = arrayStart && keyValueState == 0;//�ظ���ʼ����
                    break;
                case ']':
                    isError = (!arrayStart && valueStart != 3 && keyStart != 3) || (keyValueState == 1 && valueStart == 0);//�ظ���ʼ����[{},]1,0  ������[111,222] 1,1 [111,"22"] 1,-2 
                    break;
                case '"':
                    isError = !jsonStart && !arrayStart;//δ��ʼJson��ͬʱҲδ��ʼ���顣
                    break;
                case '\'':
                    isError = (!jsonStart && !arrayStart);//δ��ʼJson
                    if (!isError && isStrictMode)
                    {
                        isError = !((keyStart == 3 && keyValueState == 0) || (valueStart == 3 && keyValueState == 1));
                    }
                    break;
                case ':':
                    isError = (!jsonStart && !arrayStart) || (jsonStart && keyStart < 2 && valueStart < 2 && keyValueState == 1);//δ��ʼJson ͬʱ ֻ�ܴ�����ȡֵ֮ǰ��
                    break;
                case ',':
                    isError = (!jsonStart && !arrayStart)
                        || (!jsonStart && arrayStart && keyValueState == -1) //[,111]
                        || (jsonStart && keyStart < 2 && valueStart < 2 && keyValueState == 0);//δ��ʼJson ͬʱ ֻ�ܴ�����ȡֵ֮��
                    break;
                //case 't'://true
                //case 'f'://false

                //  break;
                default: //ֵ��ͷ����
                    isError = (!jsonStart && !arrayStart) || (keyStart == 0 && valueStart == 0 && keyValueState == 0);//
                    if (!isError && keyStart < 2)
                    {
                        //if ((jsonStart && !arrayStart) && state != 1)
                        if (jsonStart && keyValueState <= 0)//ȡ���׶�
                        {
                            //�������ſ�ͷ�ģ�ֻ������ĸ {aaa:1}
                            isError = isStrictMode || (c < 65 || (c > 90 && c < 97) || c > 122);
                        }
                        else if (!jsonStart && arrayStart && valueStart < 2)//
                        {
                            switch (c)
                            {
                                case ' ':
                                case 'n'://null
                                case 'u':
                                case 'l':
                                case 't'://true
                                case 'r':
                                case 'e':
                                case 'f'://false
                                case 'a':
                                case 's':
                                    break;
                                default:
                                    //�������ſ�ͷ�ģ�ֻ��������[1] �ո�null,true,false
                                    isError = c < 48 || c > 57;
                                    break;
                            }

                        }
                    }
                    if (!isError && isStrictMode)
                    {
                        if (jsonStart && valueStart == 1)//���ֵvalue:true ��value:false
                        {
                            switch (c)
                            {
                                case 'r'://true
                                    isError = lastChar != 't';
                                    break;
                                case 'u'://true,null
                                    isError = !((lastKeywordChar == 't' && lastChar == 'r') || (lastKeywordChar == 'n' && lastChar == 'n'));
                                    break;
                                case 'e'://true
                                    isError = !((lastKeywordChar == 't' && lastChar == 'u') || (lastKeywordChar == 'f' && lastChar == 's'));
                                    break;
                                case 'a'://false
                                    isError = lastChar != 'f';
                                    break;
                                case 'l'://false,null 
                                    isError = !((lastKeywordChar == 'f' && lastChar == 'a') || (lastKeywordChar == 'n' && (lastChar == 'u' || lastChar == 'l')));
                                    if (!isError && lastKeywordChar == 'n' && lastChar == 'l')
                                    {
                                        //ȡ���ؼ��֣�������� nulllll���l
                                        lastKeywordChar = ' ';
                                    }
                                    break;
                                case 's'://false
                                    isError = lastChar != 'l';
                                    break;
                                case '.'://���ֿ��Գ���С���㣬�������ظ�����
                                    isError = keyValueState != 1 || lastKeywordChar == '.';
                                    break;
                                case ' ':
                                    if (lastChar == '.') { isError = true; }
                                    else if (jsonStart && !arrayStart)
                                    {
                                        valueStart = -2;//�����ո񣬽���ȡֵ��
                                    }
                                    break;
                                default:
                                    //�������ſ�ͷ�ģ�ֻ��������[1]
                                    isError = c < 48 || c > 57;
                                    break;
                            }
                        }
                        //ֵ��ͷ�ģ�ֻ���ǣ�["xxx"] {[{}]
                    }
                    break;
            }
            if (isError)
            {
                //
            }
        }

        /// <summary>
        /// �����ַ�״̬(����true��Ϊ�ؼ��ʣ�����false��Ϊ��ͨ�ַ�����
        /// </summary>
        internal bool IsKeyword(char c)
        {
            bool isKeyword = false;
            switch (c)
            {
                case '{'://[{ "[{A}]":[{"[{B}]":3,"m":"C"}]}]
                    #region ������
                    if (keyStart <= 0 && valueStart <= 0)
                    {
                        if (jsonStart && keyValueState == 1)
                        {
                            valueStart = 0;
                            childrenStart = true;
                        }
                        else
                        {
                            keyValueState = 0;
                        }
                        jsonStart = true;//��ʼ��
                        isKeyword = true;
                    }
                    #endregion
                    break;
                case '}':
                    #region �����Ž���
                    if (lastChar != '.')
                    {
                        if (keyStart <= 0 && valueStart < 2)
                        {
                            if (jsonStart)
                            {
                                jsonStart = false;//����������
                                valueStart = -1;
                                keyValueState = 0;
                                setDicValue = true;
                            }
                            isKeyword = true;
                        }
                    }
                    #endregion
                    break;
                case '[':
                    #region �����ſ�ʼ
                    if (!jsonStart)
                    {
                        arrayStart = true;
                        isKeyword = true;
                    }
                    else if (jsonStart && keyValueState == 1 && valueStart < 2)
                    {
                        childrenStart = true;
                        isKeyword = true;
                    }
                    #endregion
                    break;
                case ']':

                    #region �����Ž���
                    if (lastChar != '.')
                    {
                        if (!jsonStart && (keyStart <= 0 && valueStart <= 0) || (keyStart == -1 && valueStart == 1))
                        {
                            if (arrayStart)// && !childrenStart
                            {
                                arrayStart = false;
                            }
                            isKeyword = true;
                        }
                    }
                    #endregion
                    break;
                case '"':
                case '\'':
                    // CheckIsError(c);
                    if (isStrictMode && c == '\'')
                    {
                        break;
                    }
                    #region ����
                    if (jsonStart || arrayStart)
                    {
                        if (!jsonStart && arrayStart)
                        {
                            keyValueState = 1;//��������飬ֻ��ȡֵ��û��Key������ֱ������0
                        }
                        if (keyValueState == 0)//key�׶�
                        {
                            keyStart = (keyStart <= 0 ? (c == '"' ? 3 : 2) : -2);
                            isKeyword = true;
                        }
                        else if (keyValueState == 1)//ֵ�׶�
                        {
                            if (valueStart <= 0)
                            {
                                valueStart = (c == '"' ? 3 : 2);
                                isKeyword = true;
                            }
                            else if ((valueStart == 2 && c == '\'') || (valueStart == 3 && c == '"'))
                            {
                                if (!escapeChar)
                                {
                                    valueStart = -2;
                                    isKeyword = true;
                                }
                                else
                                {
                                    escapeChar = false;
                                }
                            }

                        }
                    }
                    #endregion
                    break;
                case ':':
                    // CheckIsError(c);
                    #region ð��
                    if (jsonStart && keyStart < 2 && valueStart < 2 && keyValueState == 0)
                    {
                        keyStart = -2;//0 ����key
                        keyValueState = 1;
                        isKeyword = true;
                    }
                    #endregion
                    break;
                case ',':
                    #region ���� {"a": [11,"22", ], "Type": 2.}
                    if (lastChar != '.')
                    {
                        if (jsonStart && keyStart < 2 && valueStart < 2 && keyValueState == 1)
                        {
                            keyValueState = 0;
                            valueStart = 0;
                            setDicValue = true;
                            isKeyword = true;
                        }
                        else if (arrayStart && !jsonStart) //[a,b]  [",",33] [{},{}]
                        {
                            if ((keyValueState == -1 && valueStart == -1) || (valueStart < 2 && keyValueState == 1))
                            {
                                valueStart = 0;
                                isKeyword = true;
                            }
                        }
                    }
                    #endregion
                    break;
                case ' ':
                case '\r':
                case '\n':
                case '\t':
                    if (jsonStart && keyStart <= 1 && valueStart <= 1)
                    {
                        isKeyword = true;
                        // return true;//�����ո�
                    }
                    break;
                case 't'://true
                case 'f'://false
                case 'n'://null
                case '-'://-388.8 //�������ַ���
                    if (lastKeywordChar != c && lastKeywordChar != '.')
                    {
                        if (valueStart <= 1 && ((arrayStart && !jsonStart && keyStart == -1) || (jsonStart && keyValueState == 1 && valueStart <= 0)))
                        {
                            //ֻ��״̬�����ǹؼ���
                            valueStart = 1;
                            lastChar = c;
                            lastKeywordChar = c;
                            return false;//ֱ�ӷ��أ���������
                        }
                    }
                    break;
                case '.':
                    if ((jsonStart || arrayStart) && keyValueState == 1 && valueStart == 1 && lastKeywordChar != c)
                    {
                        lastChar = c;
                        lastKeywordChar = c;//��¼���ţ�����ֻ����һ�����š�
                        return false;//��������
                    }
                    break;
                default: //ֵ��ͷ����
                    // CheckIsError(c);
                    if (c == '\\') //ת�����
                    {
                        if (escapeChar)
                        {
                            escapeChar = false;
                        }
                        else
                        {
                            escapeChar = true;
                        }
                    }
                    if (jsonStart)
                    {
                        if (keyStart <= 0 && keyValueState <= 0)
                        {
                            keyStart = 1;//�����ŵ�
                        }
                        else if (valueStart <= 0 && keyValueState == 1)
                        {
                            valueStart = 1;//�����ŵ�
                        }
                    }
                    else if (arrayStart)
                    {
                        keyValueState = 1;
                        if (valueStart < 1)
                        {
                            valueStart = 1;//�����ŵ�
                        }
                    }
                    break;
            }
            if (escapeChar && c != '\\')
            {
                escapeChar = false;
            }
            if (!isKeyword)
            {
                CheckIsError(c);
            }
            else
            {
                lastKeywordChar = c;
            }
            lastChar = c;
            return isKeyword;
        }
    }
}
