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
    /// 列结构修选项
    /// </summary>
    public enum AlterOp
    {
        /// <summary>
        /// 默认不修改状态
        /// </summary>
        None,
        /// <summary>
        /// 添加或修改状态
        /// </summary>
        AddOrModify,
        /// <summary>
        /// 删除列状态
        /// </summary>
        Drop,
        /// <summary>
        /// 重命名列状态
        /// </summary>
        Rename
    }
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
        internal object Value = null;
    }

    /// <summary>
    /// 单元结构属性
    /// </summary>
    public partial class MCellStruct
    {
        private MDataColumn _MDataColumn = null;
        /// <summary>
        /// 结构集合
        /// </summary>
        public MDataColumn MDataColumn
        {
            get
            {
                return _MDataColumn;
            }
            internal set
            {
                _MDataColumn = value;
            }
        }
        /// <summary>
        /// 是否对值进行格式校验
        /// </summary>
        //public bool IsCheckValue = true;
        /// <summary>
        /// 是否关键字
        /// </summary>
        public bool IsPrimaryKey = false;

        /// <summary>
        /// 是否唯一索引
        /// </summary>
        public bool IsUniqueKey = false;

        /// <summary>
        /// 是否外键
        /// </summary>
        public bool IsForeignKey = false;
        /// <summary>
        /// 外键表名
        /// </summary>
        public string FKTableName;

        /// <summary>
        /// 字段描述
        /// </summary>
        public string Description;
        /// <summary>
        /// 默认值
        /// </summary>
        public object DefaultValue;
        /// <summary>
        /// 是否允许为Null
        /// </summary>
        public bool IsCanNull;
        /// <summary>
        /// 是否自增加
        /// </summary>
        public bool IsAutoIncrement;
        /// <summary>
        /// 旧的列名（AlterOp为Rename时可用）
        /// </summary>
        public string OldName;
        private string _ColumnName = string.Empty;
        /// <summary>
        /// 列名
        /// </summary>
        public string ColumnName
        {
            get
            {
                return _ColumnName;
            }
            set
            {
                _ColumnName = value;
                if (_MDataColumn != null)
                {
                    _MDataColumn.IsColumnNameChanged = true;//列名已变更，存储索引也需要变更
                }
            }
        }
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName;
        private SqlDbType _SqlType;
        /// <summary>
        /// SqlDbType类型
        /// </summary>
        public SqlDbType SqlType
        {
            get
            {
                return _SqlType;
            }
            set
            {
                _SqlType = value;
                ValueType = DataType.GetType(_SqlType, DalType);
            }
        }
        /// <summary>
        /// 最大字节
        /// </summary>
        public int MaxSize;

        /// <summary>
        /// 精度（小数位）
        /// </summary>
        public short Scale;
        /// <summary>
        /// 原始的数据库字段类型名称
        /// </summary>
        internal string SqlTypeName;
        internal Type ValueType;
        private DalType dalType = DalType.None;
        internal DalType DalType
        {
            get
            {
                if (_MDataColumn != null)
                {
                    return _MDataColumn.dalType;
                }
                return dalType;
            }
        }
        private AlterOp _AlterOp = AlterOp.None;
        /// <summary>
        /// 列结构改变状态
        /// </summary>
        public AlterOp AlterOp
        {
            get { return _AlterOp; }
            set { _AlterOp = value; }
        }
        //内部使用的索引，在字段名为空时使用
        internal int ReaderIndex = -1;

        #region 构造函数
        internal MCellStruct(DalType dalType)
        {
            this.dalType = dalType;
        }
        public MCellStruct(string columnName, SqlDbType sqlType)
        {
            Init(columnName, sqlType, false, true, false, -1, null);
        }
        public MCellStruct(string columnName, SqlDbType sqlType, bool isAutoIncrement, bool isCanNull, int maxSize)
        {
            Init(columnName, sqlType, isAutoIncrement, isCanNull, false, maxSize, null);
        }
        internal void Init(string columnName, SqlDbType sqlType, bool isAutoIncrement, bool isCanNull, bool isPrimaryKey, int maxSize, object defaultValue)
        {
            ColumnName = columnName.Trim();
            SqlType = sqlType;
            IsAutoIncrement = isAutoIncrement;
            IsCanNull = isCanNull;
            MaxSize = maxSize;
            IsPrimaryKey = isPrimaryKey;
            DefaultValue = defaultValue;
        }
        internal void Load(MCellStruct ms)
        {
            ColumnName = ms.ColumnName;
            SqlType = ms.SqlType;
            IsAutoIncrement = ms.IsAutoIncrement;
            IsCanNull = ms.IsCanNull;
            MaxSize = ms.MaxSize;
            Scale = ms.Scale;
            IsPrimaryKey = ms.IsPrimaryKey;
            IsUniqueKey = ms.IsUniqueKey;
            IsForeignKey = ms.IsForeignKey;
            FKTableName = ms.FKTableName;
            SqlTypeName = ms.SqlTypeName;
            AlterOp = ms.AlterOp;

            if (ms.DefaultValue != null)
            {
                DefaultValue = ms.DefaultValue;
            }
            if (!string.IsNullOrEmpty(ms.Description))
            {
                Description = ms.Description;
            }
        }
        /// <summary>
        /// 克隆一个对象。
        /// </summary>
        /// <returns></returns>
        public MCellStruct Clone()
        {
            MCellStruct ms = new MCellStruct(dalType);
            ms.ColumnName = ColumnName;
            ms.SqlType = SqlType;
            ms.IsAutoIncrement = IsAutoIncrement;
            ms.IsCanNull = IsCanNull;
            ms.MaxSize = MaxSize;
            ms.Scale = Scale;
            ms.IsPrimaryKey = IsPrimaryKey;
            ms.IsUniqueKey = IsUniqueKey;
            ms.IsForeignKey = IsForeignKey;
            ms.FKTableName = FKTableName;
            ms.SqlTypeName = SqlTypeName;
            ms.DefaultValue = DefaultValue;
            ms.Description = Description;
            ms.MDataColumn = MDataColumn;
            ms.AlterOp = AlterOp;
            ms.TableName = TableName;
            return ms;
        }
        #endregion
    }
    /// <summary>
    /// 单元格
    /// </summary>
    public partial class MDataCell
    {
        internal MCellValue _CellValue;
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
        internal string strValue = string.Empty;
        /// <summary>
        /// 值
        /// </summary>
        public object Value
        {
            get
            {
                return CellValue.Value;
            }
            set
            {
                //if (!_CellStruct.IsCheckValue)
                //{
                //    cellValue.Value = value;
                //    return;
                //}

                bool valueIsNull = value == null || value == DBNull.Value;
                if (valueIsNull)
                {
                    if (CellValue.IsNull)
                    {
                        CellValue.State = (value == DBNull.Value) ? 2 : 1;
                    }
                    else
                    {
                        CellValue.State = 2;
                        CellValue.Value = null;
                        CellValue.IsNull = true;
                        strValue = string.Empty;
                    }
                }
                else
                {
                    strValue = value.ToString();
                    int groupID = DataType.GetGroup(_CellStruct.SqlType);
                    if (_CellStruct.SqlType != SqlDbType.Variant)
                    {
                        if (strValue == "" && groupID > 0)
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

                    if (!CellValue.IsNull && (CellValue.Value.Equals(value) || (groupID != 999 && CellValue.Value.ToString() == strValue)))//对象的比较值，用==号则比例引用地址。
                    {
                        CellValue.State = 1;
                    }
                    else
                    {
                        CellValue.Value = value;
                        CellValue.State = 2;
                        CellValue.IsNull = false;
                    }

                }
            }
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
            strValue = Convert.ToString(value);
            if (value == null)
            {
                CellValue.IsNull = true;
                return value;
            }
            if (groupID > 0 && strValue == "")
            {
                CellValue.IsNull = true;
                return null;
            }
            try
            {
                #region 类型转换
                if (groupID == 1)
                {
                    switch (strValue)
                    {
                        case "正无穷大":
                            strValue = "Infinity";
                            break;
                        case "负无穷大":
                            strValue = "-Infinity";
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
                                string[] items = strValue.Split(' ');
                                if (items.Length > 1)
                                {
                                    strValue = items[1];
                                }
                            }
                            value = strValue;
                            break;
                        case 1:
                            switch (strValue.ToLower())
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
                            switch (strValue.ToLower().TrimEnd(')', '('))
                            {
                                case "now":
                                case "getdate":
                                case "current_timestamp":
                                    value = DateTime.Now;
                                    break;
                                default:
                                    DateTime dt = DateTime.Parse(strValue);
                                    value = dt == DateTime.MinValue ? (DateTime)SqlDateTime.MinValue : dt;
                                    break;
                            }
                            break;
                        case 3:
                            switch (strValue.ToLower())
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
                            if (strValue == SqlValue.Guid || strValue.StartsWith("newid"))
                            {
                                value = Guid.NewGuid();
                            }
                            else
                            {
                                value = new Guid(strValue);
                            }
                            break;
                        default:
                        err:
                            if (convertionType.Name.EndsWith("[]"))
                            {
                                value = Convert.FromBase64String(strValue);
                                strValue = "System.Byte[]";
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
                string msg = string.Format("ChangeType Error：ColumnName【{0}】({1}) ， Value：【{2}】\r\n", _CellStruct.ColumnName, _CellStruct.ValueType.FullName, strValue);
                strValue = null;
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
                return CellValue.IsNull;
            }
        }
        /// <summary>
        /// 值是否为Null或为空[只读属性]
        /// </summary>
        public bool IsNullOrEmpty
        {
            get
            {
                return CellValue.IsNull || strValue.Length == 0;
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
                return CellValue.State;
            }
            set
            {
                CellValue.State = value;
            }
        }
        #endregion

        #region 方法
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
            return strValue ?? "";
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
            return strValue.ToLower() == Convert.ToString(value).ToLower();
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
            string text = strValue;
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

