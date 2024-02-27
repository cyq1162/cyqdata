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
    public partial class MDataRow : IList<MDataCell>
    {
        public int Count
        {
            get { return CellList.Count; }
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

        /// <summary>
        /// 开放给Emit调用：MDataRowToKeyValue
        /// </summary>
        /// <returns></returns>
        public IEnumerator<MDataCell> GetEnumerator()
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

}
