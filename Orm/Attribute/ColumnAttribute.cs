using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Orm
{
    /// <summary>
    /// 列长度
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class LengthAttribute : Attribute
    {
        private int _MaxSize = -1;
        /// <summary>
        /// 长度
        /// </summary>
        public int MaxSize
        {
            get { return _MaxSize; }
            set { _MaxSize = value; }
        }
        private short _Scale = -1;
        /// <summary>
        /// 精度
        /// </summary>
        public short Scale
        {
            get { return _Scale; }
            set { _Scale = value; }
        }

        public LengthAttribute(int maxSize)
        {
            _MaxSize = maxSize;
        }
        public LengthAttribute(int maxSize, short scale)
        {
            _MaxSize = maxSize;
            _Scale = scale;
        }
    }
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
    /// <summary>
    /// 默认值
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
    /// <summary>
    /// 表名或字段描述
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

    /// <summary>
    /// 数据库：字段名称
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ColumnNameAttribute : Attribute
    {
        private string _ColumnName;

        public string ColumneName
        {
            get { return _ColumnName; }
            set { _ColumnName = value; }
        }
        public ColumnNameAttribute(string columnName)
        {
            _ColumnName = columnName;
        }
    }

    /// <summary>
    /// 数据库：表名
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class TableNameAttribute : Attribute
    {
        private string _TableName;

        public string TableName
        {
            get { return _TableName; }
            set { _TableName = value; }
        }
        public TableNameAttribute(string tableName)
        {
            _TableName = tableName;
        }
    }

    /// <summary>
    /// Json 转换忽略的字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class JsonIgnoreAttribute : Attribute
    {

    }
}
