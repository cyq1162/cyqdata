using System;
using System.Xml;
using CYQ.Data.Table;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using CYQ.Data.Tool;
using System.Text;
using System.Web;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;

namespace CYQ.Data.Xml
{
    /// <summary>
    /// Xml/Html操作类
    /// </summary>
    public partial class XHtmlAction : XHtmlBase
    {
        #region 构造函数
        /// <summary>
        /// 默认构造函数[操作无名称空间的Xml]
        /// </summary>
        public XHtmlAction()
            : base()
        {

        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="isForHtml">true时，将自动载入html的名称空间(http://www.w3.org/1999/xhtml)</param>
        public XHtmlAction(bool isForHtml)
            : base()
        {
            if (isForHtml)
            {
                base.LoadNameSpace(htmlNameSpace);
            }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="isForHtml">true时，将自动载入html的名称空间(http://www.w3.org/1999/xhtml)</param>
        /// <param name="isNoClone">true时文档应为只读，所获取是同一份文档引用；false时文档可写，每次获取会克隆一份文档返回。</param>
        public XHtmlAction(bool isForHtml, bool isNoClone)
            : base()
        {
            if (isForHtml)
            {
                base.LoadNameSpace(htmlNameSpace);
            }
            IsNoClone = isNoClone;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="nameSpaceUrl">当Xml的名称空间[若有]</param>
        public XHtmlAction(string nameSpaceUrl)
            : base()
        {
            base.LoadNameSpace(nameSpaceUrl);
        }
        #endregion

        #region 查询
        /// <summary>
        /// GetByID or GetByName
        /// </summary>
        /// <param name="idOrName">id or name</param>
        /// <returns></returns>
        public XmlNode Get(string idOrName)
        {
            return Get(idOrName, null);
        }
        public XmlNode Get(string idOrName, XmlNode parentNode)
        {
            XmlNode node = GetByID(idOrName, parentNode);
            if (node == null)
            {
                node = GetByName(idOrName, parentNode);
                if (node == null)
                {
                    switch (idOrName.ToLower())
                    {
                        case "head":
                        case "body":
                        case "title":
                        case "form":
                        case "style":
                        case "meta":
                        case "link":
                        case "script":
                            XmlNodeList xList = GetList(idOrName.ToLower(), parentNode);
                            if (xList != null)
                            {
                                node = xList[0];
                            }
                            break;
                    }
                }
            }
            return node;
        }
        public XmlNode GetByID(string id)
        {
            return Fill(GetXPath("*", "id", id), null);
        }
        public XmlNode GetByID(string id, XmlNode parentNode)
        {
            return Fill(GetXPath("*", "id", id), parentNode);
        }
        public XmlNode GetByName(string name)
        {
            return Fill(GetXPath("*", "name", name), null);
        }
        public XmlNode GetByName(string name, XmlNode parentNode)
        {
            return Fill(GetXPath("*", "name", name), parentNode);
        }

        public XmlNode Get(string tag, string attr, string value, XmlNode parentNode)
        {
            return Fill(GetXPath(tag, attr, value), parentNode);
        }

        public XmlNodeList GetList(string tag, string attr, string value)
        {
            return Select(GetXPath(tag, attr, value), null);
        }
        public XmlNodeList GetList(string tag, string attr, string value, XmlNode parentNode)
        {
            return Select(GetXPath(tag, attr, value), parentNode);
        }
        public XmlNodeList GetList(string tag, string attr)
        {
            return Select(GetXPath(tag, attr, null), null);
        }
        public XmlNodeList GetList(string tag, string attr, XmlNode parentNode)
        {
            return Select(GetXPath(tag, attr, null), parentNode);
        }
        public XmlNodeList GetList(string tag)
        {
            return Select(GetXPath(tag, null, null), null);
        }
        public XmlNodeList GetList(string tag, XmlNode parentNode)
        {
            return Select(GetXPath(tag, null, null), parentNode);
        }
        #endregion

        #region 创建
        public void CreateNodeTo(XmlNode parentNode, string tag, string text, params string[] attrAndValue)
        {
            if (parentNode != null)
            {
                parentNode.AppendChild(CreateNode(tag, text, attrAndValue));
            }
        }
        public XmlNode CreateNode(string tag, string text, params string[] attrAndValue)
        {
            XmlElement xElement = Create(tag);
            try
            {
                xElement.InnerXml = text;
            }
            catch
            {
                xElement.InnerXml = SetCDATA(text);
            }
            if (attrAndValue != null && attrAndValue.Length % 2 == 0)
            {
                for (int i = 0; i < attrAndValue.Length; i++)
                {
                    string attr = attrAndValue[i];
                    i++;
                    string value = attrAndValue[i];
                    xElement.SetAttribute(attr, value);
                }
            }
            return xElement as XmlNode;
        }
        #endregion

        #region 附加
        public void AppendNode(XmlNode parentNode, XmlNode childNode)
        {
            if (parentNode != null && childNode != null)
            {
                parentNode.AppendChild(childNode);
            }
        }
        /// <summary>
        /// 添加节点
        /// </summary>
        /// <param name="position">parentNode的第N个子节点之后</param>
        public void AppendNode(XmlNode parentNode, XmlNode childNode, int position)
        {
            if (parentNode != null && childNode != null)// A B
            {
                if (parentNode.ChildNodes.Count == 0 || position >= parentNode.ChildNodes.Count)
                {
                    parentNode.AppendChild(childNode);
                }
                else if (position == 0)
                {
                    InsertBefore(childNode, parentNode.ChildNodes[0]);
                }
                else
                {
                    InsertAfter(childNode, parentNode.ChildNodes[position - 1]);
                }
            }
        }

        #endregion

        #region 删除
        /// <summary>
        /// 保留节点,但清除节点所内容/属性
        /// </summary>
        public void Clear(XmlNode node)
        {
            node.RemoveAll();
        }
        public void Remove(XmlNode node)
        {
            if (node != null)
            {
                node.ParentNode.RemoveChild(node);
            }
        }
        public void Remove(string idOrName)
        {
            XmlNode node = Get(idOrName);
            if (node != null)
            {
                node.ParentNode.RemoveChild(node);
            }
        }
        public void RemoveAllChild(XmlNode node)
        {
            RemoveChild(node, 0);
        }
        public void RemoveAllChild(string idOrName)
        {
            RemoveChild(idOrName, 0);
        }
        /// <summary>
        /// 移除子节点
        /// </summary>
        /// <param name="id">节点的id</param>
        /// <param name="start">从第几个子节点开始删除[索引从0开始]</param>
        public void RemoveChild(string idOrName, int start)
        {
            XmlNode node = Get(idOrName);
            if (node != null)
            {
                RemoveChild(node, start);
            }
        }
        /// <summary>
        /// 移除子节点
        /// </summary>
        /// <param name="node">节点</param>
        /// <param name="start">从第几个子节点开始删除[索引从0开始]</param>
        public void RemoveChild(XmlNode node, int start)
        {
            if (start == 0)
            {
                node.InnerXml = "";
                return;
            }
            if (node.ChildNodes.Count > start) //1个子节点, 0
            {
                for (int i = node.ChildNodes.Count - 1; i >= start; i--)
                {
                    node.RemoveChild(node.ChildNodes[i]);
                }
            }
        }
        /// <summary>
        /// 移除多个属性
        /// </summary>
        /// <param name="ids">要移除的名称列表</param>
        public void RemoveAttrList(params string[] attrNames)
        {
            XmlNodeList nodeList = null;
            foreach (string name in attrNames)
            {
                nodeList = GetList("*", name);
                if (nodeList != null && nodeList.Count > 0)
                {
                    for (int i = 0; i < nodeList.Count; i++)
                    {
                        nodeList[i].Attributes.Remove(nodeList[i].Attributes[name]);
                    }
                }
            }
        }
        /// <summary>
        /// 属性移除
        /// </summary>
        /// <param name="attrName">属性名称</param>
        /// <param name="excludeSetType">排除的节点类型</param>
        public void RemoveAttrList(string attrName, SetType excludeSetType)
        {
            XmlNodeList nodeList = GetList("*", attrName);
            if (nodeList != null && nodeList.Count > 0)
            {
                XmlNode node = null;
                string setType = excludeSetType.ToString().ToLower();
                for (int i = 0; i < nodeList.Count; i++)
                {
                    node = nodeList[i];
                    if (node.Name != setType)
                    {
                        node.Attributes.Remove(node.Attributes[attrName]);
                    }
                }
            }
        }
        /// <summary>
        /// 移除注释节点
        /// </summary>
        /// <param name="node">移除此节点的注释文本</param>
        public void RemoveCommentNode(XmlNode node)
        {
            if (node != null)
            {
                XmlNodeList xmlNodeList = Select("//comment()", node);

                foreach (XmlNode xNode in xmlNodeList)
                {
                    xNode.ParentNode.RemoveChild(xNode);
                }

            }
        }
        /// <summary>
        /// 移除注释节点
        /// </summary>
        public override void RemoveCommentNode()
        {
            RemoveCommentNode(XmlDoc.DocumentElement);
        }
        #endregion

        #region 其它交换节点/插入节点

        /// <summary>
        /// 两个节点交换位置
        /// </summary>
        /// <param name="XNodeFirst">第一个节点</param>
        /// <param name="XNodeLast">第二个节点</param>
        public void InterChange(XmlNode xNodeFirst, XmlNode xNodeLast)
        {
            if (xNodeFirst != null && xNodeLast != null)
            {
                if (xNodeFirst.ParentNode != null && xNodeLast.ParentNode != null)
                {
                    xNodeFirst.ParentNode.ReplaceChild(xNodeLast.Clone(), xNodeFirst);
                    xNodeLast.ParentNode.ReplaceChild(xNodeFirst.Clone(), xNodeLast);
                }
                else
                {
                    _XmlDocument.DocumentElement.ReplaceChild(xNodeLast.Clone(), xNodeFirst);
                    _XmlDocument.DocumentElement.ReplaceChild(xNodeFirst.Clone(), xNodeLast);
                }
            }
        }
        public void ReplaceNode(XmlNode newNode, string oldNodeIDorName)
        {
            ReplaceNode(newNode, Get(oldNodeIDorName));
        }
        /// <summary>
        /// 节点替换[支持两个的文档间替换]
        /// </summary>
        /// <param name="NewXNode"></param>
        /// <param name="OldXNode"></param>
        public void ReplaceNode(XmlNode newNode, XmlNode oldNode)
        {
            if (newNode != null && oldNode != null)
            {
                if (newNode.Name == oldNode.Name) // 节点名相同。
                {
                    oldNode.RemoveAll();//清空旧节点
                    oldNode.InnerXml = newNode.InnerXml;
                    XmlAttributeCollection attrs = newNode.Attributes;//设置属性
                    if (attrs != null && attrs.Count > 0)
                    {
                        for (int i = 0; i < attrs.Count; i++)
                        {
                            ((XmlElement)oldNode).SetAttribute(attrs[i].Name, attrs[i].Value);
                        }
                    }
                }
                else
                {
                    XmlNode xNode = CreateNode(newNode.Name, newNode.InnerXml);//先创建一个节点。
                    XmlAttributeCollection attrs = newNode.Attributes;
                    if (attrs != null && attrs.Count > 0)
                    {
                        for (int i = 0; i < attrs.Count; i++)
                        {
                            ((XmlElement)xNode).SetAttribute(attrs[i].Name, attrs[i].Value);
                        }
                    }
                    oldNode.ParentNode.InsertAfter(xNode, oldNode);//挂在旧节点后面。
                    Remove(oldNode);
                }

            }
        }
        /// <summary>
        /// 节点之后插入[支持两文档之间的插入]
        /// </summary>
        /// <param name="NewNode">要被插入的新节点</param>
        /// <param name="RefNode">在此节点后插入NewNode节点</param>
        public void InsertAfter(XmlNode newNode, XmlNode refNode)
        {
            XmlNode xDocNode = CreateNode(newNode.Name, "");
            ReplaceNode(newNode, xDocNode);
            refNode.ParentNode.InsertAfter(xDocNode, refNode);
        }
        /// <summary>
        /// 节点之前插入[支持两文档之间的插入]
        /// </summary>
        /// <param name="NewNode">要被插入的新节点</param>
        /// <param name="RefNode">在此节点前插入NewNode节点</param>
        public void InsertBefore(XmlNode newNode, XmlNode refNode)
        {
            XmlNode xDocNode = CreateNode(newNode.Name, "");
            ReplaceNode(newNode, xDocNode);
            refNode.ParentNode.InsertBefore(xDocNode, refNode);
        }
        #endregion

        #region 节点判断
        public bool Contains(string idOrName)
        {
            return Get(idOrName) != null;
        }
        public bool Contains(string idOrName, XmlNode parentNode)
        {
            return Get(idOrName, parentNode) != null;
        }
        #endregion

        #region 属性判断/取值

        public bool HasAttr(string idOrName, string attrName)
        {
            return GetAttrValue(idOrName, attrName) != string.Empty;
        }
        public bool HasAttr(XmlNode node, string attrName)
        {
            return GetAttrValue(node, attrName) != string.Empty;
        }
        public string GetAttrValue(string idOrName, string attrName, params string[] defaultValue)
        {
            XmlNode node = Get(idOrName);
            return GetAttrValue(node, attrName, defaultValue);
        }
        public string GetAttrValue(XmlNode node, string attrName, params string[] defaultValue)
        {
            if (node != null)
            {
                switch (attrName)
                {
                    case "InnerText":
                        if (!string.IsNullOrEmpty(node.InnerText))
                        {
                            return node.InnerText;
                        }
                        break;
                    case "InnerXml":
                        if (!string.IsNullOrEmpty(node.InnerXml))
                        {
                            return node.InnerXml;
                        }
                        break;
                    default:
                        if (node.Attributes != null && node.Attributes[attrName] != null)
                        {
                            return node.Attributes[attrName].Value;
                        }
                        break;
                }
            }
            if (defaultValue.Length > 0)
            {
                return defaultValue[0];
            }
            return string.Empty;
        }
        public void RemoveAttr(string idOrName, params string[] attrNames)
        {
            XmlNode node = Get(idOrName);
            RemoveAttr(node, attrNames);
        }
        public void RemoveAttr(XmlNode node, params string[] attrNames)
        {
            if (node != null && node.Attributes != null)
            {
                foreach (string name in attrNames)
                {
                    if (node.Attributes[name] != null)
                    {
                        node.Attributes.Remove(node.Attributes[name]);
                    }
                }

            }
        }

        #endregion

        #region 操作数据

        private bool _IsCurrentLang = true;
        /// <summary>
        /// 当前请求是否用户当前设置的语言
        /// </summary>
        public bool IsCurrentLang
        {
            get
            {
                return _IsCurrentLang;
            }
            set
            {
                _IsCurrentLang = value;
            }
        }
        /// <summary>
        /// 是否开始自定义语言分隔(分隔符号为：[#langsplit])
        /// </summary>
        public bool IsUseLangSplit = true;

        private string SetValue(string sourceValue, string newValue, bool addCData)
        {
            if (string.IsNullOrEmpty(newValue))
            {
                return sourceValue;
            }
            newValue = newValue.Replace(ValueReplace.Source, sourceValue);
            if (IsUseLangSplit)
            {
                int split = newValue.IndexOf(ValueReplace.LangSplit);
                if (split > -1)
                {
                    newValue = _IsCurrentLang ? newValue.Substring(0, split) : newValue.Substring(split + ValueReplace.LangSplit.Length);
                }
            }
            if (addCData)
            {
                newValue = SetCDATA(newValue);
            }
            return newValue;
        }
        private void SetAttrValue(XmlNode node, string key, string value)
        {
            if (node == null || node.Attributes == null)
            {
                return;
            }
            if (node.Attributes[key] == null)
            {
                XmlAttribute attr = _XmlDocument.CreateAttribute(key);
                node.Attributes.Append(attr);
            }
            value = SetValue(node.Attributes[key].InnerXml, value, false);
            try
            {
                node.Attributes[key].Value = value;
            }
            catch
            {
                node.Attributes[key].Value = SetCDATA(value);
            }
        }
        /// <summary>
        /// 为节点赋值[通常值是在values中赋值]
        /// </summary>
        public void Set(XmlNode node, SetType setType, params string[] values)
        {

            if (node != null && values != null)
            {
                switch (setType)
                {
                    case SetType.InnerText:
                        string value = SetValue(node.InnerText, values[0], false);
                        try
                        {
                            node.InnerText = value;
                        }
                        catch
                        {
                            node.InnerText = SetCDATA(value);
                        }
                        break;
                    case SetType.InnerXml:
                        node.InnerXml = SetValue(node.InnerXml, values[0], true);
                        break;
                    case SetType.Value:
                    case SetType.Href:
                    case SetType.Src:
                    case SetType.Class:
                    case SetType.Disabled:
                    case SetType.ID:
                    case SetType.Name:
                    case SetType.Visible:
                    case SetType.Title:
                    case SetType.Style:
                        string key = setType.ToString().ToLower();
                        SetAttrValue(node, key, values[0]);
                        break;
                    case SetType.Custom:
                        for (int i = 0; i < values.Length; i++)
                        {
                            if (i > 0 && i % 2 == 1)
                            {
                                key = values[i - 1].ToLower();
                                switch (key)
                                {
                                    case "innertext":
                                        Set(node, SetType.InnerText, values[i]);
                                        break;
                                    case "innerhtml":
                                    case "innerxml":
                                        Set(node, SetType.InnerXml, values[i]);
                                        break;
                                    default:
                                        SetAttrValue(node, key, values[i]);
                                        break;
                                }
                            }
                        }

                        break;
                    case SetType.A:
                        node.InnerXml = SetValue(node.InnerXml, values[0], true);
                        if (values.Length > 1)
                        {
                            SetAttrValue(node, "href", values[1]);
                            if (values.Length > 2)
                            {
                                SetAttrValue(node, "title", values[2]);
                                if (values.Length > 3)
                                {
                                    SetAttrValue(node, "target", values[3]);
                                }
                            }
                        }
                        break;
                    case SetType.Select:
                        if (node.InnerXml.Contains(AppConfig.XHtml.CDataLeft))//带特殊字符
                        {
                            string innerHtml = node.InnerXml.Replace(string.Format("value=\"{0}\"", values[0]), string.Format("selected=\"selected\" value=\"{0}\"", values[0]));
                            try
                            {
                                node.InnerXml = innerHtml;
                            }
                            catch
                            {
                                node.InnerXml = SetCDATA(innerHtml);
                            }
                        }
                        else
                        {
                            string lowerValue = values[0].ToLower();
                            foreach (XmlNode option in node.ChildNodes)
                            {
                                string opValue = option.InnerText.ToLower();
                                if (option.Attributes["value"] != null)
                                {
                                    opValue = option.Attributes["value"].Value.Split(',')[0].ToLower();
                                }
                                if (opValue == lowerValue || opValue == (lowerValue == "true" ? "1" : (lowerValue == "false" ? "0" : lowerValue)))
                                {
                                    SetAttrValue(option, "selected", "selected");
                                    break;
                                }


                            }
                        }
                        break;
                    case SetType.Checked:
                        if (node.Name == "input" && node.Attributes["type"].Value == "radio")
                        {
                            values[0] = "1";
                        }
                        switch (values[0].ToLower())
                        {
                            case "1":
                            case "true":
                            case "check":
                            case "checked":
                                key = setType.ToString().ToLower();
                                SetAttrValue(node, key, key);
                                break;
                        }
                        break;
                }
            }
        }
        public void Set(string idOrName, SetType setType, params string[] values)
        {
            Set(null, idOrName, setType, values);
        }
        public void Set(XmlNode parentNode, string idOrName, SetType setType, params string[] values)
        {
            XmlNode node = Get(idOrName, parentNode);
            Set(node, setType, values);
        }
        public void Set(string idOrName, string value)
        {
            Set(null, idOrName, value);
        }
        /// <summary>
        /// 对节点赋值（此方法会忽略hidden隐藏域，隐藏节点赋值请用其它重载方法）
        /// </summary>
        /// <param name="idOrName"></param>
        /// <param name="value"></param>
        public void Set(XmlNode parentNode, string idOrName, string value)
        {
            XmlNode node = Get(idOrName, parentNode);
            if (node != null)
            {
                SetType setType = SetType.InnerXml;
                switch (node.Name)
                {
                    case "input":
                        switch (GetAttrValue(node, "type"))
                        {
                            case "hidden":
                                return;//此方法不对隐藏域处理。
                            case "checkbox":
                                setType = SetType.Checked; break;
                            case "image":
                                setType = SetType.Src; break;
                            case "radio"://情况复杂一点
                                XmlNodeList nodeList = GetList("input", "type", "radio");
                                for (int i = 0; i < nodeList.Count; i++)
                                {
                                    if (GetAttrValue(nodeList[i], "name") == idOrName)
                                    {
                                        RemoveAttr(nodeList[i], "checked");
                                        if (GetAttrValue(nodeList[i], "value") == value)
                                        {
                                            node = nodeList[i];
                                        }
                                    }
                                }
                                setType = SetType.Checked; break;
                            default:
                                setType = SetType.Value;
                                break;

                        }
                        break;
                    case "select":
                        setType = SetType.Select; break;
                    case "a":
                        setType = SetType.Href; break;
                    case "img":
                        setType = SetType.Src; break;
                }
                //try
                //{
                Set(node, setType, value);
                //}
                //catch (Exception err)
                //{

                //    throw;
                //}


            }
        }

        #endregion

        #region 重写最终输出OutXml
        public override string OutXml
        {
            get
            {
                if (_XmlDocument != null)
                {
                    #region 处理clearflag标签
                    string key = "clearflag";
                    XmlNodeList xnl = GetList("*", key);
                    if (xnl != null)
                    {
                        XmlNode xNode = null;
                        for (int i = xnl.Count - 1; i >= 0; i--)
                        {
                            xNode = xnl[i];
                            switch (GetAttrValue(xnl[i], key))
                            {
                                case "0":
                                    RemoveAttr(xNode, key);
                                    xNode.InnerXml = "";
                                    break;
                                case "1":
                                    Remove(xNode);
                                    break;
                            }

                        }
                    }
                    #endregion

                    #region 处理XHtml 头前缀、清空CData

                    string xml = _XmlDocument.InnerXml.Replace(".dtd\"[]>", ".dtd\">");
                    if (xml.IndexOf(" xmlns=") > -1)
                    {
                        xml = xml.Replace(" xmlns=\"\"", string.Empty).Replace(" xmlns=\"" + xnm.LookupNamespace(PreXml) + "\"", string.Empty);
                    }
                    string html = ClearCDATA(xml);
                    if (!string.IsNullOrEmpty(docTypeHtml))
                    {
                        html = html.Replace(docTypeHtml, "<!DOCTYPE html>");
                    }
                    html = html.Replace("&gt;", ">").Replace("&lt;", "<").Replace("&amp;", "&");//html标签符号。
                    #endregion

                    #region 处理剩余的占位符替换
                    if (html.IndexOf("${") > -1 || html.IndexOf("<%#") > -1)
                    {
                        html = FormatHtml(html, null);
                    }
                    #endregion

                    return html;
                }
                return string.Empty;
            }
        }
        /// <summary>
        /// 对Html的内容，进行标签替换【1次替换】
        /// </summary>
        /// <param name="html">需要被替换的内容</param>
        /// <param name="values">用于值替换搜索的字典数据</param>
        /// <returns></returns>
        private string FormatHtml(string html, MDictionary<string, string> values)
        {

            if (html.IndexOf("${") > -1)
            {
                #region 替换占位符号
                MatchCollection matchs = Regex.Matches(html, @"\$\{([\S\s]*?)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                if (matchs != null && matchs.Count > 0)
                {
                    //过滤重复的占位符。
                    List<string> keys = new List<string>(matchs.Count);
                    foreach (Match match in matchs)
                    {
                        //原始的占位符 ${txt#name:xx#xx}
                        string value = match.Groups[0].Value;//${txt#name:xx#xx}，${'aaa'+txt#name:xx#xx+'bbb'}
                        if (string.IsNullOrEmpty(value) || keys.Contains(value))
                        {
                            continue;
                        }
                        keys.Add(value);

                        #region 分解占位符
                        //先处理+号 ${'aaa'+txt#name:xx#xx+'bbb'} formatter= 'aaa'{0}
                        string formatter = "", key = "";
                        string[] items = match.Groups[1].Value.Trim().Split('+');//['aa',txt#name]
                        foreach (string item in items)
                        {
                            if (item[0] == '"')
                            {
                                formatter += item.Trim('"');
                            }
                            else if (item[0] == '\'')
                            {
                                formatter += item.Trim('\'');
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(key))
                                {
                                    break;//只允许存在一个key : txt#name:这是描述说明。
                                }
                                formatter += "{0}";
                                key = item.Trim();
                            }
                        }
                        string columnName = key.Split(':')[0];//name
                        #endregion

                        string replaceValue = "";

                        #region 获取占位符的值
                        if (values != null)
                        {
                            replaceValue = GetValue1(columnName, values);
                        }
                        if (string.IsNullOrEmpty(replaceValue))
                        {
                            replaceValue = GetValueByKeyValue2(columnName);
                        }
                        if (string.IsNullOrEmpty(replaceValue))
                        {
                            replaceValue = GetValueByRequest3(columnName, key);
                        }
                        #endregion

                        html = html.Replace(value, string.IsNullOrEmpty(replaceValue) ? "" : string.Format(formatter, replaceValue));

                    }
                    keys.Clear();
                    keys = null;
                    matchs = null;
                }
                #endregion
            }
            if (html.IndexOf("<%#") > -1)//js eval 执行
            {
                #region 替换JavaScript语法
                MatchCollection matchs = Regex.Matches(html, @"<%#([\S\s]*?)%>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                if (matchs != null && matchs.Count > 0)
                {
                    foreach (Match match in matchs)
                    {
                        //原始的占位符
                        string value = match.Groups[0].Value;//${txt#name:xx#xx}，${'aaa'+txt#name:xx#xx+'bbb'}
                        string value1 = match.Groups[1].Value;
                        string evalValue = null;
                        try
                        {
                            evalValue = Convert.ToString(Microsoft.JScript.Eval.JScriptEvaluate(value1, Microsoft.JScript.Vsa.VsaEngine.CreateEngine()));
                        }
                        catch (Exception err)
                        {
                            Log.WriteLogToTxt(err);
                        }
                        html = html.Replace(value, evalValue);
                    }
                }
                #endregion
            }

            return html;
        }
        private string GetValue1(string columnName, MDictionary<string, string> values)//, MDataRow row
        {
            if (values.ContainsKey(columnName))//值可能被格式化过，所以优先取值。
            {
                return values[columnName];
            }
            else if (columnName.Length < 3)
            {
                int i = 0;
                if (int.TryParse(columnName, out i) && i < values.Count)
                {
                    return values[i]; //数字
                }
            }
            //else
            //{
            //    MDataCell matchCell = row[columnName];
            //    if (matchCell != null)
            //    {
            //        return matchCell.ToString();
            //    }
            //}
            return string.Empty;
        }
        private string GetValueByKeyValue2(string columnName)
        {
            //处理keyValue替换
            if (KeyValue.Count > 0 && KeyValue.ContainsKey(columnName))
            {
                return KeyValue[columnName];
            }
            return string.Empty;
        }

        private string GetValueByRequest3(string columnName, string key)
        {
            string replaceValue = string.Empty;
            if (HttpContext.Current != null && HttpContext.Current.Handler != null)
            {
                replaceValue = HttpContext.Current.Request[columnName] ?? HttpContext.Current.Request.Headers[columnName];
            }
            if (string.IsNullOrEmpty(replaceValue) && key.Contains(":"))//如果为空，设置默认的说明。
            {
                replaceValue = key.Substring(key.LastIndexOf(':') + 1);
            }
            return replaceValue;
        }
        #endregion
    }

    //扩展交互
    public partial class XHtmlAction
    {
        #region 操作数据
        MDataRow _Row;

        #region 加载表格循环方式

        public delegate string SetForeachEventHandler(string text, MDictionary<string, string> values, int rowIndex);
        /// <summary>
        /// 对于SetForeach函数调用的格式化事件
        /// </summary>
        public event SetForeachEventHandler OnForeach;
        public void SetForeach(MDataTable dataSource)
        {
            if (dataSource != null)
            {
                XmlNode node = Get(dataSource.TableName + "View");
                if (node == null)
                {
                    node = Get("defaultView");
                }
                if (node != null)
                {
                    SetForeach(dataSource, node, node.InnerXml, OnForeach);
                }
            }
        }
        public void SetForeach(MDataTable dataSource, string idOrName)
        {
            XmlNode node = Get(idOrName);
            SetForeach(dataSource, node, node.InnerXml, OnForeach);
        }
        public void SetForeach(MDataTable dataSource, string idOrName, string text)
        {
            XmlNode node = Get(idOrName);
            SetForeach(dataSource, node, text, OnForeach);
        }

        public void SetForeach(MDataTable dataSource, string idOrName, SetType setType)
        {
            string text = string.Empty;
            XmlNode node = Get(idOrName);
            if (node == null)
            {
                return;
            }
            switch (setType)
            {
                case SetType.InnerText:
                    text = node.InnerText;
                    break;
                case SetType.InnerXml:
                    text = node.InnerXml;
                    break;
                case SetType.Value:
                case SetType.Href:
                case SetType.Src:
                case SetType.Class:
                case SetType.Disabled:
                case SetType.ID:
                case SetType.Name:
                case SetType.Visible:
                case SetType.Title:
                case SetType.Style:
                    string key = setType.ToString().ToLower();
                    if (node.Attributes[key] != null)
                    {
                        text = node.Attributes[key].Value;
                    }
                    break;
            }
            SetForeach(dataSource, node, text, OnForeach);
        }

        public void SetForeach(MDataTable dataSource, XmlNode node)
        {
            if (node == null) { return; }
            SetForeach(dataSource, node, node.InnerXml, OnForeach);
        }
        public void SetForeach(MDataTable dataSource, XmlNode node, string text)
        {
            if (node == null) { return; }
            SetForeach(dataSource, node, text, OnForeach);
        }
        /// <summary>
        /// 对列表进行循环绑定处理
        /// </summary>
        /// <param name="dataSource">数据源</param>
        /// <param name="node">处理的节点</param>
        /// <param name="text">用于循环的内容【通常传递node.InnerXml】</param>
        /// <param name="eventOnForeach">自定义事件</param>
        public void SetForeach(MDataTable dataSource, XmlNode node, string text, SetForeachEventHandler eventOnForeach)
        {
            try
            {
                #region 前置条件处理
                if (node == null || string.IsNullOrEmpty(text))
                {
                    return;
                }
                if (dataSource == null || dataSource.Rows.Count == 0 || dataSource.Columns.Count == 0)
                {
                    if (node.Attributes["clearflag"] == null)
                    {
                        node.InnerXml = "";
                    }
                    return;
                }
                RemoveAttr(node, "clearflag");
                #endregion

                StringBuilder innerXml = new StringBuilder();

                for (int k = 0; k < dataSource.Rows.Count; k++)
                {
                    string newText = text;
                    MDictionary<string, string> values = dataSource.Rows[k].ToEntity<MDictionary<string, string>>();  
                    if (eventOnForeach != null)
                    {
                        newText = eventOnForeach(text, values, k);//遍历每一行，产生新text。
                    }
                    try
                    {
                        if (newText.IndexOf("${") > -1 || newText.IndexOf("<%#") > -1)
                        {
                            newText = FormatHtml(newText, values);
                        }
                        innerXml.Append(newText);
                    }
                    catch (Exception err)
                    {
                        Log.Write(err, LogType.Error);
                    }
                    finally
                    {
                        values.Clear();
                    }
                }
                try
                {
                    node.InnerXml = innerXml.ToString();
                }
                catch
                {
                    try
                    {
                        node.InnerXml = SetCDATA(innerXml.ToString());
                    }
                    catch (Exception err)
                    {
                        Log.Write(err, LogType.Error);
                    }
                }

            }
            finally
            {
                if (eventOnForeach != null)
                {
                    eventOnForeach = null;
                }
            }
        }
        #endregion

        #region 加载行数据后操作方式
        /// <summary>
        /// 用于替换占位符的数据。
        /// </summary>
        public MDictionary<string, string> KeyValue = new MDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 加载MDatarow行数据（CMS替换）
        /// </summary>
        /// <param name="pre">为所有字段名指定前缀（如："a.",或空前缀：""</param>
        public void LoadData(object anyObjToRow, string pre)
        {
            LoadData(MDataRow.CreateFrom(anyObjToRow), pre);
        }
        /// <summary>
        /// 装载数据行 （一般后续配合SetFor方法使用或CMS替换）
        /// </summary>
        /// <param name="pre">为所有字段名指定前缀（如："a.",或空前缀：""）</param>
        public void LoadData(MDataRow row, string pre)
        {
            if (pre != null && row != null)
            {
                foreach (MDataCell cell in row)
                {
                    if (cell.IsNullOrEmpty) { continue; }
                    string cName = pre + cell.ColumnName;
                    if (KeyValue.ContainsKey(cName))
                    {
                        KeyValue[cName] = cell.ToString();
                    }
                    else
                    {
                        KeyValue.Add(cName, cell.ToString());
                    }
                }
            }
            _Row = row;
        }
        /// <summary>
        /// 装载行数据 （一般后续配合SetFor方法使用）
        /// </summary>
        /// <param name="row">数据行</param>
        public void LoadData(MDataRow row)
        {
            _Row = row;
        }
        /// <summary>
        /// 为节点设置值，通常在LoadData后使用。
        /// </summary>
        /// <param name="idOrName">节点的id或name</param>
        public void SetFor(string idOrName)
        {
            SetFor(idOrName, SetType.InnerXml);
        }
        /// <summary>
        /// 为节点设置值，通常在LoadData后使用。
        /// </summary>
        /// <param name="setType">节点的类型</param>
        public void SetFor(string idOrName, SetType setType)
        {
            SetFor(idOrName, setType, GetRowValue(idOrName));
        }
        /// <summary>
        /// 为节点设置值，通常在LoadData后使用。
        /// </summary>
        /// <param name="values">setType为Custom时，可自定义值，如“"href","http://www.cyqdata.com","target","_blank"”</param>
        public void SetFor(string idOrName, SetType setType, params string[] values)
        {
            int i = setType == SetType.Custom ? 1 : 0;
            for (; i < values.Length; i++)
            {
                if (values[i].Contains(ValueReplace.New))
                {
                    values[i] = values[i].Replace(ValueReplace.New, GetRowValue(idOrName));
                }
            }
            Set(Get(idOrName), setType, values);
        }
        private string GetRowValue(string idOrName)
        {
            string rowValue = "";
            if (_Row != null)
            {
                MDataCell cell = _Row[idOrName];
                if (cell == null && idOrName.Length > 3)
                {
                    cell = _Row[idOrName.Substring(3)];
                }
                if (cell != null)
                {
                    rowValue = cell.IsNull ? "" : cell.StringValue;
                }
            }
            return rowValue;
        }
        #endregion

        #endregion


        public override void Dispose()
        {
            base.Dispose();
            KeyValue.Clear();
            _Row = null;
        }
    }
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


                    MDictionary<string, StringBuilder> jsonDic = new MDictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);
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
