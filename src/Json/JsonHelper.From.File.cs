using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using CYQ.Data.Table;
using System.Data;
using System.Text.RegularExpressions;
using CYQ.Data.SQL;
using System.IO;
using System.Reflection;
using CYQ.Data.Xml;
using System.ComponentModel;
using CYQ.Data.Tool;

namespace CYQ.Data.Json
{
    public partial class JsonHelper
    {
        /// <summary>
        /// 读取文本中的Json（并去掉注释）
        /// </summary>
        /// <returns></returns>
        internal static string ReadJson(string filePath)
        {
            string json = IOHelper.ReadAllText(filePath);
            if (!string.IsNullOrEmpty(json))
            {
                int index = json.LastIndexOf("/*");
                if (index > -1)//去掉注释
                {
                    json = Regex.Replace(json, @"/\*[^:][.\s\S]*?\*/", string.Empty, RegexOptions.IgnoreCase);
                }
                char splitChar = '\n';
                if (json.IndexOf(splitChar) > -1)
                {
                    string[] items = json.Split(splitChar);
                    StringBuilder sb = new StringBuilder();
                    foreach (string item in items)
                    {
                        if (!item.TrimStart(' ', '\r', '\t').StartsWith("//"))
                        {
                            sb.Append(item.Trim(' ', '\r', '\t'));
                        }
                    }
                    json = sb.ToString();
                }
                if (json.IndexOf("\\\\") > -1)
                {
                    json = json.Replace("\\\\", "\\");
                }
            }
            return json;
        }
    }
}
