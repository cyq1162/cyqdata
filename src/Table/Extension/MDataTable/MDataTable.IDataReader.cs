using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Xml;
using System.IO;
using System.Data.Common;
using System.ComponentModel;
using CYQ.Data.UI;
using CYQ.Data.Cache;
using CYQ.Data.SQL;
using CYQ.Data.Tool;
using System.Collections.Specialized;
using System.Web;
using CYQ.Data.Json;
using CYQ.Data.Orm;
using CYQ.Data.Aop;

namespace CYQ.Data.Table
{

    public partial class MDataTable : IDataReader, IEnumerable//, IEnumerator
    {
        private int _Ptr = -1;//������
        #region IDataRecord ��Ա
        /// <summary>
        /// ��ȡ�е�����
        /// </summary>
        int IDataRecord.FieldCount
        {
            get
            {
                if (Columns != null)
                {
                    return Columns.Count;
                }
                return 0;
            }
        }

        bool IDataRecord.GetBoolean(int i)
        {
            return (bool)_Rows[_Ptr][i].Value;
        }

        byte IDataRecord.GetByte(int i)
        {
            return (byte)_Rows[_Ptr][i].Value;
        }

        long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            return (byte)_Rows[_Ptr][i].Value;
        }

        char IDataRecord.GetChar(int i)
        {
            return (char)_Rows[_Ptr][i].Value;
        }

        long IDataRecord.GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            return (char)_Rows[_Ptr][i].Value;
        }

        IDataReader IDataRecord.GetData(int i)
        {
            return this;
        }

        string IDataRecord.GetDataTypeName(int i)
        {
            return "";//�󶨵Ŀ��Բ���Ҫ����
            //return DataType.GetDbType(Columns[i].SqlType.ToString()).ToString();
        }

        DateTime IDataRecord.GetDateTime(int i)
        {
            return (DateTime)_Rows[_Ptr][i].Value;
        }

        decimal IDataRecord.GetDecimal(int i)
        {
            return (decimal)_Rows[_Ptr][i].Value;
        }

        double IDataRecord.GetDouble(int i)
        {
            return (double)_Rows[_Ptr][i].Value;
        }

        Type IDataRecord.GetFieldType(int i)
        {
            return _Columns[i].ValueType;
        }

        float IDataRecord.GetFloat(int i)
        {
            return (float)_Rows[_Ptr][i].Value;
        }

        Guid IDataRecord.GetGuid(int i)
        {
            return (Guid)_Rows[_Ptr][i].Value;
        }

        short IDataRecord.GetInt16(int i)
        {
            return (short)_Rows[_Ptr][i].Value;
        }

        int IDataRecord.GetInt32(int i)
        {
            return (int)_Rows[_Ptr][i].Value;
        }

        long IDataRecord.GetInt64(int i)
        {
            return (long)_Rows[_Ptr][i].Value;
        }

        string IDataRecord.GetName(int i)
        {
            return _Columns[i].ColumnName;
        }

        int IDataRecord.GetOrdinal(string name)
        {
            return _Columns.GetIndex(name);
        }

        string IDataRecord.GetString(int i)
        {
            return Convert.ToString(_Rows[_Ptr][i].Value);
        }

        object IDataRecord.GetValue(int i)
        {
            return _Rows[_Ptr][i].Value;
        }

        int IDataRecord.GetValues(object[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = _Rows[_Ptr][i].Value;
            }
            return values.Length;
        }

        bool IDataRecord.IsDBNull(int i)
        {
            return _Rows[_Ptr][i].IsNull;
        }

        object IDataRecord.this[string name]
        {
            get
            {
                return _Rows[_Ptr][name];
            }
        }

        object IDataRecord.this[int i]
        {
            get
            {
                return _Rows[i];
            }
        }

        #endregion

        #region IDataReader ��Ա
        /// <summary>
        /// ���������
        /// </summary>
        void IDataReader.Close()
        {
            _Rows.Clear();
        }
        /// <summary>
        /// ��ȡ����������
        /// </summary>
        int IDataReader.Depth
        {
            get
            {
                if (_Rows != null)
                {
                    return _Rows.Count;
                }
                return 0;
            }
        }

        DataTable IDataReader.GetSchemaTable()
        {
            return ToDataTable();
        }
        /// <summary>
        /// �Ƿ��Ѷ�ȡ�����������ݣ�������˼�¼��
        /// </summary>
        bool IDataReader.IsClosed
        {
            get
            {
                return _Rows.Count == 0 && _Ptr >= _Rows.Count - 1;
            }
        }

        /// <summary>
        /// �Ƿ�����һ������
        /// </summary>
        /// <returns></returns>
        bool IDataReader.NextResult()
        {
            return _Ptr < _Rows.Count - 1;
        }
        /// <summary>
        /// �����Ƶ���һ����׼�����ж�ȡ��
        /// </summary>
        bool IDataReader.Read()
        {
            if (_Ptr < _Rows.Count - 1)
            {
                _Ptr++;
                return true;
            }
            else
            {
                _Ptr = -1;
                return false;
            }
        }

        private int _RecordsAffected;
        /// <summary>
        /// ���أ���ѯʱ����¼������
        /// </summary>
        public int RecordsAffected
        {
            get
            {
                if (_RecordsAffected == 0)
                {
                    return _Rows.Count;
                }
                return _RecordsAffected;
            }
            set
            {
                _RecordsAffected = value;
            }
        }

        #endregion

        #region IDisposable ��Ա

        void IDisposable.Dispose()
        {
            _Rows.Clear();
            _Rows = null;
        }

        #endregion

        #region IEnumerable ��Ա

        IEnumerator IEnumerable.GetEnumerator()
        {
            //for (int i = 0; i < Rows.Count; i++)
            //{
            //    yield return Rows[i];
            //}
            return new System.Data.Common.DbEnumerator(this);
        }

        #endregion

    }
}
