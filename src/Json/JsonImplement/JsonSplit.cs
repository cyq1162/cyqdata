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
                        //�������ؼ��ֲ����ܽ���������Ӧ�ò��ᱻ���õ���
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

    }
   
}
