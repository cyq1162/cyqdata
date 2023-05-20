using System;
using System.Web.UI.WebControls;
using System.Text;
using System.Web.UI;
using Win = System.Windows.Forms;
using CYQ.Data.Table;
using CYQ.Data.Xml;
using System.Reflection;
using System.Xml;

namespace CYQ.Data.UI
{
    internal class MBindUI
    {
        public static void Bind(object ct, object source, string nodeID)
        {
            if (ct == null)
            {
                return;
            }
            if (ct is XHtmlAction)
            {
                #region XHtmlAction ������
                XHtmlAction doc = ct as XHtmlAction;
                MDataTable dt = source as MDataTable;
                if (string.IsNullOrEmpty(nodeID))
                {
                    doc.SetForeach(dt);
                }
                else
                {
                    XmlNode node = doc.Get(nodeID);
                    if (node != null)
                    {
                        doc.SetForeach(dt, node, node.InnerXml);
                    }
                }
                #endregion
            }
            else
            {
                #region ��������б�ؼ�
                if (ct is ListControl)
                {
                    BindList(ct as ListControl, source as MDataTable);
                }
                else if (ct is Win.ListControl)
                {
                    BindList(ct as Win.ListControl, source as MDataTable);
                }
                else
                {

                    Type t = ct.GetType();
                    PropertyInfo p = t.GetProperty("DataSource");
                    if (p != null)
                    {
                        #region DataGridView����
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
                        #endregion
                    }
                    else //wpf,sliverlight
                    {
                        p = t.GetProperty("ItemsSource");
                        if (p != null)
                        {
                            MDataTable dt = null;
                            if (source is MDataTable)
                            {
                                dt = source as MDataTable;
                                source = dt.ToDataTable(true).DefaultView;
                            }
                            p.SetValue(ct, source, null);//winform
                            p = t.GetProperty("SelectedValuePath");//�ж��ǲ��������б�
                            if (p != null)
                            {
                                p.SetValue(ct, dt.Columns[0].ColumnName, null);
                                p = t.GetProperty("DisplayMemberPath");
                                p.SetValue(ct, dt.Columns[dt.Columns.Count > 1 ? 1 : 0].ColumnName, null);
                            }
                        }
                    }
                }
                #endregion
            }
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
                if (source.Columns.Count > 0)
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
        }
        private static void BindList(Win.ListControl listControl, MDataTable source)
        {
            try
            {
                if (source.Columns.Count > 0)
                {
                    listControl.DataSource = new MDataView(ref source);
                    listControl.ValueMember = source.Columns[0].ColumnName;
                    listControl.DisplayMember = source.Columns[source.Columns.Count > 1 ? 1 : 0].ColumnName;
                }
            }
            catch (Exception err)
            {
                Log.Write(err, LogType.Error);
            }
        }
        private static void BindList(ListControl listControl, MDataTable source)
        {
            if (source.Columns.Count > 0)
            {
                listControl.DataSource = source;
                listControl.DataValueField = source.Columns[0].ColumnName;
                listControl.DataTextField = source.Columns[source.Columns.Count > 1 ? 1 : 0].ColumnName;
                listControl.DataBind();
            }
        }
        public static string GetID(object ct)
        {
            Type t = ct.GetType();
            if (t.FullName.IndexOf('.') != t.FullName.LastIndexOf('.'))
            {
                PropertyInfo p = t.GetProperty(t.FullName.Contains(".Web.") ? "id" : "Name");
                if (p == null)
                {
                    return string.Empty;
                }
                string propName = Convert.ToString(p.GetValue(ct, null));
                if (propName.Length > 4 && propName[0] <= 'z')//Сĸ��ĸ��ͷ��
                {
                    propName = propName.Substring(3);
                }
                return propName;
            }
            return string.Empty;
        }
    }
}
