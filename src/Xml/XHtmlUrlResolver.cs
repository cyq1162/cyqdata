using CYQ.Data.Properties;
using CYQ.Data.Tool;
using System;
using System.IO;

namespace CYQ.Data.Xml
{
    //internal class ResolverDtd
    //{
    //    public static void Resolver(ref System.Xml.XmlDocument xmlDoc)
    //    {
    //        string uri = XHtmlUrlResolver.Instance.DtdUri;// xmlDoc.XmlResolver =
    //    }
    //}
    internal class XHtmlUrlResolver : System.Xml.XmlUrlResolver
    {
        private static XHtmlUrlResolver _Resolver = null;
        public static XHtmlUrlResolver Instance
        {
            get
            {
                if (_Resolver == null)
                {
                    _Resolver = new XHtmlUrlResolver();
                }
                return _Resolver;
            }

        }
        private string dtdUri = null;

        public string DtdUri
        {
            get
            {
                if (dtdUri == null)
                {
                    InitDTD();
                }
                return dtdUri;
            }
        }
        ///assemblyShortName;component/resourceLocation， 例如"/SilverlightLibraryAssembly;component/image.png"。 请注意，需要使用前导斜杠和 component 关键字（后跟一个斜杠）。
        public override Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            if (relativeUri.Contains("xhtml1-transitional.dtd") && DtdUri != null)
            {
                relativeUri = DtdUri;
            }
            return base.ResolveUri(baseUri, relativeUri);
        }

        /// <summary>
        /// 初始化DTD文件
        /// </summary>
        public void InitDTD()
        {
            dtdUri = AppConfig.XHtml.DtdUri;
            string folder = Path.GetDirectoryName(dtdUri);
            //检测文件是否存在
            if (!Directory.Exists(folder))//有异常直接抛
            {
                Directory.CreateDirectory(folder);
            }
            string[] items = new string[] { "/xhtml1-transitional.dtd", "/xhtml-lat1.ent", "/xhtml-special.ent", "/xhtml-symbol.ent" };
            if (!File.Exists(folder + items[0]))
            {
                using (FileStream fs = File.Create(folder + items[0]))
                {
                    fs.Write(Resources.xhtml1_transitional, 0, Resources.xhtml1_transitional.Length);
                }
                using (FileStream fs = File.Create(folder + items[1]))
                {
                    fs.Write(Resources.xhtml_lat1, 0, Resources.xhtml_lat1.Length);
                }
                using (FileStream fs = File.Create(folder + items[2]))
                {
                    fs.Write(Resources.xhtml_special, 0, Resources.xhtml_special.Length);
                }
                using (FileStream fs = File.Create(folder + items[3]))
                {
                    fs.Write(Resources.xhtml_symbol, 0, Resources.xhtml_symbol.Length);
                }
            }
        }

    }
}
