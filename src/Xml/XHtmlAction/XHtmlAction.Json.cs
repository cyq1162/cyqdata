using System;
using System.Xml;
using CYQ.Data.Table;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using CYQ.Data.Tool;
using System.Text;
using System.Web;
using System.Threading;
using CYQ.Data.Json;

namespace CYQ.Data.Xml
{
    /// <summary>
    /// Json 交互
    /// </summary>
    public partial class XHtmlAction
    {
        #region 转Json
        /// <summary>
        /// 转Json
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return ToJson(XmlDoc.DocumentElement, true);
        }
        /// <summary>
        /// 转Json
        /// <param name="parent">可设置一个节点（默认根节点）</param>
        /// <param name="isWithAttr">是否将属性值也输出</param>
        /// </summary>
        public string ToJson(XmlNode parent, bool isWithAttr)
        {
            parent = parent ?? XmlDoc.DocumentElement;
            if (parent == null)
            {
                return string.Empty;
            }
            JsonHelper js = new JsonHelper(false, false);
            //分解递归，不然真他妈不好调试

            js.Add(parent.Name, GetRootJson(parent, isWithAttr), true);
            js.AddBr();
            return js.ToString();
        }
        private string GetRootJson(XmlNode root, bool isWithAttr)
        {
            JsonHelper js = new JsonHelper(false, false);
            //js.Escape = EscapeOp.No;
            if (isWithAttr && root.Attributes != null && root.Attributes.Count > 0)
            {
                foreach (XmlAttribute item in root.Attributes)
                {
                    js.Add(item.Name, item.Value);
                }
            }
            if (root.HasChildNodes)
            {
                foreach (XmlNode item in root.ChildNodes)
                {
                    string childJson = GetChildJson(item, isWithAttr);
                    js.Add(item.Name, childJson, !string.IsNullOrEmpty(childJson) && (childJson[0] == '{' || childJson[0] == '['));
                }
            }
            string result = js.ToString();
            return result;
        }
        private string GetChildJson(XmlNode parent, bool isWithAttr)
        {
            JsonHelper js = new JsonHelper(false, false);
            //js.Escape = EscapeOp.No;
            if (isWithAttr && parent.Attributes != null && parent.Attributes.Count > 0)
            {
                foreach (XmlAttribute item in parent.Attributes)
                {
                    js.Add(item.Name, item.Value);
                }

            }
            if (parent.HasChildNodes)
            {
                XmlNode x0 = parent.ChildNodes[0];//XXList xx
                int childCount = parent.ChildNodes.Count;
                if (x0.NodeType != XmlNodeType.Element && childCount == 1)
                {
                    if (js.RowCount == 0)
                    {
                        return parent.InnerText;
                    }
                    else
                    {
                        js.Add(parent.Name, parent.InnerText);
                    }
                }
                else
                {
                    #region MyRegion


                    Dictionary<string, StringBuilder> jsonDic = new Dictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);
                    List<string> arrayName = new List<string>();
                    foreach (XmlNode item in parent.ChildNodes)
                    {
                        string childJson = GetChildJson(item, isWithAttr);
                        if (!jsonDic.ContainsKey(item.Name))
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append(childJson);
                            jsonDic.Add(item.Name, sb);
                        }
                        else // 重复的TagName
                        {
                            if (!arrayName.Contains(item.Name))
                            {
                                arrayName.Add(item.Name);
                            }
                            jsonDic[item.Name].Append("," + childJson);// = "[" + jsonDic[item.Name].TrimStart('[').TrimEnd(']') + "," + childJson + "]";
                        }
                    }
                    bool isList = parent.Name.EndsWith("List") && jsonDic.Count == 1;
                    string cName = parent.Name.Substring(0, parent.Name.Length - 4);
                    foreach (KeyValuePair<string, StringBuilder> kv in jsonDic)
                    {
                        string v = kv.Value.ToString();
                        if (v.Length > 0 && v[0] != '[' && (isList || arrayName.Contains(kv.Key)))
                        {
                            v = "[" + v + "]";
                        }
                        if (isList && kv.Key == cName)
                        {
                            return v;
                            // js.Add(parent.Name, v, true);
                        }
                        else
                        {
                            js.Add(kv.Key, v, !string.IsNullOrEmpty(v) && (v[0] == '{' || v[0] == '['));
                        }
                    }
                    jsonDic.Clear();
                    jsonDic = null;
                    arrayName.Clear();
                    arrayName = null;
                    #endregion
                }
            }

            js.AddBr();
            string result = js.ToString();
            if (result == "{}")
            {
                return "";
            }
            else
            {
                if (js.RowCount == 0)
                {
                    return "";
                    // return "fuck"; //卧槽，怎么有这样的代码？
                }
            }
            return result;
        }
        #endregion
    }
}
