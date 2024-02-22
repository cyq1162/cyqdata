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
    //��չ��������
    public partial class MDataRow
    {
        #region CreateFrom

        /// <summary>
        /// ��ʵ�塢Json��Xml��IEnumerable�ӿ�ʵ�ֵ��ࡢMDataRow
        /// </summary>
        /// <returns></returns>
        public static MDataRow CreateFrom(object anyObj)
        {
            return CreateFrom(anyObj, null, BreakOp.None, JsonHelper.DefaultEscape);
        }
        public static MDataRow CreateFrom(object anyObj, Type valueType)
        {
            return CreateFrom(anyObj, valueType, BreakOp.None, JsonHelper.DefaultEscape);
        }
        public static MDataRow CreateFrom(object anyObj, Type valueType, BreakOp op)
        {
            return CreateFrom(anyObj, valueType, op, JsonHelper.DefaultEscape);
        }
        /// <summary>
        /// ��ʵ�塢Json��Xml��IEnumerable�ӿ�ʵ�ֵ��ࡢMDataRow
        /// </summary>
        public static MDataRow CreateFrom(object anyObj, Type valueType, BreakOp breakOp, EscapeOp escapeOp)
        {
            MDataRow row = new MDataRow();
            if (anyObj is MDataRow)
            {
                row.LoadFrom(anyObj as MDataRow, (breakOp == BreakOp.Null ? RowOp.IgnoreNull : RowOp.None), true);
            }
            else if (anyObj is string)
            {
                row.LoadFrom(anyObj as string, escapeOp, breakOp);
            }
            else if (anyObj is IEnumerable)
            {
                row.LoadFrom(anyObj as IEnumerable, valueType, escapeOp, breakOp);
            }
            else
            {
                row.LoadFrom(anyObj, breakOp);
            }
            row.SetState(1);//�ⲿ������״̬Ĭ����Ϊ1.
            return row;
        }
        #endregion

        #region ToJson��ToXml��ToTable��ToEntity
        
        /// <summary>
        /// ����е�����Json
        /// </summary>
        public string ToJson()
        {
            return JsonHelper.ToJson(this);
        }

        /// <summary>
        /// ���Json
        /// </summary>
        /// <param name="jsonOp">Json ѡ��</param>
        /// <returns></returns>
        public string ToJson(JsonOp jsonOp)
        {
            return JsonHelper.ToJson(this, jsonOp);
        }

        internal string ToXml(bool isConvertNameToLower)
        {
            string xml = string.Empty;
            foreach (MDataCell cell in this)
            {
                xml += cell.ToXml(isConvertNameToLower);
            }
            return xml;
        }
        /// <summary>
        /// ���е�����ת�����У�ColumnName��Value���ı�
        /// </summary>
        public MDataTable ToTable()
        {
            MDataTable dt = this.Columns.ToTable();
            MCellStruct msValue = new MCellStruct("Value", SqlDbType.Variant);
            MCellStruct msState = new MCellStruct("State", SqlDbType.Int);
            dt.Columns.Insert(1, msValue);
            dt.Columns.Insert(2, msState);
            for (int i = 0; i < Count; i++)
            {
                dt.Rows[i][1].Value = this[i].Value;
                dt.Rows[i][2].Value = this[i].State;
            }
            return dt;
        }
        /// <summary>
        /// ���е�����ת�����У�ColumnName��Value��State���ı�
        /// </summary>
        /// <param name="onlyData">�����ݣ�������ͷ�ṹ��</param>
        /// <returns></returns>
        public MDataTable ToTable(bool onlyData)
        {
            if (onlyData)
            {
                MDataTable dt = new MDataTable(this.TableName);
                dt.Columns.Add("ColumnName", SqlDbType.NVarChar);
                dt.Columns.Add("Value", SqlDbType.Variant);
                dt.Columns.Add("State", SqlDbType.Int);
                for (int i = 0; i < Count; i++)
                {
                    MDataCell cell = this[i];
                    dt.NewRow(true)
                        .Set(0, cell.ColumnName)
                        .Set(1, cell.Value)
                        .Set(2, cell.State);
                }
                return dt;
            }
            return ToTable();
        }

        /// <summary>
        /// תʵ����
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <returns></returns>
        public T ToEntity<T>()
        {
            Type t = typeof(T);
            return (T)ToEntity(t);
        }

        internal object ToEntity(Type t)
        {
            if (t.Name == "MDataRow")
            {
                return this;
            }
            if (t.IsValueType || t.Name == "String")
            {
                return ConvertTool.ChangeType(this[0].Value, t);
            }
            bool isCustom = ReflectTool.GetSystemType(ref t) == SysType.Custom;

            if (isCustom)
            {
                return MDataRowToEntity.Delegate(t, this.Columns)(this);
            }
            else
            {
                return ConvertTool.GetObj(t, this);
            }
        }

        #endregion

        #region SetToUI


        /// <summary>
        /// ��ֵ��������UI
        /// </summary>
        public void SetToAll(params object[] parentControls)
        {
            SetToAll(null, parentControls);
        }
        /// <summary>
        /// ��ֵ��������UI
        /// </summary>
        /// <param name="autoPrefix">�Զ�ǰ׺��������ö��ŷָ�</param>
        /// <param name="parentControls">ҳ��ؼ�</param>
        public void SetToAll(string autoPrefix, params object[] parentControls)
        {
            if (Count > 0)
            {
                MDataRow row = this;
                using (MActionUI mui = new MActionUI(ref row, null, null))
                {
                    if (!string.IsNullOrEmpty(autoPrefix))
                    {
                        string[] pres = autoPrefix.Split(',');
                        mui.SetAutoPrefix(pres[0], pres);
                    }
                    mui.SetAll(parentControls);
                }
            }
        }

        #endregion

        #region LoadFrom

        /// <summary>
        /// ��Web Post����ȡֵ��
        /// </summary>
        public void LoadFrom()
        {
            LoadFrom(true);
        }
        /// <summary>
        /// ��Web Post����ȡֵ �� ��Winform��WPF�ı��ؼ���ȡֵ��
        /// <param name="isWeb">TrueΪWebӦ�ã���֮ΪWinӦ��</param>
        /// <param name="prefixOrParentControl">Webʱ����ǰ׺��Winʱ���ø������ؼ�</param>
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

        /// <summary>
        /// �ӱ���м���ֵ
        /// </summary>
        public void LoadFrom(MDataRow row)
        {
            LoadFrom(row, RowOp.None, Count == 0);
        }
        /// <summary>
        /// �ӱ���м���ֵ
        /// </summary>
        public void LoadFrom(MDataRow row, RowOp rowOp, bool isAllowAppendColumn)
        {
            LoadFrom(row, rowOp, isAllowAppendColumn, true);
        }
        /// <summary>
        /// �ӱ���м���ֵ
        /// </summary>
        /// <param name="row">Ҫ�������ݵ���</param>
        /// <param name="rowOp">��ѡ��[�������м��ص�����]</param>
        /// <param name="isAllowAppendColumn">���row�������У��Ƿ����</param>
        /// <param name="isWithValueState">�Ƿ�ͬʱ����ֵ��״̬[Ĭ��ֵΪ��true]</param>
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
                        //cell.Value = rowCell.Value;//�����ڸ�ֵ����Ϊ��ͬ�ļܹ����������������� int[access] int64[sqlite]����ת��ʱ�����
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
        /// <summary>
        /// �����������ֵ
        /// </summary>
        /// <param name="values"></param>
        public void LoadFrom(object[] values)
        {
            if (values != null && values.Length <= Count)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this[i].Value = values[i];
                }
            }
        }
        public void LoadFrom(string json)
        {
            LoadFrom(json, JsonHelper.DefaultEscape);
        }
        /// <summary>
        /// ��json�����ֵ
        /// </summary>
        public void LoadFrom(string json, EscapeOp op)
        {
            LoadFrom(json, op, BreakOp.None);
        }
        /// <summary>
        /// ��json�����ֵ
        /// </summary>
        public void LoadFrom(string json, EscapeOp op, BreakOp breakOp)
        {
            if (!string.IsNullOrEmpty(json))
            {
                Dictionary<string, string> dic = JsonHelper.Split(json);
                if (dic != null && dic.Count > 0)
                {
                    LoadFrom(dic, null, op, breakOp);
                }
            }
            else
            {
                LoadFrom(true);
            }
        }
        /// <summary>
        /// �ӷ����ֵ伯�������
        /// </summary>
        public void LoadFrom(IEnumerable dic)
        {
            LoadFrom(dic, null, JsonHelper.DefaultEscape, BreakOp.None);
        }
        internal void LoadFrom(IEnumerable dic, Type valueType, EscapeOp op, BreakOp breakOp)
        {
            if (dic != null)
            {
                if (dic is MDataRow)
                {
                    LoadFrom(dic as MDataRow, RowOp.None, true);
                    return;
                }
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
            }
        }
        //internal void LoadFrom2(IEnumerable dic, Type valueType, EscapeOp op, BreakOp breakOp)
        //{
        //    if (dic != null)
        //    {
        //        if (dic is MDataRow)
        //        {
        //            LoadFrom(dic as MDataRow, RowOp.None, true);
        //            return;
        //        }
        //        bool isNameValue = dic is NameValueCollection;
        //        bool isAddColumn = Columns.Count == 0;
        //        SqlDbType sdt = SqlDbType.NVarChar;
        //        if (isAddColumn)
        //        {
        //            if (valueType != null)
        //            {
        //                sdt = DataType.GetSqlType(valueType);
        //            }
        //            else if (!isNameValue)
        //            {
        //                Type type = dic.GetType();
        //                if (type.IsGenericType)
        //                {
        //                    sdt = DataType.GetSqlType(type.GetGenericArguments()[1]);
        //                }
        //                else
        //                {
        //                    sdt = SqlDbType.Variant;
        //                }
        //            }
        //        }
        //        string key = null; object value = null;
        //        Type t = null;
        //        PropertyInfo keyPro = null;
        //        PropertyInfo valuePro = null;
        //        int i = -1;
        //        foreach (object o in dic)
        //        {
        //            i++;
        //            if (isNameValue)
        //            {
        //                if (o == null)
        //                {
        //                    key = "null";
        //                    value = ((NameValueCollection)dic)[i];
        //                }
        //                else
        //                {
        //                    key = Convert.ToString(o);
        //                    value = ((NameValueCollection)dic)[key];
        //                }
        //            }
        //            else
        //            {
        //                if (t == null)
        //                {
        //                    t = o.GetType();
        //                    keyPro = t.GetProperty("Key");
        //                    valuePro = t.GetProperty("Value");
        //                }
        //                value = valuePro.GetValue(o, null);
        //                bool isContinue = false;
        //                switch (breakOp)
        //                {
        //                    case BreakOp.Null:
        //                        if (value == null)
        //                        {
        //                            isContinue = true;
        //                        }
        //                        break;
        //                    case BreakOp.Empty:
        //                        if (value != null && Convert.ToString(value) == "")
        //                        {
        //                            isContinue = true;
        //                        }
        //                        break;
        //                    case BreakOp.NullOrEmpty:
        //                        if (Convert.ToString(value) == "")
        //                        {
        //                            isContinue = true;
        //                        }
        //                        break;

        //                }
        //                if (isContinue) { continue; }
        //                key = Convert.ToString(keyPro.GetValue(o, null));
        //                //if (value != null)
        //                //{

        //                //}
        //            }
        //            //if (value != null)
        //            //{
        //            if (isAddColumn)
        //            {
        //                SqlDbType sdType = sdt;
        //                if (sdt == SqlDbType.Variant)
        //                {
        //                    if (value == null)
        //                    {
        //                        sdType = SqlDbType.NVarChar;
        //                    }
        //                    else
        //                    {
        //                        sdType = DataType.GetSqlType(value.GetType());
        //                    }
        //                }
        //                Add(key, sdType, value);
        //            }
        //            else
        //            {
        //                if (value != null && value is string)
        //                {
        //                    value = JsonHelper.UnEscape(value.ToString(), op);
        //                }
        //                Set(key, value);
        //            }
        //            // }
        //        }
        //    }
        //}
        /// <summary>
        /// ��ʵ��ת�������С�
        /// </summary>
        /// <param name="entity">ʵ�����</param>
        public void LoadFrom(object entity)
        {
            if (entity == null || entity is Boolean)
            {
                LoadFrom(true);
            }
            else if (entity is String)
            {
                LoadFrom(entity as String);
            }
            else if (entity is MDataRow)
            {
                LoadFrom(entity as MDataRow);
            }
            else if (entity is IEnumerable)
            {
                LoadFrom(entity as IEnumerable);
            }
            else
            {
                LoadFrom(entity, BreakOp.None);
            }
        }
        /// <summary>
        /// ��ʵ��ת�������С�
        /// </summary>
        /// <param name="entity">ʵ�����</param>
        public void LoadFrom(object entity, BreakOp op)
        {
            LoadFrom(entity, op, 2);
        }
        /// <summary>
        /// ��ʵ��ת�������С�
        /// </summary>
        /// <param name="entity">ʵ�����</param>
        /// <param name="op">�������õ�ѡ��</param>
        /// <param name="initState">��ʼֵ��0�޷�����͸��£�1�ɲ��룻2�ɲ���ɸ���(Ĭ��ֵ)</param>
        public void LoadFrom(object entity, BreakOp op, int initState)
        {
            if (entity == null)
            {
                return;
            }
            try
            {
                Type t = entity.GetType();
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
                loadEntityAction(this, entity, op, initState);

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
            catch (Exception err)
            {
                Log.Write(err, LogType.Error);
            }
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
        //        Set(index, objValue, initState);//�ⲿ״̬1���ڲ�Ĭ����2��
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
        ///// ��ָ���е����ݸ���ʵ�����
        ///// </summary>
        ///// <param name="obj"></param>
        ///// <param name="row"></param>
        //internal void SetToEntity(ref object obj, MDataRow row)
        //{
        //    SetToEntity(ref obj, row, RowOp.IgnoreNull);
        //}
        #endregion

        #region SetToEntity

        /// <summary>
        /// �����е�����ֵ����ʵ�����
        /// </summary>
        /// <param name="obj">ʵ�����</param>
        public void SetToEntity(object obj)
        {
            if (obj == null || this.Count == 0)
            {
                return;
            }
            Type objType = obj.GetType();
            var setToEntity = MDataRowSetToEntity.Delegate(objType, this.Columns);
            setToEntity(this, obj);

            //string objName = objType.FullName, cellName = "";
            //try
            //{
            //    #region �������
            //    List<PropertyInfo> pis = ReflectTool.GetPropertyList(objType);
            //    if (pis.Count > 0)
            //    {
            //        foreach (PropertyInfo p in pis)//����ʵ��
            //        {
            //            if (p.CanWrite)
            //            {
            //                SetValueToPropertyOrField(ref obj, row, p, null, op, out cellName);
            //            }
            //        }
            //    }
            //    else
            //    {
            //        List<FieldInfo> fis = ReflectTool.GetFieldList(objType);
            //        if (fis.Count > 0)
            //        {
            //            foreach (FieldInfo f in fis)//����ʵ��
            //            {
            //                SetValueToPropertyOrField(ref obj, row, null, f, op, out cellName);
            //            }
            //        }
            //    }
            //    #endregion
            //}
            //catch (Exception err)
            //{
            //    string msg = "[AttachInfo]:" + string.Format("ObjName:{0} PropertyName:{1}", objName, cellName) + "\r\n";
            //    msg += Log.GetExceptionMessage(err);
            //    Log.WriteLogToTxt(msg, LogType.Error);
            //}
        }

        //private void SetValueToPropertyOrField(ref object obj, MDataRow row, PropertyInfo p, FieldInfo f, RowOp op, out string cellName)
        //{
        //    cellName = p != null ? p.Name : f.Name;
        //    MDataCell cell = row[cellName];
        //    if (cell == null)
        //    {
        //        return;
        //    }
        //    if (op == RowOp.IgnoreNull && cell.IsNull)
        //    {
        //        return;
        //    }
        //    else if (op == RowOp.Insert && cell.State == 0)
        //    {
        //        return;
        //    }
        //    else if (op == RowOp.Update && cell.State != 2)
        //    {
        //        return;
        //    }
        //    Type propType = p != null ? p.PropertyType : f.FieldType;
        //    object objValue = GetObj(propType, cell.Value);

        //    if (p != null)
        //    {
        //        p.SetValue(obj, objValue, null);
        //    }
        //    else
        //    {
        //        f.SetValue(obj, objValue);
        //    }
        //}
        #endregion

    }

}
