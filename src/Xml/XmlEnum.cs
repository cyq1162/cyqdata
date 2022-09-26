using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Xml
{

    /// <summary>
    /// ���ò����Ľڵ���������
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
        /// A���ӱ�ʶ�����ɸ�4��ֵΪ:InnerXml,href,title,target
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
        /// <summary>
        /// �Զ��壬�ԡ�����,ֵ,����,ֵ�������Ķ�Ӧ���ã��޸������ơ�
        /// </summary>
        Custom,
    }

    /// <summary>
    /// ��������
    /// </summary>
    public enum LanguageKey
    {
        /// <summary>
        /// δ����״̬
        /// </summary>
        None = 0,
        /// <summary>
        /// ����
        /// </summary>
        Chinese = 1,
        /// <summary>
        /// Ӣ��
        /// </summary>
        English = 2,
        /// <summary>
        /// ����
        /// </summary>
        French = 3,

        /// <summary>
        /// ����
        /// </summary>
        German = 4,

        /// <summary>
        /// ����
        /// </summary>
        Korean = 5,

        /// <summary>
        /// ����
        /// </summary>
        Japanese = 6,

        /// <summary>
        /// ӡ����
        /// </summary>
        Hindi = 7,

        /// <summary>
        ///  ����
        /// </summary>
        Russian = 8,

        /// <summary>
        /// �������
        /// </summary>
        Italian = 9,
        /// <summary>
        /// �Զ�������
        /// </summary>
        Custom = 10
    }

    /// <summary>
    /// Xml|XHtml ���غ󻺴漶��
    /// </summary>
    public enum XmlCacheLevel
    {
        /// <summary>
        /// �޻���
        /// </summary>
        NoCache = -1,
        /// <summary>
        /// �ͻ���,5����
        /// </summary>
        Lower = 5,
        /// <summary>
        /// Ĭ��ȡ��������CacheTimeʱ��,
        /// </summary>
        Default = 0,

        /// <summary>
        /// 1����
        /// </summary>
        Minute,
        /// <summary>
        /// 1Сʱ
        /// </summary>
        Hour = 60,
        /// <summary>
        /// 1��ʱ��
        /// </summary>
        Day = 1440,
        /// <summary>
        /// һ��ʱ��
        /// </summary>
        Week = 10080
    }
}
