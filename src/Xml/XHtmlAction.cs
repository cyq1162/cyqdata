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
        /// <param name="isNoClone">trueʱ�ĵ�ӦΪֻ��������ȡ��ͬһ���ĵ����ã�falseʱ�ĵ���д��ÿ�λ�ȡ���¡һ���ĵ����ء�</param>
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

        #region ����
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
        /// <param name="XNodeFirst">��һ���ڵ�</param>
        /// <param name="XNodeLast">�ڶ����ڵ�</param>
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
        /// <param name="NewXNode"></param>
        /// <param name="OldXNode"></param>
        public void ReplaceNode(XmlNode newNode, XmlNode oldNode)
        {
            if (newNode != null && oldNode != null)
            {
                if (newNode.Name == oldNode.Name) // �ڵ�����ͬ��
                {
                    oldNode.RemoveAll();//��վɽڵ�
                    oldNode.InnerXml = newNode.InnerXml;
                    XmlAttributeCollection attrs = newNode.Attributes;//��������
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
        /// <param name="NewNode">Ҫ��������½ڵ�</param>
        /// <param name="RefNode">�ڴ˽ڵ�����NewNode�ڵ�</param>
        public void InsertAfter(XmlNode newNode, XmlNode refNode)
        {
            XmlNode xDocNode = CreateNode(newNode.Name, "");
            ReplaceNode(newNode, xDocNode);
            refNode.ParentNode.InsertAfter(xDocNode, refNode);
        }
        /// <summary>
        /// �ڵ�֮ǰ����[֧�����ĵ�֮��Ĳ���]
        /// </summary>
        /// <param name="NewNode">Ҫ��������½ڵ�</param>
        /// <param name="RefNode">�ڴ˽ڵ�ǰ����NewNode�ڵ�</param>
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
        /// �Ƿ�ʼ�Զ������Էָ�(�ָ�����Ϊ��[#langsplit])
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
                        if (node.InnerXml.Contains(AppConfig.XHtml.CDataLeft))//�������ַ�
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
                    html = html.Replace("&gt;", ">").Replace("&lt;", "<").Replace("&amp;", "&");//html��ǩ���š�
                    #endregion

                    #region ����ʣ���ռλ���滻
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
            return replaceValue;
        }
        #endregion
    }

    //��չ����
    public partial class XHtmlAction
    {
        #region ��������
        MDataRow _Row;

        #region ���ر��ѭ����ʽ

        public delegate string SetForeachEventHandler(string text, MDictionary<string, string> values, int rowIndex);
        /// <summary>
        /// ����SetForeach�������õĸ�ʽ���¼�
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
        /// ���б����ѭ���󶨴���
        /// </summary>
        /// <param name="dataSource">����Դ</param>
        /// <param name="node">����Ľڵ�</param>
        /// <param name="text">����ѭ�������ݡ�ͨ������node.InnerXml��</param>
        /// <param name="eventOnForeach">�Զ����¼�</param>
        public void SetForeach(MDataTable dataSource, XmlNode node, string text, SetForeachEventHandler eventOnForeach)
        {
            try
            {
                #region ǰ����������
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
                        newText = eventOnForeach(text, values, k);//����ÿһ�У�������text��
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

        #region ���������ݺ������ʽ
        /// <summary>
        /// �����滻ռλ�������ݡ�
        /// </summary>
        public MDictionary<string, string> KeyValue = new MDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// ����MDatarow�����ݣ�CMS�滻��
        /// </summary>
        /// <param name="pre">Ϊ�����ֶ���ָ��ǰ׺���磺"a.",���ǰ׺��""</param>
        public void LoadData(object anyObjToRow, string pre)
        {
            LoadData(MDataRow.CreateFrom(anyObjToRow), pre);
        }
        /// <summary>
        /// װ�������� ��һ��������SetFor����ʹ�û�CMS�滻��
        /// </summary>
        /// <param name="pre">Ϊ�����ֶ���ָ��ǰ׺���磺"a.",���ǰ׺��""��</param>
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
        /// װ�������� ��һ��������SetFor����ʹ�ã�
        /// </summary>
        /// <param name="row">������</param>
        public void LoadData(MDataRow row)
        {
            _Row = row;
        }
        /// <summary>
        /// Ϊ�ڵ�����ֵ��ͨ����LoadData��ʹ�á�
        /// </summary>
        /// <param name="idOrName">�ڵ��id��name</param>
        public void SetFor(string idOrName)
        {
            SetFor(idOrName, SetType.InnerXml);
        }
        /// <summary>
        /// Ϊ�ڵ�����ֵ��ͨ����LoadData��ʹ�á�
        /// </summary>
        /// <param name="setType">�ڵ������</param>
        public void SetFor(string idOrName, SetType setType)
        {
            SetFor(idOrName, setType, GetRowValue(idOrName));
        }
        /// <summary>
        /// Ϊ�ڵ�����ֵ��ͨ����LoadData��ʹ�á�
        /// </summary>
        /// <param name="values">setTypeΪCustomʱ�����Զ���ֵ���硰"href","http://www.cyqdata.com","target","_blank"��</param>
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
    /// Json ����
    /// </summary>
    public partial class XHtmlAction
    {
        #region תJson
        /// <summary>
        /// תJson
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return ToJson(XmlDoc.DocumentElement, true);
        }
        /// <summary>
        /// תJson
        /// <param name="parent">������һ���ڵ㣨Ĭ�ϸ��ڵ㣩</param>
        /// <param name="isWithAttr">�Ƿ�����ֵҲ���</param>
        /// </summary>
        public string ToJson(XmlNode parent, bool isWithAttr)
        {
            parent = parent ?? XmlDoc.DocumentElement;
            if (parent == null)
            {
                return string.Empty;
            }
            JsonHelper js = new JsonHelper(false, false);
            //�ֽ�ݹ飬��Ȼ�����費�õ���

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
                        else // �ظ���TagName
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
                    // return "fuck"; //�Բۣ���ô�������Ĵ��룿
                }
            }
            return result;
        }
        #endregion
    }
}
