using System;
using System.ComponentModel;
using System.Collections;

namespace CYQ.Data.Table
{

    /// <summary>
    /// 仅用于Winform的列表绑定。
    /// </summary>
    [Serializable]
    internal class MDataView : IListSource
    {
        MDataTable table;
        public MDataView(ref MDataTable dt)
        {
            table = dt;
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
}
