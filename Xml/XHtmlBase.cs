using System;
using System.Xml;
using CYQ.Data.Cache;
using System.Web.Caching;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;
using System.Text.RegularExpressions;
using CYQ.Data.Tool;

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
        protected Dictionary<string, MDataRow> dicForAutoSetValue;
        /// <summary>
        /// xml对象
        /// </summary>
        protected XmlDocument _XmlDocument;
        /// <summary>
        /// 内部XmlDocument对象
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
        protected CacheManage theCache;
        /// <summary>
        /// Html名称空间
        /// </summary>
        protected string htmlNameSpace = "http://www.w3.org/1999/xhtml";
        internal string PreXml = "preXml";
        /// <summary>
        /// 加载的Xml文件的完整（路径）名称
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
        public string xmlCacheKey = string.Empty;
        private bool _NoClone;
        /// <summary>
        /// 是否只读，只读时缓存[存取]不再Clone
        /// </summary>
        public bool NoClone
        {
            get
            {
                return _NoClone;
            }
            set
            {
                _NoClone = value;
            }
        }
        private bool _DocIsCache;
        /// <summary>
        /// 文档是否取自缓存
        /// </summary>
        public bool DocIsCache
        {
            get
            {
                return _DocIsCache;
            }
            set
            {
                _DocIsCache = value;
            }
        }
        /// <summary>
        /// Cache发生变化[用户更改缓存并设置更改标识]
        /// </summary>
        public bool CacheIsChanged
        {
            get
            {
                return theCache.GetHasChanged(xmlCacheKey);
            }
            set
            {
                theCache.SetChange(xmlCacheKey, value);
            }
        }
        /// <summary>
        /// 目标XHtml文件是否被修改
        /// </summary>
        public bool XHtmlIsChanged
        {
            get
            {
                return theCache.GetFileDependencyHasChanged(xmlCacheKey);
            }
        }
        //private CacheItemPriority _CacheLevel = CacheItemPriority.Default;
        ///// <summary>
        ///// XHtml缓存级别[默认Default,非Default的将不受定时缓存清理的影响]，设置值时需要在Load方法调用之前使用才有效
        ///// </summary>
        //public CacheItemPriority CacheLevel
        //{
        //    get
        //    {
        //        return _CacheLevel;
        //    }

        //    set
        //    {
        //        _CacheLevel = value;
        //    }
        //}
        private double _CacheMinutes = 5;
        /// <summary>
        /// 缓存分钟数
        /// </summary>
        public double CacheMinutes
        {
            get
            {
                return _CacheMinutes;
            }
        }
        /// <summary>
        /// 返回最终格式化后的XHtml内容。
        /// </summary>
        public string OutXml
        {
            get
            {
                if (_XmlDocument != null)
                {
                    string xml = _XmlDocument.InnerXml.Replace(".dtd\"[]>", ".dtd\">");
                    if (xml.IndexOf(" xmlns=") > -1)
                    {
                        xml = xml.Replace(" xmlns=\"\"", string.Empty).Replace(" xmlns=\"" + xnm.LookupNamespace(PreXml) + "\"", string.Empty);
                    }
                    string html = ClearCDATA(xml);
                    if (dicForAutoSetValue != null && dicForAutoSetValue.Count > 0 && html.Contains("{$"))
                    {
                        MatchCollection matchs = Regex.Matches(html, @"\{\$([\S\s]*?)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                        if (matchs != null && matchs.Count > 0)
                        {
                            MDataCell matchCell = null;
                            string[] items = null;
                            string columnName = null, value = null; ;
                            List<string> keys = new List<string>(matchs.Count);
                            foreach (Match match in matchs)
                            {
                                value = match.Groups[0].Value;
                                if (!keys.Contains(value))
                                {
                                    keys.Add(value);
                                    items = match.Groups[1].Value.Trim().Split('#', '-');
                                    string pre = items.Length > 1 ? items[0] : "";
                                    columnName = items.Length > 1 ? items[1] : items[0];
                                    if (dicForAutoSetValue.ContainsKey(pre))
                                    {
                                        matchCell = dicForAutoSetValue[pre][columnName];
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
                    }
                    return html;
                }
                return string.Empty;
            }
        }
        ///// <summary>
        ///// 加载Html后是否清除所有注释节点。
        ///// </summary>
        //private bool clearCommentOnLoad = false;

        public XHtmlBase()
        {
            //License.Check(DAL.DalCreate.XHtmlClient);
            _XmlDocument = new XmlDocument();
            theCache = CacheManage.LocalInstance;
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
            fileName = fileName.Replace(AppDomain.CurrentDomain.BaseDirectory, "XHtmlBase_");
            fileName = fileName.Replace("/", "").Replace("\\", "");
            return fileName;
        }

        #region 加载xml
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
                    xml = xml.Replace("http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", AppConfig.XHtml.DtdUri);
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
            xmlCacheKey = GenerateKey(fileName);//从路径中获得文件名做为key
            if (level != XmlCacheLevel.NoCache)
            {
                loadState = LoadFromCache(xmlCacheKey);//从Cache加载Xml
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
                if (_NoClone)
                {
                    _XmlDocument = theCache.Get(key) as XmlDocument;
                }
                else
                {
                    _XmlDocument = GetCloneFrom(theCache.Get(key) as XmlDocument);
                }
                _DocIsCache = true;
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
        /// 从文件加载XML
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
                    if (AppConfig.XHtml.UseFileLoadXml)
                    {
                        html = IOHelper.ReadAllText(fileName, _Encoding);
                    }
                    else
                    {
                        ResolverDtd.Resolver(ref _XmlDocument);
                    }
                }

                if (html != string.Empty)
                {
                    LoadXml(html);//从字符串加载xml
                }
                else
                {
                    _XmlDocument.Load(fileName);//从文件加载xml
                }
                if (clearCommentNode)
                {
                    RemoveCommentNode();
                }
                xmlCacheKey = GenerateKey(fileName);
                if (!theCache.Contains(xmlCacheKey))
                {
                    SaveToCache(xmlCacheKey, NoClone);
                }
                return true;
            }
            catch (Exception err)
            {
                Log.WriteLog(err.Message + "filename : " + fileName);
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
                    theCache.Add(key, _XmlDocument, _FileName, cacheTimeMinutes);//添加Cache缓存
                }
                else
                {
                    theCache.Add(key, GetCloneFrom(_XmlDocument), _FileName, cacheTimeMinutes);//添加Cache缓存Clone
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
                Log.WriteLogToTxt("XHtmlBase.Save : InvalidPath : " + fileName);
                return false;
            }
            string xHtml = string.Empty;
            if (_XmlDocument != null && _XmlDocument.InnerXml.Length > 0)
            {
                xHtml = _XmlDocument.InnerXml.Replace(AppConfig.XHtml.DtdUri, "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd");

            }
            if (!string.IsNullOrEmpty(xHtml))
            {
                if (Directory.Exists(Path.GetDirectoryName(fileName)))
                {
                    try
                    {
                        File.WriteAllText(fileName, xHtml, _Encoding);
                        return true;
                    }
                    catch
                    {
                        Thread.Sleep(20);
                        try
                        {
                            File.WriteAllText(fileName, xHtml, _Encoding);
                            return true;
                        }
                        catch (Exception err)
                        {
                            Log.WriteLogToTxt(err);
                        }
                    }
                    //}
                }
                else
                {
                    Log.WriteLogToTxt("No exist path folder:" + fileName);
                }
            }
            return false;
        }
        private XmlDocument GetCloneFrom(XmlDocument xDoc)
        {
            XmlDocument newDoc = new XmlDocument();
            if (xnm != null && !AppConfig.XHtml.UseFileLoadXml)
            {
                ResolverDtd.Resolver(ref newDoc);
            }
            try
            {
                newDoc.LoadXml(xDoc.InnerXml);
            }
            catch (Exception err)
            {
                Log.WriteLogToTxt(err);
                newDoc.InnerXml = xDoc.InnerXml;
            }
            return newDoc;
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
                    xPath += "[@" + attr + "='" + value + "']";
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
        /// <param name="text">对象字符</param>
        /// <returns></returns>
        public string SetCDATA(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            text = text.Replace(AppConfig.XHtml.CDataLeft, string.Empty).Replace(AppConfig.XHtml.CDataRight, string.Empty);
            text = text.Replace("<![CDATA[", "&lt;![CDATA[").Replace("]]>", "]]&gt;");
            //text = text.Replace(((char)10).ToString(), "<BR>");
            //text = text.Replace(((char)13).ToString(), "<BR>");
            //text = text.Replace(((char)34).ToString(), "&quot;");
            //text = text.Replace(((char)39).ToString(), "&#39;");
            text = text.Replace("\\", "#!!#").Replace("\0", "#!0!#");
            text = Filter(text);
            return AppConfig.XHtml.CDataLeft + text + AppConfig.XHtml.CDataRight;
        }
        /// <summary>
        /// 清除CDATA
        /// </summary>
        /// <param name="text">对象字符</param>
        /// <returns></returns>
        public string ClearCDATA(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            text = text.Replace("#!!#", "\\").Replace("#!0!#", "\\0");
            text = text.Replace(AppConfig.XHtml.CDataLeft, string.Empty).Replace(AppConfig.XHtml.CDataRight, string.Empty);
            return text;
        }
        /// <summary>
        /// 替换掉MMS前缀
        /// </summary>
        /// <param name="text">对象字符</param>
        /// <returns></returns>
        public string ClearMMS(string text)
        {
            return text.Replace("MMS::", string.Empty).Replace("::MMS", string.Empty);
        }
        /// <summary>
        /// 过滤XML(十六进制值 0x1D)无效的字符
        /// </summary>
        protected string Filter(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            StringBuilder info = new StringBuilder(text.Length);
            foreach (char cc in text)
            {
                int ss = (int)cc;
                if (((ss >= 0) && (ss <= 8)) || ((ss >= 11) && (ss <= 12)) || ((ss >= 14) && (ss <= 32)))
                {
                    info.AppendFormat(" ", ss);//&#x{0:X};
                }
                else
                {
                    info.Append(cc);
                }
            }
            return info.ToString();
            // return System.Text.RegularExpressions.Regex.Replace(html, @"[\x00-\x08]|[\x0B-\x0C]|[\x0E-\x1F]", "");  
        }
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

    /// <summary>
    /// 开线程保存xml文件
    /// </summary>
    //internal class ThreadSaveXml
    //{
    //    private static readonly object lockWriteObj = new object();
    //    /// <summary>
    //    /// 保存的文件列表。
    //    /// </summary>
    //    private static Dictionary<string, string> saveList = new Dictionary<string, string>();
    //    public static void AddToSaveList(string savePath, string html)
    //    {
    //        try
    //        {
    //            if (saveList.ContainsKey(savePath))
    //            {
    //                saveList[savePath] = html;
    //            }
    //            else
    //            {
    //                saveList.Add(savePath, html);
    //            }
    //        }
    //        catch (Exception err)
    //        {
    //            Log.WriteLogToTxt(err);
    //        }
    //        //修改线程属性。
    //        Run();
    //    }

    //    static Thread thread = null;
    //    public static void Run()
    //    {
    //        lock (lockWriteObj)
    //        {
    //            if (thread == null || !thread.IsAlive)
    //            {
    //                thread = new Thread(new ThreadStart(WriteFile));
    //                thread.IsBackground = true;

    //            }
    //        }
    //        switch (thread.ThreadState)
    //        {
    //            case ThreadState.Background | ThreadState.Suspended:
    //                thread.Resume();
    //                break;
    //            case ThreadState.Background:
    //            case ThreadState.Background | ThreadState.WaitSleepJoin:
    //                break;
    //            default:
    //                thread.Start();
    //                break;
    //        }
    //    }
    //    private static void WriteFile()
    //    {
    //        while (true)
    //        {
    //            if (saveList.Count > 0)
    //            {
    //                Dictionary<string, string> newList = saveList;
    //                saveList = new Dictionary<string, string>();//直接转移。
    //                foreach (KeyValuePair<string, string> list in newList)
    //                {
    //                    try
    //                    {
    //                        File.WriteAllText(list.Key, list.Value, Encoding.UTF8);
    //                    }
    //                    catch (Exception err)
    //                    {
    //                        Log.WriteLogToTxt("XHtmlBase.Save : InvalidPath : " + fileName);
    //                        Log.WriteLogToTxt(err);
    //                    }
    //                    finally
    //                    {
    //                        Thread.Sleep(10);
    //                    }
    //                }
    //                newList.Clear();
    //                newList = null;
    //                Thread.Sleep(100);//0.1秒。
    //            }
    //            else
    //            {
    //                Thread.Sleep(10000);//10秒
    //            }
    //        }
    //    }
    //}
}
