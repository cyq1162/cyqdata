using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Extension;

namespace CYQ.Data.SyntaxExtended
{
    /// <summary>
    /// 表处理。
    /// </summary>
    public partial class TField : IField
    {
        public TField Select(params IField[] columnNames)
        {
            foreach (IField field in columnNames)
            {
                this.Sql += field.Name + ",";
            }
            this.Sql = this.Sql.TrimEnd(',');
            return this;
        }
        public TField LeftJoin(TField table)
        {
            this.Sql += _Name + " left join " + table._Name;
            return this;
        }
        public TField RightJoin(TField table)
        {
            this.Sql += _Name + " left join " + table._Name;
            return this;
        }
        public TField InnerJoin(TField table)
        {
            this.Sql += _Name + " left join " + table._Name;
            return this;
        }
        public TField On(IField onCondition)
        {
            this.Sql += " on "+ onCondition.Sql;
            return this;
        }
        public TField GroupBy(params IField[] columnNames)
        {
            this.Sql += " group by ";
            foreach (IField field in columnNames)
            {
                this.Sql += field.Name + ",";
            }
            this.Sql = this.Sql.TrimEnd(',');
            return this;
        }
        private int _ColID = -1;
        private string _Name;
        private bool _TypeIsInt = false;
        private string _Sql = string.Empty;

        public TField(string tableName)
        {
           _Name = tableName;
        }
        internal static string GetObjValue(object value, bool isInt)
        {
            return isInt ? Convert.ToString(value) : ("'" + Convert.ToString(value) + "'");
        }
        internal static TField Get(TField field, object objValue, string sign)
        {
            if (objValue is IField)
            {
                return new TField(" (" + field._Sql + sign + ((IField)objValue).Sql + ") ");
            }
            else if (sign == "+")
            {
                field._Sql = (string.IsNullOrEmpty(field._Sql) ? field.Name : field._Sql) + GetObjValue(objValue, field._TypeIsInt);
            }
            else
            {
                field._Sql = field._Name + sign + GetObjValue(objValue, field._TypeIsInt);
            }
            return field;
        }
        public override string ToString()
        {
            return _Name;
        }
        #region IField 成员

        public string Sql
        {
            get
            {
                if (string.IsNullOrEmpty(_Sql))
                {
                    return _Name;
                }
                return _Sql;
            }
            set
            {
                _Sql = value;
            }
        }

        public int ColID
        {
            get
            {
                return _ColID;
            }
        }

        public string Name
        {
            get
            {
                return _Name;
            }
        }

        #endregion
    }
    //public partial class TFiled
    //{
    //    public static TFiled operator ==(TFiled field, object value)
    //    {
    //        return Get(field, value, "=");
    //    }
    //    public static TFiled operator !=(TFiled field, object value)
    //    {
    //        return Get(field, value, "<>");
    //    }
    //    public static TFiled operator >(TFiled field, object value)
    //    {
    //        return Get(field, value, ">");
    //    }
    //    public static TFiled operator >=(TFiled field, object value)
    //    {
    //        return Get(field, value, ">=");
    //    }
    //    public static TFiled operator <(TFiled field, object value)
    //    {
    //        return Get(field, value, "<");
    //    }
    //    public static TFiled operator <=(TFiled field, object value)
    //    {
    //        return Get(field, value, "<=");
    //    }
    //    public static TFiled operator &(TFiled fieldLeft, object fildRight)
    //    {
    //        return Get(fieldLeft, fildRight, " and ");
    //    }
    //    public static TFiled operator |(TFiled fieldLeft, object fildRight)
    //    {
    //        return Get(fieldLeft, fildRight, " or ");
    //    }
    //    #region 加减乘除/取余
    //    public static TFiled operator +(TFiled field, object value)
    //    {
    //        return Get(field, value, "+");
    //    }
    //    public static TFiled operator -(TFiled field, object value)
    //    {
    //        return Get(field, value, "-");
    //    }
    //    public static TFiled operator *(TFiled field, object value)
    //    {
    //        return Get(field, value, "*");
    //    }
    //    public static TFiled operator /(TFiled field, object value)
    //    {
    //        return Get(field, value, "/");
    //    }
    //    public static TFiled operator %(TFiled field, object value)
    //    {
    //        return Get(field, value, "%");
    //    }
    //    #endregion

    //}
}
