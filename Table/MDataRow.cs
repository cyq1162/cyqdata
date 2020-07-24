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
    /// 一行记录
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
        /// 获取列头
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
        /// 所依属的数据库配置项名称[当MAction从带有架构的行加载时,此链接若存在,优先成为默认的数据库链接]
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
        /// 原始表名[未经过多数据库兼容处理]
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
        /// 动态存储数据
        /// </summary>
        public object DynamicData
        {
            get { return _DynamicData; }
            set { _DynamicData = value; }
        }
        /// <summary>
        /// 输入枚举型数据
        /// </summary>
        public MDataCell this[object field]
        {
            get
            {
                if (field == null) { return null; }
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
                    //判断是否为数字。
                    if (!int.TryParse(key, out index))
                    {
                        index = -1;
                    }
                }
                if (index == -1)
                {
                    index = Columns.GetIndex(key);//重新检测列是否一致。
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
        /// 获取该行拥有其架构的 MDataTable。
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
        /// 通过一个数组来获取或设置此行的所有值。
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
        /// 获取或设置行的自定义错误说明。
        /// </summary>
        public string RowError
        {
            get { return _RowError; }
            set { _RowError = value; }
        }



        /// <summary>
        /// 获取第一个关键主键列
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
        /// 获取联合主键列表（若有多个主键）
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
        /// 此方法被Emit所调用
        /// </summary>
        public object GetItemValue(int index)//必须Public
        {
            MDataCell cell = this[index];
            if (cell == null || cell.Value == null || cell.Value == DBNull.Value)
            {
                return null;
            }
            return cell.Value;
        }
        /// <summary>
        /// 返回数组值
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
        /// 取值
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
        /// 将行的数据转成两列（ColumnName、Value）的表
        /// </summary>
        public MDataTable ToTable()
        {
            MDataTable dt = this.Columns.ToTable();
            MCellStruct msValue = new MCellStruct("Value", SqlDbType.Variant);
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
        /// 将行的数据转成两列（ColumnName、Value）的表
        /// </summary>
        /// <param name="onlyData">仅数据（不含列头结构）</param>
        /// <returns></returns>
        public MDataTable ToTable(bool onlyData)
        {
            if (onlyData)
            {
                MDataTable dt = new MDataTable(this.TableName);
                dt.Columns.Add("ColumnName", SqlDbType.NVarChar);
                dt.Columns.Add("Value", SqlDbType.Variant);
                for (int i = 0; i < Count; i++)
                {
                    dt.NewRow(true).Set(0, this[i].ColumnName)
                        .Set(1, this[i].Value);
                }
                return dt;
            }
            return ToTable();
        }

        /// <summary>
        /// 将行的数据行的值全重置为Null
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < this.Count; i++)
            {
                this[i].Clear();
            }
        }

        /// <summary>
        /// 获取行的当前状态[0:未更改；1:已赋值,值相同[可插入]；2:已赋值,值不同[可更新]]
        /// </summary>
        /// <returns></returns>
        public int GetState()
        {
            return GetState(false);
        }
        /// <summary>
        /// 获取行的当前状态[0:未更改；1:已赋值,值相同[可插入]；2:已赋值,值不同[可更新]]
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
        /// 为行连续设置值多个值
        /// </summary>
        /// <param name="startKey">起始行索引||起始列名</param>
        /// <param name="values">多个值</param>
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
        /// 为行设置值
        /// </summary>
        public MDataRow Set(object key, object value)
        {
            return Set(key, value, -1);
        }
        /// <summary>
        /// 为行设置值
        /// </summary>
        /// <param name="key">字段名</param>
        /// <param name="value">值</param>
        /// <param name="state">手工设置状态[0:未更改；1:已赋值,值相同[可插入]；2:已赋值,值不同[可更新]]</param>
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
        /// 将行的数据行的状态全部重置
        /// </summary>
        /// <param name="state">状态[0:未更改；1:已赋值,值相同[可插入]；2:已赋值,值不同[可更新]]</param>
        public MDataRow SetState(int state)
        {
            return SetState(state, BreakOp.None);
        }
        /// <summary>
        /// 将行的数据行的状态全部重置
        /// </summary>
        /// <param name="state">状态[0:未更改；1:已赋值,值相同[可插入]；2:已赋值,值不同[可更新]]</param>
        /// <param name="op">状态设置选项</param>
        public MDataRow SetState(int state, BreakOp op)
        {
            return SetState(state, op, string.Empty);
        }
        /// <param name="columns"><para>批量指定某些列</para></param>
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
        /// <summary>
        /// 无参时默认自-1
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
                if (index > -1 && index < Count)
                {
                    return CellList[index];
                }
                return null;
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

        #region IList<MDataCell> 成员

        int IList<MDataCell>.IndexOf(MDataCell item)
        {
            return CellList.IndexOf(item);
        }

        #endregion

        #region ICollection<MDataCell> 成员

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

        #region IEnumerable<MDataCell> 成员

        IEnumerator<MDataCell> IEnumerable<MDataCell>.GetEnumerator()
        {
            return CellList.GetEnumerator();
        }

        #endregion

        #region IEnumerable 成员

        IEnumerator IEnumerable.GetEnumerator()
        {
            return CellList.GetEnumerator();
        }

        #endregion
    }
    //扩展交互部分
    public partial class MDataRow
    {
        /// <summary>
        /// 从实体、Json、Xml、IEnumerable接口实现的类、MDataRow
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
        /// 从实体、Json、Xml、IEnumerable接口实现的类、MDataRow
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
            row.SetState(1);//外部创建的状态默认置为1.
            return row;
        }

        /// <summary>
        /// 输出行的数据Json
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
        /// 输出行的数据Json
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
        /// 输出Json
        /// </summary>
        /// <param name="op">过滤条件</param>
        /// <param name="escapeOp">转义选项</param>
        /// <returns></returns>
        public string ToJson(RowOp op, bool isConvertNameToLower, EscapeOp escapeOp)
        {
            JsonHelper helper = new JsonHelper();
            if (DynamicData != null && DynamicData is MDictionary<int, int>)
            {
                helper.LoopCheckList = DynamicData as MDictionary<int, int>;//继承父的数据，避免循环引用父
                helper.Level = helper.LoopCheckList[helper.LoopCheckList.Count - 1] + 1;
            }
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
        /// 将json保存到指定文件中
        /// </summary>
        public bool WriteJson(string fileName, RowOp op)
        {
            return IOHelper.Write(fileName, ToJson(op));
        }
        */
        /// <summary>
        /// 转成实体
        /// </summary>
        /// <typeparam name="T">实体名称</typeparam>
        public T ToEntity<T>()
        {
            //Type t = typeof(T);
            //switch (ReflectTool.GetSystemType(ref t))
            //{
            //    case SysType.Base:
            //        return (T)ConvertTool.ChangeType(this[0].Value, t);

            //}
            //object obj = Activator.CreateInstance(t);
            //SetToEntity(ref obj, this);
            //return (T)obj;
            return (T)ToEntity(typeof(T));
        }
        internal object ToEntity(Type t)
        {
            if (t.Name == "MDataRow")
            {
                return this;
            }
            switch (ReflectTool.GetSystemType(ref t))
            {
                case SysType.Base:
                    return ConvertTool.ChangeType(this[0].Value, t);

            }
            object obj = Activator.CreateInstance(t);
            SetToEntity(ref obj, this);
            return obj;
        }

        private object GetValue(MDataRow row, Type type)
        {
            switch (ReflectTool.GetSystemType(ref type))
            {
                case SysType.Base:
                    return ConvertTool.ChangeType(row[0].Value, type);
                case SysType.Enum:
                    return Enum.Parse(type, row[0].ToString());
                default:
                    object o = Activator.CreateInstance(type);
                    SetToEntity(ref o, row);
                    return o;
            }
        }
        /// <summary>
        /// 将值批量赋给UI
        /// </summary>
        public void SetToAll(params object[] parentControls)
        {
            SetToAll(null, parentControls);
        }
        /// <summary>
        /// 将值批量赋给UI
        /// </summary>
        /// <param name="autoPrefix">自动前缀，多个可用逗号分隔</param>
        /// <param name="parentControls">页面控件</param>
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
        /// 从Web Post表单里取值。
        /// </summary>
        public void LoadFrom()
        {
            LoadFrom(true);
        }
        /// <summary>
        /// 从Web Post表单里取值 或 从Winform、WPF的表单控件里取值。
        /// <param name="isWeb">True为Web应用，反之为Win应用</param>
        /// <param name="prefixOrParentControl">Web时设置前缀，Win时设置父容器控件</param>
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
        /// 从别的行加载值
        /// </summary>
        public void LoadFrom(MDataRow row)
        {
            LoadFrom(row, RowOp.None, Count == 0);
        }
        /// <summary>
        /// 从别的行加载值
        /// </summary>
        public void LoadFrom(MDataRow row, RowOp rowOp, bool isAllowAppendColumn)
        {
            LoadFrom(row, rowOp, isAllowAppendColumn, true);
        }
        /// <summary>
        /// 从别的行加载值
        /// </summary>
        /// <param name="row">要加载数据的行</param>
        /// <param name="rowOp">行选项[从其它行加载的条件]</param>
        /// <param name="isAllowAppendColumn">如果row带有新列，是否加列</param>
        /// <param name="isWithValueState">是否同时加载值的状态[默认值为：true]</param>
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
                        cell.LoadValue(rowCell, isWithValueState);
                        //if (cell.Struct.SqlType == rowCell.Struct.SqlType)
                        //{
                        //    cell.CellValue.StringValue = rowCell.StringValue;
                        //    cell.CellValue.Value = rowCell.CellValue.Value;
                        //    cell.CellValue.IsNull = rowCell.CellValue.IsNull;
                        //}
                        //else
                        //{
                        //cell.Value = rowCell.Value;//用属于赋值，因为不同的架构，类型若有区别如 int[access] int64[sqlite]，在转换时会出错
                        //cell._CellValue.IsNull = rowCell._CellValue.IsNull;//
                        //}

                        //if (isWithValueState)
                        //{
                        //    cell.State = rowCell.State;
                        //}
                    }
                }

            }
        }
        /// <summary>
        /// 从数组里加载值
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
        /// 从json里加载值
        /// </summary>
        public void LoadFrom(string json, EscapeOp op)
        {
            LoadFrom(json, op, BreakOp.None);
        }
        /// <summary>
        /// 从json里加载值
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
        /// 从泛型字典集合里加载
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
        /// 将实体转成数据行。
        /// </summary>
        /// <param name="entity">实体对象</param>
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
        /// 将实体转成数据行。
        /// </summary>
        /// <param name="entity">实体对象</param>
        public void LoadFrom(object entity, BreakOp op)
        {
            if (entity == null)
            {
                return;
            }
            try
            {
                Type t = entity.GetType();
                if (Columns.Count == 0)
                {
                    MDataColumn mcs = TableSchema.GetColumnByType(t);
                    MCellStruct ms = null;
                    for (int i = 0; i < mcs.Count; i++)
                    {
                        ms = mcs[i];
                        MDataCell cell = new MDataCell(ref ms);
                        Add(cell);
                    }
                }

                if (string.IsNullOrEmpty(TableName))
                {
                    TableName = t.Name;
                }
                List<PropertyInfo> pis = ReflectTool.GetPropertyList(t);
                if (pis.Count > 0)
                {
                    foreach (PropertyInfo p in pis)
                    {
                        SetValueToCell(entity, op, p, null);
                    }
                }
                else
                {
                    List<FieldInfo> fis = ReflectTool.GetFieldList(t);
                    if (fis.Count > 0)
                    {
                        foreach (FieldInfo f in fis)
                        {
                            SetValueToCell(entity, op, null, f);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Log.Write(err, LogType.Error);
            }
        }
        private void SetValueToCell(object entity, BreakOp op, PropertyInfo p, FieldInfo f)
        {
            string name = p != null ? p.Name : f.Name;
            int index = Columns.GetIndex(name);
            if (index > -1)
            {
                object objValue = p != null ? p.GetValue(entity, null) : f.GetValue(entity);
                switch (op)
                {
                    case BreakOp.Null:
                        if (objValue == null)
                        {
                            return;
                        }
                        break;
                    case BreakOp.Empty:
                        if (Convert.ToString(objValue) == "")
                        {
                            return;
                        }
                        break;
                    case BreakOp.NullOrEmpty:
                        if (objValue == null || Convert.ToString(objValue) == "")
                        {
                            return;
                        }
                        break;
                }
                Set(index, objValue, 2);//它的状态应该值设置，改为1是不对的。
            }
        }
        /// <summary>
        /// 将行中的数据值赋给实体对象
        /// </summary>
        /// <param name="obj">实体对象</param>
        public void SetToEntity(object obj)
        {
            SetToEntity(obj, RowOp.IgnoreNull);
        }
        public void SetToEntity(object obj, RowOp op)
        {
            SetToEntity(ref obj, this, op);
        }
        /// <summary>
        /// 将指定行的数据赋给实体对象。
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="row"></param>
        internal void SetToEntity(ref object obj, MDataRow row)
        {
            SetToEntity(ref obj, row, RowOp.IgnoreNull);
        }
        internal void SetToEntity(ref object obj, MDataRow row, RowOp op)
        {
            if (obj == null || row == null || row.Count == 0)
            {
                return;
            }
            Type objType = obj.GetType();
            string objName = objType.FullName, cellName = "";
            try
            {
                #region 处理核心
                List<PropertyInfo> pis = ReflectTool.GetPropertyList(objType);
                if (pis.Count > 0)
                {
                    foreach (PropertyInfo p in pis)//遍历实体
                    {
                        if (p.CanWrite)
                        {
                            SetValueToPropertyOrField(ref obj, row, p, null, op, out cellName);
                        }
                    }
                }
                else
                {
                    List<FieldInfo> fis = ReflectTool.GetFieldList(objType);
                    if (fis.Count > 0)
                    {
                        foreach (FieldInfo f in fis)//遍历实体
                        {
                            SetValueToPropertyOrField(ref obj, row, null, f, op, out cellName);
                        }
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

        private void SetValueToPropertyOrField(ref object obj, MDataRow row, PropertyInfo p, FieldInfo f, RowOp op, out string cellName)
        {
            cellName = p != null ? p.Name : f.Name;
            MDataCell cell = row[cellName];
            if (cell == null)
            {
                return;
            }
            if (op == RowOp.IgnoreNull && cell.IsNull)
            {
                return;
            }
            else if (op == RowOp.Insert && cell.State == 0)
            {
                return;
            }
            else if (op == RowOp.Update && cell.State != 2)
            {
                return;
            }
            Type propType = p != null ? p.PropertyType : f.FieldType;
            object objValue = GetObj(propType, cell.Value);

            if (p != null)
            {
                p.SetValue(obj, objValue, null);
            }
            else
            {
                f.SetValue(obj, objValue);
            }
        }

        internal object GetObj(Type toType, object objValue)
        {
            if (objValue == null) { return null; }
            Type propType = toType;
            string value = Convert.ToString(objValue);
            object returnObj = null;
            SysType sysType = ReflectTool.GetSystemType(ref propType);
            switch (sysType)
            {
                case SysType.Enum:
                    returnObj = ConvertTool.ChangeType(objValue, toType);// Enum.Parse(propType, value);
                    break;
                case SysType.Base:
                    #region 基础类型处理
                    if (propType.Name == "String")
                    {
                        //去掉转义符号
                        if (value.IndexOf("\\\"") > -1)
                        {
                            returnObj = value.Replace("\\\"", "\"");
                        }
                        else
                        {
                            returnObj = value;
                        }
                    }
                    else
                    {
                        returnObj = ConvertTool.ChangeType(value, propType);
                    }
                    #endregion
                    break;
                case SysType.Array:
                case SysType.Collection:
                case SysType.Generic:
                    #region 数组处理
                    if (objValue.GetType() == propType)
                    {
                        returnObj = objValue;
                    }
                    else
                    {
                        Type[] argTypes = null;
                        int len = ReflectTool.GetArgumentLength(ref propType, out argTypes);
                        if (len == 1) // Table
                        {

                            if (value.Contains(":") && value.Contains("{"))
                            {
                                #region Json 嵌套处理，复杂数组处理。
                                MDataTable dt = MDataTable.CreateFrom(value);//, SchemaCreate.GetColumns(argTypes[0])
                                returnObj = dt.ToList(propType);
                                //returnObj = Activator.CreateInstance(propType, dt.Rows.Count);//创建实例
                                //Type objListType = returnObj.GetType();
                                //bool isArray = sysType == SysType.Array;
                                //for (int i = 0; i < dt.Rows.Count; i++)
                                //{
                                //    MDataRow rowItem = dt.Rows[i];
                                //    object o = GetValue(rowItem, argTypes[0]);
                                //    MethodInfo method;
                                //    if (isArray)
                                //    {
                                //        Type objType = propType.Assembly.GetType(propType.FullName.Replace("[]", ""));
                                //        Object item = rowItem.ToEntity(objType);
                                //        method = objListType.GetMethod("Set");
                                //        if (method != null)
                                //        {
                                //            method.Invoke(returnObj, new object[] { i, item });
                                //        }
                                //    }
                                //    else
                                //    {
                                //        method = objListType.GetMethod("Add");
                                //        if (method == null)
                                //        {
                                //            method = objListType.GetMethod("Push");
                                //        }
                                //        if (method != null)
                                //        {
                                //            method.Invoke(returnObj, new object[] { o });
                                //        }
                                //    }
                                //}
                                dt = null;
                                #endregion
                            }
                            else
                            {
                                #region 单纯的基础类型数组处理["xxx","xxx2","xx3"]
                                List<string> items = JsonSplit.SplitEscapeArray(value);//内部去掉转义符号
                                if (items == null) { return null; }
                                returnObj = Activator.CreateInstance(propType, items.Count);//创建实例
                                Type objListType = returnObj.GetType();
                                bool isArray = sysType == SysType.Array;
                                for (int i = 0; i < items.Count; i++)
                                {
                                    MethodInfo method;
                                    if (isArray)
                                    {
                                        Object item = ConvertTool.ChangeType(items[i], Type.GetType(propType.FullName.Replace("[]", "")));
                                        method = objListType.GetMethod("Set");
                                        if (method != null)
                                        {
                                            method.Invoke(returnObj, new object[] { i, item });
                                        }
                                    }
                                    else
                                    {
                                        Object item = ConvertTool.ChangeType(items[i], argTypes[0]);
                                        method = objListType.GetMethod("Add");
                                        if (method == null)
                                        {
                                            method = objListType.GetMethod("Push");
                                        }
                                        if (method != null)
                                        {
                                            method.Invoke(returnObj, new object[] { item });
                                        }
                                    }
                                }
                                #endregion
                            }
                        }
                        else if (len == 2) // row
                        {
                            MDataRow mRow = MDataRow.CreateFrom(value, argTypes[1]);
                            returnObj = Activator.CreateInstance(propType, mRow.Columns.Count);//创建实例
                            foreach (MDataCell mCell in mRow)
                            {
                                object mObj = GetValue(mCell.ToRow(), argTypes[1]);
                                returnObj.GetType().GetMethod("Add").Invoke(returnObj, new object[] { mCell.ColumnName, mObj });
                            }
                            mRow = null;
                        }
                    }
                    #endregion
                    break;
                case SysType.Custom://继续递归
                    MDataRow mr = new MDataRow(TableSchema.GetColumnByType(propType));
                    mr.LoadFrom(value);
                    returnObj = Activator.CreateInstance(propType);
                    SetToEntity(ref returnObj, mr);
                    mr = null;
                    break;

            }
            return returnObj;
        }
    }

}
