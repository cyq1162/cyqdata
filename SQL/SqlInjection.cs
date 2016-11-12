using System;
using System.Collections.Generic;




namespace CYQ.Data.SQL
{
    internal static class SqlInjection
    {
        //select;from,
        internal const string filterSqlInjection = "select;into,delete;from,drop;table,drop;database,update;set,truncate;table,create;table,exists;select,insert;into,xp_cmdshell,declare;@,exec;master,waitfor;delay";
        //internal const string replaceSqlInjection = "--";
        private static List<string> filterKeyList = null;
        /// <summary>
        /// 用List 也是因为内存读写异常问题（所有的[]数组，看似都有这问题）
        /// </summary>
        internal static List<string> FilterKeyList
        {
            get
            {
                if (filterKeyList == null)
                {
                    filterKeyList = new List<string>();
                    filterKeyList.AddRange(filterSqlInjection.TrimEnd(',').Split(','));
                }
                return filterKeyList;
            }
            set
            {
                filterKeyList = value;
            }
        }
        public static string Filter(string text, DalType dalType)
        {
            string[] items = null;
            if (text.IndexOf("--") > -1)
            {
                items = text.Split(new string[] { "--" }, StringSplitOptions.None);
                for (int i = 0; i < items.Length - 1; i++)
                {
                    if (items[i].Split('\'').Length % 2 == (i == 0 ? 1 : 0))
                    {
                        text = text.Replace("--", string.Empty);//name like'% --aaa' --or name='--aa'  前面的 ' 号必须是单数
                        break;
                    }
                }
                items = null;
            }
            //foreach (string item in replaceSqlInjection.Split(','))
            //{
            //    text = text.Replace(item, string.Empty);
            //}
            //text = text.Replace("--", "").Replace(";", "").Replace("&", "").Replace("*", "").Replace("||", "");
            items = text.Split(' ', '(', ')');
            if (items.Length == 1 && text.Length > 30)
            {
                if (text.IndexOf("%20") > -1)
                {
                    Log.WriteLog(true, text);//记录日志
                    return "SqlInjection error:" + text;
                }
            }
            else
            {
                switch (dalType)
                {
                    case DalType.MySql:
                    case DalType.Oracle:
                    case DalType.SQLite:
                        for (int i = 0; i < items.Length; i++)//去掉字段的[字段]，两个符号
                        {
                            if (!items[i].StartsWith("[#") && items[i].StartsWith("[") && items[i].EndsWith("]"))
                            {
                                text = text.Replace(items[i], items[i].Replace("[", string.Empty).Replace("]", string.Empty));
                            }
                        }
                        break;
                }
            }
            string lowerText = text.ToLower();
            items = lowerText.Split(' ', '(', ')');

            int keyIndex = -1;
            bool isOK = false;
            string tempKey = string.Empty;
            string filterKey = string.Empty;
            string[] filterSpitItems = null;
            for (int i = 0; i < FilterKeyList.Count; i++)
            {
                filterSpitItems = filterKeyList[i].Split(';');//分隔
                filterKey = filterSpitItems[0];//取第一个为关键词
                if (filterSpitItems.Length > 2)
                {
                    continue;
                }
                else if (filterSpitItems.Length == 2) // 如果是两个词的。
                {
                    keyIndex = Math.Min(lowerText.IndexOf(filterKey), lowerText.IndexOf(filterSpitItems[1]));
                }
                else
                {
                    keyIndex = lowerText.IndexOf(filterKey);//过滤的关键词或词组
                }
                if (keyIndex > -1)
                {
                    foreach (string item in items) // 用户传进来的每一个单独的词
                    {
                        tempKey = item.Trim('\'', '|', '!', '%', '^');
                        if (tempKey.IndexOf(filterKey) > -1 && tempKey.Length > filterKey.Length)
                        {
                            isOK = true;
                            break;
                        }
                    }
                    if (!isOK)
                    {
                        Log.WriteLog(true, FilterKeyList[i] + ":" + text);//记录日志
                        return "SqlInjection error:" + text;
                    }
                    else
                    {
                        isOK = false;
                    }
                }
            }
            return text;

        }
    }
}
