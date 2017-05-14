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
    /// 单元格
    /// </summary>
    public partial class MDataCell
    {
        /// <summary>
        /// 单元结构的值
        /// </summary>
        private class MCellValue
        {
            internal bool IsNull = true;
            /// <summary>
            /// 状态改变:0;未改,1;进行赋值操作[但值相同],2:赋值,值不同改变了
            /// </summary>
            internal int State = 0;
            internal object Value = null;
        }

        private MCellValue _CellValue;
        private MCellValue CellValue
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
        private string _StringValue = null;
        /// <summary>
        /// 字符串值
        /// </summary>
        public string StringValue
        {
            get
            {
                CheckNewValue();
                return _StringValue;
            }
            internal set
            {
                _StringValue = value;
            }
        }
        private object newValue = null;
        private bool isNewValue = false;
        /// <summary>
        /// 值
        /// </summary>
        public object Value
        {
            get
            {
                CheckNewValue();
                return CellValue.Value;
            }
            set
            {
                //只是赋值，值的检测延时到获取属性时触发
                newValue = value;
                isNewValue = true;
                isAllowChangeState = true;
            }
        }
        internal object SourceValue
        {
            set
            {
                CellValue.Value = value;
            }
        }
        /// <summary>
        /// 延时检测值的类型
        /// </summary>
        private void CheckNewValue()
        {
            if (isNewValue)
            {
                isNewValue = false;
                FixValue(newValue);
                newValue = null;
                isAllowChangeState = true;//恢复可设置状态。
            }
        }
        private void FixValue(object value)
        {
            #region CheckValue
            bool valueIsNull = value == null || value == DBNull.Value;
            if (valueIsNull)
            {
                if (CellValue.IsNull)
                {
                    CellValue.State = (value == DBNull.Value) ? 2 : 1;
                }
                else
                {
                    if (isAllowChangeState)
                    {
                        CellValue.State = 2;
                    }
                    CellValue.Value = null;
                    CellValue.IsNull = true;
                    StringValue = string.Empty;
                }
            }
            else
            {
                StringValue = value.ToString();
                int groupID = DataType.GetGroup(_CellStruct.SqlType);
                if (_CellStruct.SqlType != SqlDbType.Variant)
                {
                    if (StringValue == "" && groupID > 0)
                    {
                        CellValue.Value = null;
                        CellValue.IsNull = true;
                        return;
                    }
                    value = ChangeValue(value, _CellStruct.ValueType, groupID);
                    if (value == null)
                    {
                        return;
                    }
                }

                if (!CellValue.IsNull && (CellValue.Value.Equals(value) || (groupID != 999 && CellValue.Value.ToString() == StringValue)))//对象的比较值，用==号则比例引用地址。
                {
                    if (isAllowChangeState)
                    {
                        CellValue.State = 1;
                    }
                }
                else
                {
                    CellValue.Value = value;
                    CellValue.IsNull = false;
                    if (isAllowChangeState)
                    {
                        CellValue.State = 2;
                    }
                }

            }
            #endregion
        }
        /// <summary>
        /// 数据类型被切换，重新修正值的类型。
        /// </summary>
        public bool FixValue()
        {
            Exception err = null;
            return FixValue(out err);
        }
        /// <summary>
        /// 数据类型被切换，重新修正值的类型。
        /// </summary>
        public bool FixValue(out Exception ex)
        {
            ex = null;
            if (!IsNull)
            {
                CellValue.Value = ChangeValue(CellValue.Value, _CellStruct.ValueType, DataType.GetGroup(_CellStruct.SqlType), out ex);
            }
            return ex == null;
        }
        internal object ChangeValue(object value, Type convertionType, int groupID)
        {
            Exception err;
            return ChangeValue(value, convertionType, groupID, out err);
        }
        /// <summary>
        ///  值的数据类型转换。
        /// </summary>
        /// <param name="value">要被转换的值</param>
        /// <param name="convertionType">要转换成哪种类型</param>
        /// <param name="groupID">数据库类型的组号</param>
        /// <returns></returns>
        internal object ChangeValue(object value, Type convertionType, int groupID, out Exception ex)
        {
            ex = null;
            StringValue = Convert.ToString(value);
            if (value == null)
            {
                CellValue.IsNull = true;
                return value;
            }
            if (groupID > 0 && StringValue == "")
            {
                CellValue.IsNull = true;
                return null;
            }
            try
            {
                #region 类型转换
                if (groupID == 1)
                {
                    switch (StringValue)
                    {
                        case "正无穷大":
                            StringValue = "Infinity";
                            break;
                        case "负无穷大":
                            StringValue = "-Infinity";
                            break;
                    }
                }
                if (value.GetType() != convertionType)
                {
                    #region 折叠
                    switch (groupID)
                    {
                        case 0:
                            if (_CellStruct.SqlType == SqlDbType.Time)//time类型的特殊处理。
                            {
                                string[] items = StringValue.Split(' ');
                                if (items.Length > 1)
                                {
                                    StringValue = items[1];
                                }
                            }
                            value = StringValue;
                            break;
                        case 1:
                            switch (StringValue.ToLower())
                            {
                                case "true":
                                    value = 1;
                                    break;
                                case "false":
                                    value = 0;
                                    break;
                                case "infinity":
                                    value = double.PositiveInfinity;
                                    break;
                                case "-infinity":
                                    value = double.NegativeInfinity;
                                    break;
                                default:
                                    goto err;
                            }
                            break;
                        case 2:
                            switch (StringValue.ToLower().TrimEnd(')', '('))
                            {
                                case "now":
                                case "getdate":
                                case "current_timestamp":
                                    value = DateTime.Now;
                                    break;
                                default:
                                    DateTime dt = DateTime.Parse(StringValue);
                                    value = dt == DateTime.MinValue ? (DateTime)SqlDateTime.MinValue : dt;
                                    break;
                            }
                            break;
                        case 3:
                            switch (StringValue.ToLower())
                            {
                                case "yes":
                                case "true":
                                case "1":
                                case "on":
                                case "是":
                                    value = true;
                                    break;
                                case "no":
                                case "false":
                                case "0":
                                case "":
                                case "否":
                                default:
                                    value = false;
                                    break;
                            }
                            break;
                        case 4:
                            if (StringValue == SqlValue.Guid || StringValue.StartsWith("newid"))
                            {
                                value = Guid.NewGuid();
                            }
                            else
                            {
                                value = new Guid(StringValue);
                            }
                            break;
                        default:
                        err:
                            if (convertionType.Name.EndsWith("[]"))
                            {
                                value = Convert.FromBase64String(StringValue);
                                StringValue = "System.Byte[]";
                            }
                            else
                            {
                                value = StaticTool.ChangeType(value, convertionType);
                            }
                            break;
                    }
                    #endregion
                }
                //else if (groupID == 2 && strValue.StartsWith("000"))
                //{
                //    value = SqlDateTime.MinValue;
                //}
                #endregion
            }
            catch (Exception err)
            {
                value = null;
                CellValue.Value = null;
                CellValue.IsNull = true;
                ex = err;
                string msg = string.Format("ChangeType Error：ColumnName【{0}】({1}) ， Value：【{2}】\r\n", _CellStruct.ColumnName, _CellStruct.ValueType.FullName, StringValue);
                StringValue = null;
                if (AppConfig.Log.IsWriteLog)
                {
                    Log.WriteLog(true, msg);
                }

            }
            return value;
        }

        internal T Get<T>()
        {
            if (CellValue.IsNull)
            {
                return default(T);
            }
            Type t = typeof(T);
            return (T)ChangeValue(CellValue.Value, t, DataType.GetGroup(DataType.GetSqlType(t)));
        }

        /// <summary>
        /// 值是否为Null值[只读属性]
        /// </summary>
        public bool IsNull
        {
            get
            {
                CheckNewValue();
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
                CheckNewValue();
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
        private bool isAllowChangeState = true;
        /// <summary>
        /// Value的状态:0;未改,1;进行赋值操作[但值相同],2:赋值,值不同改变了
        /// </summary>
        public int State
        {
            get
            {
                CheckNewValue();
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
            CellValue.Value = null;
            CellValue.State = 0;
            CellValue.IsNull = true;
            StringValue = null;
        }
        internal void LoadValue(MDataCell cell)
        {
            StringValue = cell.StringValue;
            CellValue.Value = cell.Value;
            CellValue.State = cell.State;
            CellValue.IsNull = cell.IsNull;
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
            string text = StringValue;
            switch (DataType.GetGroup(_CellStruct.SqlType))
            {
                case 999:
                    MDataRow row = null;
                    MDataTable table = null;
                    Type t = Value.GetType();
                    if (!t.FullName.StartsWith("System."))//普通对象。
                    {
                        row = new MDataRow(TableSchema.GetColumns(t));
                        row.LoadFrom(Value);
                    }
                    else if (Value is IEnumerable)
                    {
                        int len = StaticTool.GetArgumentLength(ref t);
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

