using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using CYQ.Data.SQL;
using CYQ.Data.Orm;
using CYQ.Data.Tool;
using CYQ.Data.Json;

namespace CYQ.Data.Table
{
    /// <summary>
    /// 列结构修选项
    /// </summary>
    [Flags]
    public enum AlterOp
    {
        /// <summary>
        /// 默认不修改状态
        /// </summary>
        None = 0,
        /// <summary>
        /// 添加或修改状态
        /// </summary>
        AddOrModify = 1,
        /// <summary>
        /// 删除列状态
        /// </summary>
        Drop = 2,
        /// <summary>
        /// 重命名列状态
        /// </summary>
        Rename = 4
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
        [JsonIgnore]
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
        /// 是否忽略Json转换
        /// </summary>
        [JsonIgnore]
        public bool IsJsonIgnore { get; set; }

        private string _TableName;
        /// <summary>
        /// 表名
        /// </summary>
        [JsonIgnore]
        public string TableName
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName) && _MDataColumn != null)
                {
                    return _MDataColumn.TableName;
                }
                return _TableName;
            }
            set { _TableName = value; }

        }
        /// <summary>
        /// 外键表名
        /// </summary>
        [JsonIgnore]
        public string FKTableName { get; set; }
        /// <summary>
        /// 旧的列名（AlterOp为Rename时可用）
        /// </summary>
        [JsonIgnore]
        public string OldName { get; set; }
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
                    _MDataColumn.IsNeedRefleshIndex = true;//列名已变更，存储索引也需要变更
                }
            }
        }

        /// <summary>
        /// 字段描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否关键字
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// 是否自增加
        /// </summary>
        public bool IsAutoIncrement { get; set; }

        /// <summary>
        /// 是否允许为Null
        /// </summary>
        public bool IsCanNull { get; set; }

        /// <summary>
        /// 是否唯一索引
        /// </summary>
        public bool IsUniqueKey { get; set; }

        /// <summary>
        /// 是否外键
        /// </summary>
        public bool IsForeignKey { get; set; }


        private object _DefaultValue;
        /// <summary>
        /// 默认值
        /// </summary>
        public object DefaultValue
        {
            get { return _DefaultValue; }
            set
            {
                DataGroupType group = DataType.GetGroup(SqlType);
                if (group == DataGroupType.Number || group == DataGroupType.Bool)
                {
                    string defaultValue = Convert.ToString(value);
                    if (!string.IsNullOrEmpty(defaultValue) && (defaultValue[0] == 'N' || defaultValue[0] == '('))
                    {
                        defaultValue = defaultValue.Trim('N', '(', ')');//处理int型默认值（1）带括号的问题。
                    }
                    _DefaultValue = defaultValue;
                }
                else
                {
                    _DefaultValue = value;
                }
            }
        }


        private SqlDbType _SqlType;
        /// <summary>
        /// SqlDbType类型
        /// </summary>
        [JsonEnumToString]
        public SqlDbType SqlType
        {
            get
            {
                return _SqlType;
            }
            set
            {
                _SqlType = value;
                //ValueType = DataType.GetType(_SqlType, DalType,SqlTypeName);
            }
        }
        /// <summary>
        /// 最大字节
        /// </summary>
        public int MaxSize { get; set; }

        /// <summary>
        /// 精度（小数位）
        /// </summary>
        public short Scale { get; set; }


        /// <summary>
        /// 原始的数据库字段类型名称
        /// </summary>
        internal string SqlTypeName { get; set; }

        [NonSerialized]
        internal Type valueType;
        internal Type ValueType
        {
            get
            {
                if (valueType == null)
                {
                    valueType = DataType.GetType(_SqlType, DalType, SqlTypeName);
                }
                return valueType;
            }
        }

        private DataBaseType dalType = DataBaseType.None;
        internal DataBaseType DalType
        {
            get
            {
                if (_MDataColumn != null)
                {
                    return _MDataColumn.DataBaseType;
                }
                return dalType;
            }
        }

        private AlterOp _AlterOp = AlterOp.None;
        /// <summary>
        /// 列结构改变状态
        /// </summary>
        [JsonIgnore]
        public AlterOp AlterOp
        {
            get { return _AlterOp; }
            set { _AlterOp = value; }
        }

        internal int _ReaderIndex = -1;
        //内部使用的索引，在字段名为空时使用
        internal int ReaderIndex
        {
            get
            {
                if (_ReaderIndex == -1 && _MDataColumn != null)
                {
                    return _MDataColumn.GetIndex(this.ColumnName);
                }
                return _ReaderIndex;
            }
            set
            {
                _ReaderIndex = value;
            }
        }

        #region 构造函数
        internal MCellStruct(DataBaseType dalType)
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
            SqlTypeName = ms.SqlTypeName;
            SqlType = ms.SqlType;
            IsAutoIncrement = ms.IsAutoIncrement;
            IsCanNull = ms.IsCanNull;
            MaxSize = ms.MaxSize;
            Scale = ms.Scale;
            IsPrimaryKey = ms.IsPrimaryKey;
            IsUniqueKey = ms.IsUniqueKey;
            IsForeignKey = ms.IsForeignKey;
            FKTableName = ms.FKTableName;
            AlterOp = ms.AlterOp;
            IsJsonIgnore = ms.IsJsonIgnore;
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
            ms.SqlTypeName = SqlTypeName;
            ms.SqlType = SqlType;
            ms.IsAutoIncrement = IsAutoIncrement;
            ms.IsCanNull = IsCanNull;
            ms.MaxSize = MaxSize;
            ms.Scale = Scale;
            ms.IsPrimaryKey = IsPrimaryKey;
            ms.IsUniqueKey = IsUniqueKey;
            ms.IsForeignKey = IsForeignKey;
            ms.FKTableName = FKTableName;
            ms.DefaultValue = DefaultValue;
            ms.Description = Description;
            ms.MDataColumn = MDataColumn;
            ms.AlterOp = AlterOp;
            ms.TableName = TableName;
            ms.IsJsonIgnore = IsJsonIgnore;
            return ms;
        }
        #endregion
    }

    public partial class MCellStruct // 扩展几个常用方法
    {
        /// <summary>
        /// 为列的所有行设置值
        /// </summary>
        public MCellStruct Set(object value)
        {
            Set(value, -1);
            return this;
        }
        public MCellStruct Set(object value, int state)
        {
            if (_MDataColumn != null)
            {
                MDataTable dt = _MDataColumn._Table;
                if (dt != null && dt.Rows.Count > 0)
                {
                    int index = _MDataColumn.GetIndex(ColumnName);
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        dt.Rows[i].Set(index, value, state);
                    }
                }
            }
            return this;
        }
        /// <summary>
        /// 返回该列的where In 条件 : Name in("aa","bb")
        /// </summary>
        /// <returns></returns>
        public string GetWhereIn()
        {
            if (_MDataColumn != null)
            {
                MDataTable dt = _MDataColumn._Table;
                if (dt != null && dt.Rows.Count > 0)
                {
                    List<string> items = dt.GetColumnItems<string>(ColumnName, BreakOp.NullOrEmpty, true);
                    if (items != null && items.Count > 0)
                    {
                        return SqlCreate.GetWhereIn(this, items, dalType);
                    }
                }
            }
            return string.Empty;
        }
    }
}
