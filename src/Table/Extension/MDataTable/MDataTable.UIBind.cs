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
    /// ���
    /// </summary>
    public partial class MDataTable
    {
        /// <summary>
        /// �����ݱ�󶨵��б�ؼ�
        /// </summary>
        /// <param name="control">�б�ؼ�[����Repeater/DataList/GridView/DataGrid��]</param>
        public void Bind(object control)
        {
            Bind(control, null);
        }
        /// <summary>
        /// �����ݱ�󶨵��б�ؼ�
        /// </summary>
        /// <param name="control">�б�ؼ�[����Repeater/DataList/GridView/DataGrid��]</param>
        /// <param name="nodeID">��ControlΪXHtmlAction����ʱ����Ҫָ���󶨵Ľڵ�id</param>
        public void Bind(object control, string nodeID)
        {
            MBindUI.Bind(control, this, nodeID);
        }
    }
}
