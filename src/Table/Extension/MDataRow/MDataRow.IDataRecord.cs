using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Collections;
using System.ComponentModel;
using CYQ.Data.SQL;
using CYQ.Data.Tool;
using System.Reflection;
using System.Collections.Specialized;
using CYQ.Data.UI;
using CYQ.Data.Json;
using CYQ.Data.Emit;

namespace CYQ.Data.Table
{
    /// <summary>
    /// 一行记录
    /// </summary>
    public partial class MDataRow : IDataRecord
    {

        #region ICloneable 成员
        /// <summary>
        /// 复制一行
        /// </summary>
        /// <returns></returns>
        public MDataRow Clone()
        {
            MDataRow row = new MDataRow();

            for (int i = 0; i < Count; i++)
            {
                MCellStruct mcb = this[i].Struct;
                MDataCell mdc = new MDataCell(ref mcb);
                mdc.LoadValue(this[i], true);
                row.Add(mdc);
            }
            //row._Table = _Table;//不能带，会造成单行移除列时，移除的是原引用的行，而不是自身
            row.RowError = RowError;
            row.TableName = TableName;
            row.Conn = Conn;
            return row;
        }

        #endregion

        #region IDataRecord 成员

        int IDataRecord.FieldCount
        {
            get
            {
                return Count;
            }
        }

        bool IDataRecord.GetBoolean(int i)
        {
            return (bool)this[i].Value;
        }

        byte IDataRecord.GetByte(int i)
        {
            return (byte)this[i].Value;
        }

        long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            return (byte)this[i].Value;
        }

        char IDataRecord.GetChar(int i)
        {
            return (char)this[i].Value;
        }

        long IDataRecord.GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            return (long)this[i].Value;
        }

        IDataReader IDataRecord.GetData(int i)
        {
            return null;
        }

        string IDataRecord.GetDataTypeName(int i)
        {
            //return "";
            return this[i].Struct.SqlTypeName;
        }

        DateTime IDataRecord.GetDateTime(int i)
        {
            return (DateTime)this[i].Value;
        }

        decimal IDataRecord.GetDecimal(int i)
        {
            return (decimal)this[i].Value;
        }

        double IDataRecord.GetDouble(int i)
        {
            return (double)this[i].Value;
        }

        Type IDataRecord.GetFieldType(int i)
        {
            return this[i].Struct.ValueType;
        }

        float IDataRecord.GetFloat(int i)
        {
            return (float)this[i].Value;
        }

        Guid IDataRecord.GetGuid(int i)
        {
            return (Guid)this[i].Value;
        }

        short IDataRecord.GetInt16(int i)
        {
            return (short)this[i].Value;
        }

        int IDataRecord.GetInt32(int i)
        {
            return (int)this[i].Value;
        }

        long IDataRecord.GetInt64(int i)
        {
            return (long)this[i].Value;
        }

        string IDataRecord.GetName(int i)
        {
            return this[i].ColumnName;
        }

        int IDataRecord.GetOrdinal(string name)
        {
            return this.Columns.GetIndex(name);
        }

        string IDataRecord.GetString(int i)
        {
            return (string)this[i].Value;
        }

        object IDataRecord.GetValue(int i)
        {
            return this[i].Value;
        }

        int IDataRecord.GetValues(object[] values)
        {
            if (values != null && this.Count == values.Length)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    values[i] = this[i].Value;
                }
            }
            return this.Count;
        }

        bool IDataRecord.IsDBNull(int i)
        {
            return this[i].IsNull;
        }

        object IDataRecord.this[string name]
        {

            get
            {
                return this[name];
            }
        }

        object IDataRecord.this[int i]
        {
            get
            {
                return this[i];
            }
        }

        #endregion

    }

}
