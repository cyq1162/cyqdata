using System;
using System.Text;
using System.Web.UI.WebControls;
using System.Web.UI;
using Win = System.Windows.Forms;
using CYQ.Data.Table;
using System.Collections.Generic;
using CYQ.Data.SQL;
using System.Data;
using System.ComponentModel;
using CYQ.Data.Extension;
using System.Reflection;
using CYQ.Data.Tool;
using CYQ.Data.Xml;
using System.Xml;
namespace CYQ.Data.UI
{
    /// <summary>
    /// UI 对外接口
    /// </summary>
    public partial class MActionUI
    {
        #region UI操作

        /// <summary>
        /// 批量对所有控件自动赋值【默认前缀为（txt,ddl,chb）,可通过调用SetAutoPrefix调整】
        /// </summary>
        /// <param name="parentControl">父控件（可设置：this）</param>
        /// <param name="otherParentControls">可选（其它的父控件）</param>
        /// <example><code>
        /// action.SetAutoPrefix("txt","ddl");//设置控件的前缀
        /// action.UI.SetToAll(this);
        /// </code></example>
        public void SetToAll(object parentControl, params object[] otherParentControls)
        {
            SetAll(parentControl, otherParentControls);
        }

        /// <summary>
        /// 将值赋给控件
        /// </summary>
        /// <param name="control">控件对象</param>
        /// <example><code>
        /// 示例：action.UI.SetTo(txtUserName);//同于：txtUserName.Text=action.Get&lt;string&gt;(Users.UserName);
        /// </code></example>
        public void SetTo(object control)
        {
            Set(control, null, -1, null);
        }
        /// <param name="controlPropName">指定对某个属性赋值</param>
        public void SetTo(object control, string controlPropName)
        {
            Set(control, controlPropName, -1, null);
        }
        /// <param name="isControlEnabled">设置控件是否可用</param>
        public void SetTo(object control, string controlPropName, bool isControlEnabled)
        {
            Set(control, controlPropName, isControlEnabled ? 1 : 0, null);
        }
        /// <summary>
        /// 从控件中取值
        /// </summary>
        /// <param name="control">控件对象</param>
        /// <example><code>
        /// 示例：action.UI.GetFrom(txtUserName);//获取TextBox默认Text属性的值
        /// </code></example>
        public void GetFrom(object control)
        {
            GetFrom(control, null, null);
        }
        /// <param name="controlPropName">从指定的属性里取值</param>
        public void GetFrom(object control, string controlPropName)
        {
            GetFrom(control, controlPropName, null);
        }
        /// <param name="defaultValue">若控件无值，则取此默认值</param>
        public void GetFrom(object control, string controlPropName, object defaultValue)
        {
            GetFrom2(control, controlPropName, defaultValue);
        }
        internal string GetFrom2(object control, string controlPropName, object defaultValue)
        {
            string value = Get(control, controlPropName, defaultValue);
            if (OnAfterGetFromEvent != null)
            {
                OnAfterGetFromEvent(value);
            }
            return value;
        }

        /// <summary>
        /// 绑定DrowDownList等列表控件
        /// </summary>
        /// <param name="control">（下拉）列表控件</param>
        /// <returns></returns>
        public void Bind(object control)
        {
            Bind(control, string.Empty, MBindUI.GetID(control), _Data.Columns.FirstPrimary.ColumnName);
        }
        /// <summary>
        /// 绑定DrowDownList等列表控件
        /// </summary>
        public void Bind(object control, string where)
        {
            Bind(control, where, MBindUI.GetID(control), _Data.Columns.FirstPrimary.ColumnName);
        }
        /// <summary>
        /// 绑定下拉等列表,控件需要继承自：ListControl。
        /// </summary>
        /// <param name="control">DropDown/CheckBoxList/RadioButtonList等</param>
        /// <param name="where">对表的数据进行过滤如:"id>15 and Url='cyqdata.com'"</param>
        /// <param name="text">绑定时显示的字段名[默认字段名取自控件的id(去掉前三个字母前缀)]</param>
        /// <param name="value">绑定时显示字段对应的值[默认值的字段名为:id]</param>
        public void Bind(object control, object where, object text, object value)
        {
            string sql = _SqlCreate.GetBindSql(where, text, value);
            MDataTable mTable = null;
            switch (_DalBase.DataBaseType)
            {
                case DalType.Txt:
                case DalType.Xml:
                    NoSqlCommand cmd = new NoSqlCommand(sql, _DalBase);
                    mTable = cmd.ExeMDataTable();
                    cmd.Dispose();
                    break;
                default:
                    mTable = _DalBase.ExeDataReader(sql, false);
                    // dalHelper.ResetConn();//重置Slave
                    break;
            }

            bool result = (mTable != null && mTable.Rows.Count > 0);
            if (result)
            {
                MBindUI.BindList(control, mTable);
            }
        }



        /// <summary>
        /// 自动获取值前缀设置,可传多个前缀[至少1个]
        /// </summary>
        /// <param name="autoPrefix">第一个前缀[必须]</param>
        /// <param name="otherPrefix">后面N个前缀[可选]</param>
        public void SetAutoPrefix(string autoPrefix, params string[] otherPrefix)
        {
            autoPrefixList.Clear();
            string[] items = autoPrefix.Split(',');
            foreach (string item in items)
            {
                if (!autoPrefixList.Contains(item))
                {
                    autoPrefixList.Add(item);
                }
            }
            foreach (string item in otherPrefix)
            {
                if (!autoPrefixList.Contains(item))
                {
                    autoPrefixList.Add(item);
                }
            }
        }
        /// <summary>
        /// （Win或WPF）自动获取值父控件设置,可传多个父控件[至少1个]
        /// </summary>
        /// <param name="parent">第一个父控件名称[必须，可传：this]</param>
        /// <param name="otherParent">后面N个[可选]</param>
        public void SetAutoParentControl(object parent, params object[] otherParent)
        {
            if (autoParentList == null)
            {
                autoParentList = new List<object>();
            }
            else
            {
                autoParentList.Clear();
            }
            autoParentList.Add(parent);
            foreach (object item in otherParent)
            {
                if (!autoParentList.Contains(item))
                {
                    autoParentList.Add(item);
                }
            }
        }
        #endregion

        internal delegate void OnAfterGetFrom(string propValue);
        internal event OnAfterGetFrom OnAfterGetFromEvent;
        internal bool IsOnAfterGetFromEventNull // 外部不能判断，只能内部判断
        {
            get
            {
                return OnAfterGetFromEvent == null;
            }
        }
    }

    public partial class MActionUI : IDisposable
    {
        private List<string> autoPrefixList;//调用插入和更新,自动获取控件名的前缀（Web使用）
        private List<object> autoParentList;//调用插入和更新，自动获取控件的父控件（Win使用）
        internal MDataRow _Data;
        internal DalBase _DalBase;
        internal SqlCreate _SqlCreate;
        private MActionUI()
        {

        }
        internal MActionUI(ref MDataRow row, DalBase dalBase, SqlCreate sqlCreate)
        {
            _Data = row;
            _DalBase = dalBase;
            _SqlCreate = sqlCreate;
            autoPrefixList = new List<string>(3);
        }

        #region UI控件单个操作SET设值、GET取值
        private int GetSysValue(Type t)
        {
            return t.FullName.StartsWith("System.Web") ? 1 : (t.FullName.StartsWith("System.Windows.Forms") ? 2 : (t.FullName.StartsWith("System.Windows.Controls") ? 3 : 4));
        }
        private PropertyInfo GetProperty(Type t, bool isWpf, string keyA, string keyB)
        {
            if (isWpf)
            {
                return t.GetProperty(keyA) ?? t.GetProperty(keyB);
            }
            else
            {
                return t.GetProperty(keyB) ?? t.GetProperty(keyA);
            }
        }
        /// <summary>
        /// MDataRow值-》赋给控件。
        /// </summary>
        internal void Set(object ct, string ctPropName, int enabledState, object forceValue)
        {
            try
            {


                if (ct is IUIValue)
                {
                    #region 自定义接口值
                    IUIValue uiValue = (IUIValue)ct;
                    uiValue.MValue = forceValue != null ? forceValue : _Data[uiValue.MID.Substring(3)].Value;
                    if (enabledState > -1)
                    {
                        uiValue.MEnabled = enabledState == 1;
                    }
                    #endregion
                }
                else
                {
                    Type t = ct.GetType();
                    int sysValue = GetSysValue(t);//web,win,wpf
                    PropertyInfo p;
                    if (sysValue == 4)//第三方控件，不知道会搞id还是Name
                    {
                        p = GetProperty(t, true, "ID", "Name");
                    }
                    else
                    {
                        p = t.GetProperty(sysValue == 1 ? "ID" : "Name");
                    }
                    string propName = Convert.ToString(p.GetValue(ct, null));
                    if (propName.Length > 4 && propName[0] <= 'z')//小母字母开头。
                    {
                        propName = propName.Substring(3);
                    }
                    object value = forceValue != null ? forceValue : _Data[propName].Value;
                    #region 控件是否启用
                    if (enabledState > -1)
                    {
                        PropertyInfo pe = GetProperty(t, sysValue == 3, "IsEnabled", "Enabled");
                        if (pe != null)
                        {
                            pe.SetValue(ct, enabledState == 1, null);
                        }
                    }
                    #endregion
                    if (!string.IsNullOrEmpty(ctPropName))
                    {
                        p = t.GetProperty(ctPropName);
                        if (value != null)
                        {
                            value = StaticTool.ChangeType(value, p.PropertyType);
                            p.SetValue(ct, value, null);
                        }
                        else if (!p.PropertyType.IsValueType)
                        {
                            p.SetValue(ct, "", null);
                        }

                    }
                    else
                    {
                        string strValue = Convert.ToString(value);
                        switch (t.Name)
                        {
                            case "PasswordBox"://wpf
                                t.GetProperty("Password").SetValue(ct, strValue, null);
                                break;
                            case "TextBox"://wpf , web , win
                            case "TextBlock"://wpf
                            case "ComboBox"://wpf , win
                            case "DatePicker"://wpf
                            case "Literal"://web
                            case "Label"://web , win
                            case "RichTextBox"://win
                                t.GetProperty("Text").SetValue(ct, strValue, null);
                                break;
                            case "HiddenField"://web
                            case "HtmlTextArea"://web
                            case "HtmlInputText"://web html runat=server
                            case "HtmlInputHidden":
                                t.GetProperty("Value").SetValue(ct, strValue, null);
                                break;
                            case "ListBox"://wpf , win
                            case "RadioButtonList":
                            case "DropDownList"://web
                                t.GetProperty("SelectedValue").SetValue(ct, strValue, null);
                                break;
                            case "CheckBox"://wpf
                            case "RadioButton"://web,win,wpf
                            case "HtmlInputCheckBox":
                                p = GetProperty(t, sysValue == 3, "IsChecked", "Checked");
                                value = (t.Name == "CheckBox" || t.Name == "HtmlInputCheckBox") ? (strValue == "1" || strValue.ToLower() == "true") :
                                  strValue == Convert.ToString(GetProperty(t, sysValue == 3, "Content", "Text").GetValue(ct, null));
                                p.SetValue(ct, value, null);
                                break;
                            case "HtmlInputRadioButton":
                                //先取值，再判断值是否相等。
                                if (Convert.ToString(t.GetProperty("Value").GetValue(ct, null)) == strValue)
                                {
                                    t.GetProperty("Checked").SetValue(ct, true, null);
                                }
                                else
                                {
                                    t.GetProperty("Checked").SetValue(ct, false, null);
                                }
                                break;
                            case "Image"://web
                                t.GetProperty("ImageUrl").SetValue(ct, strValue, null);
                                break;
                            case "HtmlInputImage":
                                t.GetProperty("Src").SetValue(ct, strValue, null);
                                break;
                            case "DateTimePicker"://win
                                DateTime dt = DateTime.MinValue;
                                if (strValue == "" || DateTime.TryParse(strValue, out dt))
                                {
                                    PropertyInfo pv = t.GetProperty("Value");
                                    if (strValue == "")
                                    {
                                        PropertyInfo pi = t.GetProperty("MinDate");
                                        if (pi != null)
                                        {
                                            pv.SetValue(ct, pi.GetValue(ct, null), null);
                                            break;
                                        }
                                    }
                                    pv.SetValue(ct, dt, null);
                                }
                                break;
                            case "NumericUpDown"://win
                                decimal result = 0;
                                if (decimal.TryParse(strValue, out result))
                                {
                                    t.GetProperty("Value").SetValue(ct, result, null);
                                }
                                break;
                            default:
                                if (RegisterUI.UIList.ContainsKey(t.Name))
                                {
                                    p = t.GetProperty(RegisterUI.UIList[t.Name]);
                                }
                                else
                                {
                                    p = t.GetProperty("Text") ?? t.GetProperty("Value");
                                }
                                if (p != null)
                                {
                                    value = StaticTool.ChangeType(value, p.PropertyType);
                                    p.SetValue(ct, value, null);
                                }
                                break;

                        }
                    }
                }
            }
            catch (Exception err)
            {

                Log.Write(err, LogType.Error);
            }
        }
        /// <summary>
        /// 从控件里取值-》MDataRow （返回属性名称）
        /// </summary>
        internal string Get(object ct, string ctPropName, object defaultValue)
        {
            string propName = string.Empty;
            try
            {
                if (ct is IUIValue)
                {
                    IUIValue uiValue = (IUIValue)ct;
                    propName = uiValue.MID.Substring(3);
                    _Data[propName].Value = uiValue.MValue != null ? uiValue.MValue : defaultValue;
                }
                else
                {
                    Type t = ct.GetType();
                    int sysValue = GetSysValue(t);//web,win,wpf
                    PropertyInfo p;
                    if (sysValue == 4)//第三方控件，不知道会搞id还是Name
                    {
                        p = GetProperty(t, true, "ID", "Name");
                    }
                    else
                    {
                        p = t.GetProperty(sysValue == 1 ? "ID" : "Name");
                    }
                    propName = Convert.ToString(p.GetValue(ct, null));
                    if (propName.Length > 4 && propName[0] <= 'z')//小母字母开头。
                    {
                        propName = propName.Substring(3);
                    }
                    object value = null;
                    if (!string.IsNullOrEmpty(ctPropName))
                    {
                        value = t.GetProperty(ctPropName).GetValue(ct, null);
                    }
                    else
                    {
                        switch (t.Name)
                        {
                            case "PasswordBox"://wpf
                                value = t.GetProperty("Password").GetValue(ct, null);
                                break;
                            case "TextBox"://wpf , web , win
                            case "TextBlock"://wpf
                            case "ComboBox"://wpf , win
                            case "DatePicker"://wpf
                            case "Literal"://web
                            case "Label"://web , win
                            case "RichTextBox"://win
                                value = t.GetProperty("Text").GetValue(ct, null);
                                break;
                            case "HiddenField"://web
                            case "HtmlTextArea"://web
                            case "DateTimePicker"://win
                            case "NumericUpDown"://win
                                value = t.GetProperty("Value").GetValue(ct, null);
                                break;
                            case "ListBox"://wpf , win
                            case "DropDownList"://web
                            case "RadioButtonList":
                                value = t.GetProperty("SelectedValue").GetValue(ct, null);
                                break;
                            case "CheckBox"://wpf
                            case "RadioButton"://web,win,wpf
                                p = GetProperty(t, sysValue == 3, "IsChecked", "Checked");
                                value = p.GetValue(ct, null);
                                if (t.Name == "RadioButton")
                                {
                                    if (Convert.ToBoolean(value))
                                    {
                                        value = GetProperty(t, sysValue == 3, "Content", "Text").GetValue(ct, null);
                                    }
                                    else
                                    {
                                        return propName;
                                    }
                                }
                                break;
                            case "Image"://web
                                value = t.GetProperty("ImageUrl").GetValue(ct, null);
                                break;
                            default:
                                if (RegisterUI.UIList.ContainsKey(t.Name))
                                {
                                    p = t.GetProperty(RegisterUI.UIList[t.Name]);
                                }
                                else
                                {
                                    p = t.GetProperty("Text") ?? t.GetProperty("Value");
                                }
                                if (p != null)
                                {
                                    value = p.GetValue(ct, null);
                                }
                                break;
                        }
                    }
                    string strValue = Convert.ToString(value).Trim(' ');
                    _Data[propName].Value = defaultValue != null && strValue == "" ? defaultValue : strValue;
                }
            }
            catch (Exception err)
            {
                Log.Write(err, LogType.Error);
            }
            return propName;
        }
        #endregion



        #region 自动批量取值
        /// <summary>
        /// 自动设置列的值(true为插入,false为更新)
        /// </summary>
        internal void GetAll(bool isInsert)
        {
            if (autoPrefixList.Count == 0)
            {
                SetDefaultPrefix();
            }
            if (System.Web.HttpContext.Current != null)
            {
                GetAllOnWeb(isInsert);
            }
            else
            {
                if (autoParentList == null)
                {
                    return;
                    //Error.Throw("please call method : SetAutoParentControl(this) first.");
                }
                GetAllOnWin(isInsert);
            }

        }
        /// <summary>
        /// 对单个值自动取值。
        /// </summary>
        /// <param name="cell"></param>
        internal void AutoGetValue(MDataCell cell)
        {
            if (autoPrefixList.Count == 0)
            {
                SetDefaultPrefix();
            }
            if (System.Web.HttpContext.Current != null)
            {
                GetValueOnWeb(cell);
            }
            else
            {
                if (autoParentList == null)
                {
                    Error.Throw("AutoGetValue fail,please first call method : SetAutoParentControl(this) ");
                }
                GetValueOnWebWin(cell);
            }
        }

        private void GetAllOnWeb(bool isInsert)
        {
            MDataCell cell;

            for (int i = 0; i < _Data.Count; i++)
            {
                cell = _Data[i];
                if (cell.State > 1 || (isInsert && cell.Struct.IsAutoIncrement))//由于Fill完的状态更改为1，所以这里的判断从0变更为1
                {
                    continue;
                }
                GetValueOnWeb(cell);
            }

        }
        private void GetValueOnWeb(MDataCell cell)
        {
            try
            {
                string columnName = cell.ColumnName;
                bool isContainLine = columnName.Contains("_");
                string key = string.Empty, noLineKey = null;
                System.Web.HttpRequest rq = System.Web.HttpContext.Current.Request;
                foreach (string autoPrefix in autoPrefixList)
                {
                    key = autoPrefix + columnName;
                    if (isContainLine) { noLineKey = key.Replace("_", ""); }
                    string requestValue = rq.QueryString[key] ?? rq.Form[key];
                    if (requestValue == null && isContainLine)
                    {
                        requestValue = rq.QueryString[noLineKey] ?? rq.Form[noLineKey];
                    }
                    if (requestValue != null)
                    {
                        if (requestValue.Trim().Length == 0)//空字符串
                        {
                            #region Set Value
                            int groupID = DataType.GetGroup(cell.Struct.SqlType);
                            if (groupID > 0)
                            {
                                if (cell.Struct.DefaultValue == null)
                                {
                                    cell.Value = DBNull.Value;
                                }
                                else
                                {
                                    if (groupID == 2)
                                    {
                                        cell.Value = DateTime.Now;
                                    }
                                    else
                                    {
                                        cell.Value = cell.Struct.DefaultValue;
                                    }
                                }
                                break;
                            }
                            #endregion
                        }
                        cell.Value = requestValue.Trim(' ');
                        break;
                    }
                    else if (autoPrefix == "chb" && cell.Struct.SqlType == SqlDbType.Bit)
                    {
                        //检测是否存在相应的控件，如果存在，则设置值。
                        if (System.Web.HttpContext.Current.CurrentHandler is Page)
                        {
                            if (((Page)System.Web.HttpContext.Current.CurrentHandler).FindControl(key) != null
                                || (isContainLine && ((Page)System.Web.HttpContext.Current.CurrentHandler).FindControl(noLineKey) != null)
                                )
                            {
                                cell.Value = false;
                            }
                        }
                    }
                }
            }
            catch
            {

            }
        }


        private void GetAllOnWin(bool isInsert)
        {
            MDataCell cell;
            for (int i = 0; i < _Data.Columns.Count; i++)
            {
                cell = _Data[i];
                if (cell.State > 1 || (isInsert && cell.Struct.IsAutoIncrement))//由于Fill完的状态更改为1，所以这里的判断从0变更为1
                {
                    continue;
                }
                GetValueOnWebWin(cell);
            }

        }
        private void GetValueOnWebWin(MDataCell cell)
        {
            string columnName = cell.ColumnName, mapColumnName = null;
            bool isContainLine = cell.ColumnName.Contains("_");
            if (isContainLine)
            {
                mapColumnName = columnName.Replace("_", "");
            }
            foreach (object parentControl in autoParentList)
            {
                foreach (string fix in autoPrefixList)//遍历控件前缀，内部加Break，只取第一个控件的值。
                {
                    if (parentControl is Win.Control) // winform
                    {
                        Win.Control ctParent = parentControl as Win.Control;
                        Win.Control[] cts = ctParent.Controls.Find(fix + columnName, true);
                        if (cts.Length == 0 && isContainLine)
                        {
                            cts = ctParent.Controls.Find(fix + mapColumnName, true);
                        }
                        if (cts.Length > 0)
                        {
                            Get(cts[0], null, null);
                            break;
                        }
                    }
                    else //wpf
                    {
                        MethodInfo meth = parentControl.GetType().GetMethod("FindName");
                        if (meth != null)
                        {
                            object ct = meth.Invoke(parentControl, new object[] { fix + columnName });
                            if (ct == null && isContainLine)
                            {
                                ct = meth.Invoke(parentControl, new object[] { fix + mapColumnName });
                            }
                            if (ct != null)
                            {
                                Get(ct, null, null);
                                break;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region 自动批量赋值
        internal void SetAll(params object[] parentControls)//批量赋值时，直接把父控件的值都往这里传了。
        {
            if (parentControls != null && parentControls.Length > 0)
            {
                if (autoPrefixList.Count == 0)
                {
                    SetDefaultPrefix();
                }

                #region 由前缀引导的
                string columnName, mapColumnName = null;
                foreach (MDataCell cell in _Data)//遍历行及数据结构
                {
                    columnName = cell.ColumnName;
                    bool isContainLine = cell.ColumnName.Contains("_");
                    if (isContainLine)
                    {
                        mapColumnName = columnName.Replace("_", "");
                    }
                    foreach (object parentControl in parentControls)
                    {
                        foreach (string fix in autoPrefixList)//遍历控件前缀，内部不加Break，允许对多个控件设值。
                        {
                            if (parentControl is Control)
                            {
                                Control ctParent = parentControl as Control;
                                Control ct = ctParent.FindControl(fix + columnName);
                                if (ct == null && isContainLine)
                                {
                                    ct = ctParent.FindControl(fix + mapColumnName);
                                }
                                if (ct != null)
                                {
                                    Set(ct, null, -1, cell.Value);
                                }
                            }
                            else if (parentControl is Win.Control) // winform
                            {
                                Win.Control ctParent = parentControl as Win.Control;
                                Win.Control[] cts = ctParent.Controls.Find(fix + columnName, true);
                                if (cts.Length == 0 && isContainLine)
                                {
                                    cts = ctParent.Controls.Find(fix + mapColumnName, true);
                                }
                                foreach (Win.Control ct in cts)
                                {
                                    Set(ct, null, -1, cell.Value);
                                }
                            }
                            else if (parentControl is XHtmlAction) // Html
                            {
                                XHtmlAction doc = parentControl as XHtmlAction;
                                doc.Set(fix + columnName, cell.StringValue);
                            }
                            else // wpf
                            {
                                MethodInfo meth = parentControl.GetType().GetMethod("FindName");
                                if (meth != null)
                                {
                                    object ct = meth.Invoke(parentControl, new object[] { fix + columnName });
                                    if (ct == null && isContainLine)
                                    {
                                        ct = meth.Invoke(parentControl, new object[] { fix + mapColumnName });
                                    }
                                    if (ct != null)
                                    {
                                        Set(ct, null, -1, cell.Value);
                                    }
                                }

                            }
                        }
                    }
                }
                #endregion

                //else
                //{
                #region 直接由子控件遍历的 已注释掉
                /*

                    string columnName = string.Empty;
                    foreach (object parentControl in parentControls)
                    {
                        if (parentControl is Control)
                        {
                            Control ctParent = parentControl as Control;

                            foreach (Control ct in ctParent.Controls)
                            {
                                if (!string.IsNullOrEmpty(ct.id) && ct.ID.Length > 3)
                                {
                                    columnName = ct.ID.Substring(3);
                                    if (_Row.Columns.Contains(columnName))//包含列名。
                                    {
                                        Set(ct, _Row[columnName].Value, -1);
                                    }
                                }
                            }
                        }
                        else if (parentControl is Win.Control) // winform
                        {
                            Win.Control ctParent = parentControl as Win.Control;

                            foreach (Win.Control ct in ctParent.Controls)
                            {
                                if (!string.IsNullOrEmpty(ct.Name) && ct.Name.Length > 3)
                                {
                                    columnName = ct.Name.Substring(3);
                                    if (_Row.Columns.Contains(columnName))//包含列名。
                                    {
                                        Set(ct, _Row[columnName].Value, -1);
                                    }
                                }
                            }
                        }

                    }
                    */
                #endregion
                //}
            }
        }
        #endregion

        #region 其它方法
        /// <summary>
        /// 主键自动取值。
        /// </summary>
        internal void PrimayAutoGetValue()
        {
            foreach (MDataCell cell in _Data.JointPrimaryCell)
            {
                if (cell.IsNullOrEmpty)
                {
                    AutoGetValue(cell);
                }
            }
        }

        private void SetDefaultPrefix()
        {
            autoPrefixList.Clear();
            autoPrefixList.Add("");//无前缀（加强easyui交互）
            string[] items = AppConfig.UI.AutoPrefixs.Split(',');
            if (items != null && items.Length > 0)
            {
                foreach (string item in items)
                {
                    autoPrefixList.Add(item);
                }
            }
            //autoPrefixList.Add("txt");
            //autoPrefixList.Add("ddl");
            //autoPrefixList.Add("chb");
            if (_Data != null && !string.IsNullOrEmpty(_Data.TableName) && !_Data.TableName.Contains(" "))
            {
                autoPrefixList.Add(_Data.TableName + "_");
                autoPrefixList.Add(_Data.TableName + ".");
            }
        }

        #endregion

        #region IDisposable 成员

        public void Dispose()
        {
            autoPrefixList = null;
            autoParentList = null;
        }

        #endregion

        #region 注释掉的代码
        /*
        #region WebUI操作
        private void SetTo(Control ct, object value, int enabledState)
        {
            if (ct.ID.Length < 4)
            {
                return;
            }
            string propName = ct.ID.Substring(3);
            if (value == null)
            {
                value = _Row[propName].Value;
            }
            switch (ct.GetType().Name)
            {
                case "TextBox":
                    ((TextBox)ct).Text = Convert.ToString(value);
                    if (enabledState > -1)
                    {
                        ((TextBox)ct).Enabled = enabledState == 1;
                    }
                    break;
                case "Literal":
                    ((Literal)ct).Text = Convert.ToString(value);
                    break;
                case "Label":
                    ((Label)ct).Text = Convert.ToString(value);
                    break;
                case "HiddenField":
                    ((HiddenField)ct).Value = Convert.ToString(value);
                    break;
                case "DropDownList":
                    ((DropDownList)ct).SelectedValue = Convert.ToString(value);
                    if (enabledState > -1)
                    {
                        ((DropDownList)ct).Enabled = enabledState == 1;
                    }
                    break;
                case "CheckBox":
                    bool tempValue;
                    if (Convert.ToString(value) == "1")
                    {
                        tempValue = true;
                    }
                    else
                    {
                        bool.TryParse(Convert.ToString(value), out tempValue);
                    }
                    ((CheckBox)ct).Checked = tempValue;
                    if (enabledState > -1)
                    {
                        ((CheckBox)ct).Enabled = enabledState == 1;
                    }
                    break;
                case "RadioButtonList":
                    ((RadioButtonList)ct).SelectedValue = Convert.ToString(value);
                    if (enabledState > -1)
                    {
                        ((RadioButtonList)ct).Enabled = enabledState == 1;
                    }
                    break;
                case "Image":
                    ((Image)ct).ImageUrl = Convert.ToString(value);
                    break;
            }

        }
        private void GetFrom(Control ct, object value)
        {
            if (ct.ID.Length < 4)
            {
                return;
            }
            string propName = ct.ID.Substring(3);
            if (value == null)
            {
                switch (ct.GetType().Name)
                {
                    case "TextBox":
                        value = ((TextBox)ct).Text;
                        break;
                    case "Literal":
                        value = ((Literal)ct).Text;
                        break;
                    case "Label":
                        value = ((Label)ct).Text;
                        break;
                    case "HiddenField":
                        value = ((HiddenField)ct).Value;
                        break;
                    case "DropDownList":
                        value = ((DropDownList)ct).SelectedValue;
                        break;
                    case "CheckBox":
                        value = ((CheckBox)ct).Checked;
                        break;
                    case "RadioButtonList":
                        value = ((RadioButtonList)ct).SelectedValue;
                        break;
                    case "Image":
                        value = ((Image)ct).ImageUrl;
                        break;
                }
            }
            _Row[propName].Value = value;
        }
        #endregion

        #region WinUI操作
        private void SetTo(Win.Control ct, object value, int enabledState)
        {
            if (ct.Name.Length < 4)
            {
                return;
            }
            string propName = ct.Name.Substring(3);
            if (value == null)
            {
                value = _Row[propName].Value;
            }
            switch (ct.GetType().Name)
            {
                case "TextBox":
                    ((Win.TextBox)ct).Text = Convert.ToString(value);
                    if (enabledState > -1)
                    {
                        ((Win.TextBox)ct).Enabled = enabledState == 1;
                    }
                    break;
                case "ComboBox":

                    ((Win.ComboBox)ct).Text = Convert.ToString(value);
                    break;
                case "Label":
                    ((Win.Label)ct).Text = Convert.ToString(value);
                    break;
                case "DateTimePicker":
                    DateTime dt;
                    if (DateTime.TryParse(Convert.ToString(value), out dt))
                    {
                        ((Win.DateTimePicker)ct).Value = dt;
                    }
                    break;
                case "ListBox":
                    ((Win.ListBox)ct).Text = Convert.ToString(value);
                    break;
                case "CheckBox":
                    bool tempValue;
                    if (Convert.ToString(value) == "1")
                    {
                        tempValue = true;
                    }
                    else
                    {
                        bool.TryParse(Convert.ToString(value), out tempValue);
                    }
                    ((Win.CheckBox)ct).Checked = tempValue;
                    if (enabledState > -1)
                    {
                        ((Win.CheckBox)ct).Enabled = enabledState == 1;
                    }
                    break;
                case "NumericUpDown":
                    decimal result = 0;
                    if (decimal.TryParse(Convert.ToString(value), out result))
                    {
                        ((Win.NumericUpDown)ct).Value = result;
                    }
                    break;
                case "RichTextBox":
                    ((Win.RichTextBox)ct).Text = Convert.ToString(value);
                    break;
            }

        }
        private void GetFrom(Win.Control ct, object value)
        {
            if (ct.Name.Length < 4)
            {
                return;
            }
            string propName = ct.Name.Substring(3);
            if (value == null)
            {
                switch (ct.GetType().Name)
                {
                    case "TextBox":
                        value = ((Win.TextBox)ct).Text;
                        break;
                    case "ComboBox":
                        value = ((Win.ComboBox)ct).Text;
                        break;
                    case "Label":
                        value = ((Win.Label)ct).Text;
                        break;
                    case "DateTimePicker":
                        value = ((Win.DateTimePicker)ct).Value;
                        break;
                    case "ListBox":
                        value = ((Win.ListBox)ct).Text;
                        break;
                    case "CheckBox":
                        value = ((Win.CheckBox)ct).Checked;
                        break;
                    case "NumericUpDown":
                        value = ((Win.NumericUpDown)ct).Value;
                        break;
                    case "RichTextBox":
                        value = ((Win.RichTextBox)ct).Text;
                        break;
                }
            }
            _Row[propName].Value = value;
        }
        #endregion

        #region WPFUI操作
        private void SetTo(object ct, object value, int enabledState)
        {
            Type t = ct.GetType();
            PropertyInfo p = t.GetProperty("Name");
            string propName = string.Empty;
            if (p != null)
            {
                propName = Convert.ToString(p.GetValue(ct, null));
            }
            if (propName.Length > 4)
            {
                propName = propName.Substring(3);
            }
            if (value == null)
            {
                value = _Row[propName].Value;
            }
            switch (t.Name)
            {

                case "PasswordBox":
                    t.GetProperty("Password").SetValue(ct, value, null);
                    break;
                case "TextBox":
                case "TextBlock":
                case "ComboBox":
                case "DatePicker":
                    t.GetProperty("Text").SetValue(ct, Convert.ToString(value), null);
                    if (enabledState > -1)
                    {
                        PropertyInfo pe = t.GetProperty("IsEnabled");
                        if (pe != null)
                        {
                            pe.SetValue(ct, enabledState == 1, null);
                        }
                    }
                    break;
                case "ListBox":
                    t.GetProperty("SelectedValue").SetValue(ct, value, null);
                    break;
                case "CheckBox":
                    bool tempValue;
                    if (Convert.ToString(value) == "1")
                    {
                        tempValue = true;
                    }
                    else
                    {
                        bool.TryParse(Convert.ToString(value), out tempValue);
                    }
                    t.GetProperty("IsChecked").SetValue(tempValue, value, null);
                    if (enabledState > -1)
                    {
                        PropertyInfo pe = t.GetProperty("IsEnabled");
                        if (pe != null)
                        {
                            pe.SetValue(ct, enabledState == 1, null);
                        }
                    }
                    break;
                //case "RichTextBox":
                //    System.IO.StringReader sr = new System.IO.StringReader(Convert.ToString(value));
                //    System.Xml.XmlReader xr = System.Xml.XmlReader.Create(sr);
                //    ((RichTextBox)ct).Document = (FlowDocument)System.Windows.Markup.XamlReader.Load(xr);
                //    break;
            }

        }
        private void GetFrom(object ct, object value)
        {
            Type t = ct.GetType();
            PropertyInfo p = t.GetProperty("Name");
            string propName = string.Empty;
            if (p != null)
            {
                propName = Convert.ToString(p.GetValue(ct, null));
            }
            if (propName.Length > 4)
            {
                propName = propName.Substring(3);
            }
            if (value == null)
            {
                switch (t.Name)
                {
                    case "TextBox":
                    case "TextBlock":
                    case "ComboBox":
                    case "DatePicker":
                        value = t.GetProperty("Text").GetValue(ct, null);
                        break;
                    case "PasswordBox":
                        value = t.GetProperty("Password").GetValue(ct, null);
                        break;
                    case "ListBox":
                        value = t.GetProperty("SelectedValue").GetValue(ct, null);
                        break;
                    case "CheckBox":
                        value = t.GetProperty("IsChecked").GetValue(ct, null);
                        break;
                    //case "RichTextBox":
                    //    value = System.Windows.Markup.XamlWriter.Save(((RichTextBox)ct).Document);
                    //    break;
                }
            }
            _Row[propName].Value = value;
        }
        #endregion
        */
        #endregion
    }
}
