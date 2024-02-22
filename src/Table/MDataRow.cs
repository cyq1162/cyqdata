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

        private MDataTable _Table;
        /// <summary>
        /// 获取该行拥有其架构的 MDataTable。
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
        /// 通过一个数组来获取或设置此行的所有值。
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

        #region Get、GetState、GetItemValue

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
        /// 返回数组值
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

        #region Sets、Set、SetState


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

        #endregion

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
    }

}
