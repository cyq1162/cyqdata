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
                    int length = GetValueLength(isStrictMode, ref json, i, true, out err);
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
            return Split(json, 0, EscapeOp.No);
        }
        /// <summary>
        /// ����Json
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
                    #region �����߼�
                    for (int i = 0; i < jsonLength; i++)
                    {
                        char c = json[i];
                        if (!cs.IsKeyword(c))//���ùؼ�����״̬��
                        {
                            if (cs.jsonStart)//Json�����С�����
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
                            else if (!cs.arrayStart)//json�������ֲ������飬���˳���
                            {
                                break;
                            }
                        }
                        else if (cs.childrenStart)//�����ַ���ֵ״̬�¡�
                        {
                            int errIndex;
                            int length = GetValueLength(false, ref json, i, false, out errIndex);//�Ż����ٶȿ���10��

                            valueStart = i;
                            valueEnd = i + length - 1;


                            cs.childrenStart = false;
                            cs.valueStart = 0;
                            cs.setDicValue = true;
                            i = i + length - 1;
                        }
                        if (cs.setDicValue)//���ü�ֵ�ԡ�
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
                            if (cs.arrayStart)//�������顣
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
        /// ��ȡֵ�ĳ��ȣ���JsonֵǶ����"{"��"["��ͷʱ�������Ż���
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
                    if (!cs.IsKeyword(c))//���ùؼ�����״̬��
                    {
                        if (!cs.jsonStart && !cs.arrayStart)//json�������ֲ������飬���˳���
                        {
                            break;
                        }
                    }
                    else if (cs.childrenStart)//�����ַ���ֵ״̬�¡�
                    {
                        int length = GetValueLength(isStrictMode, ref json, i, breakOnErr, out errIndex);//�ݹ���ֵ������һ�����ȡ�����
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
        #region �߳�ִ��
        private static void ToInThread<T>(object entityObj)
        {

            Entity<T> entity = entityObj as Entity<T>;
            var dicFunc = DictionaryToEntity.Delegate(typeof(T));
            //Console.WriteLine("ThreadID��" + System.Threading.Thread.CurrentThread.ManagedThreadId);
            //Console.WriteLine("StartIndex��" + entity.StartIndex);


            int start = entity.StartIndex;
            int end = entity.EndIndex;
            for (int i = end; i >= start; i--)
            {
                if (entity.Items[i] == null)
                {
                    var dic = entity.Dics[i];
                    var et = (T)dicFunc(dic);
                    entity.Items[i] = et;
                }
                else
                {
                    break;
                }
            }
            //�����������Ҫ������
            start = end + 1;
            end = entity.Dics.Count;
            if (start < end)
            {
                for (int i = start; i < end; i++)
                {
                    if (entity.Items[i] == null)
                    {
                        var dic = entity.Dics[i];
                        var et = (T)dicFunc(dic);
                        entity.Items[i] = et;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        #endregion

        class Entity<T>
        {
            public T[] Items { get; set; }
            public List<Dictionary<string, string>> Dics;
            public int StartIndex;
            public int EndIndex;
        }

        internal static List<T> ToList<T>(string json, int topN, EscapeOp op)
        {
            Type t = typeof(T);
            if (t.IsValueType || t.Name == "String")
            {
                return ConvertTool.GetObj(typeof(List<T>), json) as List<T>;
            }
            List<T> result = new List<T>();
            var dicFunc = DictionaryToEntity.Delegate(t);
            //if (dicFunc != null)
            //{
            #region Emit ����


            var dics = Split(json, topN, op);
            if (dics == null) { return result; }
            int dicCount = dics.Count;
            if (dicCount == 0) { return result; }
            if (dicCount < 120)
            {
                #region �������ֱ࣬�Ӵ���

                foreach (var dic in dics)
                {
                    result.Add((T)dicFunc(dic));
                }

                #endregion

                return result;
            }


            #region �����϶࣬���̷߳�������

            int batchSize = Math.Max(80, dicCount / 5);
            int batchCount = (dicCount % batchSize) == 0 ? dicCount / batchSize : dicCount / batchSize + 1;//ҳ��
            T[] items = new T[dicCount];
            int i = -1, batchID = 1;
            var firstList = new List<Dictionary<string, string>>(batchSize);
            Entity<T> entity = null;
            int batchNum = 0;
            foreach (var dic in dics)
            {
                i++;

                //�Լ����������
                if (i < batchSize) { firstList.Add(dic); continue; }//

                if (i % batchSize == 0)
                {
                    entity = new Entity<T>();
                    entity.StartIndex = i;
                    entity.Items = items;
                    entity.Dics = dics;
                    batchNum = 1;
                }
                else
                {
                    batchNum++;
                }
                if (batchNum == batchSize || i == dicCount - 1)
                {
                    entity.EndIndex = i;
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ToInThread<T>), entity);
                    batchID++;
                }
            }

            foreach (var dic in firstList)
            {
                //�Լ�����һ��
                result.Add((T)dicFunc(dic));
            }
            i = batchSize;

            while (true)
            {
                if (i > items.Length - 1)
                {
                    break;
                }
                if (items[i] != null)
                {
                    result.Add(items[i]);
                }
                else
                {
                    result.Add((T)dicFunc(dics[i]));
                }
                i++;
            }
            items = null;
            firstList = null;
            return result;
            #endregion

            #endregion
            //}
            /*
            if (!string.IsNullOrEmpty(json) && json.Length > 4)
            {
                var func = EntityInstance.GetFunc(t);
                //func = null;
                // object entity = Activator.CreateInstance(t);
                T entity;
                if (func != null) { entity = (T)func(); }
                else { entity = Activator.CreateInstance<T>(); }

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
                            int length = GetValueLength(false, ref json, i, false, out temp);//�Ż����ٶȿ���10��
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

                                            //EntitySetter.SetterAction(p)(entity, o);
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
                                            //EntitySetter.SetterAction(f)(entity, o);
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
                                if (func != null) { entity = (T)func(); }
                                else { entity = Activator.CreateInstance<T>(); }
                                //entity = Activator.CreateInstance<T>();
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
            */
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
                    return ConvertTool.GetObj(t, json);
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
                            int length = GetValueLength(false, ref json, i, false, out temp);//�Ż����ٶȿ���10��
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
}
