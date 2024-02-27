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
        /// <summary>
        /// 将数据表绑定到列表控件
        /// </summary>
        /// <param name="control">列表控件[包括Repeater/DataList/GridView/DataGrid等]</param>
        public void Bind(object control)
        {
            Bind(control, null);
        }
        /// <summary>
        /// 将数据表绑定到列表控件
        /// </summary>
        /// <param name="control">列表控件[包括Repeater/DataList/GridView/DataGrid等]</param>
        /// <param name="nodeID">当Control为XHtmlAction对象时，需要指定绑定的节点id</param>
        public void Bind(object control, string nodeID)
        {
            MBindUI.Bind(control, this, nodeID);
        }
    }
}
