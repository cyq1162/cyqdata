using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;

namespace System.Web.UI.WebControls
{
    internal abstract class ListControl
    {
        public MDataTable DataSource { get; internal set; }
        public string DataValueField { get; internal set; }
        public string DataTextField { get; set; }
        internal void DataBind()
        {
            throw new NotImplementedException();
        }
    }
}