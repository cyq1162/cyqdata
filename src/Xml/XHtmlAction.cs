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
    /// Xml/Html������
    /// </summary>
    public partial class XHtmlAction : XHtmlBase
    {
        #region ���캯��
        /// <summary>
        /// Ĭ�Ϲ��캯��[���������ƿռ��Xml]
        /// </summary>
        public XHtmlAction()
            : base()
        {

        }
        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="isForHtml">trueʱ�����Զ�����html�����ƿռ�(http://www.w3.org/1999/xhtml)</param>
        public XHtmlAction(bool isForHtml)
            : base()
        {
            if (isForHtml)
            {
                base.LoadNameSpace(htmlNameSpace);
            }
        }
        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="isForHtml">trueʱ�����Զ�����html�����ƿռ�(http://www.w3.org/1999/xhtml)</param>
        /// <param name="isReadOnly">trueʱ�ĵ�ӦΪֻ��������ȡ��ͬһ���ĵ����ã�falseʱ�ĵ���д��ÿ�λ�ȡ���¡һ���ĵ����ء�</param>
        public XHtmlAction(bool isForHtml, bool isReadOnly)
            : base()
        {
            if (isForHtml)
            {
                base.LoadNameSpace(htmlNameSpace);
            }
            IsReadOnly = isReadOnly;
        }
        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="nameSpaceUrl">��Xml�����ƿռ�[����]</param>
        public XHtmlAction(string nameSpaceUrl)
            : base()
        {
            base.LoadNameSpace(nameSpaceUrl);
        }
        #endregion

        #region ��ѯ
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

        internal XmlNode GetByID(string id)
        {
            return Fill(GetXPath("*", "id", id), null);
        }
        internal XmlNode GetByID(string id, XmlNode parentNode)
        {
            return Fill(GetXPath("*", "id", id), parentNode);
        }
        internal XmlNode GetByName(string name)
        {
            return Fill(GetXPath("*", "name", name), null);
        }
        internal XmlNode GetByName(string name, XmlNode parentNode)
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

        #region ����

        protected XmlElement Create(string tag)
        {
            if (xnm == null)
            {
                return _XmlDocument.CreateElement(tag);
            }
            return _XmlDocument.CreateElement(tag, xnm.LookupNamespace(PreXml));
        }

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
                if (!string.IsNullOrEmpty(text))
                {
                    if (text.Contains("<") && text.Contains(">"))
                    {
                        xElement.InnerXml = text;
                    }
                    else
                    {
                        xElement.InnerText = text;
                    }
                }

            }
            catch
            {
                xElement.InnerText = SetCDATA(text);
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

        #region ����
        public void AppendNode(XmlNode parentNode, XmlNode childNode)
        {
            if (parentNode != null && childNode != null)
            {
                parentNode.AppendChild(childNode);
            }
        }
        /// <summary>
        /// ��ӽڵ�
        /// </summary>
        /// <param name="position">parentNode�ĵ�N���ӽڵ�֮��</param>
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

        #region ɾ��
        /// <summary>
        /// �����ڵ�,������ڵ�������/����
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
        /// �Ƴ��ӽڵ�
        /// </summary>
        /// <param name="id">�ڵ��id</param>
        /// <param name="start">�ӵڼ����ӽڵ㿪ʼɾ��[������0��ʼ]</param>
        public void RemoveChild(string idOrName, int start)
        {
            XmlNode node = Get(idOrName);
            if (node != null)
            {
                RemoveChild(node, start);
            }
        }
        /// <summary>
        /// �Ƴ��ӽڵ�
        /// </summary>
        /// <param name="node">�ڵ�</param>
        /// <param name="start">�ӵڼ����ӽڵ㿪ʼɾ��[������0��ʼ]</param>
        public void RemoveChild(XmlNode node, int start)
        {
            if (start == 0)
            {
                node.InnerXml = "";
                return;
            }
            if (node.ChildNodes.Count > start) //1���ӽڵ�, 0
            {
                for (int i = node.ChildNodes.Count - 1; i >= start; i--)
                {
                    node.RemoveChild(node.ChildNodes[i]);
                }
            }
        }
        /// <summary>
        /// �Ƴ��������
        /// </summary>
        /// <param name="ids">Ҫ�Ƴ��������б�</param>
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
        /// �����Ƴ�
        /// </summary>
        /// <param name="attrName">��������</param>
        /// <param name="excludeSetType">�ų��Ľڵ�����</param>
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
        /// �Ƴ�ע�ͽڵ�
        /// </summary>
        /// <param name="node">�Ƴ��˽ڵ��ע���ı�</param>
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
        /// �Ƴ�ע�ͽڵ�
        /// </summary>
        public override void RemoveCommentNode()
        {
            RemoveCommentNode(XmlDoc.DocumentElement);
        }
        #endregion

        #region ���������ڵ�/����ڵ�

        /// <summary>
        /// �����ڵ㽻��λ��
        /// </summary>
        /// <param name="xNodeFirst">��һ���ڵ�</param>
        /// <param name="xNodeLast">�ڶ����ڵ�</param>
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
        /// �ڵ��滻[֧���������ĵ����滻]
        /// </summary>
        /// <param name="newNode">�µ����ݽڵ�</param>
        /// <param name="oldNode">�ɵĽڵ�</param>
        public void ReplaceNode(XmlNode newNode, XmlNode oldNode)
        {
            if (newNode != null && oldNode != null)
            {
                //oldNode = newNode.Clone();
                if (newNode.Name == oldNode.Name) // �ڵ�����ͬ��
                {
                    oldNode.RemoveAll();//��վɽڵ�
                    oldNode.InnerXml = newNode.InnerXml;
                    XmlAttributeCollection attrs = newNode.Attributes;//��������
                    if (attrs != null && attrs.Count > 0)
                    {
                        for (int i = 0; i < attrs.Count; i++)
                        {
                            if (attrs[i].Name == "xml:space") { continue; }
                            ((XmlElement)oldNode).SetAttribute(attrs[i].Name, attrs[i].Value);
                        }
                    }
                }
                else
                {
                    XmlNode xNode = CreateNode(newNode.Name, newNode.InnerXml);//�ȴ���һ���ڵ㡣
                    XmlAttributeCollection attrs = newNode.Attributes;
                    if (attrs != null && attrs.Count > 0)
                    {
                        for (int i = 0; i < attrs.Count; i++)
                        {
                            ((XmlElement)xNode).SetAttribute(attrs[i].Name, attrs[i].Value);
                        }
                    }
                    oldNode.ParentNode.InsertAfter(xNode, oldNode);//���ھɽڵ���档
                    Remove(oldNode);
                }

            }
        }
        /// <summary>
        /// �ڵ�֮�����[֧�����ĵ�֮��Ĳ���]
        /// </summary>
        /// <param name="newNode">Ҫ��������½ڵ�</param>
        /// <param name="refNode">�ڴ˽ڵ�����NewNode�ڵ�</param>
        public void InsertAfter(XmlNode newNode, XmlNode refNode)
        {
            XmlNode xDocNode = CreateNode(newNode.Name, "");
            ReplaceNode(newNode, xDocNode);
            refNode.ParentNode.InsertAfter(xDocNode, refNode);
        }
        /// <summary>
        /// �ڵ�֮ǰ����[֧�����ĵ�֮��Ĳ���]
        /// </summary>
        /// <param name="newNode">Ҫ��������½ڵ�</param>
        /// <param name="refNode">�ڴ˽ڵ�ǰ����NewNode�ڵ�</param>
        public void InsertBefore(XmlNode newNode, XmlNode refNode)
        {
            XmlNode xDocNode = CreateNode(newNode.Name, "");
            ReplaceNode(newNode, xDocNode);
            refNode.ParentNode.InsertBefore(xDocNode, refNode);
        }
        #endregion

        #region �ڵ��ж�
        public bool Contains(string idOrName)
        {
            return Get(idOrName) != null;
        }
        public bool Contains(string idOrName, XmlNode parentNode)
        {
            return Get(idOrName, parentNode) != null;
        }
        #endregion

        #region �����ж�/ȡֵ

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

        #region ��������

        private bool _IsCurrentLang = true;
        /// <summary>
        /// ��ǰ�����Ƿ��û���ǰ���õ�����
        /// </summary>
        internal bool IsCurrentLang
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
        /// �Ƿ�ʼ�Զ������Էָ�(�ָ�����Ϊ��[#langsplit])
        /// </summary>
        internal bool IsUseLangSplit = false;

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
        /// Ϊ�ڵ㸳ֵ[ͨ��ֵ����values�и�ֵ]
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
                    case SetType.ClearFlag:
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
                        if (!node.InnerXml.StartsWith("<"))//�������ַ�
                        {
                            string innerHtml = node.InnerXml.Replace(string.Format("value=\"{0}\"", values[0]), string.Format("selected=\"selected\" value=\"{0}\"", values[0]));
                            try
                            {
                                node.InnerText = innerHtml;
                                //node.InnerXml = innerHtml;
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
                        string type = GetAttrValue(node, "type");
                        if (node.Name == "input" && type == "radio")
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
                            case "0":
                            case "false":
                                if (type == "checkbox")
                                {
                                    RemoveAttr(node, setType.ToString().ToLower());
                                }
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
        /// �Խڵ㸳ֵ���˷��������hidden���������ؽڵ㸳ֵ�����������ط�����
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
                                return;//�˷���������������
                            case "checkbox":
                                setType = SetType.Checked; break;
                            case "image":
                                setType = SetType.Src; break;
                            case "radio"://�������һ��
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

        #region ��д�������OutXml
        /// <summary>
        /// �����Xml���ݡ�
        /// </summary>
        public override string OutXml
        {
            get
            {
                if (_XmlDocument != null)
                {
                    #region ����clearflag��ǩ
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

                    #region ����XHtml ͷǰ׺�����CData

                    string html = _XmlDocument.InnerXml;
                    if (html.Contains(".dtd\"[]>"))
                    {
                        html = html.Replace(".dtd\"[]>", ".dtd\">");
                    }
                    if (html.Contains(" xmlns="))
                    {
                        html = html.Replace(" xmlns=\"\"", string.Empty).Replace(" xmlns=\"" + xnm.LookupNamespace(PreXml) + "\"", string.Empty);
                    }
                    if (html.Contains("&"))
                    {
                        //�Ȱ�&�����滻���������滻����ġ�
                        html = html.Replace("&amp;", "&").Replace("&gt;", ">").Replace("&lt;", "<");//html��ǩ���š�
                    }
                    //if (!string.IsNullOrEmpty(docTypeHtml))
                    //{
                    //    html = html.Replace(docTypeHtml, "<!DOCTYPE html>");
                    //}
                    #endregion

                    #region ����ʣ���ռλ���滻
                    if (html.IndexOf("${") > -1 || html.IndexOf("<%#") > -1)
                    {
                        html = FormatHtml(html, null);
                    }
                    #endregion

                    #region ���CData��ǩ
                    html = ClearCDATA(html);
                    #endregion

                    if (html.StartsWith("<html"))
                    {
                        //�ӻ���ڵ��XmlDocumentʱ��������dtd�������Ҫ����
                        html = "<!DOCTYPE html>" + html;
                    }

                    return html;
                }
                return string.Empty;
            }
        }
        /// <summary>
        /// ��Html�����ݣ����б�ǩ�滻��1���滻��
        /// </summary>
        /// <param name="html">��Ҫ���滻������</param>
        /// <param name="values">����ֵ�滻�������ֵ�����</param>
        /// <returns></returns>
        private string FormatHtml(string html, MDictionary<string, string> values)
        {

            if (html.IndexOf("${") > -1)
            {
                #region �滻ռλ����
                MatchCollection matchs = Regex.Matches(html, @"\$\{([\S\s]*?)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                if (matchs != null && matchs.Count > 0)
                {
                    //�����ظ���ռλ����
                    List<string> keys = new List<string>(matchs.Count);
                    foreach (Match match in matchs)
                    {
                        //ԭʼ��ռλ�� ${txt#name:xx#xx}
                        string value = match.Groups[0].Value;//${txt#name:xx#xx}��${'aaa'+txt#name:xx#xx+'bbb'}
                        if (string.IsNullOrEmpty(value) || keys.Contains(value))
                        {
                            continue;
                        }
                        keys.Add(value);

                        #region �ֽ�ռλ��
                        //�ȴ���+�� ${'aaa'+txt#name:xx#xx+'bbb'} formatter= 'aaa'{0}
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
                                    break;//ֻ�������һ��key : txt#name:��������˵����
                                }
                                formatter += "{0}";
                                key = item.Trim();
                            }
                        }
                        string columnName = key.Split(':')[0];//name
                        #endregion

                        string replaceValue = "";

                        #region ��ȡռλ����ֵ
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
            if (html.IndexOf("<%#") > -1)//js eval ִ��
            {
                #region �滻JavaScript�﷨
                MatchCollection matchs = Regex.Matches(html, @"<%#([\S\s]*?)%>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                if (matchs != null && matchs.Count > 0)
                {
                    foreach (Match match in matchs)
                    {
                        //ԭʼ��ռλ��
                        string value = match.Groups[0].Value;//${txt#name:xx#xx}��${'aaa'+txt#name:xx#xx+'bbb'}
                        string value1 = match.Groups[1].Value;
                        string evalValue = null;
                        try
                        {
                            evalValue = Convert.ToString(Microsoft.JScript.Eval.JScriptEvaluate(value1, Microsoft.JScript.Vsa.VsaEngine.CreateEngine()));
                        }
                        catch (Exception err)
                        {
                            Log.WriteLogToTxt(err, LogType.Error);
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
            if (values.ContainsKey(columnName))//ֵ���ܱ���ʽ��������������ȡֵ��
            {
                return values[columnName];
            }
            else if (columnName.Length < 3)
            {
                int i = 0;
                if (int.TryParse(columnName, out i) && i < values.Count)
                {
                    return values[i]; //����
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
            //����keyValue�滻
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
            if (string.IsNullOrEmpty(replaceValue) && key.Contains(":"))//���Ϊ�գ�����Ĭ�ϵ�˵����
            {
                replaceValue = key.Substring(key.LastIndexOf(':') + 1);
            }
            return replaceValue ?? "";
        }
        #endregion
    }
}
