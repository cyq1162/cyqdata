using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Json
{
    /// <summary>
    /// Json 转换忽略的字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class JsonIgnoreAttribute : Attribute
    {

    }

    /// <summary>
    /// Json 枚举字段转字符串
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class JsonEnumToStringAttribute : Attribute
    {

    }


    /*
    
    /// <summary>
    /// Json 格式化【时间】
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
    /// Json 枚举字段转属性描述
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class JsonEnumToDescriptionAttribute : Attribute
    {

    }

    */
}
