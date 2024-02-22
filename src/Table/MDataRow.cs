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
    /// һ�м�¼
    /// </summary>
    public partial class MDataRow
    {
        List<MDataCell> _CellList;
        List<MDataCell> CellList
        {
            get
            {
                if (_CellList.Count == 0 && _Table != null && _Table.Columns.Count > 0)
                {
                    MCellStruct cellStruct;
                    foreach (MCellStruct item in _Table.Columns)
                    {
                        cellStruct = item;
                        MDataCell cell = new MDataCell(ref cellStruct, null);
                        _CellList.Add(cell);
                    }
                }
                return _CellList;
            }
            set
            {
                _CellList = value;
            }
        }
        public MDataRow()
        {
            CellList = new List<MDataCell>();
        }
        public MDataRow(MDataTable dt)
        {
            if (dt != null)
            {
                _Table = dt;
                CellList = new List<MDataCell>(dt.Columns.Count);
            }
        }
        public MDataRow(MDataColumn mdc)
        {
            CellList = new List<MDataCell>(mdc.Count);
            Table.Columns = mdc;
            //Table.Columns.AddRange(mdc);
        }
        /// <summary>
        /// ��ȡ��ͷ
        /// </summary>
        public MDataColumn Columns
        {
            get
            {
                return Table.Columns;
            }
        }
        public static implicit operator MDataRow(DataRow row)
        {
            if (row == null)
            {
                return null;
            }
            MDataRow mdr = new MDataRow();
            mdr.TableName = row.Table.TableName;
            DataColumnCollection columns = row.Table.Columns;
            if (columns != null && columns.Count > 0)
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    MCellStruct cellStruct = new MCellStruct(columns[i].ColumnName, DataType.GetSqlType(columns[i].DataType), columns[i].ReadOnly, columns[i].AllowDBNull, columns[i].MaxLength);
                    cellStruct.DefaultValue = columns[i].DefaultValue;
                    mdr.Add(new MDataCell(ref cellStruct, row[i]));
                }
            }

            return mdr;
        }
        private string _Conn = string.Empty;
        /// <summary>
        /// �����������ݿ�����������[��MAction�Ӵ��мܹ����м���ʱ,������������,���ȳ�ΪĬ�ϵ����ݿ�����]
        /// </summary>
        public string Conn
        {
            get
            {
                if (_Table != null)
                {
                    return _Table.Conn;
                }
                else if (string.IsNullOrEmpty(_Conn))
                {
                    return AppConfig.DB.DefaultConn;
                }
                return _Conn;
            }
            set
            {
                if (_Table != null)
                {
                    _Table.Conn = value;
                }
                else
                {
                    _Conn = value;
                }
            }
        }
        private string _TableName;
        /// <summary>
        /// ԭʼ����[δ���������ݿ���ݴ���]
        /// </summary>
        public string TableName
        {
            get
            {
                if (_Table != null)
                {
                    return _Table.TableName;
                }
                return _TableName;
            }
            set
            {
                if (_Table != null)
                {
                    _Table.TableName = value;
                }
                else
                {
                    _TableName = value;
                }
            }
        }
        [NonSerialized]
        private object _DynamicData;
        /// <summary>
        /// ��̬�洢����
        /// </summary>
        public object DynamicData
        {
            get { return _DynamicData; }
            set { _DynamicData = value; }
        }

        private MDataTable _Table;
        /// <summary>
        /// ��ȡ����ӵ����ܹ��� MDataTable��
        /// </summary>
        [JsonIgnore]
        public MDataTable Table
        {
            get
            {
                if (_Table == null)
                {
                    _Table = new MDataTable(_TableName);
                    if (this.Count > 0)
                    {
                        foreach (MDataCell cell in this)
                        {

                            _Table.Columns.Add(cell.Struct);
                        }
                    }

                    _Table.Rows.Add(this);
                }
                return _Table;
            }
            internal set
            {
                _Table = value;
            }
        }
        /// <summary>
        /// ͨ��һ����������ȡ�����ô��е�����ֵ��
        /// </summary>
        [JsonIgnore]
        public object[] ItemArray
        {
            get
            {
                return GetItemValues();
            }
        }
        private string _RowError;
        /// <summary>
        /// ��ȡ�������е��Զ������˵����
        /// </summary>
        public string RowError
        {
            get { return _RowError; }
            set { _RowError = value; }
        }



        /// <summary>
        /// ��ȡ��һ���ؼ�������
        /// </summary>
        public MDataCell PrimaryCell
        {
            get
            {
                return JointPrimaryCell[0];
            }
        }
        private List<MDataCell> _JointPrimaryCell;
        /// <summary>
        /// ��ȡ���������б����ж��������
        /// </summary>
        public List<MDataCell> JointPrimaryCell
        {
            get
            {
                if (_JointPrimaryCell == null && Columns.Count > 0)
                {
                    _JointPrimaryCell = new List<MDataCell>(Columns.JointPrimary.Count);
                    foreach (MCellStruct st in Columns.JointPrimary)
                    {
                        _JointPrimaryCell.Add(this[st.ColumnName]);
                    }
                }
                return _JointPrimaryCell;
            }
        }

        #region Get��GetState��GetItemValue

        /// <summary>
        /// ȡֵ
        /// </summary>
        public T Get<T>(object key)
        {
            return Get<T>(key, default(T));
        }
        public T Get<T>(object key, T defaultValue)
        {
            MDataCell cell = this[key];
            if (cell == null || cell.IsNull)
            {
                return defaultValue;
            }
            return cell.Get<T>();
        }



        /// <summary>
        /// ��ȡ�еĵ�ǰ״̬[0:δ���ģ�1:�Ѹ�ֵ,ֵ��ͬ[�ɲ���]��2:�Ѹ�ֵ,ֵ��ͬ[�ɸ���]]
        /// </summary>
        /// <returns></returns>
        public int GetState()
        {
            return GetState(false);
        }
        /// <summary>
        /// ��ȡ�еĵ�ǰ״̬[0:δ���ģ�1:�Ѹ�ֵ,ֵ��ͬ[�ɲ���]��2:�Ѹ�ֵ,ֵ��ͬ[�ɸ���]]
        /// </summary>
        public int GetState(bool ignorePrimaryKey)
        {
            int state = 0;
            for (int i = 0; i < this.Count; i++)
            {
                MDataCell cell = this[i];
                if (ignorePrimaryKey && cell.Struct.IsPrimaryKey)
                {
                    continue;
                }
                state = cell.State > state ? cell.State : state;
            }
            return state;
        }

        /// <summary>
        /// ��������ֵ
        /// </summary>
        /// <returns></returns>
        public object[] GetItemValues()
        {
            object[] values = new object[Count];
            for (int i = 0; i < Count; i++)
            {
                values[i] = this[i].Value;
            }
            return values;
        }

        #endregion

        #region Sets��Set��SetState


        /// <summary>
        /// Ϊ����������ֵ���ֵ
        /// </summary>
        /// <param name="startKey">��ʼ������||��ʼ����</param>
        /// <param name="values">���ֵ</param>
        /// <returns></returns>
        public MDataRow Sets(object startKey, params object[] values)
        {
            MDataCell cell = this[startKey];
            if (cell != null)
            {
                int startIndex = cell.Struct.ReaderIndex;
                for (int i = 0; i < values.Length; i++)
                {
                    Set(startIndex + i, values[i]);
                }
            }
            return this;
        }
        /// <summary>
        /// Ϊ������ֵ
        /// </summary>
        public MDataRow Set(object key, object value)
        {
            return Set(key, value, -1);
        }
        /// <summary>
        /// Ϊ������ֵ
        /// </summary>
        /// <param name="key">�ֶ���</param>
        /// <param name="value">ֵ</param>
        /// <param name="state">�ֹ�����״̬[0:δ���ģ�1:�Ѹ�ֵ,ֵ��ͬ[�ɲ���]��2:�Ѹ�ֵ,ֵ��ͬ[�ɸ���]]</param>
        public MDataRow Set(object key, object value, int state)
        {
            MDataCell cell = this[key];
            if (cell != null)
            {
                cell.Value = value;
                if (state > -1 && state < 3)
                {
                    cell.State = state;
                }
            }
            return this;
        }
        /// <summary>
        /// ���е������е�״̬ȫ������
        /// </summary>
        /// <param name="state">״̬[0:δ���ģ�1:�Ѹ�ֵ,ֵ��ͬ[�ɲ���]��2:�Ѹ�ֵ,ֵ��ͬ[�ɸ���]]</param>
        public MDataRow SetState(int state)
        {
            return SetState(state, BreakOp.None);
        }
        /// <summary>
        /// ���е������е�״̬ȫ������
        /// </summary>
        /// <param name="state">״̬[0:δ���ģ�1:�Ѹ�ֵ,ֵ��ͬ[�ɲ���]��2:�Ѹ�ֵ,ֵ��ͬ[�ɸ���]]</param>
        /// <param name="op">״̬����ѡ��</param>
        public MDataRow SetState(int state, BreakOp op)
        {
            return SetState(state, op, string.Empty);
        }
        /// <param name="columns"><para>����ָ��ĳЩ��</para></param>
        /// <returns></returns>
        public MDataRow SetState(int state, BreakOp op, string columns)
        {
            if (!string.IsNullOrEmpty(columns))
            {
                string[] items = columns.Trim(',', ' ').Split(',');
                for (int i = 0; i < items.Length; i++)
                {
                    MDataCell cell = this[items[i]];
                    if (cell != null)
                    {
                        SetState(state, op, cell);
                    }
                }
            }
            else
            {
                for (int i = 0; i < this.Count; i++)
                {
                    SetState(state, op, this[i]);
                }
            }
            return this;
        }
        private void SetState(int state, BreakOp op, MDataCell cell)
        {
            switch (op)
            {
                case BreakOp.Null:
                    if (cell.IsNull)
                    {
                        cell.State = 0;
                        return;
                    }
                    break;
                case BreakOp.Empty:
                    if (cell.StringValue == "")
                    {
                        cell.State = 0;
                        return;
                    }
                    break;
                case BreakOp.NullOrEmpty:
                    if (cell.IsNullOrEmpty)
                    {
                        cell.State = 0;
                        return;
                    }
                    break;
            }
            cell.State = state;
        }
        /// <summary>
        /// �޲�ʱĬ����-1
        /// </summary>
        internal void SetState()
        {
            for (int i = 0; i < this.Count; i++)
            {
                if (this[i].State == 2)
                {
                    this[i].State = 1;
                }
            }
        }

        #endregion

        /// <summary>
        /// ���е������е�ֵȫ����ΪNull
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < this.Count; i++)
            {
                this[i].Clear();
            }
        }
    }

}
