using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace CYQ.Data
{
    internal class NoSqlParameter : DbParameter
    {
        DbType _DbType;
        public override DbType DbType
        {
            get
            {
                return _DbType;
            }
            set
            {
                _DbType = value;
            }
        }
        ParameterDirection _Direction;
        public override ParameterDirection Direction
        {
            get
            {
                return _Direction;
            }
            set
            {
                _Direction = value;
            }
        }
        bool _IsNullable;
        public override bool IsNullable
        {
            get
            {
                return _IsNullable;
            }
            set
            {
                _IsNullable = value;
            }
        }
        string _ParameterName;
        public override string ParameterName
        {
            get
            {
                return _ParameterName;
            }
            set
            {
                _ParameterName = value;
            }
        }

        public override void ResetDbType()
        {

        }

        int _Size;
        public override int Size
        {
            get
            {
                return _Size;
            }
            set
            {
                _Size = value;
            }
        }

        public override string SourceColumn
        {
            get
            {
                return ParameterName;
            }
            set
            {
                ParameterName = value;
            }
        }
        bool _SourceColumnNullMapping;
        public override bool SourceColumnNullMapping
        {
            get
            {
                return _SourceColumnNullMapping;
            }
            set
            {
                _SourceColumnNullMapping = value;
            }
        }
        DataRowVersion _SourceVersion;
        public override DataRowVersion SourceVersion
        {
            get
            {
                return _SourceVersion;
            }
            set
            {
                _SourceVersion = value;
            }
        }
        object _Value;
        public override object Value
        {
            get
            {
                return _Value;
            }
            set
            {
                _Value = value;
            }
        }
    }
}
