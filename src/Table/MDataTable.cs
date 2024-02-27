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
    /// 表格
    /// </summary>
    public partial class MDataTable
    {
        internal const string DefaultTableName = "SysDefault";

        #region 属性
        private MDataRowCollection _Rows;
        /// <summary>
        /// 表格行
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
        /// 动态存储数据(如：AcceptChanges 产生的异常默认由本参数存储)
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
        /// 表名
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
        /// 表名描述
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
        /// 表格的架构列
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
        /// 该表归属的数据库链接。
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

        #region 方法
        /// <summary>
        /// 新建一行
        /// </summary>
        /// <returns></returns>
        public MDataRow NewRow()
        {
            return NewRow(false);
        }
        /// <summary>
        /// 新建一行
        /// </summary>
        /// <param name="isAddToTable">是否顺带添加到表中</param>
        /// <returns></returns>
        public MDataRow NewRow(bool isAddToTable)
        {
            return NewRow(isAddToTable, -1);
        }
        /// <summary>
        /// 新建一行
        /// </summary>
        /// <param name="index">插入的索引</param>
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
        /// 将表里所有行的数据行的状态全部重置
        /// </summary>
        /// <param name="state">状态[0:未更改；1:已赋值,值相同[可插入]；2:已赋值,值不同[可更新]]</param>
        public MDataTable SetState(int state)
        {
            SetState(state, BreakOp.None); return this;
        }
        /// <summary>
        /// 将表里所有行的数据行的状态全部重置
        /// </summary>
        /// <param name="state">状态[0:未更改；1:已赋值,值相同[可插入]；2:已赋值,值不同[可更新]]</param>
        /// <param name="op">状态设置选项</param>
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
        /// 返回某列的集合
        /// <param name="columnName">列名</param>
        /// </summary>
        public List<T> GetColumnItems<T>(string columnName)
        {
            return GetColumnItems<T>(columnName, BreakOp.None, false);
        }
        /// <summary>
        /// 返回某列的集合
        /// <param name="columnName">列名</param>
        /// <param name="op">参数选项</param>
        /// </summary>
        public List<T> GetColumnItems<T>(string columnName, BreakOp op)
        {
            return GetColumnItems<T>(columnName, op, false);
        }
        /// <summary>
        /// 返回某列的集合
        /// </summary>
        /// <typeparam name="T">列的类型</typeparam>
        /// <param name="columnIndex">列名</param>
        /// <param name="op">过滤选项</param>
        /// <param name="isDistinct">是否去掉重复数据</param>
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
        /// 返回某列的集合
        /// </summary>
        /// <typeparam name="T">列的类型</typeparam>
        /// <param name="columnIndex">第N列</param>
        public List<T> GetColumnItems<T>(int columnIndex)
        {
            return GetColumnItems<T>(columnIndex, BreakOp.None);
        }

        /// <summary>
        /// 返回某列的集合
        /// </summary>
        /// <typeparam name="T">列的类型</typeparam>
        /// <param name="columnIndex">第N列</param>
        /// <param name="op">过滤选项</param>
        /// <returns></returns>
        public List<T> GetColumnItems<T>(int columnIndex, BreakOp op)
        {
            return GetColumnItems<T>(columnIndex, op, false);
        }
        /// <summary>
        /// 返回某列的集合
        /// </summary>
        /// <typeparam name="T">列的类型</typeparam>
        /// <param name="columnIndex">第N列</param>
        /// <param name="op">过滤选项</param>
        /// <param name="isDistinct">是否去掉重复数据</param>
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
        /// 复制表
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
        /// 复制表的结构
        /// </summary>
        /// <param name="clone">是否克隆表结构</param>
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
        /// 加载行(包括行架构)[提示，仅当表为空架构时有效]
        /// </summary>
        /// <param name="row"></param>
        internal void LoadRow(MDataRow row) //是否直接能用Row.Table呢？？、
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
        /// 将新表的行放到原表的下面。
        /// </summary>
        /// <param name="newTable"></param>
        public void Merge(MDataTable newTable)
        {
            if (newTable != null && newTable.Rows.Count > 0)
            {
                int count = newTable.Rows.Count;//提前获取总数，是为了避免dt.Merge(dt);//加载自身导致的死循环。
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
