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
        #region Select、FindRow、FindAll、GetIndex、GetCount、Split
        /// <summary>
        /// 使用本查询，得到克隆后的数据
        /// </summary>
        public MDataTable Select(object where)
        {
            return Select(0, 0, where);
        }
        /// <summary>
        /// 使用本查询，得到克隆后的数据
        /// </summary>
        public MDataTable Select(int topN, object where)
        {
            return Select(1, topN, where);
        }
        /// <summary>
        /// 使用本查询，得到克隆后的数据
        /// </summary>
        public MDataTable Select(int pageIndex, int pageSize, object where, params object[] selectColumns)
        {
            return MDataTableFilter.Select(this, pageIndex, pageSize, where, selectColumns);
        }
        /// <summary>
        /// 使用本查询，得到原数据的引用。
        /// </summary>
        public MDataRow FindRow(object where)
        {
            return MDataTableFilter.FindRow(this, where);
        }
        /// <summary>
        /// 使用本查询，得到原数据的引用。
        /// </summary>
        public MDataRowCollection FindAll(object where)
        {
            return MDataTableFilter.FindAll(this, where);
        }
        /// <summary>
        /// 统计满足条件的行所在的索引
        /// </summary>
        public int GetIndex(object where)
        {
            return MDataTableFilter.GetIndex(this, where);
        }
        /// <summary>
        /// 统计满足条件的行数
        /// </summary>
        public int GetCount(object where)
        {
            return MDataTableFilter.GetCount(this, where);
        }
        /// <summary>
        /// 根据条件分拆成两个表【满足条件，和非满足条件的】，分出来的数据行和原始表仍是同一个引用
        /// </summary>
        public MDataTable[] Split(object where)
        {
            return MDataTableFilter.Split(this, where);
        }
        #endregion
    }
}
