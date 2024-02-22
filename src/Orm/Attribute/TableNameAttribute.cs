using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Orm
{

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
}
