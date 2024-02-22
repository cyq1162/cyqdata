using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Orm
{
    /// <summary>
    /// д╛хож╣
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class DefaultValueAttribute : Attribute
    {
        private object _DefaultValue;

        public object DefaultValue
        {
            get { return _DefaultValue; }
            set { _DefaultValue = value; }
        }
        public DefaultValueAttribute(object defaultValue)
        {
            _DefaultValue = defaultValue;
        }
    }
   
}
