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
 * 性能优化说明：
 * 1、DTD：如果界面没有实体&xxx;则不加载，否则修改DTD路径到本地。
 * 2、XmlDocument：LoadXml 性能优化：加载后缓存，后续从缓存转化：这里要转化也没那么简单，需要解决以下问题：
 * ---------------------------------------------------------------------------------------
 * A、从缓存拿到，从缓存Doc拿出InnerXml，再重新LoadXml(xml)，兼容性最好，但可优化。
 * B、从缓存拿到，从缓存Doc调用：Clone、CloneNode(true)、两个性能差不多，节点多时，后面那个更优。
 * C、从缓存拿到，从缓存Doc发现：XmlDocument ImportNode 方法比CloneNode(true) 更优，准备采用这个。
 * D、上面B、C两种方式，产生新的问题：.NET 下标签自闭合、.NET Core下正常，改动见：GetCloneFrom 方法。
 * E、解决标签自闭合问题：继承XmlDocument，改写CreateElement，根据W3C标准找出需要自闭合的，其余条件设置XmlElement的IsEmpty=false，见：XhtmlDocument
 * F、解决上述问题后，为了性能，从缓存中不加载DTD、引发样式问题：
 * G、解决F的问题是，输出的时候检测没有DTD头，则追加：<!DOCTYPE html>头，见：XHtmlAction OutXml输出。
 * ----------------------------------------------------------------------------------------
 * 4、避免使用 InnerXml 属性，用其它方式替代：InnerText、节点引用等，但引发另一个问题，赋值text，则后续无法通过节点操作，这在循环绑定时会拿不到节点。
 */
namespace CYQ.Data.Xml
{
    /// <summary>
    /// 操作Xml及XHtml的基类
    /// </summary>
    public abstract class XHtmlBase : IDisposable
    {
        private Encoding _Encoding = Encoding.UTF8;
        /// <summary>
        /// 文件编码
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
        /// 用于批量赋值（LoadData(MDataRow,pre)方法装载)
        /// </summary>
        protected List<string> PreList;
        /// <summary>
        /// xml对象
        /// </summary>
        protected XmlDocument _XmlDocument;
        /// <summary>
        /// 内部XmlDocument对象（ReadOnly）
        /// </summary>
        public XmlDocument XmlDoc
        {
            get
            {
                return _XmlDocument;
            }
        }
        /// <summary>
        /// 命名空间对象
        /// </summary>
        protected XmlNamespaceManager xnm;
        /// <summary>
        /// 缓存对象
        /// </summary>
        protected DistributedCache theCache;
        /// <summary>
        /// Html名称空间
        /// </summary>
        protected string htmlNameSpace = "http://www.w3.org/1999/xhtml";
        internal string PreXml = "preXml";
        /// <summary>
        /// 加载的Xml文件的完整（路径）名称（ReadOnly）
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
        /// xml缓存的key
        /// </summary>
        public string XmlCacheKey = string.Empty;
        private bool _IsReadOnly;
        /// <summary>
        /// XHtml 加载的模板是否只读模式
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
        /// 文档是否取自缓存
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
        /// 缓存分钟数(ReadOnly)
        /// </summary>
        public double CacheMinutes
        {
            get
            {
                return _CacheMinutes;
            }
        }
        /// <summary>
        /// 返回最终格式化后的XHtml内容（ReadOnly）
        /// </summary>
        public virtual string OutXml
        {
            get
            {
                return _XmlDocument.OuterXml;
            }
        }
        /// <summary>
        /// 加载的Html是否已改变（ReadOnly）
        /// </summary>
        public bool IsXHtmlChanged
        {
            get
            {
                return !theCache.Contains(XmlCacheKey);
            }
        }
        ///// <summary>
        ///// 加载Html后是否清除所有注释节点。
        ///// </summary>
        //private bool clearCommentOnLoad = false;
        static XHtmlBase()
        {
            //不再需要DTD，加载xhtml时转&进行转义。
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
        /// 从绝对路径中获得文件名做为Key值
        /// </summary>
        private string GenerateKey(string fileName)
        {
            _FileName = fileName;
            fileName = fileName.Replace(AppConfig.WebRootPath, "XHtmlBase_");
            fileName = fileName.Replace("/", "").Replace("\\", "");
            return fileName;
        }

        #region 加载xml
        /// <summary>
        /// docTypeHtml是一样的。
        /// </summary>
        //protected static string docTypeHtml = string.Empty;
        //private const string docType = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
        /// <summary>
        /// 从xml字符串加载
        /// </summary>
        /// <param name="xml">xml字符串</param>
        public void LoadXml(string xml)
        {
            try
            {
                if (xnm != null)
                {
                    if (xml.StartsWith("<!DOCTYPE html"))
                    {
                        //去除DTD头。
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
                                //检测是否存在脚本，若存在，追加CData
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
        /// 从文件中加载Xml
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool Load(string fileName)
        {
            return Load(fileName, XmlCacheLevel.Lower);
        }
        /// <summary>
        /// 加载XML
        /// </summary>
        public bool Load(string fileName, XmlCacheLevel level)
        {
            return Load(fileName, level, false);
        }
        /// <summary>
        /// 加载Xml文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="level">文件缓存级别</param>
        /// <param name="clearCommentNode">加载后是否清除注释节点</param>
        public bool Load(string fileName, XmlCacheLevel level, bool clearCommentNode)
        {

            bool loadState = false;
            XmlCacheKey = GenerateKey(fileName);//从路径中获得文件名做为key
            if (level != XmlCacheLevel.NoCache)
            {
                loadState = LoadFromCache(XmlCacheKey);//从Cache加载Xml
            }
            if (!loadState)//Cache加载Xml失败
            {
                _CacheMinutes = (double)level;
                loadState = LoadFromFile(fileName, clearCommentNode);//从文件加载Xml
            }
            return loadState;
        }

        /// <summary>
        /// 从缓存中加载html
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool LoadFromCache(string key)
        {
            if (theCache.Contains(key))//缓存中存在对应值是key的对象
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
        /// 移除所有注释节点
        /// </summary>
        public virtual void RemoveCommentNode()
        {
        }
        /// <summary>
        /// 从文件加载XML （最终调用的方法）
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
                    //ResolverDtd.Resolver(ref _XmlDocument);//指定，才能生成DTD文件到本地目录。
                }

                if (html != string.Empty)
                {
                    LoadXml(html);//从字符串加载html
                }
                else
                {
                    _XmlDocument.Load(fileName);//从文件加载xml
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

        #region 其它方法
        /// <summary>
        /// 将文档缓存到全局Cache中
        /// </summary>
        /// <param name="key">缓存的Key名称</param>
        /// <param name="isClone">是否克隆副本存档</param>
        public void SaveToCache(string key, bool isClone)
        {
            if (_CacheMinutes > 0)
            {
                SaveToCache(key, isClone, _CacheMinutes);
            }
        }
        /// <summary>
        /// 将文档缓存到全局Cache中
        /// </summary>
        /// <param name="key">缓存的Key名称</param>
        /// <param name="isClone">是否克隆副本存档</param>
        /// <param name="cacheTimeMinutes">存档的分钟数</param>
        public void SaveToCache(string key, bool isClone, double cacheTimeMinutes)
        {
            if (_XmlDocument != null)
            {
                if (!isClone)
                {
                    theCache.Set(key, _XmlDocument, cacheTimeMinutes, _FileName);//添加Cache缓存
                }
                else
                {
                    var cloneDoc = GetCloneFrom(_XmlDocument);
                    theCache.Set(key, cloneDoc, cacheTimeMinutes, _FileName);//添加Cache缓存Clone
                }
            }
        }
        /// <summary>
        /// 文件保存
        /// </summary>
        public bool Save()
        {
            return Save(_FileName);
        }
        /// <param name="fileName">指定保存路径</param>
        public bool Save(string fileName)
        {
            if (Path.GetFileName(fileName).IndexOfAny(AppConst.InvalidFileNameChars) > -1)//包含无效的路径字符。
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
                //各种高效方法，都逃不开引用，后面的修改，直接修改了缓存节点。
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
                //不加载DTD，加速处理速度。
                XmlNode node = document.ImportNode(xDoc.DocumentElement, true);
                document.AppendChild(node);


                return document as XmlDocument;
            //}
            //else
            //{
            //    //ASP.NET 有 bug，用clone节点方法，会产生意外，节点丢失，连基础加载都丢失。
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
            //    //克隆速度快。
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
                    //忽略大小写搜索
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
        /// 给指定的字符加上CDATA
        /// </summary>
        /// <param name="html">对象字符</param>
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
        /// 清除CDATA
        /// </summary>
        /// <param name="html">对象字符</param>
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
        /// 过滤XML(十六进制值 0x1D)无效的字符（同时替换&gt;符号）
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
                    //                    &(逻辑与)  &amp;        
                    //<(小于)    &lt;        
                    //>(大于)    &gt;        
                    //"(双引号)  &quot;      
                    //'(单引号)  &apos; 
                }
                else
                {
                    if (i > 50 && i != text.Length - 1)
                    {
                        char nc = text[i + 1];
                        if (c == '<' && nc != '/' && nc != '!' && !IsEnChar(nc)) // 非英文字母。
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
        private bool IsEnChar(char c)//英文字母
        {
            return (c > 64 && c < 91) || (c > 96 && c < 123);
        }
        ///// <summary>
        ///// 二次正则替换。
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

        #region IDisposable 成员
        /// <summary>
        /// 释放资源
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
