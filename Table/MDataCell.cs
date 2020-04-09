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
    /// 单元结构的值
    /// </summary>
    internal partial class MCellValue
    {
        internal bool IsNull = true;
        /// <summary>
        /// 状态改变:0;未改,1;进行赋值操作[但值相同],2:赋值,值不同改变了
        /// </summary>
        internal int State = 0;
        /// <summary>
        /// 已经Fix()类型转换后的值。
        /// </summary>
        internal object Value = null;
        /// <summary>
        /// 未进行类型转换之前的值
        /// </summary>
        internal object SourceValue = null;
        internal string StringValue = null;
        /// <summary>
        /// 将值重置为空
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
    /// 单元格
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
                return _CellValue;
            }
        }

        private MCellStruct _CellStruct;

        #region 构造函数
        /// <summary>
        /// 原型模式（Prototype Method）
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

        #region 初始化
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

        #region 属性

        /// <summary>
        /// 字符串值
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
        /// 值
        /// </summary>
        public object Value
        {
            get
            {
                //值的检测延时到获取属性时触发
                CheckNewValue();
                return CellValue.Value;
            }
            set
            {
                isAllowChangeState = true;
                CellValue.StringValue = Convert.ToString(value);
                CellValue.SourceValue = value;
                NullCheck();//进行Null检测
            }
        }
        /// <summary>
        /// 是否忽略Json转换
        /// </summary>
        internal bool IsJsonIgnore
        {
            get
            {
                return _CellStruct.IsJsonIgnore;
            }
        }
        /// <summary>
        /// 延时检测值的类型
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
            int groupID = DataType.GetGroup(_CellStruct.SqlType);
            if (_CellStruct.SqlType != SqlDbType.Variant)
            {
                value = ChangeValue(value, _CellStruct.ValueType, groupID);
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
                else if (CellValue.Value.Equals(value) || (groupID != 999 && CellValue.Value.ToString() == StringValue))//对象的比较值，用==号则比例引用地址。
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
            isAllowChangeState = false;//恢复可设置状态。
        }

        /// <summary>
        ///  值的数据类型转换。
        /// </summary>
        /// <param name="value">要被转换的值</param>
        /// <param name="convertionType">要转换成哪种类型</param>
        /// <param name="groupID">数据库类型的组号</param>
        /// <returns></returns>
        internal object ChangeValue(object value, Type convertionType, int groupID)
        {
            //值不为null
            try
            {
                switch (groupID)
                {
                    case 0:
                        if (_CellStruct.SqlType == SqlDbType.Time)//time类型的特殊处理。
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
                string msg = string.Format("ChangeType Error：ColumnName【{0}】({1}) ， Value：【{2}】\r\n", _CellStruct.ColumnName, _CellStruct.ValueType.FullName, StringValue);

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
        /// 值是否为Null值[只读属性]
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
        /// 值是否为Null或为空[只读属性]
        /// </summary>
        public bool IsNullOrEmpty
        {
            get
            {
                return CellValue.IsNull || StringValue.Length == 0;
            }
        }
        /// <summary>
        /// 列名[只读属性]
        /// </summary>
        public string ColumnName
        {
            get
            {
                return _CellStruct.ColumnName;
            }
        }
        /// <summary>
        /// 单元格的结构
        /// </summary>
        public MCellStruct Struct
        {
            get
            {
                return _CellStruct;
            }
        }

        /// <summary>
        /// Value的状态:0;未改,1;进行赋值操作[但值相同],2:赋值,值不同改变了
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
                //如果设值（延时加载），又设置状态（在获取时设置的状态会失效）
                if (isNewValue) { isAllowChangeState = false; }
                CellValue.State = value;
            }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 将值重置为空
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
            CellValue.LoadValue(cell.CellValue, isWithState);
            if (isWithState) { isAllowChangeState = false; }
        }
        /// <summary>
        /// 设置默认值。
        /// </summary>
        internal void SetDefaultValueToValue()
        {
            if (Convert.ToString(_CellStruct.DefaultValue).Length > 0)
            {
                switch (DataType.GetGroup(_CellStruct.SqlType))
                {
                    case 2:
                        Value = DateTime.Now;
                        break;
                    case 4:
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
        /// 已被重载，默认返回Value值。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return StringValue ?? "";
        }
        /// <summary>
        /// 是否值相同[已重写该方法]
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
        /// 转成行
        /// </summary>
        internal MDataRow ToRow()
        {
            MDataRow row = new MDataRow();
            row.Add(this);
            return row;
        }
        #endregion

    }
    //扩展交互部分
    public partial class MDataCell
    {
        internal string ToXml(bool isConvertNameToLower)
        {
            string text = StringValue ?? "";
            switch (DataType.GetGroup(_CellStruct.SqlType))
            {
                case 999:
                    MDataRow row = null;
                    MDataTable table = null;
                    Type t = Value.GetType();
                    if (!t.FullName.StartsWith("System."))//普通对象。
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

