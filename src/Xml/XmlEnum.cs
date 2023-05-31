using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Xml
{

    /// <summary>
    /// 设置操作的节点属性类型
    /// </summary>
    public enum SetType
    {
        InnerXml,
        InnerText,
        Value,
        Href,
        Src,
        Class,
        /// <summary>
        /// A链接标识，最多可赋4个值为:InnerXml,href,title,target
        /// </summary>
        A,
        Img,
        Input,
        Select,
        Checked,
        Disabled,
        ID,
        Name,
        Visible,
        Script,
        Meta,
        Title,
        Link,
        Style,
        ClearFlag,
        /// <summary>
        /// 自定义，以“属性,值,属性,值”这样的对应设置，无个数限制。
        /// </summary>
        Custom,
    }

    /// <summary>
    /// 语言种类
    /// </summary>
    public enum LanguageKey
    {
        /// <summary>
        /// 未设置状态
        /// </summary>
        None = 0,
        /// <summary>
        /// 中文
        /// </summary>
        Chinese = 1,
        /// <summary>
        /// 英文
        /// </summary>
        English = 2,
        /// <summary>
        /// 法语
        /// </summary>
        French = 3,

        /// <summary>
        /// 德语
        /// </summary>
        German = 4,

        /// <summary>
        /// 韩语
        /// </summary>
        Korean = 5,

        /// <summary>
        /// 日语
        /// </summary>
        Japanese = 6,

        /// <summary>
        /// 印地语
        /// </summary>
        Hindi = 7,

        /// <summary>
        ///  俄语
        /// </summary>
        Russian = 8,

        /// <summary>
        /// 意大利语
        /// </summary>
        Italian = 9,
        /// <summary>
        /// 自定义语言
        /// </summary>
        Custom = 10
    }

    /// <summary>
    /// Xml|XHtml 加载后缓存级别
    /// </summary>
    public enum XmlCacheLevel
    {
        /// <summary>
        /// 无缓存
        /// </summary>
        NoCache = -1,
        /// <summary>
        /// 低缓存,5分钟
        /// </summary>
        Lower = 5,
        /// <summary>
        /// 默认取自配置项CacheTime时间,
        /// </summary>
        Default = 0,

        /// <summary>
        /// 1分钟
        /// </summary>
        Minute,
        /// <summary>
        /// 1小时
        /// </summary>
        Hour = 60,
        /// <summary>
        /// 1天时间
        /// </summary>
        Day = 1440,
        /// <summary>
        /// 一周时间
        /// </summary>
        Week = 10080
    }
}
