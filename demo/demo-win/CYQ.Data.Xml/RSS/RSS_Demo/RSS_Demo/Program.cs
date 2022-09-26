using CYQ.Data.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSS_Demo
{
    class Program
    {
	//相关文章：http://www.cnblogs.com/cyq1162/archive/2010/12/15/1906869.html
        static void Main(string[] args)
        {
            Rss rss = new Rss();
            rss.Channel.Title = "秋色园";
            rss.Channel.Link = "http://www.cyqdata.com";
            rss.Channel.Description = "秋色园-QBlog-Power by Blog.CYQ";
            for (int i = 0; i < 10; i++)
            {
                RssItem item = new RssItem();
                item.Title = string.Format("作者博客：第{0}项", i);
                item.Link = "http://www.cnblogs.com/cyq1162";
                item.Description = "很长很长的内容..作者博客链接";
                rss.Channel.Items.Add(item);
            }
            string xml = rss.OutXml;
            Console.WriteLine(xml);
            Console.Read();
        }
    }
}
