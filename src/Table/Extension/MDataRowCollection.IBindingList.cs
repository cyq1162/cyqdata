using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.SQL;
using System.ComponentModel;
using System.Collections;
using System.Data;

namespace CYQ.Data.Table
{
    /// <summary>
    /// 实现Winform DataGridView 的添加编辑和删除操作功能。
    /// </summary>
    public partial class MDataRowCollection : IBindingList
    {


        void IBindingList.AddIndex(PropertyDescriptor property)
        {

        }

        object IBindingList.AddNew()
        {
            if (Count > 0)
            {
                MDataRow row = this[Count - 1];
                foreach (MDataCell cell in row)
                {
                    if (!cell.IsNull) // 避免重复新增加空行。
                    {
                        return this.Table.NewRow(true);
                    }
                }
            }
            return null;
        }

        bool IBindingList.AllowEdit
        {
            get { return true; }
        }

        bool IBindingList.AllowNew
        {
            get { return true; }
        }

        bool IBindingList.AllowRemove
        {
            get { return true; }
        }

        void IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {

        }

        int IBindingList.Find(PropertyDescriptor property, object key)
        {
            return -1;
        }

        bool IBindingList.IsSorted
        {
            get { return false; }
        }
        private ListChangedEventHandler onListChanged;
        internal static ListChangedEventArgs ResetEventArgs = new ListChangedEventArgs(ListChangedType.Reset, -1);

        event ListChangedEventHandler IBindingList.ListChanged
        {
            add
            {
                this.onListChanged = (ListChangedEventHandler)Delegate.Combine(this.onListChanged, value);
            }
            remove
            {
                this.onListChanged = (ListChangedEventHandler)Delegate.Remove(this.onListChanged, value);
            }
        }

        void IBindingList.RemoveIndex(PropertyDescriptor property)
        {

        }

        void IBindingList.RemoveSort()
        {

        }

        ListSortDirection IBindingList.SortDirection
        {
            get { return default(ListSortDirection); }
        }

        PropertyDescriptor IBindingList.SortProperty
        {
            get { return null; }
        }

        bool IBindingList.SupportsChangeNotification
        {
            get { return true; }
        }

        bool IBindingList.SupportsSearching
        {
            get { return true; }
        }

        bool IBindingList.SupportsSorting
        {
            get { return true; }
        }

        int System.Collections.IList.Add(object value)
        {
            this.Add(value as MDataRow);
            return this.Count - 1;
        }

        void System.Collections.IList.Clear()
        {
            this.Clear();
        }

        bool System.Collections.IList.Contains(object value)
        {
            return this.Contains(value as MDataRow);
        }

        int System.Collections.IList.IndexOf(object value)
        {
            return RowList.IndexOf(value as MDataRow);
        }

        void System.Collections.IList.Insert(int index, object value)
        {

        }

        bool System.Collections.IList.IsFixedSize
        {
            get { return false; }
        }

        bool System.Collections.IList.IsReadOnly
        {
            get { return false; }
        }

        void System.Collections.IList.Remove(object value)
        {
            this.Remove(value as MDataRow);
        }

        void System.Collections.IList.RemoveAt(int index)
        {
            this.RemoveAt(index);
            this.onListChanged(this, ResetEventArgs);

        }

        object System.Collections.IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this[index] = value as MDataRow;
            }
        }

        void System.Collections.ICollection.CopyTo(Array array, int index)
        {

        }

        int System.Collections.ICollection.Count
        {
            get { return this.Count; }
        }

        bool System.Collections.ICollection.IsSynchronized
        {
            get { return false; }
        }

        object System.Collections.ICollection.SyncRoot
        {
            get { return this; }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return RowList.GetEnumerator();
        }
    }

    //internal partial class MDataView : IEditableObject, IEnumerable
    //{


    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        return new System.Data.Common.DbEnumerator(table);
    //    }

    //    void IEditableObject.BeginEdit()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    void IEditableObject.CancelEdit()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    void IEditableObject.EndEdit()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}