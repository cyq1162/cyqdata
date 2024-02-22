using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Orm
{
    /// <summary>
    /// 主键自增是否允许Null
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class KeyAttribute : Attribute
    {
        private bool _IsPrimaryKey;

        public bool IsPrimaryKey
        {
            get { return _IsPrimaryKey; }
            set { _IsPrimaryKey = value; }
        }
        private bool _IsAutoIncrement;

        public bool IsAutoIncrement
        {
            get { return _IsAutoIncrement; }
            set { _IsAutoIncrement = value; }
        }
        private bool _IsCanNull = true;

        public bool IsCanNull
        {
            get { return _IsCanNull; }
            set { _IsCanNull = value; }
        }
        public KeyAttribute(bool isAutoIncrement)
        {
            _IsAutoIncrement = isAutoIncrement;
            if (_IsAutoIncrement)
            {
                _IsPrimaryKey = true;
                _IsCanNull = false;
            }
        }
        public KeyAttribute(bool isAutoIncrement, bool isPrimaryKey)
        {
            _IsPrimaryKey = isPrimaryKey;
            _IsAutoIncrement = isAutoIncrement;
            if (isPrimaryKey)
            {
                IsCanNull = false;
            }
        }
        public KeyAttribute(bool isAutoIncrement, bool isPrimaryKey, bool isCanNull)
        {
            _IsPrimaryKey = isPrimaryKey;
            _IsAutoIncrement = isAutoIncrement;
            _IsCanNull = isCanNull;
        }
    }
   
}
