using System;
using System.ComponentModel;
using System.Collections;

namespace CYQ.Data.Table
{

    /// <summary>
    /// 仅用于Winform的列表绑定。
    /// </summary>
    [Serializable]
    internal partial class MDataView : IListSource
    {
        MDataTable table;
        public MDataView(ref MDataTable dt)
        {
            table = dt;
            //ListChanged += MDataView_ListChanged;
        }

        
        #region IListSource 成员

        public bool ContainsListCollection
        {
            get
            {
                return true;
            }
        }

        public IList GetList()
        {
            return table.Rows;
        }

        #endregion
    }
    /*
    internal partial class MDataView : IBindingList
    {
        void MDataView_ListChanged(object sender, ListChangedEventArgs e)
        {

        }
        public void AddIndex(PropertyDescriptor property)
        {
            
        }

        public object AddNew()
        {
            return null;
        }

        public bool AllowEdit
        {
            get { return true; }
        }

        public bool AllowNew
        {
            get { return true; }
        }

        public bool AllowRemove
        {
            get { return true; }
        }

        public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            
        }

        public int Find(PropertyDescriptor property, object key)
        {
            return 0;
        }

        public bool IsSorted
        {
            get { return true; }
        }

        public event ListChangedEventHandler ListChanged;

        public void RemoveIndex(PropertyDescriptor property)
        {
            
        }

        public void RemoveSort()
        {
            
        }

        public ListSortDirection SortDirection
        {
            get { return new ListSortDirection(); }
        }

        public PropertyDescriptor SortProperty
        {
            get { return null; }
        }

        public bool SupportsChangeNotification
        {
            get { return true; }
        }

        public bool SupportsSearching
        {
            get { return true; }
        }

        public bool SupportsSorting
        {
            get { return true; }
        }

        public int Add(object value)
        {
            return 0;
        }

        public void Clear()
        {
            
        }

        public bool Contains(object value)
        {
            return true;
        }

        public int IndexOf(object value)
        {
            return 0;
        }

        public void Insert(int index, object value)
        {
            
        }

        public bool IsFixedSize
        {
            get { return true; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public void Remove(object value)
        {
            
        }

        public void RemoveAt(int index)
        {
            
        }

        public object this[int index]
        {
            get
            {
                return null;
            }
            set
            {
         
            }
        }

        public void CopyTo(Array array, int index)
        {
            
        }

        public int Count
        {
            get { return 0; }
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

        public object SyncRoot
        {
            get { return null; }
        }

        public IEnumerator GetEnumerator()
        {
            return null;
        }
    }
    internal partial class MDataView : IBindingListView
    {
        public void ApplySort(ListSortDescriptionCollection sorts)
        {
            
        }

        public string Filter
        {
            get
            {
                return null;
            }
            set
            {
                
            }
        }

        public void RemoveFilter()
        {
            
        }

        public ListSortDescriptionCollection SortDescriptions
        {
            get { return null; }
        }

        public bool SupportsAdvancedSorting
        {
            get { return true; }
        }

        public bool SupportsFiltering
        {
            get { return true; }
        }
    }
     */
}
