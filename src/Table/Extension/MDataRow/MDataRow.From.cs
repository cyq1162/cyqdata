using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Collections;
using System.ComponentModel;
using CYQ.Data.SQL;
using CYQ.Data.Tool;
using System.Reflection;
using System.Collections.Specialized;
using CYQ.Data.UI;
using CYQ.Data.Json;
using CYQ.Data.Emit;

namespace CYQ.Data.Table
{
    //扩展交互部分
    public partial class MDataRow
    {
        #region CreateFrom

        /// <summary>
        /// 从实体、Json、Xml、IEnumerable接口实现的类、MDataRow
        /// </summary>
        /// <returns></returns>
        public static MDataRow CreateFrom(object anyObj)
        {
            return CreateFrom(anyObj, null, BreakOp.None, EscapeOp.No);
        }
        public static MDataRow CreateFrom(object anyObj, Type valueType)
        {
            return CreateFrom(anyObj, valueType, BreakOp.None, EscapeOp.No);
        }
        public static MDataRow CreateFrom(object anyObj, Type valueType, BreakOp op)
        {
            return CreateFrom(anyObj, valueType, op, EscapeOp.No);
        }
        /// <summary>
        /// 从实体、Json、Xml、IEnumerable接口实现的类、MDataRow
        /// </summary>
        public static MDataRow CreateFrom(object anyObj, Type valueType, BreakOp breakOp, EscapeOp escapeOp)
        {
            if (anyObj is MDataRow)
            {
                return anyObj as MDataRow;
            }
            MDataRow row = new MDataRow();
            if (anyObj is string)
            {
                row.LoadFrom(anyObj as string, escapeOp, breakOp);
            }
            else if (anyObj is IEnumerable)
            {
                row.LoadIEnumerable(anyObj as IEnumerable, valueType, escapeOp, breakOp);
            }
            else
            {
                row.LoadFrom(anyObj, breakOp);
            }
            row.SetState(1);//外部创建的状态默认置为1.
            return row;
        }
        #endregion

        #region LoadFrom

        #region 从 UI 中获取

        /// <summary>
        /// 从Web Post表单里取值 或 从Winform、WPF的表单控件里取值。
        /// <param name="isWeb">True为Web应用，反之为Win应用</param>
        /// <param name="prefixOrParentControl">Web时设置前缀，Win时设置父容器控件</param>
        /// </summary>
        public void LoadFrom(bool isWeb, params object[] prefixOrParentControl)
        {
            if (Count > 0)
            {
                MDataRow row = this;
                using (MActionUI mui = new MActionUI(ref row, null, null))
                {

                    if (prefixOrParentControl.Length > 0)
                    {
                        if (isWeb)
                        {
                            string[] items = prefixOrParentControl as string[];
                            mui.SetAutoPrefix(items[0], items);
                        }
                        else
                        {
                            mui.SetAutoParentControl(prefixOrParentControl[0], prefixOrParentControl);
                        }
                    }

                    mui.GetAll(false);
                }
            }
        }

        #endregion

        #region 从数据行中获取

        /// <summary>
        /// 从别的行加载值
        /// </summary>
        public void LoadFrom(MDataRow row)
        {
            LoadFrom(row, RowOp.None, Count == 0);
        }
        /// <summary>
        /// 从别的行加载值
        /// </summary>
        public void LoadFrom(MDataRow row, RowOp rowOp, bool isAllowAppendColumn)
        {
            LoadFrom(row, rowOp, isAllowAppendColumn, true);
        }
        /// <summary>
        /// 从别的行加载值
        /// </summary>
        /// <param name="row">要加载数据的行</param>
        /// <param name="rowOp">行选项[从其它行加载的条件]</param>
        /// <param name="isAllowAppendColumn">如果row带有新列，是否加列</param>
        /// <param name="isWithValueState">是否同时加载值的状态[默认值为：true]</param>
        public void LoadFrom(MDataRow row, RowOp rowOp, bool isAllowAppendColumn, bool isWithValueState)
        {
            if (row != null)
            {
                if (isAllowAppendColumn)
                {
                    for (int i = 0; i < row.Count; i++)
                    {
                        //if (rowOp == RowOp.IgnoreNull && row[i].IsNull)
                        //{
                        //    continue;
                        //}
                        if (!Columns.Contains(row[i].ColumnName))
                        {
                            Columns.Add(row[i].Struct);
                        }
                    }
                }
                MDataCell rowCell;
                foreach (MDataCell cell in this)
                {
                    rowCell = row[cell.ColumnName];
                    if (rowCell == null || (rowOp == RowOp.IgnoreNull && rowCell.IsNull))
                    {
                        continue;
                    }
                    if (rowOp == RowOp.None || rowCell.State >= (int)rowOp)
                    {
                        cell.LoadValue(rowCell, isWithValueState);
                        //if (cell.Struct.SqlType == rowCell.Struct.SqlType)
                        //{
                        //    cell.CellValue.StringValue = rowCell.StringValue;
                        //    cell.CellValue.Value = rowCell.CellValue.Value;
                        //    cell.CellValue.IsNull = rowCell.CellValue.IsNull;
                        //}
                        //else
                        //{
                        //cell.Value = rowCell.Value;//用属于赋值，因为不同的架构，类型若有区别如 int[access] int64[sqlite]，在转换时会出错
                        //cell._CellValue.IsNull = rowCell._CellValue.IsNull;//
                        //}

                        //if (isWithValueState)
                        //{
                        //    cell.State = rowCell.State;
                        //}
                    }
                }

            }
        }

        #endregion

        #region 从数组中加载【内部方法】

        /// <summary>
        /// 从数组里加载值
        /// </summary>
        /// <param name="values"></param>
        internal void LoadFrom(object[] values)
        {
            if (values != null && values.Length <= Count)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this[i].Value = values[i];
                }
            }
        }
        #endregion

        #region 从 Json 中加载数据

        public void LoadFrom(string json)
        {
            LoadFrom(json, JsonHelper.DefaultEscape);
        }
        /// <summary>
        /// 从json里加载值
        /// </summary>
        public void LoadFrom(string json, EscapeOp op)
        {
            LoadFrom(json, op, BreakOp.None);
        }
        /// <summary>
        /// 从json里加载值
        /// </summary>
        public void LoadFrom(string json, EscapeOp op, BreakOp breakOp)
        {
            if (!string.IsNullOrEmpty(json))
            {
                Dictionary<string, string> dic = JsonHelper.Split(json, op);
                LoadDictionary(dic, breakOp);
            }
        }
        #endregion

        #region 从任意对象中加载数据
        /// <summary>
        /// 将实体转成数据行。
        /// </summary>
        /// <param name="anyObj">实体对象</param>
        public void LoadFrom(object anyObj)
        {
            LoadFrom(anyObj, BreakOp.None, 2);
        }
        /// <summary>
        /// 将实体转成数据行。
        /// </summary>
        /// <param name="anyObj">实体对象</param>
        public void LoadFrom(object anyObj, BreakOp op)
        {
            LoadFrom(anyObj, op, 2);
        }
        /// <summary>
        /// 将实体转成数据行。
        /// </summary>
        /// <param name="anyObj">实体对象</param>
        /// <param name="op">跳过设置的选项</param>
        /// <param name="initState">初始值：0无法插入和更新；1可插入；2可插入可更新(默认值)</param>
        public void LoadFrom(object anyObj, BreakOp op, int initState)
        {
            if (anyObj == null) { return; }

            if (anyObj is Boolean)
            {
                LoadFrom((bool)anyObj);
            }
            else if (anyObj is String)
            {
                LoadFrom(anyObj as String);
            }
            else if (anyObj is MDataRow)
            {
                LoadFrom(anyObj as MDataRow);
            }
            else if (anyObj is IEnumerable)
            {
                LoadIEnumerable(anyObj as IEnumerable, null, EscapeOp.No, op);
            }
            else
            {

                if (anyObj is KeyValuePair<string, string>)
                {
                    var kv = (KeyValuePair<string, string>)anyObj;
                    Set("Key", kv.Key).Set("Value", kv.Value);
                    return;
                }
                if (anyObj is KeyValuePair<string, object>)
                {
                    var kv = (KeyValuePair<string, object>)anyObj;
                    Set("Key", kv.Key).Set("Value", kv.Value);
                    return;
                }

                LoadEntity(anyObj, op, initState);
            }
        }
        #endregion

        private void LoadDictionary(Dictionary<string, object> dic, BreakOp breakOp)
        {
            if (dic != null && dic.Count > 0)
            {
                bool isAddColumn = Columns.Count == 0;
                foreach (var item in dic)
                {
                    switch (breakOp)
                    {
                        case BreakOp.Null:
                            if (item.Value == null) { continue; }
                            break;
                        case BreakOp.Empty:
                            if (Convert.ToString(item.Value) == string.Empty) { continue; }
                            break;
                        case BreakOp.NullOrEmpty:
                            if (item.Value == null || Convert.ToString(item.Value) == string.Empty) { continue; }
                            break;
                    }
                    if (isAddColumn)
                    {
                        Add(item.Key, SqlDbType.Variant, item.Value);
                    }
                    else
                    {
                        Set(item.Key, item.Value);
                    }
                }
            }
        }
        /// <summary>
        /// 从字典里加载值
        /// </summary>
        private void LoadDictionary(Dictionary<string, string> dic, BreakOp breakOp)
        {
            if (dic != null && dic.Count > 0)
            {
                bool isAddColumn = Columns.Count == 0;
                foreach (var item in dic)
                {
                    switch (breakOp)
                    {
                        case BreakOp.Null:
                            if (item.Value == null) { continue; }
                            break;
                        case BreakOp.Empty:
                            if (item.Value == string.Empty) { continue; }
                            break;
                        case BreakOp.NullOrEmpty:
                            if (string.IsNullOrEmpty(item.Value)) { continue; }
                            break;
                    }
                    if (isAddColumn)
                    {
                        Add(item.Key, item.Value);
                    }
                    else
                    {
                        Set(item.Key, item.Value);
                    }
                }
            }
        }

        /// <summary>
        /// 从泛型字典集合里加载
        /// </summary>
        internal void LoadIEnumerable(IEnumerable dic, Type valueType, EscapeOp op, BreakOp breakOp)
        {

            if (dic != null)
            {
                if (dic is Dictionary<string, string> || dic is MDictionary<string, string>)
                {
                    LoadDictionary(dic as Dictionary<string, string>, breakOp);
                    return;
                }
                if (dic is Dictionary<string, object> || dic is MDictionary<string, object>)
                {
                    LoadDictionary(dic as Dictionary<string, object>, breakOp);
                    return;
                }
                #region 需要创建列时，如果是HashTable时，值的类型如果是Object，尝试先取值的类型。

                bool isNameValue = dic is NameValueCollection;
                bool isAddColumn = Columns.Count == 0;
                SqlDbType sdt = SqlDbType.NVarChar;
                if (isAddColumn)
                {
                    if (valueType != null)
                    {
                        sdt = DataType.GetSqlType(valueType);
                    }
                    else if (!isNameValue)
                    {
                        Type type = dic.GetType();
                        if (type.IsGenericType)
                        {
                            sdt = DataType.GetSqlType(type.GetGenericArguments()[1]);
                        }
                        else
                        {
                            sdt = SqlDbType.Variant;
                        }
                    }
                }
                string key = null; object value = null;
                Type t = null;
                PropertyInfo keyPro = null;
                PropertyInfo valuePro = null;
                int i = -1;
                foreach (object o in dic)
                {
                    i++;
                    if (isNameValue)
                    {
                        if (o == null)
                        {
                            key = "null";
                            value = ((NameValueCollection)dic)[i];
                        }
                        else
                        {
                            key = Convert.ToString(o);
                            value = ((NameValueCollection)dic)[key];
                        }
                    }
                    else
                    {
                        if (t == null)
                        {
                            t = o.GetType();
                            keyPro = t.GetProperty("Key");
                            valuePro = t.GetProperty("Value");
                        }
                        value = valuePro.GetValue(o, null);
                        bool isContinue = false;
                        switch (breakOp)
                        {
                            case BreakOp.Null:
                                if (value == null)
                                {
                                    isContinue = true;
                                }
                                break;
                            case BreakOp.Empty:
                                if (value != null && Convert.ToString(value) == "")
                                {
                                    isContinue = true;
                                }
                                break;
                            case BreakOp.NullOrEmpty:
                                if (Convert.ToString(value) == "")
                                {
                                    isContinue = true;
                                }
                                break;

                        }
                        if (isContinue) { continue; }
                        key = Convert.ToString(keyPro.GetValue(o, null));
                        //if (value != null)
                        //{

                        //}
                    }
                    //if (value != null)
                    //{
                    if (isAddColumn)
                    {
                        SqlDbType sdType = sdt;
                        if (sdt == SqlDbType.Variant)
                        {
                            if (value == null)
                            {
                                sdType = SqlDbType.NVarChar;
                            }
                            else
                            {
                                sdType = DataType.GetSqlType(value.GetType());
                            }
                        }
                        Add(key, sdType, value);
                    }
                    else
                    {
                        if (value != null && value is string)
                        {
                            value = JsonHelper.UnEscape(value.ToString(), op);
                        }
                        Set(key, value);
                    }
                    // }
                }

                #endregion
            }
        }





        private void LoadEntity(object anyObj, BreakOp op, int initState)
        {
            if (anyObj == null)
            {
                return;
            }
            try
            {
                Type t = anyObj.GetType();
                if (Columns.Count == 0)
                {
                    MDataColumn mcs = TableSchema.GetColumnByType(t);
                    MCellStruct ms = null;
                    for (int i = 0; i < mcs.Count; i++)
                    {
                        ms = mcs[i];
                        MDataCell cell = new MDataCell(ref ms);
                        Add(cell);
                    }
                }

                if (string.IsNullOrEmpty(TableName))
                {
                    TableName = t.Name;
                }

                var loadEntityAction = MDataRowLoadEntity.Delegate(t);
                loadEntityAction(this, anyObj, op, initState);
            }
            catch (Exception err)
            {
                Log.Write(err, LogType.Error);
            }
            //List<PropertyInfo> pis = ReflectTool.GetPropertyList(t);
            //if (pis.Count > 0)
            //{
            //    foreach (PropertyInfo p in pis)
            //    {
            //        SetValueToCell(entity, op, p, null, initState);
            //    }
            //}

            //List<FieldInfo> fis = ReflectTool.GetFieldList(t);
            //if (fis.Count > 0)
            //{
            //    foreach (FieldInfo f in fis)
            //    {
            //        SetValueToCell(entity, op, null, f, initState);
            //    }
            //}
        }

        //private void SetValueToCell(object entity, BreakOp op, PropertyInfo p, FieldInfo f, int initState)
        //{
        //    string name = p != null ? p.Name : f.Name;
        //    int index = Columns.GetIndex(name);
        //    if (index > -1)
        //    {

        //        object objValue = p != null ? p.GetValue(entity, null) : f.GetValue(entity);

        //        Type type = p != null ? p.PropertyType : f.FieldType;
        //        if (type.IsEnum)
        //        {
        //            if (ReflectTool.GetAttr<JsonEnumToStringAttribute>(p, f) != null)
        //            {
        //                objValue = objValue.ToString();
        //            }
        //            //else if (ReflectTool.GetAttr<JsonEnumToDescriptionAttribute>(p, f) != null)
        //            //{
        //            //    FieldInfo field = type.GetField(objValue.ToString());
        //            //    if (field != null)
        //            //    {
        //            //        DescriptionAttribute da = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
        //            //        if (da != null)
        //            //        {
        //            //            objValue = da.Description;
        //            //        }
        //            //    }
        //            //}

        //        }
        //        switch (op)
        //        {
        //            case BreakOp.Null:
        //                if (objValue == null)
        //                {
        //                    return;
        //                }
        //                break;
        //            case BreakOp.Empty:
        //                if (objValue != null && Convert.ToString(objValue) == "")
        //                {
        //                    return;
        //                }
        //                break;
        //            case BreakOp.NullOrEmpty:
        //                if (objValue == null || Convert.ToString(objValue) == "")
        //                {
        //                    return;
        //                }
        //                break;
        //        }
        //        Set(index, objValue, initState);//外部状态1，内部默认是2。
        //    }
        //}

        //public void SetToEntity(object obj)
        //{
        //    SetToEntity(ref obj, this, RowOp.IgnoreNull);
        //}
        //public void SetToEntity(object obj, RowOp op)
        //{
        //    SetToEntity(ref obj, this, op);
        //}
        ///// <summary>
        ///// 将指定行的数据赋给实体对象。
        ///// </summary>
        ///// <param name="obj"></param>
        ///// <param name="row"></param>
        //internal void SetToEntity(ref object obj, MDataRow row)
        //{
        //    SetToEntity(ref obj, row, RowOp.IgnoreNull);
        //}
        #endregion


    }

}
