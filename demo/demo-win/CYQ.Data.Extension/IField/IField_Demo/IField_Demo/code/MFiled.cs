using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Extension;

namespace CYQ.Data.SyntaxExtended
{
    /// <summary>
    /// 字段
    /// </summary>
    public partial class MField : IField
    {
        private int _ColID = -1;
        private string _Name;
        private bool _TypeIsInt = false;
        private string _Sql = string.Empty;

        public MField(string columnName, int colID, bool typeIsInt)
        {
            _ColID = colID;
            _Name = columnName;
            _TypeIsInt = typeIsInt;
        }
        internal MField(string columnName, bool typeIsInt)
        {
            _Name = columnName;
            _TypeIsInt = typeIsInt;
        }
        internal MField(string sql)
        {
            _Sql = sql;
        }
        internal static string GetObjValue(object value, bool isInt)
        {
            return isInt ? Convert.ToString(value) : ("'" + Convert.ToString(value) + "'");
        }
        internal static MField Get(MField field, object objValue, string sign)
        {
            if (objValue is IField)
            {
                return new MField(" (" + field.Sql + sign + ((IField)objValue).Sql + ") ");
            }
            else if (sign == "+")
            {
                field._Sql = field.Sql + GetObjValue(objValue, field._TypeIsInt);
            }
            else
            {
                field._Sql = field.Name + sign + GetObjValue(objValue, field._TypeIsInt);
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
    public partial class MField
    {
        public static MField operator ==(MField field, object value)
        {
            return Get(field, value, "=");
        }
        public static MField operator !=(MField field, object value)
        {
            return Get(field, value, "<>");
        }
        public static MField operator >(MField field, object value)
        {
            return Get(field, value, ">");
        }
        public static MField operator >=(MField field, object value)
        {
            return Get(field, value, ">=");
        }
        public static MField operator <(MField field, object value)
        {
            return Get(field, value, "<");
        }
        public static MField operator <=(MField field, object value)
        {
            return Get(field, value, "<=");
        }
        public static MField operator &(MField fieldLeft, object fildRight)
        {
            return Get(fieldLeft, fildRight, " and ");
        }
        public static MField operator |(MField fieldLeft, object fildRight)
        {
            return Get(fieldLeft, fildRight, " or ");
        }
        #region 加减乘除/取余
        public static MField operator +(MField field, object value)
        {
            return Get(field, value, "+");
        }
        public static MField operator -(MField field, object value)
        {
            return Get(field, value, "-");
        }
        public static MField operator *(MField field, object value)
        {
            return Get(field, value, "*");
        }
        public static MField operator /(MField field, object value)
        {
            return Get(field, value, "/");
        }
        public static MField operator %(MField field, object value)
        {
            return Get(field, value, "%");
        }
        #endregion

    }
}
