using System;
using System.Xml;
using CYQ.Data.Cache;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Tool;
using System.Windows.Forms;
/*
 * �����Ż�˵����
 * 1��DTD���������û��ʵ��&xxx;�򲻼��أ������޸�DTD·�������ء�
 * 2��XmlDocument��LoadXml �����Ż������غ󻺴棬�����ӻ���ת��������Ҫת��Ҳû��ô�򵥣���Ҫ����������⣺
 * ---------------------------------------------------------------------------------------
 * A���ӻ����õ����ӻ���Doc�ó�InnerXml��������LoadXml(xml)����������ã������Ż���
 * B���ӻ����õ����ӻ���Doc���ã�Clone��CloneNode(true)���������ܲ�࣬�ڵ��ʱ�������Ǹ����š�
 * C���ӻ����õ����ӻ���Doc���֣�XmlDocument ImportNode ������CloneNode(true) ���ţ�׼�����������
 * D������B��C���ַ�ʽ�������µ����⣺.NET �±�ǩ�Ապϡ�.NET Core���������Ķ�����GetCloneFrom ������
 * E�������ǩ�Ապ����⣺�̳�XmlDocument����дCreateElement������W3C��׼�ҳ���Ҫ�Ապϵģ�������������XmlElement��IsEmpty=false������XhtmlDocument
 * F��������������Ϊ�����ܣ��ӻ����в�����DTD��������ʽ���⣺
 * G�����F�������ǣ������ʱ����û��DTDͷ����׷�ӣ�<!DOCTYPE html>ͷ������XHtmlAction OutXml�����
 * ----------------------------------------------------------------------------------------
 * 4������ʹ�� InnerXml ���ԣ���������ʽ�����InnerText���ڵ����õȣ���������һ�����⣬��ֵtext��������޷�ͨ���ڵ����������ѭ����ʱ���ò����ڵ㡣
 */
namespace CYQ.Data.Xml
{
    /// <summary>
    /// ����Xml��XHtml�Ļ���
    /// </summary>
    public abstract class XHtmlBase : IDisposable
    {
        private Encoding _Encoding = Encoding.UTF8;
        /// <summary>
        /// �ļ�����
        /// </summary>
        public Encoding Encoding
        {
            get
            {
                return _Encoding;
            }
            set
            {
                _Encoding = value;
            }
        }
        /// <summary>
        /// ����������ֵ��LoadData(MDataRow,pre)����װ��)
        /// </summary>
        protected List<string> PreList;
        /// <summary>
        /// xml����
        /// </summary>
        protected XmlDocument _XmlDocument;
        /// <summary>
        /// �ڲ�XmlDocument����ReadOnly��
        /// </summary>
        public XmlDocument XmlDoc
        {
            get
            {
                return _XmlDocument;
            }
        }
        /// <summary>
        /// �����ռ����
        /// </summary>
        protected XmlNamespaceManager xnm;
        /// <summary>
        /// �������
        /// </summary>
        protected DistributedCache theCache;
        /// <summary>
        /// Html���ƿռ�
        /// </summary>
        protected string htmlNameSpace = "http://www.w3.org/1999/xhtml";
        internal string PreXml = "preXml";
        /// <summary>
        /// ���ص�Xml�ļ���������·�������ƣ�ReadOnly��
        /// </summary>
        public string FileName
        {
            get
            {
                return _FileName;
            }
        }
        private string _FileName = string.Empty;
        /// <summary>
        /// xml�����key
        /// </summary>
        public string XmlCacheKey = string.Empty;
        private bool _IsReadOnly;
        /// <summary>
        /// XHtml ���ص�ģ���Ƿ�ֻ��ģʽ
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return _IsReadOnly;
            }
            set
            {
                _IsReadOnly = value;
            }
        }
        private bool _IsLoadFromCache;
        /// <summary>
        /// �ĵ��Ƿ�ȡ�Ի���
        /// </summary>
        public bool IsLoadFromCache
        {
            get
            {
                return _IsLoadFromCache;
            }
            set
            {
                _IsLoadFromCache = value;
            }
        }

        private double _CacheMinutes = 5;
        /// <summary>
        /// ���������(ReadOnly)
        /// </summary>
        public double CacheMinutes
        {
            get
            {
                return _CacheMinutes;
            }
        }
        /// <summary>
        /// �������ո�ʽ�����XHtml���ݣ�ReadOnly��
        /// </summary>
        public virtual string OutXml
        {
            get
            {
                return _XmlDocument.OuterXml;
            }
        }
        /// <summary>
        /// ���ص�Html�Ƿ��Ѹı䣨ReadOnly��
        /// </summary>
        public bool IsXHtmlChanged
        {
            get
            {
                return !theCache.Contains(XmlCacheKey);
            }
        }
        ///// <summary>
        ///// ����Html���Ƿ��������ע�ͽڵ㡣
        ///// </summary>
        //private bool clearCommentOnLoad = false;
        static XHtmlBase()
        {
            //������ҪDTD������xhtmlʱת&����ת�塣
           // XHtmlUrlResolver.Instance.InitDTD();
        }


        public XHtmlBase()
        {
            _XmlDocument = new XmlDocument();
            theCache = DistributedCache.Local;
        }
        protected void LoadNameSpace(string nameSpace)
        {
            xnm = new XmlNamespaceManager(_XmlDocument.NameTable);
            xnm.AddNamespace(PreXml, nameSpace);
        }
        /// <summary>
        /// �Ӿ���·���л���ļ�����ΪKeyֵ
        /// </summary>
        private string GenerateKey(string fileName)
        {
            _FileName = fileName;
            fileName = fileName.Replace(AppConfig.WebRootPath, "XHtmlBase_");
            fileName = fileName.Replace("/", "").Replace("\\", "");
            return fileName;
        }

        #region ����xml
        /// <summary>
        /// docTypeHtml��һ���ġ�
        /// </summary>
        //protected static string docTypeHtml = string.Empty;
        //private const string docType = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
        /// <summary>
        /// ��xml�ַ�������
        /// </summary>
        /// <param name="xml">xml�ַ���</param>
        public void LoadXml(string xml)
        {
            try
            {
                if (xnm != null)
                {
                    if (xml.StartsWith("<!DOCTYPE html"))
                    {
                        //ȥ��DTDͷ��
                        xml = xml.Substring(xml.IndexOf('>') + 1).Trim();
                        //if (xml.Contains("&"))
                        //{
                        //    if (string.IsNullOrEmpty(docTypeHtml))
                        //    {
                        //        docTypeHtml = "<!DOCTYPE html PUBLIC \" -//W3C//DTD XHTML 1.0 Transitional//EN\" \"" + AppConfig.XHtml.DtdUri + "\">";
                        //    }
                        //    xml = xml.Replace(xml.Substring(0, xml.IndexOf('>') + 1), docTypeHtml);
                        //}
                        //else if (xml.StartsWith(docType))
                        //{
                        //    xml = xml.Replace(xml.Substring(0, xml.IndexOf('>') + 1), "<!DOCTYPE html>");
                        //}
                    }

                    if (xml.IndexOf(AppConfig.XHtml.CDataLeft) == -1)
                    {
                        int body = Math.Max(xml.IndexOf("<body"), xml.IndexOf("</head>"));
                        int scriptStart = xml.IndexOf("<script", body);
                        if (scriptStart > 0)
                        {
                            int scriptEnd = xml.IndexOf("</script>", scriptStart);
                            while (scriptStart < scriptEnd && scriptStart > 0)
                            {
                                //����Ƿ���ڽű��������ڣ�׷��CData
                                xml = xml.Insert(scriptEnd + 9, AppConfig.XHtml.CDataRight);
                                xml = xml.Insert(scriptStart, AppConfig.XHtml.CDataLeft);
                                scriptStart = xml.IndexOf("<script", scriptEnd + 10);
                                if (scriptStart > 0)
                                {
                                    scriptEnd = xml.IndexOf("</script>", scriptStart);
                                }
                            }
                        }
                    }
                }
                xml = Filter(xml);
                _XmlDocument.LoadXml(xml);
            }
            catch (XmlException err)
            {
                throw new XmlException(err.Message);
            }
        }
        /// <summary>
        /// ���ļ��м���Xml
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool Load(string fileName)
        {
            return Load(fileName, XmlCacheLevel.Lower);
        }
        /// <summary>
        /// ����XML
        /// </summary>
        public bool Load(string fileName, XmlCacheLevel level)
        {
            return Load(fileName, level, false);
        }
        /// <summary>
        /// ����Xml�ļ�
        /// </summary>
        /// <param name="fileName">�ļ���</param>
        /// <param name="level">�ļ����漶��</param>
        /// <param name="clearCommentNode">���غ��Ƿ����ע�ͽڵ�</param>
        public bool Load(string fileName, XmlCacheLevel level, bool clearCommentNode)
        {

            bool loadState = false;
            XmlCacheKey = GenerateKey(fileName);//��·���л���ļ�����Ϊkey
            if (level != XmlCacheLevel.NoCache)
            {
                loadState = LoadFromCache(XmlCacheKey);//��Cache����Xml
            }
            if (!loadState)//Cache����Xmlʧ��
            {
                _CacheMinutes = (double)level;
                loadState = LoadFromFile(fileName, clearCommentNode);//���ļ�����Xml
            }
            return loadState;
        }

        /// <summary>
        /// �ӻ����м���html
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool LoadFromCache(string key)
        {
            if (theCache.Contains(key))//�����д��ڶ�Ӧֵ��key�Ķ���
            {
                _XmlDocument = null;
                var cacheObj = theCache.Get(key) as XmlDocument;
                if (_IsReadOnly)
                {
                    _XmlDocument = cacheObj;
                }
                else
                {
                    var cloneDoc = GetCloneFrom(cacheObj);
                    _XmlDocument = cloneDoc;
                }
                _IsLoadFromCache = true;
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// �Ƴ�����ע�ͽڵ�
        /// </summary>
        public virtual void RemoveCommentNode()
        {
        }
        /// <summary>
        /// ���ļ�����XML �����յ��õķ�����
        /// </summary>
        private bool LoadFromFile(string fileName, bool clearCommentNode)
        {
            if (!System.IO.File.Exists(fileName))
            {
                //Log.WriteLog("filename no exist : " + fileName);
                return false;
            }
            try
            {
                string html = string.Empty;
                if (xnm != null)
                {
                    html = IOHelper.ReadAllText(fileName, 0, _Encoding);
                    //ResolverDtd.Resolver(ref _XmlDocument);//ָ������������DTD�ļ�������Ŀ¼��
                }

                if (html != string.Empty)
                {
                    LoadXml(html);//���ַ�������html
                }
                else
                {
                    _XmlDocument.Load(fileName);//���ļ�����xml
                }
                if (clearCommentNode)
                {
                    RemoveCommentNode();
                }
                XmlCacheKey = GenerateKey(fileName);
                if (!theCache.Contains(XmlCacheKey))
                {
                    SaveToCache(XmlCacheKey, !IsReadOnly);
                }
                return true;
            }
            catch (Exception err)
            {
                Error.Throw(err.Message + " FileName : " + fileName);
            }
            return false;
        }
        #endregion

        #region ��������
        /// <summary>
        /// ���ĵ����浽ȫ��Cache��
        /// </summary>
        /// <param name="key">�����Key����</param>
        /// <param name="isClone">�Ƿ��¡�����浵</param>
        public void SaveToCache(string key, bool isClone)
        {
            if (_CacheMinutes > 0)
            {
                SaveToCache(key, isClone, _CacheMinutes);
            }
        }
        /// <summary>
        /// ���ĵ����浽ȫ��Cache��
        /// </summary>
        /// <param name="key">�����Key����</param>
        /// <param name="isClone">�Ƿ��¡�����浵</param>
        /// <param name="cacheTimeMinutes">�浵�ķ�����</param>
        public void SaveToCache(string key, bool isClone, double cacheTimeMinutes)
        {
            if (_XmlDocument != null)
            {
                if (!isClone)
                {
                    theCache.Set(key, _XmlDocument, cacheTimeMinutes, _FileName);//���Cache����
                }
                else
                {
                    var cloneDoc = GetCloneFrom(_XmlDocument);
                    theCache.Set(key, cloneDoc, cacheTimeMinutes, _FileName);//���Cache����Clone
                }
            }
        }
        /// <summary>
        /// �ļ�����
        /// </summary>
        public bool Save()
        {
            return Save(_FileName);
        }
        /// <param name="fileName">ָ������·��</param>
        public bool Save(string fileName)
        {
            if (Path.GetFileName(fileName).IndexOfAny(AppConst.InvalidFileNameChars) > -1)//������Ч��·���ַ���
            {
                Log.Write("XHtmlBase.Save : InvalidPath : " + fileName, LogType.Error);
                return false;
            }
            if (_XmlDocument == null) { return false; }

            string html = _XmlDocument.InnerXml;

            if (!string.IsNullOrEmpty(html))
            {
                if (html.Contains(AppConfig.XHtml.DtdUri))
                {
                    html = _XmlDocument.InnerXml.Replace(AppConfig.XHtml.DtdUri, "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd");
                }
                return IOHelper.Write(fileName, html, _Encoding);
                //if (Directory.Exists(Path.GetDirectoryName(fileName)))
                //{
                //    try
                //    {
                //        File.WriteAllText(fileName, xHtml, _Encoding);
                //        return true;
                //    }
                //    catch
                //    {
                //        Thread.Sleep(20);
                //        try
                //        {
                //            File.WriteAllText(fileName, xHtml, _Encoding);
                //            return true;
                //        }
                //        catch (Exception err)
                //        {
                //            Log.Write(err, LogType.Error);
                //        }
                //    }
                //}
                //}
                //else
                //{
                //    Log.Write("No exist path folder:" + fileName, LogType.Error);
                //}
            }
            return false;
        }
        private XmlDocument GetCloneFrom(XmlDocument xDoc)
        {
            //if (true || AppConfig.IsNetCore)
            //{
                //���ָ�Ч���������Ӳ������ã�������޸ģ�ֱ���޸��˻���ڵ㡣
                XHtmlDocument document = new XHtmlDocument();

                ////document.CreateNode( XmlNodeType.Element,"aaa"


                //XmlElement ele = document.CreateElement("div");
                //ele.IsEmpty = false;

                //document.AppendChild(ele);

                //if (xDoc.DocumentType != null && !AppConfig.IsNetCore)
                //{
                //    XmlNode docType = document.ImportNode(xDoc.DocumentType, false);
                //    document.AppendChild(docType);
                //}
                //������DTD�����ٴ����ٶȡ�
                XmlNode node = document.ImportNode(xDoc.DocumentElement, true);
                document.AppendChild(node);


                return document as XmlDocument;
            //}
            //else
            //{
            //    //ASP.NET �� bug����clone�ڵ㷽������������⣬�ڵ㶪ʧ�����������ض���ʧ��
            //    XmlDocument newDoc = new XmlDocument();
            //    try
            //    {
            //        newDoc.LoadXml(xDoc.InnerXml);
            //    }
            //    catch (Exception err)
            //    {
            //        Log.Write(err, LogType.Error);
            //        newDoc.InnerXml = xDoc.InnerXml;
            //    }
            //    return newDoc;

            //}
            //    int length = xDoc.InnerXml.Length;
            //    XmlDocument doc = xDoc.CloneNode(true) as XmlDocument;
            //    if (doc.InnerXml.Length != length)
            //    {

            //    }
            //    if (this.FileName == "D:\\Code\\taurus.git\\Taurus.MVC\\demo\\default\\Taurus.View\\Views\\Admin\\metric.html")
            //    {

            //    }
            //    //��¡�ٶȿ졣
            //    return xDoc.Clone() as XmlDocument;

        }
        protected XmlNode Fill(string xPath, XmlNode parent)
        {
            try
            {
                if (parent != null)
                {
                    return parent.SelectSingleNode(xPath.Replace("//", "descendant::"), xnm);
                }
                return _XmlDocument.SelectSingleNode(xPath, xnm);
            }
            catch
            {
                return null;
            }
        }
        protected XmlNodeList Select(string xPath, XmlNode parent)
        {
            try
            {
                if (parent != null)
                {
                    return parent.SelectNodes(xPath.Replace("//", "descendant::"), xnm);
                }
                return _XmlDocument.SelectNodes(xPath, xnm);
            }
            catch
            {
                return null;
            }
        }
        protected XmlElement Create(string tag)
        {
            if (xnm == null)
            {
                return _XmlDocument.CreateElement(tag);
            }
            return _XmlDocument.CreateElement(tag, xnm.LookupNamespace(PreXml));
        }
        protected string GetXPath(string tag, string attr, string value)
        {
            string xPath = "//" + (xnm != null ? PreXml + ":" : "") + tag; //+ "[@" + attr + "='" + value + "']";
            if (!string.IsNullOrEmpty(attr))
            {
                if (!string.IsNullOrEmpty(value))
                {
                    //���Դ�Сд����
                    //xPath += "[@" + attr + "='" + value + "']";
                    xPath += "[translate(@" + attr + ",'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='" + value.ToLower() + "']";
                    //xPath += "[@" + attr + "=translate('" + value + "','ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')]";
                }
                else
                {
                    xPath += "[@" + attr + "]";
                }
            }
            return xPath;
        }
        /// <summary>
        /// ��ָ�����ַ�����CDATA
        /// </summary>
        /// <param name="html">�����ַ�</param>
        /// <returns></returns>
        public string SetCDATA(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }
            html = html.Replace(AppConfig.XHtml.CDataLeft, string.Empty).Replace(AppConfig.XHtml.CDataRight, string.Empty);
            html = html.Replace("<![CDATA[", "&lt;![CDATA[").Replace("]]>", "]]&gt;");
            //text = text.Replace(((char)10).ToString(), "<BR>");
            //text = text.Replace(((char)13).ToString(), "<BR>");
            //text = text.Replace(((char)34).ToString(), "&quot;");
            //text = text.Replace(((char)39).ToString(), "&#39;");
            html = html.Replace("\\", "#!!#").Replace("\0", "#!0!#");
            html = Filter(html);
            return AppConfig.XHtml.CDataLeft + html + AppConfig.XHtml.CDataRight;
        }
        /// <summary>
        /// ���CDATA
        /// </summary>
        /// <param name="html">�����ַ�</param>
        /// <returns></returns>
        public string ClearCDATA(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }
            if (html.Contains("#!"))
            {
                html = html.Replace("#!!#", "\\").Replace("#!0!#", "\\0");
            }
            if (html.Contains(AppConfig.XHtml.CDataLeft))
            {
                html = html.Replace(AppConfig.XHtml.CDataLeft, string.Empty).Replace(AppConfig.XHtml.CDataRight, string.Empty);
            }
            return html;
        }
        /// <summary>
        /// ����XML(ʮ������ֵ 0x1D)��Ч���ַ���ͬʱ�滻&gt;���ţ�
        /// </summary>
        protected string Filter(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            StringBuilder info = new StringBuilder(text.Length + 20);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (((c >= 0) && (c <= 8)) || ((c >= 11) && (c <= 12)) || ((c >= 14) && (c <= 32)))
                {
                    info.AppendFormat(" ", c);//&#x{0:X};
                }
                else if (c == '&')
                {
                    info.Append("&amp;");
                    //                    &(�߼���)  &amp;        
                    //<(С��)    &lt;        
                    //>(����)    &gt;        
                    //"(˫����)  &quot;      
                    //'(������)  &apos; 
                }
                else
                {
                    if (i > 50 && i != text.Length - 1)
                    {
                        char nc = text[i + 1];
                        if (c == '<' && nc != '/' && nc != '!' && !IsEnChar(nc)) // ��Ӣ����ĸ��
                        {
                            info.Append("&lt;");
                            continue;
                        }
                    }
                    info.Append(c);

                }
            }
            return info.ToString();
        }
        private bool IsEnChar(char c)//Ӣ����ĸ
        {
            return (c > 64 && c < 91) || (c > 96 && c < 123);
        }
        ///// <summary>
        ///// ���������滻��
        ///// </summary>
        //private string RegexReplace(string text)
        //{
        //    Regex regex = new Regex(@"</?[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        //    MatchCollection collection = regex.Matches(text);
        //    foreach (Match mat in collection)
        //    {
        //        string value = mat.Value;
        //    }
        //    return text;
        //}
        #endregion

        #region IDisposable ��Ա
        /// <summary>
        /// �ͷ���Դ
        /// </summary>
        public virtual void Dispose()
        {
            if (_XmlDocument != null)
            {
                //if (!ReadOnly)
                //{
                //xmlDoc.RemoveAll();
                //}
                _XmlDocument = null;
                //GC.Collect();
            }

        }

        #endregion
    }

}
