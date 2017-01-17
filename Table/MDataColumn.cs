using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using CYQ.Data.SQL;
using CYQ.Data.Tool;
using System.IO;



namespace CYQ.Data.Table
{
    /// <summary>
    /// 头列表集合
    /// </summary>
    public partial class MDataColumn : List<MCellStruct>
    {
        /// <summary>
        /// 列名是否变更
        /// </summary>
        internal bool IsColumnNameChanged = false;
        /// <summary>
        /// 存储列名的索引
        /// </summary>
        private MDictionary<string, int> columnIndex = new MDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// 添加列时，检测名称是否重复(默认为true)。
        /// </summary>
        public bool CheckDuplicate = true;
        /// <summary>
        /// 隐式转换列头
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static implicit operator MDataColumn(DataColumnCollection columns)
        {
            if (columns == null)
            {
                return null;
            }
            MDataColumn mColumns = new MDataColumn();

            if (columns.Count > 0)
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    MCellStruct cellStruct = new MCellStruct(columns[i].ColumnName, DataType.GetSqlType(columns[i].DataType), columns[i].ReadOnly, columns[i].AllowDBNull, columns[i].MaxLength);
                    mColumns.Add(cellStruct);
                }
            }
            return mColumns;
        }

        public MCellStruct this[string key]
        {
            get
            {
                int index = GetIndex(key);
                if (index > -1)
                {
                    return this[index];
                }
                return null;
            }
        }
        /// <summary>
        /// 架构所引用的表
        /// </summary>
        public MDataTable Table
        {
            get
            {
                return _Table;
            }
        }
        private string _Description = string.Empty;
        /// <summary>
        /// 表名描述
        /// </summary>
        public string Description
        {
            get
            {
                if (string.IsNullOrEmpty(_Description) && _Table != null)
                {
                    return _Table.Description;
                }
                return _Description;
            }
            set
            {
                _Description = value;
            }
        }

        internal MDataTable _Table;
        public MDataColumn()
            : base()
        {

        }
        internal MDataColumn(MDataTable table)
        {
            _Table = table;
        }
        public MDataColumn Clone()
        {
            MDataColumn mcs = new MDataColumn();
            mcs.dalType = dalType;
            mcs.CheckDuplicate = false;
            mcs.isViewOwner = isViewOwner;
            foreach (string item in relationTables)
            {
                mcs.AddRelateionTableName(item);
            }
            for (int i = 0; i < base.Count; i++)
            {
                mcs.Add(base[i].Clone());
            }
            return mcs;
        }
        public bool Contains(string columnName)
        {
            return GetIndex(columnName) > -1;
        }

        /// <summary>
        /// 获取列所在的索引位置(若不存在返回-1）
        /// </summary>
        public int GetIndex(string columnName)
        {
            if (columnIndex.Count == 0 || IsColumnNameChanged || columnIndex.Count != Count)
            {
                columnIndex.Clear();
                for (int i = 0; i < Count; i++)
                {
                    columnIndex.Add(this[i].ColumnName.Replace("_", ""), i);
                }
                IsColumnNameChanged = false;
            }

            if (!string.IsNullOrEmpty(columnName))
            {
                if (columnName.IndexOf('_') > -1)
                {
                    columnName = columnName.Replace("_", "");//兼容映射处理
                }
                if (columnIndex.ContainsKey(columnName))
                {
                    return columnIndex[columnName];
                }
                //for (int i = 0; i < Count; i++)
                //{
                //    if (string.Compare(this[i].ColumnName.Replace("_", ""), columnName, StringComparison.OrdinalIgnoreCase) == 0)//第三个参数用StringComparison.OrdinalIgnoreCase比用true快。
                //    {
                //        return i;
                //    }
                //}
            }
            return -1;
        }
        /// <summary>
        /// 将 列 的序号或位置更改为指定的序号或位置。
        /// </summary>
        /// <param name="columnName">列名</param>
        /// <param name="ordinal">序号</param>
        public void SetOrdinal(string columnName, int ordinal)
        {
            int index = GetIndex(columnName);
            if (index > -1 && index != ordinal)
            {
                MCellStruct mstruct = this[index];
                if (_Table != null && _Table.Rows.Count > 0)
                {
                    List<object> items = _Table.GetColumnItems<object>(index, BreakOp.None);
                    _Table.Columns.RemoveAt(index);
                    _Table.Columns.Insert(ordinal, mstruct);
                    for (int i = 0; i < items.Count; i++)
                    {
                        _Table.Rows[i].Set(ordinal, items[i]);
                    }
                    items = null;
                }
                else
                {
                    base.RemoveAt(index);//移除
                    if (ordinal >= base.Count)
                    {
                        ordinal = base.Count;
                    }
                    base.Insert(ordinal, mstruct);
                }
            }
            columnIndex.Clear();
        }

        /// <summary>
        /// 输出Json格式的表构架
        /// </summary>
        public string ToJson(bool isFullSchema)
        {
            JsonHelper helper = new JsonHelper();
            helper.Fill(this, isFullSchema);
            return helper.ToString();
        }
        /// <summary>
        /// 转成行
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public MDataRow ToRow(string tableName)
        {
            MDataRow row = new MDataRow(this);
            row.TableName = tableName;
            row.Columns.CheckDuplicate = CheckDuplicate;
            row.Columns.dalType = dalType;
            row.Columns.isViewOwner = isViewOwner;
            row.Columns.relationTables = relationTables;
            return row;
        }
        /// <summary>
        /// 保存表架构到外部文件中(json格式）
        /// </summary>
        public bool WriteSchema(string fileName)
        {
            string schema = ToJson(true).Replace("},{", "},\r\n{");//写入文本时要换行。
            return IOHelper.Write(fileName, schema);
        }

        private List<MCellStruct> _JointPrimary = new List<MCellStruct>();
        /// <summary>
        /// 联合主键
        /// </summary>
        internal List<MCellStruct> JointPrimary
        {
            get
            {
                MCellStruct autoIncrementCell = null;
                if (_JointPrimary.Count == 0 && this.Count > 0)
                {
                    foreach (MCellStruct item in this)
                    {
                        if (item.IsPrimaryKey)
                        {
                            _JointPrimary.Add(item);
                        }
                        else if (item.IsAutoIncrement)
                        {
                            autoIncrementCell = item;
                        }
                    }
                    if (_JointPrimary.Count == 0)
                    {
                        if (autoIncrementCell != null)
                        {
                            _JointPrimary.Add(autoIncrementCell);
                        }
                        else
                        {
                            _JointPrimary.Add(this[0]);
                        }
                    }
                }
                return _JointPrimary;
            }
        }

        /// <summary>
        /// 第一个主键
        /// </summary>
        internal MCellStruct FirstPrimary
        {
            get
            {
                if (JointPrimary.Count > 0)
                {
                    return JointPrimary[0];
                }
                return null;
            }
        }
        /// <summary>
        /// 首个唯一键
        /// </summary>
        internal MCellStruct FirstUnique
        {
            get
            {
                MCellStruct ms = null;
                foreach (MCellStruct item in this)
                {
                    if (item.IsUniqueKey)
                    {
                        return item;
                    }
                    else if (ms == null && !item.IsPrimaryKey && DataType.GetGroup(item.SqlType) == 0)//取第一个字符串类型
                    {
                        ms = item;
                    }
                }
                if (ms == null && this.Count > 0)
                {
                    ms = this[0];
                }
                return ms;
            }
        }

        /// <summary>
        /// 当前的数据库类型。
        /// </summary>
        internal DalType dalType = DalType.None;
        /// <summary>
        /// 该结构是否由视图拥有
        /// </summary>
        internal bool isViewOwner = false;
        internal List<string> relationTables = new List<string>();
        internal void AddRelateionTableName(string tableName)
        {
            if (!string.IsNullOrEmpty(tableName) && !relationTables.Contains(tableName))
            {
                relationTables.Add(tableName);
            }
        }

        /// <summary>
        /// 将表结构的数据转成Table显示
        /// </summary>
        /// <returns></returns>
        public MDataTable ToTable()
        {
            string tableName = string.Empty;
            if (_Table != null)
            {
                tableName = _Table.TableName;
            }
            MDataTable dt = new MDataTable(tableName);
            dt.Columns.Add("ColumnName");
            dt.Columns.Add("MaxSize");
            dt.Columns.Add("Scale");
            dt.Columns.Add("IsCanNull");
            dt.Columns.Add("IsAutoIncrement");
            dt.Columns.Add("SqlType");
            dt.Columns.Add("IsPrimaryKey");
            dt.Columns.Add("IsUniqueKey");
            dt.Columns.Add("IsForeignKey");
            dt.Columns.Add("FKTableName");
            dt.Columns.Add("DefaultValue");
            dt.Columns.Add("Description");
            dt.Columns.Add("TableName");

            for (int i = 0; i < Count; i++)
            {
                MCellStruct ms = this[i];
                dt.NewRow(true)
                    .Set(0, ms.ColumnName)
                    .Set(1, ms.MaxSize)
                    .Set(2, ms.Scale)
                    .Set(3, ms.IsCanNull)
                    .Set(4, ms.IsAutoIncrement)
                    .Set(5, ms.SqlType)
                    .Set(6, ms.IsPrimaryKey)
                    .Set(7, ms.IsUniqueKey)
                    .Set(8, ms.IsForeignKey)
                    .Set(9, ms.FKTableName)
                    .Set(10, ms.DefaultValue)
                    .Set(11, ms.Description)
                .Set(12, ms.TableName);
            }
            return dt;
        }

        /// <summary>
        /// 为列的所有行设置值
        /// </summary>
        public MDataColumn Set(object key, object value)
        {
            Set(key, value, -1);
            return this;
        }
        public MDataColumn Set(object key, object value, int state)
        {
            if (_Table != null && _Table.Rows.Count > 0)
            {
                for (int i = 0; i < _Table.Rows.Count; i++)
                {
                    _Table.Rows[i].Set(key, value, state);
                }
            }
            return this;
        }
    }
    public partial class MDataColumn
    {
        #region Add重载方法
        /// <summary>
        /// 添加列
        /// </summary>
        /// <param name="columnName">列名</param>
        public void Add(string columnName)
        {
            Add(columnName, SqlDbType.NVarChar, false, true, -1, false, null);
        }
        /// <param name="SqlType">列的数据类型</param>
        public void Add(string columnName, SqlDbType sqlType)
        {
            Add(columnName, sqlType, false, true, -1, false, null);
        }
        /// <param name="isAutoIncrement">是否自增ID列</param>
        public void Add(string columnName, SqlDbType sqlType, bool isAutoIncrement)
        {
            Add(columnName, sqlType, isAutoIncrement, !isAutoIncrement, -1, isAutoIncrement, null);
        }

        public void Add(string columnName, SqlDbType sqlType, bool isAutoIncrement, bool isCanNull, int maxSize)
        {
            Add(columnName, sqlType, isAutoIncrement, isCanNull, maxSize, false, null);
        }

        /// <param name="defaultValue">默认值[日期类型请传入SqlValue.GetDate]</param>
        public void Add(string columnName, SqlDbType sqlType, bool isAutoIncrement, bool isCanNull, int maxSize, bool isPrimaryKey, object defaultValue)
        {
            MCellStruct mdcStruct = new MCellStruct(columnName, sqlType, isAutoIncrement, isCanNull, maxSize);
            mdcStruct.IsPrimaryKey = isPrimaryKey;
            mdcStruct.DefaultValue = defaultValue;
            Add(mdcStruct);
        }

        #endregion

        public new void Add(MCellStruct item)
        {
            if (item != null && !this.Contains(item) && (!CheckDuplicate || !Contains(item.ColumnName)))
            {
                if (dalType == DalType.None)
                {
                    dalType = item.DalType;
                }
                item.MDataColumn = this;
                base.Add(item);
                if (_Table != null && _Table.Rows.Count > 0)
                {
                    for (int i = 0; i < _Table.Rows.Count; i++)
                    {
                        if (Count > _Table.Rows[i].Count)
                        {
                            _Table.Rows[i].Add(new MDataCell(ref item));
                        }
                    }
                }
                columnIndex.Clear();
            }
        }
        public new void AddRange(IEnumerable<MCellStruct> collection)
        {
            AddRange(collection as MDataColumn);
        }
        public void AddRange(MDataColumn items)
        {
            if (items.Count > 0)
            {
                foreach (MCellStruct item in items)
                {
                    if (!Contains(item.ColumnName))
                    {
                        Add(item);
                    }
                }
                columnIndex.Clear();
            }
        }
        public new void Remove(MCellStruct item)
        {
            Remove(item.ColumnName);
        }
        public void Remove(string columnName)
        {
            int index = GetIndex(columnName);
            if (index > -1)
            {
                RemoveAt(index);
                columnIndex.Clear();
            }
        }
        public new void RemoveAll(Predicate<MCellStruct> match)
        {
            Error.Throw(AppConst.Global_NotImplemented);
        }
        public new void RemoveRange(int index, int count) // 1,4
        {
            for (int i = index; i < index + count; i++)
            {
                RemoveAt(i);
            }
        }
        public new void RemoveAt(int index)
        {
            base.RemoveAt(index);
            if (_Table != null)
            {
                foreach (MDataRow row in _Table.Rows)
                {
                    if (row.Count > Count)
                    {
                        row.RemoveAt(index);
                    }
                }
            }
            columnIndex.Clear();

        }
        public new void Insert(int index, MCellStruct item)
        {
            if (item != null && !this.Contains(item) && (!CheckDuplicate || !Contains(item.ColumnName)))
            {
                item.MDataColumn = this;
                base.Insert(index, item);
                if (_Table != null && _Table.Rows.Count > 0)
                {
                    for (int i = 0; i < _Table.Rows.Count; i++)
                    {
                        if (Count > _Table.Rows[i].Count)
                        {
                            _Table.Rows[i].Insert(index, new MDataCell(ref item));
                        }
                    }
                }
                columnIndex.Clear();
            }

        }
        public new void InsertRange(int index, MDataColumn mdc)
        {
            for (int i = mdc.Count; i >= 0; i--)
            {
                Insert(index, mdc[i]);//反插
            }
        }
    }
    public partial class MDataColumn
    {
        /// <summary>
        /// 从Json或文件中加载成列信息
        /// </summary>
        /// <param name="jsonOrFileName">Json或文件（完整路径）名称</param>
        /// <returns></returns>
        public static MDataColumn CreateFrom(string jsonOrFileName)
        {
            return CreateFrom(jsonOrFileName, true);
        }
        /// <summary>
        /// 从Json或文件中加载成列信息
        /// </summary>
        /// <param name="jsonOrFileName">Json或文件（完整路径）名称</param>
        /// <param name="readTxtOrXml">是否从.txt或.xml文件中读取架构（默认为true）</param>
        /// <returns></returns>
        public static MDataColumn CreateFrom(string jsonOrFileName, bool readTxtOrXml)
        {
            MDataColumn mdc = new MDataColumn();
            MDataTable dt = null;
            try
            {
                bool isTxtOrXml = false;
                string json = string.Empty;
                string exName = Path.GetExtension(jsonOrFileName);
                switch (exName.ToLower())
                {
                    case ".ts":
                    case ".xml":
                    case ".txt":
                        string tsFileName = jsonOrFileName.Replace(exName, ".ts");
                        if (File.Exists(tsFileName))
                        {
                            json = IOHelper.ReadAllText(tsFileName);
                        }
                        else if (readTxtOrXml && File.Exists(jsonOrFileName))
                        {
                            isTxtOrXml = true;
                            if (exName == ".xml")
                            {
                                json = IOHelper.ReadAllText(jsonOrFileName, Encoding.UTF8);
                            }
                            else if (exName == ".txt")
                            {
                                json = IOHelper.ReadAllText(jsonOrFileName);
                            }
                        }
                        break;
                    default:
                        json = jsonOrFileName;
                        break;
                }
                if (!string.IsNullOrEmpty(json))
                {
                    dt = MDataTable.CreateFrom(json);
                    if (dt.Columns.Count > 0)
                    {
                        if (isTxtOrXml)
                        {
                            mdc = dt.Columns.Clone();
                        }
                        else
                        {
                            foreach (MDataRow row in dt.Rows)
                            {
                                MCellStruct cs = new MCellStruct(
                                    row.Get<string>("ColumnName"),
                                    DataType.GetSqlType(row.Get<string>("SqlType", "string")),
                                    row.Get<bool>("IsAutoIncrement", false),
                                    row.Get<bool>("IsCanNull", false),
                                    row.Get<int>("MaxSize", -1));
                                cs.Scale = row.Get<short>("Scale");
                                cs.IsPrimaryKey = row.Get<bool>("IsPrimaryKey", false);
                                cs.DefaultValue = row.Get<string>("DefaultValue");


                                //新增属性
                                cs.TableName = row.Get<string>("TableName");
                                cs.IsUniqueKey = row.Get<bool>("IsUniqueKey", false);
                                cs.IsForeignKey = row.Get<bool>("IsForeignKey", false);
                                cs.FKTableName = row.Get<string>("FKTableName");
                                mdc.Add(cs);
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Log.WriteLogToTxt(err);
            }
            finally
            {
                dt = null;
            }
            return mdc;
        }

        //internal bool AcceptChanges(AcceptOp op)
        //{
        //    if (_Table == null)
        //    {
        //        return false;
        //    }
        //    return AcceptChanges(op, _Table.TableName, _Table.Conn);
        //}
        //internal bool AcceptChanges(AcceptOp op, string tableName, string newConn)
        //{
        //    if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(newConn) || Count == 0)
        //    {
        //        return false;
        //    }
        //    return true;
        //}
    }
}
