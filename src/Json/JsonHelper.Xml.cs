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
        #region Xml 转 Json

        /// <summary>
        /// 转Json
        /// <param name="xml">xml字符串</param>
        /// <param name="isWithAttr">是否将属性值也输出</param>
        /// </summary>
        private static string XmlToJson(string xml, bool isWithAttr)
        {
            using (XHtmlAction action = new XHtmlAction(false, true))
            {
                try
                {
                    action.LoadXml(xml);
                    return action.ToJson(action.XmlDoc.DocumentElement, isWithAttr);
                }
                catch (Exception err)
                {
                    Log.WriteLogToTxt(err, LogType.Error);
                    return string.Empty;
                }

            }
        }

        #endregion


        #region Json 转 Xml
        /// <summary>
        /// Convert json to Xml
        /// <para>将一个Json转成Xml</para>
        /// </summary>
        public static string ToXml(string json)
        {
            return ToXml(json, true);
        }

        public static string ToXml(string json, bool isWithAttr)
        {
            return ToXml(json, isWithAttr, EscapeOp.No);
        }
        public static string ToXml(string json, bool isWithAttr, EscapeOp op)
        {
            return ToXml(json, isWithAttr, op, null);
        }
        /// <param name="isWithAttr">default value is true
        /// <para>是否转成属性，默认true</para></param>
        public static string ToXml(string json, bool isWithAttr, EscapeOp op, string rootName)
        {
            if (!string.IsNullOrEmpty(rootName))
            {
                json = string.Format("{{\"{0}\":{1}}}", rootName, json);
            }
            StringBuilder xml = new StringBuilder();
            xml.Append("<?xml version=\"1.0\"  standalone=\"yes\"?>");
            List<Dictionary<string, string>> dicList = JsonSplit.Split(json, 0, op);
            if (dicList != null && dicList.Count > 0)
            {
                bool addRoot = dicList.Count > 1 || dicList[0].Count > 1;
                if (addRoot)
                {
                    xml.Append(string.Format("<{0}>", rootName ?? "root"));//</root>";
                }

                xml.Append(GetXmlList(dicList, isWithAttr, op));

                if (addRoot)
                {
                    xml.Append(string.Format("</{0}>", rootName ?? "root"));//</root>";
                }

            }
            return xml.ToString();
        }

        private static string GetXmlList(List<Dictionary<string, string>> dicList, bool isWithAttr, EscapeOp op)
        {
            if (dicList == null || dicList.Count == 0)
            {
                return string.Empty;
            }
            StringBuilder xml = new StringBuilder();
            for (int i = 0; i < dicList.Count; i++)
            {
                xml.Append(GetXml(dicList[i], isWithAttr, op));
            }
            return xml.ToString();
        }

        private static string GetXml(Dictionary<string, string> dic, bool isWithAttr, EscapeOp op)
        {
            StringBuilder xml = new StringBuilder();
            bool isJson = false;
            foreach (KeyValuePair<string, string> item in dic)
            {
                isJson = IsJson(item.Value);
                if (!isJson)
                {
                    xml.AppendFormat("<{0}>{1}</{0}>", item.Key, FormatCDATA(UnEscape(item.Value, op)));
                }
                else
                {
                    string key = item.Key;
                    if (key.EndsWith("List") && isWithAttr)
                    {
                        xml.AppendFormat("<{0}>", key);
                        key = key.Substring(0, key.Length - 4);
                    }
                    List<Dictionary<string, string>> jsonList = JsonSplit.Split(item.Value, 0, op);
                    if (jsonList != null && jsonList.Count > 0)
                    {
                        if (!isWithAttr)
                        {
                            xml.AppendFormat("<{0}>", item.Key);
                        }
                        for (int j = 0; j < jsonList.Count; j++)
                        {
                            if (isWithAttr)
                            {
                                xml.Append(GetXmlElement(key, jsonList[j], op));
                            }
                            else
                            {
                                xml.Append(GetXml(jsonList[j], isWithAttr, op));
                            }
                        }
                        if (!isWithAttr)
                        {
                            xml.AppendFormat("</{0}>", key);
                        }
                    }
                    else // 空Json {}
                    {
                        xml.AppendFormat("<{0}></{0}>", key);
                    }

                    if (item.Key.EndsWith("List") && isWithAttr)
                    {
                        xml.AppendFormat("</{0}>", item.Key);
                    }
                }
            }
            return xml.ToString();
        }

        private static string GetXmlElement(string parentName, Dictionary<string, string> dic, EscapeOp op)
        {
            StringBuilder xml = new StringBuilder();
            Dictionary<string, string> jsonDic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            xml.Append("<" + parentName);
            foreach (KeyValuePair<string, string> kv in dic)
            {
                if (kv.Value.IndexOf('"') > -1 || kv.Value.Length > 50
                    || kv.Key.Contains("Remark") || kv.Key.Contains("Description") || kv.Key.Contains("Rule")
                    || IsJson(kv.Value)) // 属性不能带双引号，所以转到元素处理。
                {
                    jsonDic.Add(kv.Key, kv.Value);
                }
            }
            //InnerText 节点存在=》（如果有元素节点，则当属性处理；若无，则当InnerText）
            bool useForInnerText = dic.ContainsKey(parentName) && jsonDic.Count == 0;
            foreach (KeyValuePair<string, string> kv in dic)
            {
                if (!jsonDic.ContainsKey(kv.Key) && (kv.Key != parentName || !useForInnerText))
                {
                    xml.AppendFormat(" {0}=\"{1}\"", kv.Key, kv.Value);//先忽略同名属性，内部InnerText节点，
                }
            }
            xml.Append(">");
            if (useForInnerText)
            {
                xml.Append(FormatCDATA(dic[parentName]));//InnerText。
            }
            else if (jsonDic.Count > 0)
            {
                xml.Append(GetXml(jsonDic, true, op));//数组，当元素处理。
            }
            xml.Append("</" + parentName + ">");

            return xml.ToString();
        }
        private static string FormatCDATA(string text)
        {
            if (text.LastIndexOfAny(new char[] { '<', '>', '&' }) > -1 && !text.StartsWith("<![CDATA["))
            {
                text = "<![CDATA[" + text.Trim() + "]]>";
            }
            return text;
        }
        #endregion
    }
}
