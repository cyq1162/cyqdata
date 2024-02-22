using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Json
{
    /// <summary>
    /// Json ת�����Ե��ֶ�
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class JsonIgnoreAttribute : Attribute
    {

    }

    /// <summary>
    /// Json ö���ֶ�ת�ַ���
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class JsonEnumToStringAttribute : Attribute
    {

    }


    /*
    
    /// <summary>
    /// Json ��ʽ����ʱ�䡿
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class JsonFormatAttribute : Attribute
    {
        private string _DatetimeFormat;

        public string DatetimeFormat
        {
            get { return _DatetimeFormat; }
            set { _DatetimeFormat = value; }
        }
        public JsonFormatAttribute(string datetimeFormat)
        {
            _DatetimeFormat = datetimeFormat;
        }
    }

    /// <summary>
    /// Json ö���ֶ�ת��������
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class JsonEnumToDescriptionAttribute : Attribute
    {

    }

    */
}
