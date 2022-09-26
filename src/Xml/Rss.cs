using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using CYQ.Data.Table;
using CYQ.Data.Tool;
namespace CYQ.Data.Xml
{
    //public class RssDemo
    //{
    //    public string GetRss()
    //    {
    //        Rss2 rss = new Rss2();
    //        rss.channel.Title = "��ɫ԰";
    //        rss.channel.Link = "http://www.cyqdata.com";
    //        rss.channel.Description = "��ɫ԰-QBlog-Power by Blog.CYQ";
    //        for (int i = 0; i < 10; i++)
    //        {
    //            RssItem item = new RssItem();
    //            item.Title = string.Format("��{0}��", i);
    //            item.Link = "http://www.cyqdata.com";
    //            item.Description = "�ܳ��ܳ�������";
    //            rss.channel.Items.Add(item);
    //        }
    //        return rss.OutXml;
    //    }
    //}
    //public class Rss2
    //{
    //    XmlDocument rssDoc;
    //    public RssChannel channel;
    //    public Rss2()
    //    {
    //        rssDoc = new XmlDocument();
    //        rssDoc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?><rss version=\"2.0\"><channel></channel></rss>");
    //        channel = new RssChannel();
    //    }
    //    private void BuildRss()
    //    {
    //        XmlNode cNode = rssDoc.DocumentElement.ChildNodes[0];//ȡ��channelԪ��
    //        ForeachCreateChild(cNode, channel);//Channel����
    //        if (channel.RssImage != null)
    //        {
    //            ForeachCreateChild(Create("image", null, cNode), channel.RssImage);//Channel-Image����
    //        }
    //        if (channel.Items.Count > 0)
    //        {
    //            foreach (RssItem item in channel.Items)
    //            {
    //                ForeachCreateChild(Create("item", null, cNode), item);//Channel-Items����
    //            }
    //        }
    //    }
    //    private void ForeachCreateChild(XmlNode parent, object obj)
    //    {
    //        object propValue = null;
    //        PropertyInfo[] pis = obj.GetType().GetProperties();
    //        for (int i = 0; i < pis.Length; i++)
    //        {
    //            if (pis[i].Name == "Items" || pis[i].Name == "Image")
    //            {
    //                continue;
    //            }
    //            propValue = pis[i].GetValue(obj, null);
    //            if (propValue == null || propValue == DBNull.Value)
    //            {
    //                continue;
    //            }
    //            if (pis[i].Name == "Description")
    //            {
    //                propValue = "<![CDATA[" + propValue.ToString() + "]]>";
    //            }
    //            Create(pis[i].Name.Substring(0, 1).ToLower() + pis[i].Name.Substring(1), propValue.ToString(), parent);
    //        }
    //    }
    //    private XmlNode Create(string name, string value,XmlNode parent)
    //    {
    //        XmlElement xNode = rssDoc.CreateElement(name);
    //        if (string.IsNullOrEmpty(value))
    //        {
    //            xNode.InnerXml = value;
    //        }
    //        parent.AppendChild(xNode as XmlNode);
    //        return xNode as XmlNode;
    //    }
    //    public string OutXml
    //    {
    //        get
    //        {
    //            BuildRss();
    //            return rssDoc.OuterXml;
    //        }
    //    }
    //}

    /// <summary>
    /// Rss������
    /// </summary>
    public class Rss
    {
        internal class RssItemMap
        {
            internal string RssItemName;
            internal object[] TableColumnNames;
            internal string FormatText;
        }
        MDataTable _MTable = null;
        List<RssItemMap> mapList = new List<RssItemMap>();//��MDataTableӳ��
        private RssChannel channel;
        /// <summary>
        /// RSS Ƶ����Ĭ�ϣ�
        /// </summary>
        public RssChannel Channel
        {
            get
            {
                return channel;
            }
        }
        RssImage img;
        XHtmlAction rssDoc;
        public Rss()
        {
            rssDoc = new XHtmlAction(false);
            //rssDoc.ReadOnly = true;
            rssDoc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?><rss version=\"2.0\"><channel></channel></rss>");
            channel = new RssChannel();
        }
        /// <summary>
        /// ������վ����
        /// </summary>
        /// <param name="title">��վ����</param>
        /// <param name="link">��ַ</param>
        /// <param name="description">��վ����</param>
        public void Set(string title, string link, string description)
        {
            channel.Title = title;
            channel.Link = link;
            channel.Description = description;
        }
        /// <summary>
        /// ����ͼƬ����վLogo��
        /// </summary>
        /// <param name="url">Logo��ַ</param>
        /// <param name="title">Logo����</param>
        /// <param name="link">Logo���ʱ��ת����</param>
        public void SetImg(string url, string title, string link)
        {
            if (img == null)
            {
                img = new RssImage();
                img.Url = url;
                img.Title = title;
                img.Link = link;
            }
        }
        /// <summary>
        /// ���RSS����£�
        /// </summary>
        /// <param name="title">����</param>
        /// <param name="link">����</param>
        /// <param name="author">����</param>
        /// <param name="pubDate">��������</param>
        /// <param name="description">���ݻ�ժҪ</param>
        public void AddItem(string title, string link, string author, string pubDate, string description)
        {
            RssItem item = new RssItem();
            item.Title = title;
            item.Link = link;
            item.Author = author;
            item.PubDate = pubDate;
            item.Description = description;
            channel.Items.Add(item);
        }
        public delegate string SetForeachEventHandler(string text, MDictionary<string, string> values, int rowIndex);
        /// <summary>
        ///LoadData��MDataTable������ӳ����ڸ�ʽ��ÿһ������ʱ�����¼�
        /// </summary>
        public event SetForeachEventHandler OnForeach;
        private void BuildRss()
        {
            object propValue = null;

            XmlNode cNode = rssDoc.XmlDoc.DocumentElement.ChildNodes[0];
            CreateNode(cNode, channel);//Channel����

            XmlNode iNode = null;
            if (img != null)
            {
                iNode = rssDoc.CreateNode("image", string.Empty);
                cNode.AppendChild(iNode);
                CreateNode(iNode, img);//Channel-Image����
            }
            if (channel.Items.Count > 0)
            {
                foreach (RssItem item in channel.Items)
                {
                    iNode = rssDoc.CreateNode("item", string.Empty);
                    cNode.AppendChild(iNode);
                    CreateNode(iNode, item);//Channel-Items����
                }
            }
            else if (_MTable != null && mapList.Count > 0)
            {
                foreach (MDataRow row in _MTable.Rows)
                {
                    iNode = rssDoc.CreateNode("item", string.Empty);
                    cNode.AppendChild(iNode);
                    //foreach (RssItemMap item in mapList)
                    RssItemMap item = null;
                    for (int k = 0; k < mapList.Count; k++)
                    {
                        item = mapList[k];
                        if (item.TableColumnNames.Length > 0)
                        {
                            MDictionary<string, string> dic = new MDictionary<string, string>(item.TableColumnNames.Length, StringComparer.OrdinalIgnoreCase);
                            object[] values = new object[item.TableColumnNames.Length];
                            for (int i = 0; i < item.TableColumnNames.Length; i++)
                            {
                                string columnName = item.TableColumnNames[i].ToString();
                                values[i] = row[columnName].Value;
                                dic.Set(columnName, Convert.ToString(values[i]));

                            }
                            if (OnForeach != null)
                            {
                                item.FormatText = OnForeach(item.FormatText, dic, k);
                            }
                            if (string.IsNullOrEmpty(item.FormatText))
                            {
                                propValue = values[0];
                            }
                            else
                            {
                                propValue = string.Format(item.FormatText, values);
                            }
                        }
                        //else if (item.TableColumnNames.Length > 0)
                        //{
                        //    propValue = row[item.TableColumnNames[0].ToString()].Value;
                        //    if (!string.IsNullOrEmpty(item.FormatText))
                        //    {
                        //        propValue = string.Format(item.FormatText, propValue);
                        //    }
                        //}
                        else
                        {
                            propValue = item.FormatText;
                        }
                        if (propValue == null || propValue == DBNull.Value)
                        {
                            continue;
                        }
                        if (item.RssItemName == "Description")
                        {
                            propValue = rssDoc.SetCDATA(propValue.ToString());
                        }
                        rssDoc.CreateNodeTo(iNode, item.RssItemName.Substring(0, 1).ToLower() + item.RssItemName.Substring(1), propValue.ToString());
                    }
                }
            }
        }
        private void CreateNode(XmlNode parent, object obj)
        {
            object propValue = null;
            List<PropertyInfo> pis = Tool.ReflectTool.GetPropertyList(obj.GetType());
            for (int i = 0; i < pis.Count; i++)
            {
                if (pis[i].Name == "Items")
                {
                    continue;
                }
                propValue = pis[i].GetValue(obj, null);
                if (propValue == null || propValue == DBNull.Value)
                {
                    continue;
                }
                if (pis[i].Name == "Description")
                {
                    propValue = rssDoc.SetCDATA(propValue.ToString());
                }
                parent.AppendChild(rssDoc.CreateNode(pis[i].Name.Substring(0, 1).ToLower() + pis[i].Name.Substring(1), propValue.ToString()));
            }
        }
        /// <summary>
        /// ��ȡ�����RSS���ݡ�
        /// </summary>
        public string OutXml
        {
            get
            {
                BuildRss();
                return rssDoc.XmlDoc.OuterXml;
            }
        }

        #region ��MDataTable����
        /// <summary>
        /// ����MDataTable,֮��ɵ���SetMap�����ֶ�ӳ��
        /// </summary>
        /// <param name="table"></param>
        public void LoadData(MDataTable table)
        {
            _MTable = table;
        }
        /// <summary>
        /// ������MDataTable����ֶ�ӳ��
        /// </summary>
        /// <param name="itemName">Rss����</param>
        /// <param name="formatText">��ʽ�����硰{0}.Name����������Ҫ��ʽ����ֱ�Ӹ�Nullֵ</param>
        /// <param name="tableColumnNames">ͬDataTable���ֶ�����</param>
        public void SetMap(RssItemName itemName, string formatText, params object[] tableColumnNames)
        {
            RssItemMap map = new RssItemMap();
            map.RssItemName = itemName.ToString();
            map.FormatText = formatText;
            map.TableColumnNames = tableColumnNames;
            mapList.Add(map);
        }
        #endregion
    }
    /// <summary>
    /// RSS Ƶ��
    /// </summary>
    public class RssChannel
    {
        #region ��ѡ
        private string _Title;
        /// <summary>
        /// ����Ƶ���ı���
        /// </summary>
        public string Title
        {
            get
            {
                return _Title;
            }
            set
            {
                _Title = value;
            }
        }
        private string _Link;
        /// <summary>
        /// ����ָ��Ƶ���ĳ�����
        /// </summary>
        public string Link
        {
            get
            {
                return _Link;
            }
            set
            {
                _Link = value;
            }
        }
        private string _Description;
        /// <summary>
        /// ����Ƶ��
        /// </summary>
        public string Description
        {
            get
            {
                return _Description;
            }
            set
            {
                _Description = value;
            }
        }
        #endregion

        #region ��ѡ
        private string _Category;
        /// <summary>
        /// Ϊ feed ����������һ����������
        /// </summary>
        public string Category
        {
            get
            {
                return _Category;
            }
            set
            {
                _Category = value;
            }
        }
        private string _Cloud;
        /// <summary>
        /// ע����̣��Ի�� feed ���µ�����֪ͨ
        /// </summary>
        public string Cloud
        {
            get
            {
                return _Cloud;
            }
            set
            {
                _Cloud = value;
            }
        }
        private string _Copyright;
        /// <summary>
        /// ��֪��Ȩ����
        /// </summary>
        public string Copyright
        {
            get
            {
                return _Copyright;
            }
            set
            {
                _Copyright = value;
            }
        }
        private string _Docs;
        /// <summary>
        /// �涨ָ��ǰ RSS �ļ����ø�ʽ˵���� URL
        /// </summary>
        public string Docs
        {
            get
            {
                return _Docs;
            }
            set
            {
                _Docs = value;
            }
        }
        private string _Generator;
        /// <summary>
        /// �涨ָ��ǰ RSS �ļ����ø�ʽ˵���� URL
        /// </summary>
        public string Generator
        {
            get
            {
                return _Generator;
            }
            set
            {
                _Generator = value;
            }
        }
        private string _Language = "zh-cn";
        /// <summary>
        /// �涨��д feed ���õ�����
        /// </summary>
        public string Language
        {
            get
            {
                return _Language;
            }
            set
            {
                _Language = value;
            }
        }

        private string _LastBuildDate;
        /// <summary>
        /// ���� feed ���ݵ�����޸�����
        /// </summary>
        public string LastBuildDate
        {
            get
            {
                return _LastBuildDate;
            }
            set
            {
                _LastBuildDate = value;
            }
        }
        private string _ManagingEditor;
        /// <summary>
        /// ���� feed ���ݱ༭�ĵ����ʼ���ַ
        /// </summary>
        public string ManagingEditor
        {
            get
            {
                return _ManagingEditor;
            }
            set
            {
                _ManagingEditor = value;
            }
        }
        private string _PubDate;
        /// <summary>
        /// Ϊ feed �����ݶ�����󷢲�����
        /// </summary>
        public string PubDate
        {
            get
            {
                return _PubDate;
            }
            set
            {
                _PubDate = value;
            }
        }
        private string _Rating;
        /// <summary>
        /// feed �� PICS ����
        /// </summary>
        public string Rating
        {
            get
            {
                return _Rating;
            }
            set
            {
                _Rating = value;
            }
        }
        private string _SkipDays;
        /// <summary>
        /// �涨���� feed ���µ���
        /// </summary>
        public string SkipDays
        {
            get
            {
                return _SkipDays;
            }
            set
            {
                _SkipDays = value;
            }
        }
        private string _SkipHours;
        /// <summary>
        /// �涨���� feed ���µ�Сʱ
        /// </summary>
        public string SkipHours
        {
            get
            {
                return _SkipHours;
            }
            set
            {
                _SkipHours = value;
            }
        }
        private string _TextInput;
        /// <summary>
        /// �涨Ӧ���� feed һͬ��ʾ���ı�������
        /// </summary>
        public string TextInput
        {
            get
            {
                return _TextInput;
            }
            set
            {
                _TextInput = value;
            }
        }
        private string _Ttl;
        /// <summary>
        /// ָ���� feed Դ���´� feed ֮ǰ��feed �ɱ�����ķ�����
        /// </summary>
        public string Ttl
        {
            get
            {
                return _Ttl;
            }
            set
            {
                _Ttl = value;
            }
        }
        private string _WebMaster;
        /// <summary>
        /// ����� feed �� web ����Ա�ĵ����ʼ���ַ
        /// </summary>
        public string WebMaster
        {
            get
            {
                return _WebMaster;
            }
            set
            {
                _WebMaster = value;
            }
        }
        private string _RssImage;
        /// <summary>
        /// ����� feed �� ͼƬLogo
        /// </summary>
        public string RssImage
        {
            get
            {
                return _RssImage;
            }
            set
            {
                _RssImage = value;
            }
        }
        #endregion
        private List<RssItem> _Items = new List<RssItem>();
        /// <summary>
        /// Ƶ����
        /// </summary>
        public List<RssItem> Items
        {
            get
            {
                return _Items;
            }
            set
            {
                _Items = value;
            }
        }
    }
    /// <summary>
    /// ͼƬ��
    /// </summary>
    public class RssImage
    {
        #region ��ѡ
        private string _Url;
        /// <summary>
        /// ͼƬ��ַ
        /// </summary>
        public string Url
        {
            get
            {
                return _Url;
            }
            set
            {
                _Url = value;
            }
        }
        private string _Title;
        /// <summary>
        /// ͼƬ����
        /// </summary>
        public string Title
        {
            get
            {
                return _Title;
            }
            set
            {
                _Title = value;
            }
        }
        private string _Link;
        /// <summary>
        /// �ṩͼƬ��վ������
        /// </summary>
        public string Link
        {
            get
            {
                return _Link;
            }
            set
            {
                _Link = value;
            }
        }
        private string _Description;
        /// <summary>
        /// ����Ƶ��
        /// </summary>
        public string Description
        {
            get
            {
                return _Description;
            }
            set
            {
                _Description = value;
            }
        }
        #endregion
    }
    /// <summary>
    /// RSS��
    /// </summary>
    public class RssItem
    {
        #region ��ѡ
        private string _Title;
        /// <summary>
        /// ����Ƶ���ı���
        /// </summary>
        public string Title
        {
            get
            {
                return _Title;
            }
            set
            {
                _Title = value;
            }
        }
        private string _Link;
        /// <summary>
        /// ����ָ��Ƶ���ĳ�����
        /// </summary>
        public string Link
        {
            get
            {
                return _Link;
            }
            set
            {
                _Link = value;
            }
        }
        private string _Description;
        /// <summary>
        /// ����Ƶ��
        /// </summary>
        public string Description
        {
            get
            {
                return _Description;
            }
            set
            {
                _Description = value;
            }
        }
        #endregion

        #region ��ѡ
        private string _Category;
        /// <summary>
        /// Ϊ feed ����������һ����������
        /// </summary>
        public string Category
        {
            get
            {
                return _Category;
            }
            set
            {
                _Category = value;
            }
        }
        private string _Author;
        /// <summary>
        /// �涨��Ŀ���ߵĵ����ʼ���ַ
        /// </summary>
        public string Author
        {
            get
            {
                return _Author;
            }
            set
            {
                _Author = value;
            }
        }
        private string _Comments;
        /// <summary>
        /// ������Ŀ���ӵ��йش���Ŀ��ע�ͣ��ļ���
        /// </summary>
        public string Comments
        {
            get
            {
                return _Comments;
            }
            set
            {
                _Comments = value;
            }
        }
        private string _Enclosure;
        /// <summary>
        /// ����һ��ý���ļ�����һ������
        /// </summary>
        public string Enclosure
        {
            get
            {
                return _Enclosure;
            }
            set
            {
                _Enclosure = value;
            }
        }
        private string _Guid;
        /// <summary>
        /// Ϊ ��Ŀ����һ��Ψһ�ı�ʶ��
        /// </summary>
        public string Guid
        {
            get
            {
                return _Guid;
            }
            set
            {
                _Guid = value;
            }
        }
        private string _PubDate;
        /// <summary>
        ///�������Ŀ����󷢲�����
        /// </summary>
        public string PubDate
        {
            get
            {
                return _PubDate;
            }
            set
            {
                _PubDate = value;
            }
        }
        private string _Source;
        /// <summary>
        /// Ϊ����Ŀָ��һ����������Դ
        /// </summary>
        public string Source
        {
            get
            {
                return _Source;
            }
            set
            {
                _Source = value;
            }
        }
        #endregion
    }
    /// <summary>
    /// RSS ��ö��
    /// </summary>
    public enum RssItemName
    {
        Title,
        Link,
        Description,
        Category,
        Author,
        Comments,
        Enclosure,
        Guid,
        PubDate,
        Source
    }
}
