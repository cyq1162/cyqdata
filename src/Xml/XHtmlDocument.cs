using System;
using System.Xml;

namespace CYQ.Data.Xml
{
    class XHtmlDocument : XmlDocument
    {
        public override XmlElement CreateElement(string prefix, string localName, string namespaceURI)
        {
            XmlElement xe = base.CreateElement(prefix, localName, namespaceURI);
            switch (localName)
            {
                case "meta":
                case "br":
                case "hr":
                case "img":
                case "link":
                case "base":
                case "area":
                case "input":
                case "source":
                case "!DOCTYPE":
                    break;
                default:
                    xe.IsEmpty = false;//不自闭合标签
                    break;
            }
            
            return xe;
        }
    }
}
