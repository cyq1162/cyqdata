using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Orm
{
    /// <summary>
    /// ±íÃû»ò×Ö¶ÎÃèÊö
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DescriptionAttribute : Attribute
    {
        private string _Description;

        public string Description
        {
            get { return _Description; }
            set { _Description = value; }
        }
        public DescriptionAttribute(string description)
        {
            _Description = description;
        }
    }

}
