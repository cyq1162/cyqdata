using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Orm
{

    /// <summary>
    /// ���ݿ⣺�ֶ�����
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

}
