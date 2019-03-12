using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Collections;
using System.ComponentModel;
using CYQ.Data.SQL;
using CYQ.Data.Tool;
using CYQ.Data.Extension;
using System.Reflection;
using System.Collections.Specialized;
using CYQ.Data.UI;


namespace CYQ.Data.Table
{
    /// <summary>
    /// һ�м�¼
    /// </summary>
    public partial class MDataRow : IDataRecord
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
            Table.Columns.AddRange(mdc);
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
        /// <summary>
        /// ����ö��������
        /// </summary>
        public MDataCell this[object field]
        {
            get
            {
                if (field is int || (field is Enum && AppConfig.IsEnumToInt))
                {
                    int index = (int)field;
                    if (Count > index)
                    {
                        return this[index];
                    }
                }
                else if (field is string)
                {
                    return this[field as string];
                }
                else if (field is IField)
                {
                    IField iFiled = field as IField;
                    if (iFiled.ColID > -1)
                    {
                        return this[iFiled.ColID];
                    }
                    return this[iFiled.Name];
                }
                return this[field.ToString()];
            }
        }
        public MDataCell this[string key]
        {
            get
            {
                int index = -1;
                if (key.Length <= Count.ToString().Length) //2<=20
                {
                    //�ж��Ƿ�Ϊ���֡�
                    if (!int.TryParse(key, out index))
                    {
                        index = -1;
                    }
                }
                if (index == -1)
                {
                    index = Columns.GetIndex(key);//���¼�����Ƿ�һ�¡�
                }
                if (index > -1)
                {
                    return this[index];
                }
                return null;
            }
        }
        private MDataTable _Table;
        /// <summary>
        /// ��ȡ����ӵ����ܹ��� MDataTable��
        /// </summary>
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
        public object[] ItemArray
        {
            get
            {
                object[] values = new object[Count];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = this[i].Value;
                }
                return values;
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

        /// <summary>
        /// �˷�����Emit������
        /// </summary>
        public object GetItemValue(int index)//����Public
        {
            MDataCell cell = this[index];
            if (cell == null || cell.Value == null || cell.Value == DBNull.Value)
            {
                return null;
            }
            return cell.Value;
        }
        /// <summary>
        /// ��������ֵ
        /// </summary>
        /// <returns></returns>
        public object[] GetItemValues()
        {
            object[] values = new object[Columns.Count];
            for (int i = 0; i < this.Count; i++)
            {
                values[i] = this[i].Value;
            }
            return values;
        }
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
        /// ���е�����ת�����У�ColumnName��Value���ı�
        /// </summary>
        public MDataTable ToTable()
        {
            MDataTable dt = this.Columns.ToTable();
            MCellStruct msValue = new MCellStruct("Value", SqlDbType.NVarChar);
            MCellStruct msState = new MCellStruct("State", SqlDbType.Int);
            dt.Columns.Insert(1, msValue);
            dt.Columns.Insert(2, msState);
            for (int i = 0; i < Count; i++)
            {
                dt.Rows[i][1].Value = this[i].Value;
                dt.Rows[i][2].Value = this[i].State;
            }
            return dt;
        }


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
                        return;
                    }
                    break;
                case BreakOp.Empty:
                    if (cell.StringValue == "")
                    {
                        return;
                    }
                    break;
                case BreakOp.NullOrEmpty:
                    if (cell.IsNullOrEmpty)
                    {
                        return;
                    }
                    break;
            }
            cell.State = state;
        }


        #region ICloneable ��Ա
        /// <summary>
        /// ����һ��
        /// </summary>
        /// <returns></returns>
        public MDataRow Clone()
        {
            MDataRow row = new MDataRow();

            for (int i = 0; i < Count; i++)
            {
                MCellStruct mcb = this[i].Struct;
                MDataCell mdc = new MDataCell(ref mcb);
                mdc.LoadValue(this[i]);
                row.Add(mdc);
            }
            //row._Table = _Table;//���ܴ�������ɵ����Ƴ���ʱ���Ƴ�����ԭ���õ��У�����������
            row.RowError = RowError;
            row.TableName = TableName;
            row.Conn = Conn;
            return row;
        }

        #endregion

        #region IDataRecord ��Ա

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

    public partial class MDataRow : IList<MDataCell>
    {
        public int Count
        {
            get { return CellList.Count; }
        }
        public MDataCell this[int index]
        {
            get
            {
                return CellList[index];
            }
            set
            {
                Error.Throw(AppConst.Global_NotImplemented);
            }
        }
        public void Add(string columnName, object value)
        {
            Add(columnName, SqlDbType.NVarChar, value);
        }
        public void Add(string columnName, SqlDbType sqlType, object value)
        {
            MCellStruct cs = new MCellStruct(columnName, sqlType, false, true, -1);
            Add(new MDataCell(ref cs, value));
        }
        public void Add(MDataCell cell)
        {
            CellList.Add(cell);
            Columns.Add(cell.Struct);
        }
        public void Insert(int index, MDataCell cell)
        {
            CellList.Insert(index, cell);
            Columns.Insert(index, cell.Struct);
        }
        public void Remove(string columnName)
        {
            int index = Columns.GetIndex(columnName);
            if (index > -1)
            {
                RemoveAt(index);
            }
        }
        public bool Remove(MDataCell item)
        {
            if (Columns.Count == Count)
            {
                Columns.Remove(item.Struct);
            }
            else
            {
                CellList.Remove(item);
            }
            return true;
        }

        public void RemoveAt(int index)
        {
            if (Columns.Count == Count)
            {
                Columns.RemoveAt(index);
            }
            else
            {
                CellList.RemoveAt(index);
            }
        }

        #region IList<MDataCell> ��Ա

        int IList<MDataCell>.IndexOf(MDataCell item)
        {
            return CellList.IndexOf(item);
        }

        #endregion

        #region ICollection<MDataCell> ��Ա

        public bool Contains(MDataCell item)
        {
            return CellList.Contains(item);
        }

        void ICollection<MDataCell>.CopyTo(MDataCell[] array, int arrayIndex)
        {
            CellList.CopyTo(array, arrayIndex);
        }

        bool ICollection<MDataCell>.IsReadOnly
        {
            get { return false; }
        }

        #endregion

        #region IEnumerable<MDataCell> ��Ա

        IEnumerator<MDataCell> IEnumerable<MDataCell>.GetEnumerator()
        {
            return CellList.GetEnumerator();
        }

        #endregion

        #region IEnumerable ��Ա

        IEnumerator IEnumerable.GetEnumerator()
        {
            return CellList.GetEnumerator();
        }

        #endregion
    }
    //��չ��������
    public partial class MDataRow
    {
        /// <summary>
        /// ��ʵ�塢Json��Xml��IEnumerable�ӿ�ʵ�ֵ��ࡢMDataRow
        /// </summary>
        /// <returns></returns>
        public static MDataRow CreateFrom(object anyObj)
        {
            return CreateFrom(anyObj, null);
        }
        public static MDataRow CreateFrom(object anyObj, Type valueType)
        {
            return CreateFrom(anyObj, valueType, BreakOp.None);
        }
        public static MDataRow CreateFrom(object anyObj, Type valueType, BreakOp op)
        {
            return CreateFrom(anyObj, valueType, op, JsonHelper.DefaultEscape);
        }
        /// <summary>
        /// ��ʵ�塢Json��Xml��IEnumerable�ӿ�ʵ�ֵ��ࡢMDataRow
        /// </summary>
        public static MDataRow CreateFrom(object anyObj, Type valueType, BreakOp breakOp, EscapeOp escapeOp)
        {
            MDataRow row = new MDataRow();
            if (anyObj is string)
            {
                row.LoadFrom(anyObj as string, escapeOp, breakOp);
            }
            else if (anyObj is IEnumerable)
            {
                row.LoadFrom(anyObj as IEnumerable, valueType, escapeOp, breakOp);
            }
            else if (anyObj is MDataRow)
            {
                row.LoadFrom(row as MDataRow, (breakOp == BreakOp.Null ? RowOp.IgnoreNull : RowOp.None), true);
            }
            else
            {
                row.LoadFrom(anyObj, breakOp);
            }
            row.SetState(1);//�ⲿ������״̬Ĭ����Ϊ1.
            return row;
        }

        /// <summary>
        /// ����е�����Json
        /// </summary>
        public string ToJson()
        {
            return ToJson(RowOp.IgnoreNull, false);
        }
        public string ToJson(bool isConvertNameToLower)
        {
            return ToJson(RowOp.IgnoreNull, isConvertNameToLower);
        }
        /// <summary>
        /// ����е�����Json
        /// </summary>
        public string ToJson(RowOp op)
        {
            return ToJson(op, false);
        }
        public string ToJson(RowOp op, bool isConvertNameToLower)
        {
            return ToJson(op, isConvertNameToLower, EscapeOp.Default);
        }
        /// <summary>
        /// ���Json
        /// </summary>
        /// <param name="op">��������</param>
        /// <param name="escapeOp">ת��ѡ��</param>
        /// <returns></returns>
        public string ToJson(RowOp op, bool isConvertNameToLower, EscapeOp escapeOp)
        {
            JsonHelper helper = new JsonHelper();
            helper.IsConvertNameToLower = isConvertNameToLower;
            helper.Escape = escapeOp;
            helper.RowOp = op;
            helper.Fill(this);
            return helper.ToString();
        }

        internal string ToXml(bool isConvertNameToLower)
        {
            string xml = string.Empty;
            foreach (MDataCell cell in this)
            {
                xml += cell.ToXml(isConvertNameToLower);
            }
            return xml;
        }
        /*
        //public bool WriteJson(string fileName)
        //{
        //    return WriteJson(fileName, RowOp.IgnoreNull);
        //}
        /// <summary>
        /// ��json���浽ָ���ļ���
        /// </summary>
        public bool WriteJson(string fileName, RowOp op)
        {
            return IOHelper.Write(fileName, ToJson(op));
        }
        */
        /// <summary>
        /// ת��ʵ��
        /// </summary>
        /// <typeparam name="T">ʵ������</typeparam>
        public T ToEntity<T>()
        {
            //Type t = typeof(T);
            //switch (StaticTool.GetSystemType(ref t))
            //{
            //    case SysType.Base:
            //        return (T)StaticTool.ChangeType(this[0].Value, t);

            //}
            //object obj = Activator.CreateInstance(t);
            //SetToEntity(ref obj, this);
            //return (T)obj;
            return (T)ToEntity(typeof(T));
        }
        internal object ToEntity(Type t)
        {
            switch (StaticTool.GetSystemType(ref t))
            {
                case SysType.Base:
                    return StaticTool.ChangeType(this[0].Value, t);

            }
            object obj = Activator.CreateInstance(t);
            SetToEntity(ref obj, this);
            return obj;
        }

        private object GetValue(MDataRow row, Type type)
        {
            switch (StaticTool.GetSystemType(ref type))
            {
                case SysType.Base:
                    return StaticTool.ChangeType(row[0].Value, type);
                case SysType.Enum:
                    return Enum.Parse(type, row[0].ToString());
                default:
                    object o = Activator.CreateInstance(type);
                    SetToEntity(ref o, row);
                    return o;
            }
        }
        /// <summary>
        /// ��ֵ��������UI
        /// </summary>
        public void SetToAll(params object[] parentControls)
        {
            SetToAll(null, parentControls);
        }
        /// <summary>
        /// ��ֵ��������UI
        /// </summary>
        /// <param name="autoPrefix">�Զ�ǰ׺��������ö��ŷָ�</param>
        /// <param name="parentControls">ҳ��ؼ�</param>
        public void SetToAll(string autoPrefix, params object[] parentControls)
        {
            if (Count > 0)
            {
                MDataRow row = this;
                using (MActionUI mui = new MActionUI(ref row, null, null))
                {
                    if (!string.IsNullOrEmpty(autoPrefix))
                    {
                        string[] pres = autoPrefix.Split(',');
                        mui.SetAutoPrefix(pres[0], pres);
                    }
                    mui.SetAll(parentControls);
                }
            }
        }
        /// <summary>
        /// ��Web Post����ȡֵ��
        /// </summary>
        public void LoadFrom()
        {
            LoadFrom(true);
        }
        /// <summary>
        /// ��Web Post����ȡֵ �� ��Winform��WPF�ı��ؼ���ȡֵ��
        /// <param name="isWeb">TrueΪWebӦ�ã���֮ΪWinӦ��</param>
        /// <param name="prefixOrParentControl">Webʱ����ǰ׺��Winʱ���ø������ؼ�</param>
        /// </summary>
        public void LoadFrom(bool isWeb, params object[] prefixOrParentControl)
        {
            if (Count > 0)
            {
                MDataRow row = this;
                using (MActionUI mui = new MActionUI(ref row, null, null))
                {

                    if (prefixOrParentControl.Length > 0)
                    {
                        if (isWeb)
                        {
                            string[] items = prefixOrParentControl as string[];
                            mui.SetAutoPrefix(items[0], items);
                        }
                        else
                        {
                            mui.SetAutoParentControl(prefixOrParentControl[0], prefixOrParentControl);
                        }
                    }

                    mui.GetAll(false);
                }
            }
        }

        /// <summary>
        /// �ӱ���м���ֵ
        /// </summary>
        public void LoadFrom(MDataRow row)
        {
            LoadFrom(row, RowOp.None, Count == 0);
        }
        /// <summary>
        /// �ӱ���м���ֵ
        /// </summary>
        public void LoadFrom(MDataRow row, RowOp rowOp, bool isAllowAppendColumn)
        {
            LoadFrom(row, rowOp, isAllowAppendColumn, true);
        }
        /// <summary>
        /// �ӱ���м���ֵ
        /// </summary>
        /// <param name="row">Ҫ�������ݵ���</param>
        /// <param name="rowOp">��ѡ��[�������м��ص�����]</param>
        /// <param name="isAllowAppendColumn">���row�������У��Ƿ����</param>
        /// <param name="isWithValueState">�Ƿ�ͬʱ����ֵ��״̬[Ĭ��ֵΪ��true]</param>
        public void LoadFrom(MDataRow row, RowOp rowOp, bool isAllowAppendColumn, bool isWithValueState)
        {
            if (row != null)
            {
                if (isAllowAppendColumn)
                {
                    for (int i = 0; i < row.Count; i++)
                    {
                        //if (rowOp == RowOp.IgnoreNull && row[i].IsNull)
                        //{
                        //    continue;
                        //}
                        if (!Columns.Contains(row[i].ColumnName))
                        {
                            Columns.Add(row[i].Struct);
                        }
                    }
                }
                MDataCell rowCell;
                foreach (MDataCell cell in this)
                {
                    rowCell = row[cell.ColumnName];
                    if (rowCell == null || (rowOp == RowOp.IgnoreNull && rowCell.IsNull))
                    {
                        continue;
                    }
                    if (rowOp == RowOp.None || rowCell.State >= (int)rowOp)
                    {
                        cell.Value = rowCell.Value;//�����ڸ�ֵ����Ϊ��ͬ�ļܹ����������������� int[access] int64[sqlite]����ת��ʱ�����
                        //cell._CellValue.IsNull = rowCell._CellValue.IsNull;//
                        if (isWithValueState)
                        {
                            cell.State = rowCell.State;
                        }
                    }
                }

            }
        }
        /// <summary>
        /// �����������ֵ
        /// </summary>
        /// <param name="values"></param>
        public void LoadFrom(object[] values)
        {
            if (values != null && values.Length <= Count)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this[i].Value = values[i];
                }
            }
        }
        public void LoadFrom(string json)
        {
            LoadFrom(json, JsonHelper.DefaultEscape);
        }
        /// <summary>
        /// ��json�����ֵ
        /// </summary>
        public void LoadFrom(string json, EscapeOp op)
        {
            LoadFrom(json, JsonHelper.DefaultEscape, BreakOp.None);
        }
        /// <summary>
        /// ��json�����ֵ
        /// </summary>
        public void LoadFrom(string json, EscapeOp op, BreakOp breakOp)
        {
            if (!string.IsNullOrEmpty(json))
            {
                Dictionary<string, string> dic = JsonHelper.Split(json);
                if (dic != null && dic.Count > 0)
                {
                    LoadFrom(dic, null, op, breakOp);
                }
            }
            else
            {
                LoadFrom(true);
            }
        }
        /// <summary>
        /// �ӷ����ֵ伯�������
        /// </summary>
        public void LoadFrom(IEnumerable dic)
        {
            LoadFrom(dic, null, JsonHelper.DefaultEscape, BreakOp.None);
        }
        internal void LoadFrom(IEnumerable dic, Type valueType, EscapeOp op, BreakOp breakOp)
        {
            if (dic != null)
            {
                bool isNameValue = dic is NameValueCollection;
                bool isAddColumn = Columns.Count == 0;
                SqlDbType sdt = SqlDbType.NVarChar;
                if (isAddColumn)
                {
                    if (valueType != null)
                    {
                        sdt = DataType.GetSqlType(valueType);
                    }
                    else if (!isNameValue)
                    {
                        Type type = dic.GetType();
                        if (type.IsGenericType)
                        {
                            sdt = DataType.GetSqlType(type.GetGenericArguments()[1]);
                        }
                        else
                        {
                            sdt = SqlDbType.Variant;
                        }
                    }
                }
                string key = null; object value = null;
                Type t = null;
                int i = -1;
                foreach (object o in dic)
                {
                    i++;
                    if (isNameValue)
                    {
                        if (o == null)
                        {
                            key = "null";
                            value = ((NameValueCollection)dic)[i];
                        }
                        else
                        {
                            key = Convert.ToString(o);
                            value = ((NameValueCollection)dic)[key];
                        }
                    }
                    else
                    {
                        t = o.GetType();
                        value = t.GetProperty("Value").GetValue(o, null);
                        bool isContinue = false;
                        switch (breakOp)
                        {
                            case BreakOp.Null:
                                if (value == null)
                                {
                                    isContinue = true;
                                }
                                break;
                            case BreakOp.Empty:
                                if (value != null && Convert.ToString(value) == "")
                                {
                                    isContinue = true;
                                }
                                break;
                            case BreakOp.NullOrEmpty:
                                if (Convert.ToString(value) == "")
                                {
                                    isContinue = true;
                                }
                                break;

                        }
                        if (isContinue) { continue; }
                        key = Convert.ToString(t.GetProperty("Key").GetValue(o, null));
                        //if (value != null)
                        //{

                        //}
                    }
                    //if (value != null)
                    //{
                    if (isAddColumn)
                    {
                        SqlDbType sdType = sdt;
                        if (sdt == SqlDbType.Variant)
                        {
                            if (value == null)
                            {
                                sdType = SqlDbType.NVarChar;
                            }
                            else
                            {
                                sdType = DataType.GetSqlType(value.GetType());
                            }
                        }
                        Add(key, sdType, value);
                    }
                    else
                    {
                        if (value != null && value is string)
                        {
                            value = JsonHelper.UnEscape(value.ToString(), op);
                        }
                        Set(key, value);
                    }
                    // }
                }
            }
        }
        /// <summary>
        /// ��ʵ��ת�������С�
        /// </summary>
        /// <param name="entity">ʵ�����</param>
        public void LoadFrom(object entity)
        {
            if (entity == null || entity is Boolean)
            {
                LoadFrom(true);
            }
            else if (entity is String)
            {
                LoadFrom(entity as String);
            }
            else if (entity is MDataRow)
            {
                LoadFrom(entity as MDataRow);
            }
            else if (entity is IEnumerable)
            {
                LoadFrom(entity as IEnumerable);
            }
            else
            {
                LoadFrom(entity, BreakOp.None);
            }
        }
        /// <summary>
        /// ��ʵ��ת�������С�
        /// </summary>
        /// <param name="entity">ʵ�����</param>
        public void LoadFrom(object entity, BreakOp op)
        {
            if (entity == null)
            {
                return;
            }
            try
            {
                Type t = entity.GetType();
                List<PropertyInfo> pis = StaticTool.GetPropertyInfo(t);
                if (Columns.Count == 0)
                {
                    MDataColumn mcs = TableSchema.GetColumns(t);
                    MCellStruct ms = null;
                    for (int i = 0; i < mcs.Count; i++)
                    {
                        if (mcs[i].SqlType == SqlDbType.Variant)
                        {
                            pis.Remove(t.GetProperty(mcs[i].ColumnName));           
                            continue;
                        }
                        ms = mcs[i];
                        MDataCell cell = new MDataCell(ref ms);
                        Add(cell);
                    }
                }

                if (string.IsNullOrEmpty(TableName))
                {
                    TableName = t.Name;
                }
                
                if (pis != null)
                {
                    foreach (PropertyInfo pi in pis)
                    {
                        int index = Columns.GetIndex(pi.Name);
                        if (index > -1)
                        {
                            object propValue = entity.GetType().GetProperties()[index].GetValue(entity, null);
                            switch (op)
                            {
                                case BreakOp.Null:
                                    if (propValue == null)
                                    {
                                        continue;
                                    }
                                    break;
                                case BreakOp.Empty:
                                    if (Convert.ToString(propValue) == "")
                                    {
                                        continue;
                                    }
                                    break;
                                case BreakOp.NullOrEmpty:
                                    if (propValue == null || Convert.ToString(propValue) == "")
                                    {
                                        continue;
                                    }
                                    break;
                              
                            }
                            Set(index, propValue, 2);//����״̬Ӧ��ֵ���ã���Ϊ1�ǲ��Եġ�
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Log.WriteLogToTxt(err);
            }
        }

        /// <summary>
        /// �����е�����ֵ����ʵ�����
        /// </summary>
        /// <param name="obj">ʵ�����</param>
        public void SetToEntity(object obj)
        {
            SetToEntity(ref obj, this);
        }
        /// <summary>
        /// ��ָ���е����ݸ���ʵ�����
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="row"></param>
        internal void SetToEntity(ref object obj, MDataRow row)
        {
            if (obj == null || row == null || row.Count == 0)
            {
                return;
            }
            Type objType = obj.GetType();
            string objName = objType.FullName, cellName = string.Empty;
            try
            {
                #region �������
                List<PropertyInfo> pis = StaticTool.GetPropertyInfo(objType);
                foreach (PropertyInfo p in pis)//����ʵ��
                {
                    cellName = p.Name;
                    MDataCell cell = row[cellName];
                    if (cell == null || cell.IsNull)
                    {
                        continue;
                    }
                    Type propType = p.PropertyType;
                    object objValue = null;
                    SysType sysType = StaticTool.GetSystemType(ref propType);
                    switch (sysType)
                    {
                        case SysType.Enum:
                            p.SetValue(obj, Enum.Parse(propType, cell.ToString()), null);
                            break;
                        case SysType.Base:
                            if (propType.Name == "String")
                            {
                                //ȥ��ת�����
                                if (cell.StringValue.IndexOf("\\\"") > -1)
                                {
                                    p.SetValue(obj, cell.StringValue.Replace("\\\"", "\""), null);
                                }
                                else
                                {
                                    p.SetValue(obj, cell.StringValue, null);
                                }
                            }
                            else
                            {
                                object value = StaticTool.ChangeType(cell.Value, p.PropertyType);
                                p.SetValue(obj, value, null);
                            }
                            break;
                        case SysType.Array:
                        case SysType.Collection:
                        case SysType.Generic:
                            if (cell.Value.GetType() == propType)
                            {
                                objValue = cell.Value;
                            }
                            else
                            {
                                Type[] argTypes = null;
                                int len = StaticTool.GetArgumentLength(ref propType, out argTypes);
                                if (len == 1) // Table
                                {

                                    if (JsonSplit.IsJson(cell.StringValue) && cell.StringValue.Contains(":") && cell.StringValue.Contains("{"))
                                    {
                                        #region JsonǶ�״���
                                        MDataTable dt = MDataTable.CreateFrom(cell.StringValue);//, SchemaCreate.GetColumns(argTypes[0])
                                        objValue = Activator.CreateInstance(propType, dt.Rows.Count);//����ʵ��
                                        Type objListType = objValue.GetType();
                                        foreach (MDataRow rowItem in dt.Rows)
                                        {
                                            object o = GetValue(rowItem, argTypes[0]);
                                            MethodInfo method = objListType.GetMethod("Add");
                                            if (method == null)
                                            {
                                                method = objListType.GetMethod("Push");
                                            }
                                            if (method != null)
                                            {
                                                method.Invoke(objValue, new object[] { o });
                                            }
                                        }
                                        dt = null;
                                        #endregion
                                    }
                                    else
                                    {
                                        #region ���鴦��
                                        List<string> items = JsonSplit.SplitEscapeArray(cell.StringValue);//�ڲ�ȥ��ת�����
                                        if (items == null) { continue; }
                                        objValue = Activator.CreateInstance(propType, items.Count);//����ʵ��
                                        Type objListType = objValue.GetType();
                                        bool isArray = sysType == SysType.Array;
                                        for (int i = 0; i < items.Count; i++)
                                        {
                                            MethodInfo method;
                                            if (isArray)
                                            {
                                                Object item = StaticTool.ChangeType(items[i], Type.GetType(propType.FullName.Replace("[]", "")));
                                                method = objListType.GetMethod("Set");
                                                if (method != null)
                                                {
                                                    method.Invoke(objValue, new object[] { i, item });
                                                }
                                            }
                                            else
                                            {
                                                Object item = StaticTool.ChangeType(items[i], argTypes[0]);
                                                method = objListType.GetMethod("Add");
                                                if (method == null)
                                                {
                                                    method = objListType.GetMethod("Push");
                                                }
                                                if (method != null)
                                                {
                                                    method.Invoke(objValue, new object[] { item });
                                                }
                                            }
                                        }
                                        #endregion
                                    }
                                }
                                else if (len == 2) // row
                                {
                                    MDataRow mRow = MDataRow.CreateFrom(cell.Value, argTypes[1]);
                                    objValue = Activator.CreateInstance(propType, mRow.Columns.Count);//����ʵ��
                                    foreach (MDataCell mCell in mRow)
                                    {
                                        object mObj = GetValue(mCell.ToRow(), argTypes[1]);
                                        objValue.GetType().GetMethod("Add").Invoke(objValue, new object[] { mCell.ColumnName, mObj });
                                    }
                                    mRow = null;
                                }
                            }
                            p.SetValue(obj, objValue, null);
                            break;
                        case SysType.Custom://�����ݹ�
                            MDataRow mr = new MDataRow(TableSchema.GetColumns(propType));
                            mr.LoadFrom(cell.ToString());
                            objValue = Activator.CreateInstance(propType);
                            SetToEntity(ref objValue, mr);
                            mr = null;
                            p.SetValue(obj, objValue, null);
                            break;

                    }
                }
                #endregion
            }
            catch (Exception err)
            {
                string msg = "[AttachInfo]:" + string.Format("ObjName:{0} PropertyName:{1}", objName, cellName) + "\r\n";
                msg += Log.GetExceptionMessage(err);
                Log.WriteLogToTxt(msg);
            }
        }

    }

}
