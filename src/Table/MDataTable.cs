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

    /// <summary>
    /// ���
    /// </summary>
    public partial class MDataTable
    {
        internal const string DefaultTableName = "SysDefault";

        #region ����
        private MDataRowCollection _Rows;
        /// <summary>
        /// �����
        /// </summary>
        public MDataRowCollection Rows
        {
            get
            {
                return _Rows;
            }
        }
        [NonSerialized]
        private object _DynamicData;
        /// <summary>
        /// ��̬�洢����(�磺AcceptChanges �������쳣Ĭ���ɱ������洢)
        /// </summary>
        public object DynamicData
        {
            get { return _DynamicData; }
            set { _DynamicData = value; }
        }

        public MDataTable()
        {
            Init(DefaultTableName, null);
        }
        public MDataTable(string tableName)
        {
            Init(tableName, null);
        }
        public MDataTable(string tableName, MDataColumn mdc)
        {
            Init(tableName, mdc);
        }
        private void Init(string tableName, MDataColumn mdc)
        {
            _Rows = new MDataRowCollection(this);
            _TableName = tableName;
            if (_Columns == null)
            {
                _Columns = new MDataColumn(this);
                if (mdc != null)
                {
                    _Columns.AddRange(mdc);
                }
            }
        }
        private string _TableName = string.Empty;
        /// <summary>
        /// ����
        /// </summary>
        public string TableName
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName) && Columns != null)
                {
                    _TableName = Columns.TableName;
                }
                return _TableName;
            }
            set
            {
                _TableName = value;
            }
        }
        private string _Description = string.Empty;
        /// <summary>
        /// ��������
        /// </summary>
        public string Description
        {
            get
            {
                if (string.IsNullOrEmpty(_Description) && Columns != null)
                {
                    _Description = Columns.Description;
                }
                return _Description;
            }
            set
            {
                _Description = value;
            }
        }
        private MDataColumn _Columns;
        /// <summary>
        /// ���ļܹ���
        /// </summary>
        public MDataColumn Columns
        {
            get
            {
                return _Columns;
            }
            set
            {
                _Columns = value;
                _Columns._Table = this;
                if (string.IsNullOrEmpty(_TableName) || _TableName == DefaultTableName)
                {
                    _TableName = _Columns.TableName;
                }
                else
                {
                    _Columns.TableName = _TableName;
                }
            }
        }
        private string _Conn;
        /// <summary>
        /// �ñ���������ݿ����ӡ�
        /// </summary>
        public string Conn
        {
            get
            {
                if (string.IsNullOrEmpty(_Conn))
                {
                    return AppConfig.DB.DefaultConn;
                }
                return _Conn;
            }
            set
            {
                _Conn = value;
            }
        }

        #endregion

        #region ����
        /// <summary>
        /// �½�һ��
        /// </summary>
        /// <returns></returns>
        public MDataRow NewRow()
        {
            return NewRow(false);
        }
        /// <summary>
        /// �½�һ��
        /// </summary>
        /// <param name="isAddToTable">�Ƿ�˳����ӵ�����</param>
        /// <returns></returns>
        public MDataRow NewRow(bool isAddToTable)
        {
            return NewRow(isAddToTable, -1);
        }
        /// <summary>
        /// �½�һ��
        /// </summary>
        /// <param name="index">���������</param>
        /// <returns></returns>
        public MDataRow NewRow(bool isAddToTable, int index)
        {
            MDataRow mdr = new MDataRow(this);
            if (isAddToTable)
            {
                if (index < 0)
                {
                    Rows.Add(mdr, false);
                }
                else
                {
                    Rows.Insert(index, mdr);
                }
            }
            return mdr;
        }
      
        /// <summary>
        /// �����������е������е�״̬ȫ������
        /// </summary>
        /// <param name="state">״̬[0:δ���ģ�1:�Ѹ�ֵ,ֵ��ͬ[�ɲ���]��2:�Ѹ�ֵ,ֵ��ͬ[�ɸ���]]</param>
        public MDataTable SetState(int state)
        {
            SetState(state, BreakOp.None); return this;
        }
        /// <summary>
        /// �����������е������е�״̬ȫ������
        /// </summary>
        /// <param name="state">״̬[0:δ���ģ�1:�Ѹ�ֵ,ֵ��ͬ[�ɲ���]��2:�Ѹ�ֵ,ֵ��ͬ[�ɸ���]]</param>
        /// <param name="op">״̬����ѡ��</param>
        public MDataTable SetState(int state, BreakOp op)
        {
            if (Rows != null && Rows.Count > 0)
            {
                foreach (MDataRow row in Rows)
                {
                    row.SetState(state, op);
                }
            }
            return this;
        }
       
        /// <summary>
        /// ����ĳ�еļ���
        /// <param name="columnName">����</param>
        /// </summary>
        public List<T> GetColumnItems<T>(string columnName)
        {
            return GetColumnItems<T>(columnName, BreakOp.None, false);
        }
        /// <summary>
        /// ����ĳ�еļ���
        /// <param name="columnName">����</param>
        /// <param name="op">����ѡ��</param>
        /// </summary>
        public List<T> GetColumnItems<T>(string columnName, BreakOp op)
        {
            return GetColumnItems<T>(columnName, op, false);
        }
        /// <summary>
        /// ����ĳ�еļ���
        /// </summary>
        /// <typeparam name="T">�е�����</typeparam>
        /// <param name="columnIndex">����</param>
        /// <param name="op">����ѡ��</param>
        /// <param name="isDistinct">�Ƿ�ȥ���ظ�����</param>
        public List<T> GetColumnItems<T>(string columnName, BreakOp op, bool isDistinct)
        {
            int index = -1;
            if (Columns != null)
            {
                index = Columns.GetIndex(columnName);
            }
            return GetColumnItems<T>(index, op, isDistinct);
        }
        /// <summary>
        /// ����ĳ�еļ���
        /// </summary>
        /// <typeparam name="T">�е�����</typeparam>
        /// <param name="columnIndex">��N��</param>
        public List<T> GetColumnItems<T>(int columnIndex)
        {
            return GetColumnItems<T>(columnIndex, BreakOp.None);
        }

        /// <summary>
        /// ����ĳ�еļ���
        /// </summary>
        /// <typeparam name="T">�е�����</typeparam>
        /// <param name="columnIndex">��N��</param>
        /// <param name="op">����ѡ��</param>
        /// <returns></returns>
        public List<T> GetColumnItems<T>(int columnIndex, BreakOp op)
        {
            return GetColumnItems<T>(columnIndex, op, false);
        }
        /// <summary>
        /// ����ĳ�еļ���
        /// </summary>
        /// <typeparam name="T">�е�����</typeparam>
        /// <param name="columnIndex">��N��</param>
        /// <param name="op">����ѡ��</param>
        /// <param name="isDistinct">�Ƿ�ȥ���ظ�����</param>
        public List<T> GetColumnItems<T>(int columnIndex, BreakOp op, bool isDistinct)
        {
            List<T> items = new List<T>();
            if (Columns != null && Rows != null && Rows.Count > 0)
            {
                if (columnIndex > -1)
                {
                    MDataCell cell;
                    foreach (MDataRow row in Rows)
                    {
                        cell = row[columnIndex];
                        switch (op)
                        {
                            case BreakOp.Null:
                                if (cell.IsNull)
                                {
                                    continue;
                                }
                                break;
                            case BreakOp.Empty:
                                if (cell.StringValue == "")
                                {
                                    continue;
                                }
                                break;
                            case BreakOp.NullOrEmpty:
                                if (cell.IsNullOrEmpty)
                                {
                                    continue;
                                }
                                break;
                        }
                        T value = cell.Get<T>();// row.Get<T>(columnIndex, default(T));
                        if (!isDistinct || !items.Contains(value))
                        {
                            items.Add(value);
                        }
                    }
                }
                else
                {
                    Error.Throw(string.Format("Table {0} can not find the column", TableName));
                }
            }
            return items;
        }
        /// <summary>
        /// ���Ʊ�
        /// </summary>
        public MDataTable Clone()
        {
            MDataTable newTable = GetSchema(true);
            newTable.Conn = Conn;
            newTable.DynamicData = DynamicData;
            newTable.RecordsAffected = RecordsAffected;
            newTable.TableName = TableName;
            if (_Rows.Count > 0)
            {
                foreach (MDataRow oldRow in _Rows)
                {
                    MDataRow newRow = newTable.NewRow();
                    newRow.LoadFrom(oldRow);
                    newTable.Rows.Add(newRow, false);
                }
            }
            return newTable;
        }
        /// <summary>
        /// ���Ʊ�Ľṹ
        /// </summary>
        /// <param name="clone">�Ƿ��¡��ṹ</param>
        /// <returns></returns>
        public MDataTable GetSchema(bool clone)
        {
            MDataTable newTable = new MDataTable(_TableName);
            if (Columns.Count > 0)
            {
                newTable.Columns = clone ? Columns.Clone() : Columns;
            }
            newTable.Conn = Conn;
            return newTable;
        }

       

        /// <summary>
        /// ������(�����мܹ�)[��ʾ��������Ϊ�ռܹ�ʱ��Ч]
        /// </summary>
        /// <param name="row"></param>
        internal void LoadRow(MDataRow row) //�Ƿ�ֱ������Row.Table�أ�����
        {
            if (this.Columns.Count == 0 && row != null && row.Count > 0)
            {
                this.Columns = row.Columns.Clone();
                if (!string.IsNullOrEmpty(_TableName) && _TableName.StartsWith(DefaultTableName))
                {
                    _TableName = row.TableName;
                }
                _Conn = row.Conn;
                if (!row[0].IsNullOrEmpty)
                {
                    NewRow(true).LoadFrom(row);
                    //_Rows.Add(row);
                }
            }
        }

        /// <summary>
        /// ���±���зŵ�ԭ������档
        /// </summary>
        /// <param name="newTable"></param>
        public void Merge(MDataTable newTable)
        {
            if (newTable != null && newTable.Rows.Count > 0)
            {
                int count = newTable.Rows.Count;//��ǰ��ȡ��������Ϊ�˱���dt.Merge(dt);//���������µ���ѭ����
                for (int i = 0; i < count; i++)
                {
                    // _Rows.Add(newTable.Rows[i]);
                    NewRow(true).LoadFrom(newTable.Rows[i]);
                }
            }
        }

        public override string ToString()
        {
            return TableName;
        }

        #endregion
    }
}
