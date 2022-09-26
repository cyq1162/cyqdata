using CYQ.Data.Table;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;

namespace CYQ.Data
{
    internal class NoSqlDataReader : DbDataReader
    {
        private int readIndex = -1;
        public MDataTable table;
        internal NoSqlDataReader(MDataTable dt)
        {
            table = dt;
        }

        public override void Close()
        {
            readIndex = table.Rows.Count;
        }

        public override int Depth
        {
            get { if (table != null) { return table.Rows.Count; } return 0; }
        }

        public override int FieldCount
        {
            get { if (table != null) { return table.Columns.Count; } return 0; }
        }

        public override bool GetBoolean(int ordinal)
        {
            if (table == null || readIndex >= table.Rows.Count)
            {
                return false;
            }
            return table.Rows[readIndex].Get<bool>(ordinal);
        }

        public override byte GetByte(int ordinal)
        {
            if (table == null || readIndex >= table.Rows.Count)
            {
                return 0;
            }
            return table.Rows[readIndex].Get<byte>(ordinal);
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            if (table == null || readIndex >= table.Rows.Count)
            {
                return 0;
            }
            return table.Rows[readIndex].Get<long>(ordinal);
        }

        public override char GetChar(int ordinal)
        {
            if (table == null || readIndex >= table.Rows.Count)
            {
                return '0';
            }
            return table.Rows[readIndex].Get<char>(ordinal);
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            if (table == null || readIndex >= table.Rows.Count)
            {
                return 0;
            }
            return table.Rows[readIndex].Get<long>(ordinal);
        }

        public override string GetDataTypeName(int ordinal)
        {
            if (table == null || ordinal < 0 || ordinal >= table.Columns.Count)
            {
                return null;
            }
            return table.Columns[ordinal].SqlTypeName;
        }

        public override DateTime GetDateTime(int ordinal)
        {
            if (table == null || readIndex >= table.Rows.Count)
            {
                return DateTime.MinValue;
            }
            return table.Rows[readIndex].Get<DateTime>(ordinal);
        }

        public override decimal GetDecimal(int ordinal)
        {
            if (table == null || readIndex >= table.Rows.Count)
            {
                return 0;
            }
            return table.Rows[readIndex].Get<decimal>(ordinal);
        }

        public override double GetDouble(int ordinal)
        {
            if (table == null || readIndex >= table.Rows.Count)
            {
                return 0;
            }
            return table.Rows[readIndex].Get<double>(ordinal);
        }

        public override System.Collections.IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override Type GetFieldType(int ordinal)
        {
            if (table == null || ordinal < 0 || ordinal >= table.Columns.Count)
            {
                return null;
            }
            return table.Columns[ordinal].ValueType;
        }

        public override float GetFloat(int ordinal)
        {
            if (table == null || readIndex >= table.Rows.Count)
            {
                return 0;
            }
            return table.Rows[readIndex].Get<float>(ordinal);
        }

        public override Guid GetGuid(int ordinal)
        {
            if (table == null || readIndex >= table.Rows.Count)
            {
                return Guid.Empty;
            }
            return table.Rows[readIndex].Get<Guid>(ordinal);
        }

        public override short GetInt16(int ordinal)
        {
            if (table == null || readIndex >= table.Rows.Count)
            {
                return 0;
            }
            return table.Rows[readIndex].Get<short>(ordinal);
        }

        public override int GetInt32(int ordinal)
        {
            if (table == null || readIndex >= table.Rows.Count)
            {
                return 0;
            }
            return table.Rows[readIndex].Get<int>(ordinal);
        }

        public override long GetInt64(int ordinal)
        {
            if (table == null || readIndex >= table.Rows.Count)
            {
                return 0;
            }
            return table.Rows[readIndex].Get<long>(ordinal);
        }

        public override string GetName(int ordinal)
        {
            if (table == null || ordinal < 0 || ordinal >= table.Columns.Count)
            {
                return null;
            }
            return table.Columns[ordinal].ColumnName;
        }

        public override int GetOrdinal(string name)
        {
            if (table == null)
            {
                return -1;
            }
            return table.Columns.GetIndex(name);
        }

        public override System.Data.DataTable GetSchemaTable()
        {
            if (table == null)
            {
                return null;
            }
            return table.Columns.ToTable().ToDataTable();
        }

        public override string GetString(int ordinal)
        {
            if (table == null || readIndex >= table.Rows.Count)
            {
                return null;
            }
            return table.Rows[readIndex].Get<string>(ordinal);
        }

        public override object GetValue(int ordinal)
        {
            if (table == null || readIndex >= table.Rows.Count)
            {
                return null;
            }
            return table.Rows[readIndex].Get<object>(ordinal);
        }

        public override int GetValues(object[] values)
        {
            if (table == null || readIndex >= table.Rows.Count)
            {
                return 0;
            }
            int count = table.Columns.Count;
            MDataRow row = table.Rows[readIndex];
            for (int i = 0; i < values.Length; i++)
            {
                if (i < table.Columns.Count)
                {
                    values[i] = row[i].Value;
                }
            }
            return 1;
        }

        public override bool HasRows
        {
            get { return table != null && table.Rows.Count > 0; }
        }

        public override bool IsClosed
        {
            get { return table == null || readIndex >= table.Rows.Count; }
        }

        public override bool IsDBNull(int ordinal)
        {
            if (table == null || readIndex >= table.Rows.Count || table.Rows[readIndex][ordinal] == null)
            {
                return true;
            }
            return table.Rows[readIndex][ordinal].Value == DBNull.Value;
        }

        public override bool NextResult()
        {
            return false;
        }

        public override bool Read()
        {
            readIndex++;
            return readIndex < table.Rows.Count;
        }

        public override int RecordsAffected
        {
            get
            {
                if (table != null)
                {
                    return table.Rows.Count;
                }
                return 0;
            }
        }

        public override object this[string name]
        {
            get
            {
                if (table == null || readIndex >= table.Rows.Count)
                {
                    return 0;
                }
                return table.Rows[readIndex].Get<object>(name);
            }
        }

        public override object this[int ordinal]
        {
            get
            {
                if (table == null || readIndex >= table.Rows.Count)
                {
                    return 0;
                }
                return table.Rows[readIndex].Get<object>(ordinal);
            }
        }
    }
}
