using System;
using System.Web.UI.WebControls;
using System.Text;
using System.Web.UI;
using Win = System.Windows.Forms;
using CYQ.Data.Table;
using System.Reflection;

namespace CYQ.Data.UI
{
    internal class MBindUI
    {
        public static void Bind(object ct, object source)
        {
            Type t = ct.GetType();
            PropertyInfo p = t.GetProperty("DataSource");
            if (p != null)
            {
                MethodInfo meth = t.GetMethod("DataBind");
                if (meth != null)//web
                {
                    p.SetValue(ct, source, null);
                    meth.Invoke(ct, null);
                }
                else
                {
                    if (source is MDataTable)
                    {
                        MDataTable dt = source as MDataTable;
                        source = new MDataView(ref dt);
                    }
                    p.SetValue(ct, source, null);//winform
                }
            }
            else //wpf,sliverlight
            {
                p = t.GetProperty("ItemsSource");
                if (p != null)
                {
                    if (source is MDataTable)
                    {
                        MDataTable dt = source as MDataTable;
                        //source = new MDataView(ref dt);
                        source = dt.ToDataTable().DefaultView;
                    }
                    p.SetValue(ct, source, null);//winform
                }
            }
            //if (ct is GridView)
            //{
            //    ((GridView)ct).DataSource = source;
            //    ((GridView)ct).DataBind();
            //}
            //else if (ct is Repeater)
            //{
            //    ((Repeater)ct).DataSource = source;
            //    ((Repeater)ct).DataBind();
            //}
            //else if (ct is DataList)
            //{
            //    ((DataList)ct).DataSource = source;
            //    ((DataList)ct).DataBind();
            //}
            //else if (ct is DataGrid)
            //{
            //    ((DataGrid)ct).DataSource = source;
            //    ((DataGrid)ct).DataBind();
            //}
            //else if (ct is Win.DataGrid)
            //{
            //    ((DataGrid)ct).DataSource = source;
            //}
            //else if (ct is Win.DataGridView)
            //{
            //    ((System.Windows.Forms.DataGridView)ct).DataSource = source;
            //}
            //else if (ct is BaseDataList)//基类处理
            //{
            //    ((BaseDataList)ct).DataSource = source;
            //    ((BaseDataList)ct).DataBind();
            //}
        }
        public static void BindList(object ct, MDataTable source)
        {
            if (ct is ListControl)
            {
                BindList(ct as ListControl, source);
            }
            else if (ct is Win.ListControl)
            {
                BindList(ct as Win.ListControl, source);
            }
            else //wpf
            {
                Type t = ct.GetType();
                PropertyInfo p = t.GetProperty("ItemsSource");
                if (p != null)
                {
                    p.SetValue(ct, source, null);
                    p = t.GetProperty("SelectedValuePath");
                    if (p != null)
                    {
                        p.SetValue(ct, source.Columns[0].ColumnName, null);
                        p = t.GetProperty("DisplayMemberPath");
                        p.SetValue(ct, source.Columns[source.Columns.Count > 1 ? 1 : 0].ColumnName, null);
                    }
                }
            }
        }
        private static void BindList(Win.ListControl listControl, MDataTable source)
        {
            try
            {
                listControl.DataSource = new MDataView(ref source);
                listControl.ValueMember = source.Columns[0].ColumnName;
                listControl.DisplayMember = source.Columns[source.Columns.Count > 1 ? 1 : 0].ColumnName;
            }
            catch (Exception err)
            {
                Log.WriteLogToTxt(err);
            }
        }
        private static void BindList(ListControl listControl, MDataTable source)
        {
            listControl.DataSource = source;
            listControl.DataValueField = source.Columns[0].ColumnName;
            listControl.DataTextField = source.Columns[source.Columns.Count > 1 ? 1 : 0].ColumnName;
            listControl.DataBind();
        }
        public static string GetID(object ct)
        {
            Type t = ct.GetType();
            if (t.FullName.IndexOf('.') != t.FullName.LastIndexOf('.'))
            {
                PropertyInfo p = t.GetProperty(t.FullName.Contains(".Web.") ? "ID" : "Name");
                if (p == null)
                {
                    return string.Empty;
                }
                string propName = Convert.ToString(p.GetValue(ct, null));
                if (propName.Length > 4 && propName[0] <= 'z')//小母字母开头。
                {
                    propName = propName.Substring(3);
                }
                return propName;
            }
            return string.Empty;
        }
    }
}
