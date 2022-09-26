﻿using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.SQL;
using System.ComponentModel;
using System.Collections;
using System.Data;

namespace CYQ.Data.Table
{
    /// <summary>
    /// 行集合
    /// </summary>
    public partial class MDataRowCollection
    {
        List<MDataRow> RowList;
        /// <summary>
        /// 隐式转换列头
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static implicit operator MDataRowCollection(DataRowCollection rows)
        {
            MDataRowCollection mrc = new MDataRowCollection();
            if (rows != null && rows.Count > 0)
            {
                foreach (DataRow row in rows)
                {
                    mrc.Add(row);
                }
            }
            return mrc;


        }
        public static implicit operator MDataRowCollection(List<MDataRow> rows)
        {
            MDataRowCollection mrc = new MDataRowCollection();
            if (rows != null && rows.Count > 0)
            {
                foreach (MDataRow row in rows)
                {
                    mrc.Add(row);
                }
            }
            return mrc;
        }
        internal MDataRowCollection()
        {

            RowList = new List<MDataRow>(100);
        }
        internal MDataRowCollection(MDataTable dt)
        {
            RowList = new List<MDataRow>(100);
            _Table = dt;
        }

       
        //private static int cCount = 0;

        private MDataTable _Table;
        /// <summary>
        /// 获取该行拥有其架构的 MDataTable。
        /// </summary>
        public MDataTable Table
        {
            get
            {
                return _Table;
            }
            internal set
            {
                _Table = value;
            }
        }

    }
    public partial class MDataRowCollection : IList<MDataRow>
    {
        #region 复盖IList相关方法
        /// <summary>
        /// 创建使用指定值的行，并将其添加到 MDataRowCollection 中
        /// </summary>
        /// <param name="values"></param>
        public void Add(params object[] values)
        {
            if (_Table != null)
            {
                if (values != null && values.Length <= _Table.Columns.Count)
                {
                    _Table.NewRow(true).LoadFrom(values);
                }
            }
        }
        /// <summary>
        /// 添加行[默认重置状态为true]
        /// </summary>
        /// <param name="item">行</param>
        /// <param name="resetState">重置状态,[默认true]，重置后的行数据不参与MDataTable的批量更新操作,状态在重新赋值时改变。</param>
        public void Add(MDataRow item, bool resetState)
        {
            if (resetState)
            {
                item.SetState(0);
            }
            CheckError(item, null);
            RowList.Add(item);
        }
        public void Add(MDataRow item)
        {
            Add(item, true);
        }
        public void AddRange(IEnumerable<MDataRow> collection)
        {
            CheckError(null, collection);
            RowList.AddRange(collection);
        }
        public void Insert(int index, MDataRow item)
        {
            CheckError(item, null);
            RowList.Insert(index, item);
        }
        public void InsertRange(int index, IEnumerable<MDataRow> collection)
        {
            CheckError(null, collection);
            RowList.InsertRange(index, collection);
        }
        private void CheckError(MDataRow item, IEnumerable<MDataRow> collection)
        {
            if (_Table != null)
            {
                if (item != null)
                {
                    if (item.Columns != _Table.Columns)
                    {
                        Error.Throw("This row already belongs to another table");
                    }
                    item.Table = _Table;
                }
                else
                {
                    using (IEnumerator<MDataRow> ie = collection.GetEnumerator())
                    {
                        while (ie.MoveNext())
                        {
                            if (ie.Current.Columns != _Table.Columns)
                            {
                                Error.Throw("This row already belongs to another table");
                            }
                            ie.Current.Table = _Table;
                        }
                    }
                }
            }
        }
        public bool Remove(MDataRow item)
        {
            RowList.Remove(item);
            return true;
        }
        public void RemoveAt(int index)
        {
            RowList.RemoveAt(index);
        }
        #endregion


        public void Clear()
        {
            RowList.Clear();
        }

        #region IList<MDataRow> 成员

        int IList<MDataRow>.IndexOf(MDataRow item)
        {
            return RowList.IndexOf(item);
        }

        public MDataRow this[int index]
        {
            get
            {
                if (index < 0 || index >= RowList.Count)
                {
                    return null;
                }
                return RowList[index];
            }
            set
            {
                Error.Throw(AppConst.Global_NotImplemented);
            }
        }

        #endregion

        #region ICollection<MDataRow> 成员

        public bool Contains(MDataRow item)
        {
            return RowList.Contains(item);
        }

        void ICollection<MDataRow>.CopyTo(MDataRow[] array, int arrayIndex)
        {
            RowList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return RowList.Count; }
        }

        bool ICollection<MDataRow>.IsReadOnly
        {
            get { return false; }
        }
        internal List<MDataRow> FindAll(Predicate<MDataRow> match)
        {
            return RowList.FindAll(match);
        }
        internal void RemoveRange(int index, int count)
        {
            RowList.RemoveRange(index, count);
        }
        #endregion

        #region IEnumerable<MDataRow> 成员

        IEnumerator<MDataRow> IEnumerable<MDataRow>.GetEnumerator()
        {
            return RowList.GetEnumerator();
        }

        #endregion
    }

    public partial class MDataRowCollection : IComparer<MDataRow>
    {
        private int lastIndex = -1;//上一个索引,在多重排序中，如果二次排序遇到相同的值，采用上一个索引字段进行比较
        private bool lastIsAsc = true;
        private int index = -1;
        DataGroupType group = DataGroupType.None;
        private bool isAsc = true;
        private int useLastIndexState = 0;//0未使用,1开启使用,2正在使用
        // private object objAValue, objBValue;
        public int Compare(MDataRow x, MDataRow y)
        {
            // cCount++;
            if (x == y)//自己和自己比，返回0跳出
            {
                return 0;
            }
            int cellIndex = index;
            bool cellIsAsc = isAsc;
            DataGroupType cellGroup = group;
            if (useLastIndexState == 1)//出于多重排序的需要这么使用。
            {
                cellIndex = lastIndex;
                cellIsAsc = lastIsAsc;
                useLastIndexState = 2;
                cellGroup = DataType.GetGroup(x[cellIndex].Struct.SqlType);
            }
            else if (useLastIndexState == 2)
            {
                useLastIndexState = 0;
            }
            //Null判断处理
            if (x[cellIndex].IsNull && y[cellIndex].IsNull)
            {
                return 0;
            }
            else if (x[cellIndex].IsNull)
            {
                return cellIsAsc ? -1 : 1;
            }
            else if (y[cellIndex].IsNull)
            {
                return cellIsAsc ? 1 : -1;
            }
            object objAValue = x[cellIndex].Value;
            object objBValue = y[cellIndex].Value;
            switch (cellGroup)
            {
                case DataGroupType.Number:
                case DataGroupType.Bool:
                    double vA, vB;
                    if (cellGroup == DataGroupType.Number)
                    {
                        double.TryParse(objAValue.ToString(), out vA);
                        double.TryParse(objBValue.ToString(), out vB);
                        //vA = (int)objAValue;
                        //vB = (int)objBValue;
                    }
                    else
                    {
                        vA = (bool)objAValue ? 1 : 0;
                        vB = (bool)objBValue ? 1 : 0;
                    }
                    if (vA > vB) { return cellIsAsc ? 1 : -1; }
                    else if (vA < vB) { return cellIsAsc ? -1 : 1; }
                    else
                    {
                        if (lastIndex > -1 && useLastIndexState == 0)
                        {
                            useLastIndexState = 1;//标志为正在使用
                            return Compare(x, y);
                        }
                        return 0;
                    }
                case DataGroupType.Date:
                    return cellIsAsc ? Comparer<DateTime>.Default.Compare((DateTime)objAValue, (DateTime)objBValue) : Comparer<DateTime>.Default.Compare((DateTime)objBValue, (DateTime)objAValue);
                default:
                    //直接性能差一点return cellIsAsc ? Comparer<string>.Default.Compare((string)objAValue, (string)objBValue) : Comparer<string>.Default.Compare((string)objBValue, (string)objAValue);
                    if (cellIsAsc)
                    {
                        return Convert.ToString(objAValue).CompareTo(objBValue.ToString());
                    }
                    else
                    {
                        return Convert.ToString(objBValue).CompareTo(objAValue.ToString());
                    }
            }

        }


        public void Sort(string orderby)
        {
            if (string.IsNullOrEmpty(orderby) || this.Count < 2) { return; }
            orderby = orderby.Trim();
            if (orderby.ToLower().StartsWith("order by"))
            {
                orderby = orderby.Substring(9);//去掉order by 前缀
            }
            string[] orderbyList = orderby.Split(',');//分隔多个order by 语句
            string[] orderbyItems;

            for (int i = orderbyList.Length - 1; i > -1; i--)//反转循环
            {
                if (string.IsNullOrEmpty(orderbyList[i]))
                {
                    continue;
                }
                orderbyItems = orderbyList[i].Split(' ');//分隔

                index = this[0].Columns.GetIndex(orderbyItems[0]);
                if (index > -1)
                {
                    isAsc = (orderbyItems.Length > 1 && orderbyItems[1].ToLower() == "desc") ? false : true;
                    group = DataType.GetGroup(this[0][index].Struct.SqlType);
                    // cCount = 0;
                    RowList.Sort(Compare);
                    lastIndex = index;
                    lastIsAsc = isAsc;
                }

            }
            index = -1;
            lastIndex = -1;
        }
    }

    //public partial class MDataroCollection : ICollection, IEnumerable
    //{

    //    #region ICollection 成员
    //    LinkedList<MDataRow> rows = new LinkedList<MDataRow>();
    //    public void CopyTo(Array array, int index)
    //    {
    //        rows.CopyTo(array, index);
    //        throw new Exception("The method or operation is not implemented.");
    //    }

    //    public int Count
    //    {
    //        get { throw new Exception("The method or operation is not implemented."); }
    //    }

    //    public bool IsSynchronized
    //    {
    //        get { throw new Exception("The method or operation is not implemented."); }
    //    }

    //    public object SyncRoot
    //    {
    //        get { throw new Exception("The method or operation is not implemented."); }
    //    }

    //    #endregion

    //    #region IEnumerable 成员

    //    public IEnumerator GetEnumerator()
    //    {
    //        throw new Exception("The method or operation is not implemented.");
    //    }

    //    #endregion

    //    #region IEnumerable 成员

    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        throw new Exception("The method or operation is not implemented.");
    //    }

    //    #endregion
    //}

}