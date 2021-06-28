using System;
using System.Data;
using System.Collections.Generic;
using CYQ.Data.SQL;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Collections;
using CYQ.Data.Tool;


namespace CYQ.Data.Table
{
    /// <summary>
    /// ��Ԫ�ṹ��ֵ
    /// </summary>
    internal partial class MCellValue
    {
        internal bool IsNull = true;
        /// <summary>
        /// ״̬�ı�:0;δ�ģ��޷�����͸��£�,1;���и�ֵ����[��ֵ��ͬ](�������),2:��ֵ,ֵ��ͬ�ı��ˣ��������͸��£�
        /// </summary>
        internal int State = 0;
        /// <summary>
        /// �Ѿ�Fix()����ת�����ֵ��
        /// </summary>
        internal object Value = null;
        /// <summary>
        /// δ��������ת��֮ǰ��ֵ
        /// </summary>
        internal object SourceValue = null;
        internal string StringValue = null;
        /// <summary>
        /// ��ֵ����Ϊ��
        /// </summary>
        public void Clear()
        {
            SourceValue = null;
            Value = null;
            State = 0;
            IsNull = true;
            StringValue = null;
        }
        internal void LoadValue(MCellValue mValue, bool isWithState)
        {
            SourceValue = mValue.SourceValue;
            Value = mValue.Value;
            IsNull = mValue.IsNull;
            StringValue = mValue.StringValue;
            if (isWithState)
            {
                State = mValue.State;
            }
        }
    }
    /// <summary>
    /// ��Ԫ��
    /// </summary>
    public partial class MDataCell
    {
        private MCellValue _CellValue;
        internal MCellValue CellValue
        {
            get
            {
                if (_CellValue == null)
                {
                    _CellValue = new MCellValue();
                }
                else// if(isNewValue && !IsNull)
                {
                   // CheckNewValue();//������ʱֵ�����⡣
                }
                return _CellValue;
            }
        }

        private MCellStruct _CellStruct;

        #region ���캯��
        /// <summary>
        /// ԭ��ģʽ��Prototype Method��
        /// </summary>
        /// <param name="dataStruct"></param>
        internal MDataCell(ref MCellStruct dataStruct)
        {
            Init(dataStruct, null);
        }

        internal MDataCell(ref MCellStruct dataStruct, object value)
        {
            Init(dataStruct, value);
        }

        #endregion

        #region ��ʼ��
        private void Init(MCellStruct dataStruct, object value)
        {
            _CellStruct = dataStruct;
            if (value != null)
            {
                _CellValue = new MCellValue();
                Value = value;
            }

        }
        #endregion

        #region ����

        /// <summary>
        /// �ַ���ֵ
        /// </summary>
        public string StringValue
        {
            get
            {
                return CellValue.StringValue;
            }
        }

        private bool isNewValue = false;
        private bool isAllowChangeState = true;

        /// <summary>
        /// ֵ
        /// </summary>
        public object Value
        {
            get
            {
                //ֵ�ļ����ʱ����ȡ����ʱ����
                CheckNewValue();
                return CellValue.Value;
            }
            set
            {
                isAllowChangeState = true;
                CellValue.StringValue = Convert.ToString(value);
                CellValue.SourceValue = value;
                NullCheck();//����Null���
            }
        }
        /// <summary>
        /// �Ƿ����Jsonת��
        /// </summary>
        internal bool IsJsonIgnore
        {
            get
            {
                return _CellStruct.IsJsonIgnore;
            }
        }
        /// <summary>
        /// ��ʱ���ֵ������
        /// </summary>
        private void CheckNewValue()
        {
            if (isNewValue && !IsNull)
            {
                FixValue();
            }
        }
        private void NullCheck()
        {
            bool valueIsNull = CellValue.SourceValue == null || CellValue.SourceValue == DBNull.Value;
            if (!valueIsNull && CellValue.StringValue.Length == 0 && DataType.GetGroup(_CellStruct.SqlType) > 0)
            {
                valueIsNull = true;
            }
            if (valueIsNull)
            {
                if (CellValue.IsNull)
                {
                    CellValue.State = (CellValue.SourceValue == DBNull.Value) ? 2 : 1;
                }
                else
                {
                    if (isAllowChangeState)
                    {
                        CellValue.State = 2;
                    }
                    CellValue.Value = null;
                    CellValue.IsNull = true;
                }
                isAllowChangeState = false;
            }
            else
            {
                CellValue.IsNull = false;
                isNewValue = true;
                isAllowChangeState = true;
            }
        }
        internal void FixValue()
        {
            object value = CellValue.SourceValue;
            DataGroupType group = DataType.GetGroup(_CellStruct.SqlType);
            if (_CellStruct.SqlType != SqlDbType.Variant)
            {
                value = ChangeValue(value, _CellStruct.ValueType, group);
                if (value == null)
                {
                    return;
                }
            }
            if (isAllowChangeState)
            {
                if (CellValue.Value == null || CellValue.Value == DBNull.Value)
                {
                    CellValue.State = 2;
                }
                else if (CellValue.Value.Equals(value) || (group != DataGroupType.Object && CellValue.Value.ToString() == StringValue))//����ıȽ�ֵ����==����������õ�ַ��
                {
                    CellValue.State = 1;
                }
                else
                {
                    CellValue.State = 2;
                }
            }
            CellValue.Value = value;
            isNewValue = false;
            isAllowChangeState = false;//�ָ�������״̬��
        }

        /// <summary>
        ///  ֵ����������ת����
        /// </summary>
        /// <param name="value">Ҫ��ת����ֵ</param>
        /// <param name="convertionType">Ҫת������������</param>
        /// <param name="group">���ݿ����͵����</param>
        /// <returns></returns>
        internal object ChangeValue(object value, Type convertionType, DataGroupType group)
        {
            //ֵ��Ϊnull
            try
            {
                switch (group)
                {
                    case DataGroupType.Text:
                        if (_CellStruct.SqlType == SqlDbType.Time)//time���͵����⴦��
                        {
                            string[] items = StringValue.Split(' ');
                            if (items.Length > 1)
                            {
                                CellValue.StringValue = items[1];
                            }
                        }
                        value = StringValue;
                        break;

                    default:
                        value = ConvertTool.ChangeType(value, convertionType);
                        //if (convertionType.Name.EndsWith("[]"))
                        //{
                        //    value = Convert.FromBase64String(StringValue);
                        //    CellValue.StringValue = "System.Byte[]";
                        //}
                        //else
                        //{

                        //}
                        break;
                }
            }
            catch (Exception err)
            {
                CellValue.Value = null;
                CellValue.IsNull = true;
                CellValue.StringValue = null;
                isNewValue = false;
                string msg = string.Format("��MDataCell.ChangeValue��ChangeType Error��ColumnName��{0}��({1}) �� Value����{2}��\r\n", _CellStruct.ColumnName, _CellStruct.ValueType.FullName, StringValue);

                Log.Write(msg, LogType.Error);
                return null;
            }
            return value;
        }

        internal T Get<T>()
        {
            if (CellValue.IsNull)
            {
                return default(T);
            }
            return ConvertTool.ChangeType<T>(Value);
            //if (isNewValue)
            //{
            //    Type t = typeof(T);
            //    return (T)ChangeValue(CellValue.SourceValue, t, DataType.GetGroup(DataType.GetSqlType(t)));
            //}
            // return (T)Value;
        }

        /// <summary>
        /// ֵ�Ƿ�ΪNullֵ[ֻ������]
        /// </summary>
        public bool IsNull
        {
            get
            {
                return CellValue.IsNull;
            }
            internal set
            {
                CellValue.IsNull = value;
            }
        }
        /// <summary>
        /// ֵ�Ƿ�ΪNull��Ϊ��[ֻ������]
        /// </summary>
        public bool IsNullOrEmpty
        {
            get
            {
                return CellValue.IsNull || StringValue.Length == 0;
            }
        }
        /// <summary>
        /// ����[ֻ������]
        /// </summary>
        public string ColumnName
        {
            get
            {
                return _CellStruct.ColumnName;
            }
        }
        /// <summary>
        /// ��Ԫ��Ľṹ
        /// </summary>
        public MCellStruct Struct
        {
            get
            {
                return _CellStruct;
            }
        }

        /// <summary>
        /// Value��״̬:0;δ��,1;���и�ֵ����[��ֵ��ͬ],2:��ֵ,ֵ��ͬ�ı���
        /// </summary>
        public int State
        {
            get
            {
                if (isAllowChangeState)
                {
                    CheckNewValue();
                }
                return CellValue.State;
            }
            set
            {
                //�����ֵ����ʱ���أ���������״̬���ڻ�ȡʱ���õ�״̬��ʧЧ��
                if (isNewValue) { isAllowChangeState = false; }
                CellValue.State = value;
            }
        }
        #endregion

        #region ����
        /// <summary>
        /// ��ֵ����Ϊ��
        /// </summary>
        public void Clear()
        {
            isNewValue = false;
            isAllowChangeState = true;
            CellValue.Clear();
        }
        internal void LoadValue(MDataCell cell, bool isWithState)
        {
            isNewValue = true;
            cell.CheckNewValue();//��ԭ������ֵ״̬��
            CellValue.LoadValue(cell.CellValue, isWithState);
            if (isWithState) { isAllowChangeState = false; }
        }
        /// <summary>
        /// ����Ĭ��ֵ��
        /// </summary>
        internal void SetDefaultValueToValue()
        {
            if (Convert.ToString(_CellStruct.DefaultValue).Length > 0)
            {
                switch (DataType.GetGroup(_CellStruct.SqlType))
                {
                    case DataGroupType.Date:
                        Value = DateTime.Now;
                        break;
                    case DataGroupType.Guid:
                        if (_CellStruct.DefaultValue.ToString().Length == 36)
                        {
                            Value = new Guid(_CellStruct.DefaultValue.ToString());
                        }
                        else
                        {
                            Value = Guid.NewGuid();
                        }
                        break;
                    default:
                        Value = _CellStruct.DefaultValue;
                        break;
                }
            }
        }
        /// <summary>
        /// �ѱ����أ�Ĭ�Ϸ���Valueֵ��
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _CellStruct.SqlType == SqlDbType.Int ? Convert.ToString(Value) : (StringValue ?? "");//int �Ǵ���ö���жϡ�
        }
        /// <summary>
        /// �Ƿ�ֵ��ͬ[����д�÷���]
        /// </summary>
        public override bool Equals(object value)
        {
            bool valueIsNull = (value == null || value == DBNull.Value);
            if (CellValue.IsNull)
            {
                return valueIsNull;
            }
            if (valueIsNull)
            {
                return CellValue.IsNull;
            }
            return StringValue.ToLower() == Convert.ToString(value).ToLower();
        }
        /// <summary>
        /// ת����
        /// </summary>
        internal MDataRow ToRow()
        {
            MDataRow row = new MDataRow();
            row.Add(this);
            return row;
        }
        #endregion

    }
    //��չ��������
    public partial class MDataCell
    {
        internal string ToXml(bool isConvertNameToLower)
        {
            string text = StringValue ?? "";
            switch (DataType.GetGroup(_CellStruct.SqlType))
            {
                case DataGroupType.Object:
                    MDataRow row = null;
                    MDataTable table = null;
                    Type t = Value.GetType();
                    if (!t.FullName.StartsWith("System."))//��ͨ����
                    {
                        row = new MDataRow(TableSchema.GetColumnByType(t));
                        row.LoadFrom(Value);
                    }
                    else if (Value is IEnumerable)
                    {
                        int len = ReflectTool.GetArgumentLength(ref t);
                        if (len == 1)
                        {
                            table = MDataTable.CreateFrom(Value);
                        }
                        else if (len == 2)
                        {
                            row = MDataRow.CreateFrom(Value);
                        }
                    }
                    if (row != null)
                    {
                        text = row.ToXml(isConvertNameToLower);
                    }
                    else if (table != null)
                    {
                        text = string.Empty;
                        foreach (MDataRow r in table.Rows)
                        {
                            text += r.ToXml(isConvertNameToLower);
                        }
                        text += "\r\n    ";
                    }
                    return string.Format("\r\n    <{0}>{1}</{0}>", isConvertNameToLower ? ColumnName.ToLower() : ColumnName, text);
                default:

                    if (text.LastIndexOfAny(new char[] { '<', '>', '&' }) > -1 && !text.StartsWith("<![CDATA["))
                    {
                        text = "<![CDATA[" + text.Trim() + "]]>";
                    }
                    return string.Format("\r\n    <{0}>{1}</{0}>", isConvertNameToLower ? ColumnName.ToLower() : ColumnName, text);
            }

        }
    }
}

