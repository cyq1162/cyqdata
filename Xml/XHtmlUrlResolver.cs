using System;

namespace CYQ.Data.Xml
{
    internal class ResolverDtd
    {
        public static void Resolver(ref System.Xml.XmlDocument xmlDoc)
        {
            xmlDoc.XmlResolver = XHtmlUrlResolver.Instance;
        }
    }
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
                    dtdUri = AppConfig.XHtml.DtdUri;
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
    }
}
