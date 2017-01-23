using System;
using System.Xml;
using CYQ.Data.Table;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using CYQ.Data.Tool;
using System.Text;

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
            XmlNode node = GetByID(idOrName);
            if (node == null)
            {
                node = GetByName(idOrName);
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
                            XmlNodeList xList = GetList(idOrName);
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
        public XmlNode Get(string idOrName, XmlNode parentNode)
        {
            XmlNode node = GetByID(idOrName, parentNode);
            if (node == null)
            {
                node = GetByName(idOrName, parentNode);
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
                string attr = "", value = "";
                for (int i = 0; i < attrAndValue.Length; i++)
                {
                    attr = attrAndValue[i];
                    i++;
                    value = attrAndValue[i];
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
        /// <param name="OldNode"></param>
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
        /// <param name="id">节点的ID</param>
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
        /// <param name="ids"></param>
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
                            foreach (XmlNode option in node.ChildNodes)
                            {
                                if (option.Attributes["value"] != null && (option.Attributes["value"].Value == values[0] || option.Attributes["value"].Value.Split(',')[0] == values[0]))
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
            XmlNode node = Get(idOrName);
            Set(node, setType, values);
        }
        /// <summary>
        /// 对节点赋值（此方法会忽略hidden隐藏域，隐藏节点赋值请用其它重载方法）
        /// </summary>
        /// <param name="idOrName"></param>
        /// <param name="value"></param>
        public void Set(string idOrName, string value)
        {
            XmlNode node = Get(idOrName);
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
                    if (dicForAutoSetValue != null && dicForAutoSetValue.Count > 0 && html.Contains("${")) // 替换自定义标签。
                    {
                        #region 替换自定义标签
                        MatchCollection matchs = Regex.Matches(html, @"\$\{([\S\s]*?)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                        if (matchs != null && matchs.Count > 0)
                        {
                            List<string> keys = new List<string>(matchs.Count);
                            foreach (Match match in matchs)
                            {
                                string value = match.Groups[0].Value;
                                if (!keys.Contains(value))
                                {
                                    keys.Add(value);
                                    string[] items = match.Groups[1].Value.Trim().Split('#', '-');
                                    string pre = items.Length > 1 ? items[0] : "";
                                    string columnName = items.Length > 1 ? items[1] : items[0];
                                    if (dicForAutoSetValue.ContainsKey(pre))
                                    {
                                        MDataCell matchCell = dicForAutoSetValue[pre][columnName];
                                        if (matchCell != null)
                                        {
                                            html = html.Replace(value, matchCell.ToString());
                                        }
                                    }
                                }
                            }
                            keys.Clear();
                            keys = null;
                        }
                        dicForAutoSetValue.Clear();
                        dicForAutoSetValue = null;
                        matchs = null;
                        #endregion
                    }
                    return html;
                }
                return string.Empty;
            }
        }
        #endregion
    }

    //扩展交互
    public partial class XHtmlAction
    {
        #region 操作数据
        MDataRow _Row;
        MDataTable _Table;



        #region 加载表格循环方式
        public void LoadData(object data)
        {
            if (data.GetType().IsGenericType)
            {
                LoadData(MDataTable.CreateFrom(data));
            }
            else
            {
                LoadData(MDataRow.CreateFrom(data));
            }
        }
        /// <summary>
        /// 装载数据行 （一般后续配合SetForeach方法使用）
        /// </summary>
        public void LoadData(MDataTable table)
        {
            _Table = table;
            if (_Table.Rows.Count > 0)
            {
                _Row = _Table.Rows[0];
            }
        }
        public delegate string SetForeachEventHandler(string text, MDictionary<string, string> values, int rowIndex);
        /// <summary>
        /// 对于SetForeach函数调用的格式化事件
        /// </summary>
        public event SetForeachEventHandler OnForeach;
        public void SetForeach()
        {
            if (_Table != null)
            {
                XmlNode node = Get(_Table.TableName + "View");
                if (node == null)
                {
                    node = Get("defaultView");
                }
                if (node != null)
                {
                    SetForeach(node, node.InnerXml);
                }
            }
        }
        public void SetForeach(string idOrName, SetType setType, params object[] formatValues)
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
            SetForeach(node, text, formatValues);
        }
        public void SetForeach(string idOrName, string text, params object[] formatValues)
        {
            XmlNode node = Get(idOrName);
            SetForeach(node, text, formatValues);
        }
        public void SetForeach(XmlNode node, string text, params object[] formatValues)
        {
            try
            {
                #region 基本判断
                if (node == null || _Table == null || _Table.Rows.Count == 0) { return; }
                RemoveAttr(node, "clearflag");
                #endregion

                int fvLen = formatValues.Length;
                int colLen = _Table.Columns.Count;
                StringBuilder innerXml = new StringBuilder();
                if (string.IsNullOrEmpty(text))
                {
                    if (node.Name == "select")
                    {
                        text = "<option value=\"{0}\">{1}</option>";
                    }
                    else
                    {
                        #region 补列头
                        for (int i = 0; i < colLen; i++)
                        {
                            if (i == 0)
                            {
                                innerXml.Append(_Table.Columns[i].ColumnName);
                            }
                            else
                            {
                                innerXml.Append(" - " + _Table.Columns[i].ColumnName);
                            }
                        }
                        innerXml.Append("<hr />");
                        #endregion

                        #region 空文本，默认补个简单的Table给它。
                        StringBuilder sb = new StringBuilder();
                        int min = fvLen == 0 ? colLen : Math.Min(fvLen, colLen);
                        for (int i = 0; i < min; i++)
                        {
                            if (i == 0)
                            {
                                sb.Append("{0}");
                            }
                            else
                            {
                                sb.Append(" - {" + i + "}");
                            }
                        }
                        sb.Append("<hr />");
                        text = sb.ToString();
                        #endregion
                    }
                }
                if (fvLen == 0)
                {
                    formatValues = new object[colLen];
                }
                MDictionary<string, string> values = new MDictionary<string, string>(formatValues.Length, StringComparer.OrdinalIgnoreCase);
                // object[] values = new object[formatValues.Length];//用于格式化{0}、{1}的占位符

                //foreach (MDataRow row in _Table.Rows)
                string newText = text;
                MDataCell cell;
                for (int k = 0; k < _Table.Rows.Count; k++)
                {
                    for (int i = 0; i < formatValues.Length; i++)
                    {
                        #region 从指定的列把值放到values数组
                        if (formatValues[i] == null)
                        {
                            formatValues[i] = i;
                        }
                        cell = _Table.Rows[k][formatValues[i].ToString()];
                        if (cell == null && string.Compare(formatValues[i].ToString(), "row", true) == 0) // 多年以后，自己也没看懂比较row是为了什么
                        {
                            values.Add(formatValues[i].ToString(), (k + 1).ToString());
                            //values[i] = k + 1;//也没看懂，为啥值是自增加1，我靠，难道是行号？或者楼层数？隐匿功能？
                        }
                        else if (cell != null)
                        {
                            values.Add(cell.ColumnName, cell.strValue);
                            //values[i] = cell.Value;
                        }
                        #endregion
                    }
                    if (OnForeach != null)
                    {
                        newText = OnForeach(text, values, k);//遍历每一行，产生新text。
                    }
                    try
                    {
                        string tempText = newText;
                        int j = 0;
                        foreach (KeyValuePair<string, string> kv in values)
                        {
                            tempText = tempText.Replace("{" + j + "}", kv.Value);//格式化{0}、{1}的占位符
                            j++;
                        }
                        //for (int j = 0; j < values.Length; j++)
                        //{
                        //    tempText = tempText.Replace("{" + j + "}", Convert.ToString(values[j]));//格式化{0}、{1}的占位符
                        //}
                        if (tempText.Contains("${"))
                        {
                            #region 处理标签占位符
                            MatchCollection matchs = Regex.Matches(tempText, @"\$\{([\S\s]*?)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                            if (matchs != null && matchs.Count > 0)
                            {
                                MDataCell matchCell = null;
                                string columnName = null, value = null;
                                List<string> keys = new List<string>(matchs.Count);
                                foreach (Match match in matchs)
                                {
                                    value = match.Groups[0].Value;
                                    columnName = match.Groups[1].Value.Trim();
                                    if (!keys.Contains(value))
                                    {
                                        keys.Add(value);
                                        if (value.Contains(columnName))//值可能被格式化过，所以优先取值。
                                        {
                                            tempText = tempText.Replace(value, values[columnName]);
                                        }
                                        else
                                        {
                                            matchCell = _Table.Rows[k][columnName];
                                            if (matchCell != null)
                                            {
                                                tempText = tempText.Replace(value, matchCell.ToString());
                                            }
                                        }
                                    }
                                }
                                keys.Clear();
                                keys = null;
                            }
                            matchs = null;
                            #endregion
                        }
                        //处理${}语法
                        innerXml.Append(tempText);
                        // innerXml += tempText;//string.Format(newText, values);
                    }
                    catch (Exception err)
                    {
                        Log.WriteLogToTxt(err);
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
                        Log.WriteLogToTxt(err);
                    }
                }

            }
            finally
            {
                if (OnForeach != null)
                {
                    OnForeach = null;
                }
            }
        }
        #endregion

        #region 加载行数据后操作方式


        /// <summary>
        /// 装载数据行 （一般后续配合SetFor方法使用）
        /// </summary>
        /// <param name="autoSetValuePre">批量赋值的前缀（如："txt#",或空前缀：""）赋值后，将在获取OutXml属性时处理赋值</param>
        public void LoadData(MDataRow row, string autoSetValuePre)
        {
            if (autoSetValuePre != null)
            {
                autoSetValuePre = autoSetValuePre.TrimEnd('#', '-');
                if (dicForAutoSetValue == null)
                {
                    dicForAutoSetValue = new Dictionary<string, MDataRow>(6);
                }
                if (!dicForAutoSetValue.ContainsKey(autoSetValuePre))
                {
                    dicForAutoSetValue.Add(autoSetValuePre, row);
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
        /// <param name="id">节点的ID</param>
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
                    rowValue = cell.strValue;
                }
            }
            return rowValue;
        }
        #endregion

        #endregion

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
            js.Add(parent.Name, GetChildJson(parent, isWithAttr), true);
            js.AddBr();
            return js.ToString();
        }
        private string GetChildJson(XmlNode parent, bool isWithAttr)
        {
            JsonHelper js = new JsonHelper(false, false);
            if (isWithAttr && parent.Attributes != null && parent.Attributes.Count > 0)
            {
                foreach (XmlAttribute item in parent.Attributes)
                {
                    js.Add(item.Name, item.Value);
                }

            }
            if (parent.HasChildNodes)
            {
                XmlNode x0 = parent.ChildNodes[0];
                int childCount = parent.ChildNodes.Count;
                if (x0.NodeType != XmlNodeType.Element && childCount == 1)
                {
                    js.Add(parent.Name, parent.InnerText);
                }
                else
                {
                    Dictionary<string, string> jsonDic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (XmlNode item in parent.ChildNodes)
                    {
                        string childJson = GetChildJson(item, isWithAttr);
                        if (!jsonDic.ContainsKey(item.Name))
                        {
                            jsonDic.Add(item.Name, childJson);
                        }
                        else // 重复的TagName
                        {
                            jsonDic[item.Name] = "[" + jsonDic[item.Name].TrimStart('[').TrimEnd(']') + "," + childJson + "]";
                        }
                    }
                    foreach (KeyValuePair<string, string> kv in jsonDic)
                    {
                        js.Add(kv.Key, kv.Value, true);
                    }
                }
            }
            js.AddBr();
            return js.ToString();
        }
        #endregion
    }

}
