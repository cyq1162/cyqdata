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
    internal partial class JsonSplit
    {
        /// <summary>
        /// 将json数组分成字符串List，【不支持[{xx:1},123]混合模式。】
        /// </summary>
        /// <param name="jsonArray">["a,","bbb,,"]</param>
        /// <returns></returns>
        internal static List<string> SplitEscapeArray(string jsonArray)
        {
            if (string.IsNullOrEmpty(jsonArray)) { return null; }
            jsonArray = jsonArray.Trim();
            if (jsonArray[0] != '[' || jsonArray[jsonArray.Length - 1] != ']') { return null; }
            jsonArray = jsonArray.Trim('[', ']');//["a,","bbb,,"]
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
