using System;
using System.Collections.Generic;




namespace CYQ.Data.SQL
{
    internal static class SqlInjection
    {
        //select;from,
        internal const string filterSqlInjection = "select;into,delete;from,drop;table,drop;database,update;set,truncate;table,create;table,exists;select,insert;into,xp_cmdshell,declare;@,exec;master,waitfor;delay";
        //internal const string replaceSqlInjection = "--";
        private static List<string> filterKeyList = new List<string>();
        private static readonly object lockObj = new object();
        /// <summary>
        /// ��List Ҳ����Ϊ�ڴ��д�쳣���⣨���е�[]���飬���ƶ��������⣩
        /// </summary>
        internal static List<string> FilterKeyList
        {
            get
            {
                if (filterKeyList.Count == 0)
                {
                    lock (lockObj)
                    {
                        if (filterKeyList.Count == 0)
                        {
                            filterKeyList.AddRange(filterSqlInjection.TrimEnd(',').Split(','));
                        }
                    }
                }
                return filterKeyList;
            }
            set
            {
                filterKeyList = value;
            }
        }
        public static string Filter(string text, DataBaseType dalType)
        {
            if (string.IsNullOrEmpty(text) || text == "1=1") { return text; }
            try
            {

                if (text.IndexOf("--") > -1)
                {
                    string[] ts = text.Split(new string[] { "--" }, StringSplitOptions.None);
                    for (int i = 0; i < ts.Length - 1; i++)
                    {
                        if (ts[i].Split('\'').Length % 2 == (i == 0 ? 1 : 0))
                        {
                            text = text.Replace("--", string.Empty);//name like'% --aaa' --or name='--aa'  ǰ��� ' �ű����ǵ���
                            break;
                        }
                    }
                }

                string[] items = text.Split(' ', '(', ')');
                if (items.Length == 1 && text.Length > 30)
                {
                    if (text.IndexOf("%20") > -1 && text.IndexOf("%20") != text.LastIndexOf("%20"))
                    {
                        //���%20���
                        Log.Write("SqlInjection %20 Error:" + text, LogType.Warn);
                        Error.Throw("SqlInjection %20 Error:" + text);

                    }
                }
                else
                {
                    switch (dalType)
                    {
                        case DataBaseType.MySql:
                        case DataBaseType.Oracle:
                        case DataBaseType.SQLite:
                            for (int j = 0; j < items.Length; j++)//ȥ���ֶε�[�ֶ�]����������
                            {
                                if (!items[j].StartsWith("[#") && items[j].StartsWith("[") && items[j].EndsWith("]"))
                                {
                                    text = text.Replace(items[j], items[j].Replace("[", string.Empty).Replace("]", string.Empty));
                                }
                            }
                            break;
                    }
                }

                if (FilterKeyList.Count > 0 && filterKeyList.Count > 0)
                {
                    #region Filter Keys

                    string lowerText = text.ToLower();
                    items = lowerText.Split(' ', '(', ')', '/', ';', '=', '-', '\'', '|', '!', '%', '^');

                    int keyIndex = -1;
                    bool isOK = false;
                    for (int k = 0; k < filterKeyList.Count; k++)
                    {
                        if (k >= filterKeyList.Count)
                        {
                            Log.Write("No1:" + k + "," + filterKeyList.Count, LogType.Info);
                            continue;
                        }
                        string filterKeyValue = filterKeyList[k];
                        if (string.IsNullOrEmpty(filterKeyValue)) { continue; }

                        string[] filterSpitItems = filterKeyValue.Split(';');//�ָ�
                        if (filterSpitItems != null && filterSpitItems.Length > 0)
                        {
                            string filterKey = filterSpitItems[0];//ȡ��һ��Ϊ�ؼ���
                            if (filterSpitItems.Length > 2)
                            {
                                continue;
                            }
                            else if (filterSpitItems.Length == 2) // ����������ʵġ�
                            {
                                keyIndex = Math.Min(lowerText.IndexOf(filterKey), lowerText.IndexOf(filterSpitItems[1]));
                            }
                            else
                            {
                                keyIndex = lowerText.IndexOf(filterKey);//���˵Ĺؼ��ʻ����
                            }
                            if (keyIndex > -1)
                            {
                                foreach (string item in items) // �û���������ÿһ�������Ĵ�
                                {
                                    if (string.IsNullOrEmpty(item))
                                    {
                                        continue;
                                    }
                                    if (item.IndexOf(filterKey) > -1 && item.Length > filterKey.Length)
                                    {
                                        isOK = true;
                                        break;
                                    }
                                }
                                if (!isOK)
                                {
                                    Log.Write("SqlInjection FilterKey Error:" + filterKeyValue + ":" + text, LogType.Warn);
                                    Error.Throw("SqlInjection FilterKey Error:" + text);
                                }
                                else
                                {
                                    isOK = false;
                                }
                            }
                        }
                        else
                        {
                            Log.Write("No2:" + filterKeyValue, LogType.Info);
                        }
                    }
                    #endregion

                }
            }
            catch (Exception err)
            {
                Log.Write("SqlInjection Error:" + err.Message + ":" + text, LogType.Warn);
            }
            return text;

        }
    }
}
