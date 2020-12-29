using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Xml;
using System.IO;
using System.Data.Common;
using System.ComponentModel;
using CYQ.Data.UI;
using CYQ.Data.Cache;
using CYQ.Data.SQL;
using CYQ.Data.Tool;
using System.Collections.Specialized;
using System.Web;
namespace CYQ.Data.Table
{

    /// <summary>
    /// 表格
    /// </summary>
    public partial class MDataTable
    {
        private const string DefaultTableName = "SysDefault";
        #region 隐式转换

        /// <summary>
        /// 从DataReader隐式转换成MDataTable
        /// </summary>
        public static implicit operator MDataTable(DbDataReader sdr)
        {
            MDataTable dt = CreateFrom(sdr);
            if (sdr != null)
            {
                sdr.Close();
                sdr.Dispose();
                sdr = null;
            }
            return dt;
        }

        /// <summary>
        /// 从DataTable隐式转换成MDataTable
        /// </summary>
        public static implicit operator MDataTable(DataTable dt)
        {
            if (dt == null)
            {
                return null;
            }
            MDataTable mdt = new MDataTable(dt.TableName);
            if (dt.Columns != null && dt.Columns.Count > 0)
            {
                foreach (DataColumn item in dt.Columns)
                {
                    MCellStruct mcs = new MCellStruct(item.ColumnName, DataType.GetSqlType(item.DataType), item.ReadOnly, item.AllowDBNull, item.MaxLength);
                    mcs.valueType = item.DataType;
                    mdt.Columns.Add(mcs);
                }
                foreach (DataRow row in dt.Rows)
                {
                    MDataRow mdr = mdt.NewRow();
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        mdr[i].Value = row[i];
                    }
                    mdt.Rows.Add(mdr, row.RowState != DataRowState.Modified);
                }
            }
            return mdt;
        }
        /// <summary>
        /// 从行集合隐式转换成MDataTable
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static implicit operator MDataTable(List<MDataRow> rows)
        {
            return (MDataRowCollection)rows;
        }
        /// <summary>
        /// 将一行数据装载成一个表。
        /// </summary>
        /// <returns></returns>
        public static implicit operator MDataTable(MDataRow row)
        {
            MDataTable mTable = new MDataTable(row.TableName);
            mTable.Conn = row.Conn;
            mTable.LoadRow(row);
            return mTable;
        }
        /// <summary>
        /// 将一行数据装载成一个表。
        /// </summary>
        /// <returns></returns>
        public static implicit operator MDataTable(MDataRowCollection rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return null;
            }
            MDataTable mdt = new MDataTable(rows[0].TableName);
            mdt.Conn = rows[0].Conn;
            mdt.Columns = rows[0].Columns;
            mdt.Rows.AddRange(rows);
            return mdt;

        }
        #endregion

        #region 属性
        private MDataRowCollection _Rows;
        /// <summary>
        /// 表格行
        /// </summary>
        public MDataRowCollection Rows
        {
            get
            {
                return _Rows;
            }
        }
        [NonSerialized]
        private object _DynamicData;
        /// <summary>
        /// 动态存储数据(如：AcceptChanges 产生的异常默认由本参数存储)
        /// </summary>
        public object DynamicData
        {
            get { return _DynamicData; }
            set { _DynamicData = value; }
        }

        public MDataTable()
        {
            Init(DefaultTableName, null);
        }
        public MDataTable(string tableName)
        {
            Init(tableName, null);
        }
        public MDataTable(string tableName, MDataColumn mdc)
        {
            Init(tableName, mdc);
        }
        private void Init(string tableName, MDataColumn mdc)
        {
            _Rows = new MDataRowCollection(this);
            _TableName = tableName;
            if (_Columns == null)
            {
                _Columns = new MDataColumn(this);
                if (mdc != null)
                {
                    _Columns.AddRange(mdc);
                }
            }
        }
        private string _TableName = string.Empty;
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName
        {
            get
            {
                if (string.IsNullOrEmpty(_TableName) && Columns != null)
                {
                    _TableName = Columns.TableName;
                }
                return _TableName;
            }
            set
            {
                _TableName = value;
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
                if (string.IsNullOrEmpty(_Description) && Columns != null)
                {
                    _Description = Columns.Description;
                }
                return _Description;
            }
            set
            {
                _Description = value;
            }
        }
        private MDataColumn _Columns;
        /// <summary>
        /// 表格的架构列
        /// </summary>
        public MDataColumn Columns
        {
            get
            {
                return _Columns;
            }
            set
            {
                _Columns = value;
                _Columns._Table = this;
                if (_TableName == DefaultTableName)
                {
                    _TableName = _Columns.TableName;
                }
                else
                {
                    _Columns.TableName = _TableName;
                }
            }
        }
        private string _Conn;
        /// <summary>
        /// 该表归属的数据库链接。
        /// </summary>
        public string Conn
        {
            get
            {
                if (string.IsNullOrEmpty(_Conn))
                {
                    return AppConfig.DB.DefaultConn;
                }
                return _Conn;
            }
            set
            {
                _Conn = value;
            }
        }

        #endregion

        #region 方法
        /// <summary>
        /// 新建一行
        /// </summary>
        /// <returns></returns>
        public MDataRow NewRow()
        {
            return NewRow(false);
        }
        /// <summary>
        /// 新建一行
        /// </summary>
        /// <param name="isAddToTable">是否顺带添加到表中</param>
        /// <returns></returns>
        public MDataRow NewRow(bool isAddToTable)
        {
            return NewRow(isAddToTable, -1);
        }
        /// <summary>
        /// 新建一行
        /// </summary>
        /// <param name="index">插入的索引</param>
        /// <returns></returns>
        public MDataRow NewRow(bool isAddToTable, int index)
        {
            MDataRow mdr = new MDataRow(this);
            if (isAddToTable)
            {
                if (index < 0)
                {
                    Rows.Add(mdr, false);
                }
                else
                {
                    Rows.Insert(index, mdr);
                }
            }
            return mdr;
        }
        #region 准备新开始的方法
        /// <summary>
        /// 使用本查询，得到克隆后的数据
        /// </summary>
        public MDataTable Select(object where)
        {
            return Select(0, 0, where);
        }
        /// <summary>
        /// 使用本查询，得到克隆后的数据
        /// </summary>
        public MDataTable Select(int topN, object where)
        {
            return Select(1, topN, where);
        }
        /// <summary>
        /// 使用本查询，得到克隆后的数据
        /// </summary>
        public MDataTable Select(int pageIndex, int pageSize, object where, params object[] selectColumns)
        {
            return MDataTableFilter.Select(this, pageIndex, pageSize, where, selectColumns);
        }
        /// <summary>
        /// 使用本查询，得到原数据的引用。
        /// </summary>
        public MDataRow FindRow(object where)
        {
            return MDataTableFilter.FindRow(this, where);
        }
        /// <summary>
        /// 使用本查询，得到原数据的引用。
        /// </summary>
        public MDataRowCollection FindAll(object where)
        {
            return MDataTableFilter.FindAll(this, where);
        }
        /// <summary>
        /// 统计满足条件的行所在的索引
        /// </summary>
        public int GetIndex(object where)
        {
            return MDataTableFilter.GetIndex(this, where);
        }
        /// <summary>
        /// 统计满足条件的行数
        /// </summary>
        public int GetCount(object where)
        {
            return MDataTableFilter.GetCount(this, where);
        }
        /// <summary>
        /// 根据条件分拆成两个表【满足条件，和非满足条件的】，分出来的数据行和原始表仍是同一个引用
        /// </summary>
        public MDataTable[] Split(object where)
        {
            return MDataTableFilter.Split(this, where);
        }
        #endregion

        /// <summary>
        /// 加载行(包括行架构)[提示，仅当表为空架构时有效]
        /// </summary>
        /// <param name="row"></param>
        internal void LoadRow(MDataRow row) //是否直接能用Row.Table呢？？、
        {
            if (this.Columns.Count == 0 && row != null && row.Count > 0)
            {
                this.Columns = row.Columns.Clone();
                if (!string.IsNullOrEmpty(_TableName) && _TableName.StartsWith("SysDefault"))
                {
                    _TableName = row.TableName;
                }
                _Conn = row.Conn;
                if (!row[0].IsNullOrEmpty)
                {
                    NewRow(true).LoadFrom(row);
                    //_Rows.Add(row);
                }
            }
        }

        /// <summary>
        /// 转换成DataTable
        /// </summary>
        public DataTable ToDataTable()
        {
            DataTable dt = new DataTable(_TableName);
            if (Columns != null && Columns.Count > 0)
            {
                bool checkDuplicate = Columns.CheckDuplicate;
                List<string> duplicateName = new List<string>();
                for (int j = 0; j < Columns.Count; j++)
                {
                    MCellStruct item = Columns[j];
                    if (string.IsNullOrEmpty(item.ColumnName))
                    {
                        item.ColumnName = "Empty_" + item;
                    }
                    if (!checkDuplicate && dt.Columns.Contains(item.ColumnName))//去重。
                    {
                        string rndName = Guid.NewGuid().ToString();
                        dt.Columns.Add(rndName, item.ValueType);
                        duplicateName.Add(rndName);
                        continue;
                    }
                    dt.Columns.Add(item.ColumnName, item.ValueType);
                }
                int count = dt.Columns.Count;
                foreach (MDataRow row in Rows)
                {
                    DataRow dr = dt.NewRow();
                    for (int i = 0; i < count; i++)
                    {
                        if (row[i].IsNull)
                        {
                            dr[i] = DBNull.Value;
                        }
                        else
                        {
                            dr[i] = row[i].Value;
                        }
                    }

                    dt.Rows.Add(dr);
                }
                for (int i = 0; i < duplicateName.Count; i++)
                {
                    dt.Columns.Remove(duplicateName[i]);
                }
            }
            dt.AcceptChanges();
            return dt;
        }
        /// <summary>
        /// 输出Xml文档
        /// </summary>
        public string ToXml()
        {
            return ToXml(false);
        }
        public string ToXml(bool isConvertNameToLower)
        {
            return ToXml(isConvertNameToLower, true, true);
        }
        /// <summary>
        /// 输出Xml文档
        /// </summary>
        /// <param name="isConvertNameToLower">名称转小写</param>
        /// <returns></returns>
        public string ToXml(bool isConvertNameToLower, bool needHeader, bool needRootNode)
        {
            StringBuilder xml = new StringBuilder();
            if (Columns.Count > 0)
            {
                string tableName = string.IsNullOrEmpty(_TableName) ? "Root" : _TableName;
                string rowName = string.IsNullOrEmpty(_TableName) ? "Row" : _TableName;
                if (isConvertNameToLower)
                {
                    tableName = tableName.ToLower();
                    rowName = rowName.ToLower();
                }
                if (needHeader)
                {
                    xml.Append("<?xml version=\"1.0\" standalone=\"yes\"?>");
                }
                if (needRootNode)
                {
                    xml.AppendFormat("\r\n<{0}>", tableName);
                }
                foreach (MDataRow row in Rows)
                {
                    xml.AppendFormat("\r\n  <{0}>", rowName);
                    foreach (MDataCell cell in row)
                    {
                        xml.Append(cell.ToXml(isConvertNameToLower));
                    }
                    xml.AppendFormat("\r\n  </{0}>", rowName);
                }
                if (needRootNode)
                {
                    xml.AppendFormat("\r\n</{0}>", tableName);
                }
            }
            return xml.ToString();
        }
        public bool WriteXml(string fileName)
        {
            return WriteXml(fileName, false);
        }
        /// <summary>
        /// 保存Xml
        /// </summary>
        public bool WriteXml(string fileName, bool isConvertNameToLower)
        {
            return IOHelper.Write(fileName, ToXml(isConvertNameToLower), Encoding.UTF8);
        }

        /// <summary>
        /// 输出Json
        /// </summary>
        public string ToJson()
        {
            return ToJson(true);
        }
        public string ToJson(bool addHead)
        {
            return ToJson(addHead, false);
        }
        /// <param name="addHead">输出头部信息[带count、Success、ErrorMsg](默认true)</param>
        /// <param name="addSchema">首行输出表架构信息,反接收时可还原架构(默认false)</param>
        public string ToJson(bool addHead, bool addSchema)
        {
            return ToJson(addHead, addSchema, RowOp.None);
        }
        /// <param name="rowOp">过滤选项</param>
        public string ToJson(bool addHead, bool addSchema, RowOp rowOp)
        {
            return ToJson(addHead, addSchema, rowOp, false);
        }

        public string ToJson(bool addHead, bool addSchema, RowOp rowOp, bool isConvertNameToLower)
        {
            return ToJson(addHead, addSchema, rowOp, isConvertNameToLower, JsonHelper.DefaultEscape);
        }
        /// <param name="op">符号转义选项</param>
        public string ToJson(bool addHead, bool addSchema, RowOp rowOp, bool isConvertNameToLower, EscapeOp escapeOp)
        {
            JsonHelper helper = new JsonHelper(addHead, addSchema);
            if (DynamicData != null && DynamicData is MDictionary<int, int>)
            {
                helper.LoopCheckList = DynamicData as MDictionary<int, int>;//继承父的数据，避免循环引用父
                helper.Level = helper.LoopCheckList[helper.LoopCheckList.Count - 1] + 1;
            }
            helper.Escape = escapeOp;
            helper.IsConvertNameToLower = isConvertNameToLower;
            helper.RowOp = rowOp;
            helper.Fill(this);
            bool checkArrayEnd = !addHead && !addSchema;
            return helper.ToString(checkArrayEnd);
        }
        /// <summary>
        /// 输出Json[可指定保存路径]
        /// </summary>
        public bool WriteJson(bool addHead, bool addSchema, string fileName)
        {
            return IOHelper.Write(fileName, ToJson(addHead, addSchema));
        }


        /// <summary>
        /// 将数据表绑定到列表控件
        /// </summary>
        /// <param name="control">列表控件[包括Repeater/DataList/GridView/DataGrid等]</param>
        public void Bind(object control)
        {
            Bind(control, null);
        }
        /// <summary>
        /// 将数据表绑定到列表控件
        /// </summary>
        /// <param name="control">列表控件[包括Repeater/DataList/GridView/DataGrid等]</param>
        /// <param name="nodeid">当Control为XHtmlAction对象时，需要指定绑定的节点id</param>
        public void Bind(object control, string nodeid)
        {
            MBindUI.Bind(control, this, nodeid);
        }
        /// <summary>
        /// 将新表的行放到原表的下面。
        /// </summary>
        /// <param name="newTable"></param>
        public void Merge(MDataTable newTable)
        {
            if (newTable != null && newTable.Rows.Count > 0)
            {
                int count = newTable.Rows.Count;//提前获取总数，是为了避免dt.Merge(dt);//加载自身导致的死循环。
                for (int i = 0; i < count; i++)
                {
                    // _Rows.Add(newTable.Rows[i]);
                    NewRow(true).LoadFrom(newTable.Rows[i]);
                }
            }
        }
        /// <summary>
        /// 将表里所有行的数据行的状态全部重置
        /// </summary>
        /// <param name="state">状态[0:未更改；1:已赋值,值相同[可插入]；2:已赋值,值不同[可更新]]</param>
        public MDataTable SetState(int state)
        {
            SetState(state, BreakOp.None); return this;
        }
        /// <summary>
        /// 将表里所有行的数据行的状态全部重置
        /// </summary>
        /// <param name="state">状态[0:未更改；1:已赋值,值相同[可插入]；2:已赋值,值不同[可更新]]</param>
        /// <param name="op">状态设置选项</param>
        public MDataTable SetState(int state, BreakOp op)
        {
            if (Rows != null && Rows.Count > 0)
            {
                foreach (MDataRow row in Rows)
                {
                    row.SetState(state, op);
                }
            }
            return this;
        }
        /// <summary>
        /// 转实体列表
        /// </summary>
        /// <param name="useEmit">是否使用Emit方式转换[数据越多[大于500条]性能越高],不写默认自适应判断</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> ToList<T>(params bool[] useEmit)
        {
            List<T> list = new List<T>();
            if (Rows != null && Rows.Count > 0)
            {
                if (((Rows.Count > 500 && useEmit.Length == 0) || (useEmit.Length > 0 && useEmit[0])) && typeof(T).BaseType.Name != "OrmBase")//远程代理实体的属性会变，无法用Emit
                {
                    FastToT<T>.EmitHandle emit = FastToT<T>.Create(this);
                    foreach (MDataRow row in Rows)
                    {
                        list.Add(emit(row));
                    }
                }
                else
                {
                    foreach (MDataRow row in Rows)
                    {
                        list.Add(row.ToEntity<T>());
                    }

                }
            }
            return list;
        }
        internal object ToList(Type t)
        {
            if (t.Name == "MDataTable")
            {
                return this;
            }
            object listObj = Activator.CreateInstance(t);//创建实例
            if (Rows != null && Rows.Count > 0)
            {
                Type[] paraTypeList = null;
                Type listObjType = listObj.GetType();
                ReflectTool.GetArgumentLength(ref listObjType, out paraTypeList);
                MethodInfo method = listObjType.GetMethod("Add");
                foreach (MDataRow row in Rows)
                {
                    method.Invoke(listObj, new object[] { row.ToEntity(paraTypeList[0]) });
                }
            }
            return listObj;
        }
        /// <summary>
        /// 批量插入或更新 [提示：操作和当前表名有关，如当前表名不是要提交入库的表名,请给TableName属性重新赋值]
        /// </summary>
        /// <param name="op">操作选项[插入|更新]</param>
        /// <returns>返回false时，若有异常，存在：DynamicData 参数中</returns>
        public bool AcceptChanges(AcceptOp op)
        {
            return AcceptChanges(op, string.Empty);
        }

        /// <param name="op">操作选项[插入|更新]</param>
        /// <param name="newConn">指定新的数据库链接</param>
        /// <param name="jointPrimaryKeys">AcceptOp为Update或Auto时，若需要设置联合主键为唯一检测或更新条件，则可设置多个字段名</param>
        /// <returns>返回false时，若有异常，存在：DynamicData 参数中</returns>
        public bool AcceptChanges(AcceptOp op, string newConn, params object[] jointPrimaryKeys)
        {
            bool result = false;
            if (Columns.Count == 0 || Rows.Count == 0)
            {
                return false;//木有可更新的。
            }
            MDataTableBatchAction action = new MDataTableBatchAction(this, newConn);
            if ((op & AcceptOp.Truncate) != 0)
            {
                action.IsTruncate = true;
                op = (AcceptOp)(op - AcceptOp.Truncate);
            }
            action.SetJoinPrimaryKeys(jointPrimaryKeys);
            switch (op)
            {
                case AcceptOp.Insert:
                    result = action.Insert(false);
                    break;
                case AcceptOp.InsertWithID:
                    result = action.Insert(true);
                    break;
                case AcceptOp.Update:
                    result = action.Update();
                    break;
                case AcceptOp.Delete:
                    result = action.Delete();
                    break;
                case AcceptOp.Auto:
                    result = action.Auto();
                    break;
            }
            if (result && AppConfig.Cache.IsAutoCache)
            {
                //取消AOP缓存。
                AutoCache.ReadyForRemove(AutoCache.GetBaseKey(TableName, newConn));
            }
            return result;
        }
        /// <summary>
        /// 获取修改过的数据
        /// </summary>
        /// <returns></returns>
        public MDataTable GetChanges()
        {
            return GetChanges(RowOp.Update);
        }
        /// <summary>
        /// 获取修改过的数据(若无修改，则返回Null）
        /// </summary>
        /// <param name="rowOp">仅Insert和Update选项可用</param>
        /// <returns></returns>
        public MDataTable GetChanges(RowOp rowOp)
        {
            MDataTable dt = new MDataTable(_TableName);
            dt.Columns = Columns;
            dt.Conn = Conn;
            dt.DynamicData = DynamicData;
            dt.joinOnIndex = joinOnIndex;
            dt.JoinOnName = dt.JoinOnName;
            dt.RecordsAffected = RecordsAffected;
            if (this.Rows.Count > 0)
            {
                if (rowOp == RowOp.Insert || rowOp == RowOp.Update)
                {
                    int stateValue = (int)rowOp;
                    foreach (MDataRow row in Rows)
                    {
                        if (row.GetState() >= stateValue)
                        {
                            dt.Rows.Add(row, false);
                        }
                    }
                }
            }
            return dt;
        }
        /// <summary>
        /// 返回某列的集合
        /// <param name="columnName">列名</param>
        /// </summary>
        public List<T> GetColumnItems<T>(string columnName)
        {
            return GetColumnItems<T>(columnName, BreakOp.None, false);
        }
        /// <summary>
        /// 返回某列的集合
        /// <param name="columnName">列名</param>
        /// <param name="op">参数选项</param>
        /// </summary>
        public List<T> GetColumnItems<T>(string columnName, BreakOp op)
        {
            return GetColumnItems<T>(columnName, op, false);
        }
        /// <summary>
        /// 返回某列的集合
        /// </summary>
        /// <typeparam name="T">列的类型</typeparam>
        /// <param name="columnIndex">列名</param>
        /// <param name="op">过滤选项</param>
        /// <param name="isDistinct">是否去掉重复数据</param>
        public List<T> GetColumnItems<T>(string columnName, BreakOp op, bool isDistinct)
        {
            int index = -1;
            if (Columns != null)
            {
                index = Columns.GetIndex(columnName);
            }
            return GetColumnItems<T>(index, op, isDistinct);
        }
        /// <summary>
        /// 返回某列的集合
        /// </summary>
        /// <typeparam name="T">列的类型</typeparam>
        /// <param name="columnIndex">第N列</param>
        public List<T> GetColumnItems<T>(int columnIndex)
        {
            return GetColumnItems<T>(columnIndex, BreakOp.None);
        }

        /// <summary>
        /// 返回某列的集合
        /// </summary>
        /// <typeparam name="T">列的类型</typeparam>
        /// <param name="columnIndex">第N列</param>
        /// <param name="op">过滤选项</param>
        /// <returns></returns>
        public List<T> GetColumnItems<T>(int columnIndex, BreakOp op)
        {
            return GetColumnItems<T>(columnIndex, op, false);
        }
        /// <summary>
        /// 返回某列的集合
        /// </summary>
        /// <typeparam name="T">列的类型</typeparam>
        /// <param name="columnIndex">第N列</param>
        /// <param name="op">过滤选项</param>
        /// <param name="isDistinct">是否去掉重复数据</param>
        public List<T> GetColumnItems<T>(int columnIndex, BreakOp op, bool isDistinct)
        {
            List<T> items = new List<T>();
            if (Columns != null && Rows != null && Rows.Count > 0)
            {
                if (columnIndex > -1)
                {
                    MDataCell cell;
                    foreach (MDataRow row in Rows)
                    {
                        cell = row[columnIndex];
                        switch (op)
                        {
                            case BreakOp.Null:
                                if (cell.IsNull)
                                {
                                    continue;
                                }
                                break;
                            case BreakOp.Empty:
                                if (cell.StringValue == "")
                                {
                                    continue;
                                }
                                break;
                            case BreakOp.NullOrEmpty:
                                if (cell.IsNullOrEmpty)
                                {
                                    continue;
                                }
                                break;
                        }
                        T value = cell.Get<T>();// row.Get<T>(columnIndex, default(T));
                        if (!isDistinct || !items.Contains(value))
                        {
                            items.Add(value);
                        }
                    }
                }
                else
                {
                    Error.Throw(string.Format("Table {0} can not find the column", TableName));
                }
            }
            return items;
        }
        /// <summary>
        /// 复制表
        /// </summary>
        public MDataTable Clone()
        {
            MDataTable newTable = GetSchema(true);
            newTable.Conn = Conn;
            newTable.DynamicData = DynamicData;
            newTable.RecordsAffected = RecordsAffected;
            newTable.TableName = TableName;
            if (_Rows.Count > 0)
            {
                foreach (MDataRow oldRow in _Rows)
                {
                    MDataRow newRow = newTable.NewRow();
                    newRow.LoadFrom(oldRow);
                    newTable.Rows.Add(newRow, false);
                }
            }
            return newTable;
        }
        /// <summary>
        /// 复制表的结构
        /// </summary>
        /// <param name="clone">是否克隆表结构</param>
        /// <returns></returns>
        public MDataTable GetSchema(bool clone)
        {
            MDataTable newTable = new MDataTable(_TableName);
            if (Columns.Count > 0)
            {
                newTable.Columns = clone ? Columns.Clone() : Columns;
            }
            newTable.Conn = Conn;
            return newTable;
        }
        /// <summary>
        /// 过滤掉重复的数据行（仅比较基础类型、复杂数据类型仅比较类型名称）。
        /// <param name="filterRows">被过滤的数据集表</param>
        /// </summary>
        public void Distinct(out MDataTable filterRows)
        {
            filterRows = null;
            if (Rows.Count > 0)
            {
                List<MDataRow> rowList = new List<MDataRow>();
                int cCount = Columns.Count;
                for (int i = 0; i < Rows.Count; i++)
                {
                    for (int j = Rows.Count - 1; j >= 0 && j != i; j--)//反序检测。
                    {
                        int eqCount = 0;
                        for (int k = 0; k < cCount; k++)//比较列
                        {
                            if (Rows[i][k].StringValue == Rows[j][k].StringValue)
                            {
                                eqCount++;
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (eqCount == cCount)
                        {
                            rowList.Add(Rows[j]);
                            Rows.RemoveAt(j);
                        }
                    }
                }
                if (rowList.Count > 0)
                {
                    filterRows = rowList;
                }
            }
        }
        /// <summary>
        /// 过滤掉重复的数据行（仅比较基础类型、复杂数据类型仅比较类型名称）。
        /// </summary>
        public void Distinct()
        {
            MDataTable filterRows;
            Distinct(out filterRows);
            filterRows = null;
        }
        #endregion

        public override string ToString()
        {
            return TableName;
        }
    }

    public partial class MDataTable : IDataReader, IEnumerable//, IEnumerator
    {
        private int _Ptr = -1;//行索引
        #region IDataRecord 成员
        /// <summary>
        /// 获取列的总数
        /// </summary>
        int IDataRecord.FieldCount
        {
            get
            {
                if (Columns != null)
                {
                    return Columns.Count;
                }
                return 0;
            }
        }

        bool IDataRecord.GetBoolean(int i)
        {
            return (bool)_Rows[_Ptr][i].Value;
        }

        byte IDataRecord.GetByte(int i)
        {
            return (byte)_Rows[_Ptr][i].Value;
        }

        long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            return (byte)_Rows[_Ptr][i].Value;
        }

        char IDataRecord.GetChar(int i)
        {
            return (char)_Rows[_Ptr][i].Value;
        }

        long IDataRecord.GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            return (char)_Rows[_Ptr][i].Value;
        }

        IDataReader IDataRecord.GetData(int i)
        {
            return this;
        }

        string IDataRecord.GetDataTypeName(int i)
        {
            return "";//绑定的可以不需要返回
            //return DataType.GetDbType(Columns[i].SqlType.ToString()).ToString();
        }

        DateTime IDataRecord.GetDateTime(int i)
        {
            return (DateTime)_Rows[_Ptr][i].Value;
        }

        decimal IDataRecord.GetDecimal(int i)
        {
            return (decimal)_Rows[_Ptr][i].Value;
        }

        double IDataRecord.GetDouble(int i)
        {
            return (double)_Rows[_Ptr][i].Value;
        }

        Type IDataRecord.GetFieldType(int i)
        {
            return _Columns[i].ValueType;
        }

        float IDataRecord.GetFloat(int i)
        {
            return (float)_Rows[_Ptr][i].Value;
        }

        Guid IDataRecord.GetGuid(int i)
        {
            return (Guid)_Rows[_Ptr][i].Value;
        }

        short IDataRecord.GetInt16(int i)
        {
            return (short)_Rows[_Ptr][i].Value;
        }

        int IDataRecord.GetInt32(int i)
        {
            return (int)_Rows[_Ptr][i].Value;
        }

        long IDataRecord.GetInt64(int i)
        {
            return (long)_Rows[_Ptr][i].Value;
        }

        string IDataRecord.GetName(int i)
        {
            return _Columns[i].ColumnName;
        }

        int IDataRecord.GetOrdinal(string name)
        {
            return _Columns.GetIndex(name);
        }

        string IDataRecord.GetString(int i)
        {
            return Convert.ToString(_Rows[_Ptr][i].Value);
        }

        object IDataRecord.GetValue(int i)
        {
            return _Rows[_Ptr][i].Value;
        }

        int IDataRecord.GetValues(object[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = _Rows[_Ptr][i].Value;
            }
            return values.Length;
        }

        bool IDataRecord.IsDBNull(int i)
        {
            return _Rows[_Ptr][i].IsNull;
        }

        object IDataRecord.this[string name]
        {
            get
            {
                return _Rows[_Ptr][name];
            }
        }

        object IDataRecord.this[int i]
        {
            get
            {
                return _Rows[i];
            }
        }

        #endregion

        #region IDataReader 成员
        /// <summary>
        /// 清除所有行
        /// </summary>
        void IDataReader.Close()
        {
            _Rows.Clear();
        }
        /// <summary>
        /// 获取数据行总数
        /// </summary>
        int IDataReader.Depth
        {
            get
            {
                if (_Rows != null)
                {
                    return _Rows.Count;
                }
                return 0;
            }
        }

        DataTable IDataReader.GetSchemaTable()
        {
            return ToDataTable();
        }
        /// <summary>
        /// 是否已读取完所所有数据，并清空了记录。
        /// </summary>
        bool IDataReader.IsClosed
        {
            get
            {
                return _Rows.Count == 0 && _Ptr >= _Rows.Count - 1;
            }
        }

        /// <summary>
        /// 是否还有下一条数据
        /// </summary>
        /// <returns></returns>
        bool IDataReader.NextResult()
        {
            return _Ptr < _Rows.Count - 1;
        }
        /// <summary>
        /// 索引移到下一条，准备进行读取。
        /// </summary>
        bool IDataReader.Read()
        {
            if (_Ptr < _Rows.Count - 1)
            {
                _Ptr++;
                return true;
            }
            else
            {
                _Ptr = -1;
                return false;
            }
        }

        private int _RecordsAffected;
        /// <summary>
        /// 返回（查询时）记录总数。
        /// </summary>
        public int RecordsAffected
        {
            get
            {
                if (_RecordsAffected == 0)
                {
                    return _Rows.Count;
                }
                return _RecordsAffected;
            }
            set
            {
                _RecordsAffected = value;
            }
        }

        #endregion

        #region IDisposable 成员

        void IDisposable.Dispose()
        {
            _Rows.Clear();
            _Rows = null;
        }

        #endregion

        #region IEnumerable 成员

        IEnumerator IEnumerable.GetEnumerator()
        {
            //for (int i = 0; i < Rows.Count; i++)
            //{
            //    yield return Rows[i];
            //}
            return new System.Data.Common.DbEnumerator(this);
        }

        #endregion

    }

    public partial class MDataTable
    {
        #region 静态方法 CreateFrom

        /// <summary>
        /// 不关闭Sdr（因为外部MProc.ExeMDataTableList还需要使用）
        /// </summary>
        /// <param name="sdr"></param>
        /// <returns></returns>
        internal static MDataTable CreateFrom(DbDataReader sdr)
        {
            if (sdr != null && sdr is NoSqlDataReader)
            {
                return ((NoSqlDataReader)sdr).ResultTable;
            }
            MDataTable mTable = new MDataTable("SysDefault");
            if (sdr != null && sdr.FieldCount > 0)
            {

                DataTable dt = null;
                bool noSchema = OracleDal.clientType == 0;
                if (!noSchema)
                {
                    try
                    {
                        dt = sdr.GetSchemaTable();
                    }
                    catch { noSchema = true; }
                }
                #region 读表结构
                if (noSchema) //OracleClient 不支持子查询的GetSchemaTable，但ODP.NET是支持的。
                {
                    //从DataReader读取表结构，不管有没有数据。
                    string hiddenFields = "," + AppConfig.DB.HiddenFields.ToLower() + ",";
                    MCellStruct mStruct;
                    for (int i = 0; i < sdr.FieldCount; i++)
                    {
                        string name = sdr.GetName(i);
                        if (string.IsNullOrEmpty(name))
                        {
                            name = "Empty_" + i;
                        }
                        bool isHiddenField = hiddenFields.IndexOf("," + name + ",", StringComparison.OrdinalIgnoreCase) > -1;
                        if (!isHiddenField)
                        {
                            mStruct = new MCellStruct(name, DataType.GetSqlType(sdr.GetFieldType(i)));
                            mStruct.ReaderIndex = i;
                            mTable.Columns.Add(mStruct);
                        }
                    }
                }
                else if (dt != null && dt.Rows.Count > 0)
                {
                    mTable.Columns = TableSchema.GetColumnByTable(dt, sdr, false);
                    mTable.Columns.DataBaseType = DalCreate.GetDalTypeByReaderName(sdr.GetType().Name);
                }
                #endregion
                if (sdr.HasRows)
                {
                    MDataRow mRecord = null;
                    List<int> errIndex = new List<int>();//SQLite提供的dll不靠谱，sdr[x]类型转不过时，会直接抛异常
                    while (sdr.Read())
                    {
                        #region 读数据行
                        mRecord = mTable.NewRow(true);

                        for (int i = 0; i < mTable.Columns.Count; i++)
                        {
                            MCellStruct ms = mTable.Columns[i];
                            object value = null;
                            try
                            {
                                if (errIndex.Contains(i))
                                {
                                    value = sdr.GetString(ms.ReaderIndex);
                                }
                                else
                                {
                                    value = sdr[ms.ReaderIndex];
                                }
                            }
                            catch
                            {
                                if (!errIndex.Contains(i))
                                {
                                    errIndex.Add(i);
                                }
                                value = sdr.GetString(ms.ReaderIndex);
                            }

                            if (value == null || value == DBNull.Value)
                            {
                                mRecord[i].Value = DBNull.Value;
                                mRecord[i].State = 0;
                            }
                            else if (Convert.ToString(value) == string.Empty)
                            {
                                mRecord[i].Value = string.Empty;
                                mRecord[i].State = 1;
                            }
                            else
                            {
                                mRecord[i].Value = value; //sdr.GetValue(i); 用此赋值，内部会进行类型转换。
                                mRecord[i].State = 1;//初始始状态为1
                            }

                        }
                        #endregion
                    }
                }
            }
            return mTable;
        }
        /// <summary>
        /// 从List列表里加载成MDataTable
        /// </summary>
        /// <param name="entityList">实体列表对象</param>
        /// <returns></returns>
        public static MDataTable CreateFrom(object entityList, BreakOp op)
        {
            if (entityList is MDataView)//为了处理UI绑定的表格双击再弹表格
            {
                return ((MDataView)entityList).Table;
            }
            MDataTable dt = new MDataTable();
            if (entityList != null)
            {
                try
                {
                    bool isObj = true;
                    Type t = entityList.GetType();
                    dt.TableName = t.Name;
                    if (t.IsGenericType)
                    {
                        #region 处理列头
                        Type[] types;
                        int len = ReflectTool.GetArgumentLength(ref t, out types);
                        if (len == 2)//字典
                        {
                            dt.Columns.Add("Key", DataType.GetSqlType(types[0]));
                            dt.Columns.Add("Value", DataType.GetSqlType(types[1]));
                        }
                        else
                        {
                            Type objType = types[0];
                            if ((objType.FullName.StartsWith("System.") && objType.FullName.Split('.').Length == 2) || objType.IsEnum)//系统类型。
                            {
                                isObj = false;
                                string name = objType.Name.Split('`')[0];
                                if (name.StartsWith("Nullable"))
                                {
                                    name = Nullable.GetUnderlyingType(objType).Name;
                                }
                                dt.Columns.Add(name, DataType.GetSqlType(objType), false);
                            }
                            else
                            {
                                dt.TableName = objType.Name;
                                dt.Columns = TableSchema.GetColumnByType(objType);
                            }
                        }
                        #endregion
                    }
                    else if (entityList is Hashtable)
                    {
                        dt.Columns.Add("Key");
                        dt.Columns.Add("Value");
                    }
                    else if (entityList is DbParameterCollection)
                    {
                        dt.Columns = TableSchema.GetColumnByType(typeof(DbParameter));
                    }
                    else
                    {
                        if (entityList is IEnumerable)
                        {
                            isObj = false;
                            dt.Columns.Add(t.Name.Replace("[]", ""), SqlDbType.Variant, false);
                        }
                        else
                        {
                            MDataRow row = MDataRow.CreateFrom(entityList);
                            if (row != null)
                            {
                                return row.Table;
                            }
                        }
                    }
                    foreach (object o in entityList as IEnumerable)
                    {
                        MDataRow row = dt.NewRow();
                        if (isObj)
                        {
                            row.LoadFrom(o, op);//初始值状态为1
                        }
                        else
                        {
                            row.Set(0, o, 1);
                        }
                        dt.Rows.Add(row);
                    }
                }
                catch (Exception err)
                {
                    Log.Write(err, LogType.Error);
                }
            }
            return dt;
        }
        public static MDataTable CreateFrom(object entityList)
        {
            return CreateFrom(entityList, BreakOp.None);
        }
        public static MDataTable CreateFrom(NameObjectCollectionBase noc)
        {
            MDataTable dt = new MDataTable();
            if (noc != null)
            {
                if (noc is NameValueCollection)
                {
                    dt.TableName = "NameValueCollection";
                    dt.Columns.Add("Key,Value");
                    NameValueCollection nv = noc as NameValueCollection;
                    foreach (string key in nv)
                    {
                        dt.NewRow(true).Sets(0, key, nv[key]);
                    }
                }
                else if (noc is HttpCookieCollection)
                {
                    dt.TableName = "HttpCookieCollection";
                    HttpCookieCollection nv = noc as HttpCookieCollection;
                    dt.Columns.Add("Name,Value,HttpOnly,Domain,Expires,Path");
                    for (int i = 0; i < nv.Count; i++)
                    {
                        HttpCookie cookie = nv[i];
                        dt.NewRow(true).Sets(0, cookie.Name, cookie.Value, cookie.HttpOnly, cookie.Domain, cookie.Expires, cookie.Path);
                    }
                }
                else
                {
                    dt = CreateFrom(noc as IEnumerable);
                }
            }
            return dt;
        }

        /// <summary>
        /// 从Json或Xml字符串反加载成MDataTable
        /// </summary>
        public static MDataTable CreateFrom(string jsonOrXml)
        {
            return CreateFrom(jsonOrXml, null);
        }
        public static MDataTable CreateFrom(string jsonOrXml, MDataColumn mdc)
        {
            return CreateFrom(jsonOrXml, mdc, JsonHelper.DefaultEscape);
        }
        /// <summary>
        /// 从Json或Xml字符串反加载成MDataTable
        /// </summary>
        public static MDataTable CreateFrom(string jsonOrXml, MDataColumn mdc, EscapeOp op)
        {
            if (!string.IsNullOrEmpty(jsonOrXml))
            {
                if (jsonOrXml[0] == '<' || jsonOrXml.EndsWith(".xml"))
                {
                    return CreateFromXml(jsonOrXml, mdc);
                }
                else
                {
                    return JsonHelper.ToMDataTable(jsonOrXml, mdc, op);
                }
            }
            return new MDataTable();
        }
        internal static MDataTable CreateFromXml(string xmlOrFileName, MDataColumn mdc)
        {
            MDataTable dt = new MDataTable();
            if (mdc != null)
            {
                dt.Columns = mdc;
            }
            if (string.IsNullOrEmpty(xmlOrFileName))
            {
                return dt;
            }
            xmlOrFileName = xmlOrFileName.Trim();
            XmlDocument doc = new XmlDocument();
            bool loadOk = false;
            if (!xmlOrFileName.StartsWith("<"))//可能是文件路径
            {
                dt.TableName = Path.GetFileNameWithoutExtension(xmlOrFileName);
                dt.Columns = MDataColumn.CreateFrom(xmlOrFileName, false);
                if (File.Exists(xmlOrFileName))
                {
                    try
                    {
                        doc.Load(xmlOrFileName);
                        loadOk = true;
                    }
                    catch (Exception err)
                    {
                        Log.Write(err, LogType.Error);
                    }
                }
            }
            else  // xml 字符串
            {
                try
                {
                    doc.LoadXml(xmlOrFileName);
                    loadOk = true;
                }
                catch (Exception err)
                {
                    Log.Write(err, LogType.Error);
                }
            }
            if (loadOk)
            {
                if (doc.DocumentElement.ChildNodes.Count > 0)
                {
                    dt.TableName = doc.DocumentElement.Name;
                    if (dt.Columns.Count == 0)
                    {
                        //初如化表架构
                        bool useChildToGetSchema = doc.DocumentElement.ChildNodes[0].ChildNodes.Count > 0;
                        foreach (XmlNode item in doc.DocumentElement.ChildNodes)
                        {
                            if (useChildToGetSchema)
                            {
                                if (item.ChildNodes.Count > 0)//带子节点,用子节点的名称当字段
                                {
                                    foreach (XmlNode child in item.ChildNodes)
                                    {
                                        if (!dt.Columns.Contains(child.Name))
                                        {
                                            dt.Columns.Add(child.Name);
                                        }
                                    }
                                }
                            }
                            else//不带子节点，用当前节点的属性当字段
                            {
                                if (item.Attributes != null && item.Attributes.Count > 0)//带子节点,用子节点的名称当字段
                                {
                                    foreach (XmlAttribute attr in item.Attributes)
                                    {
                                        if (!dt.Columns.Contains(attr.Name))
                                        {
                                            dt.Columns.Add(attr.Name);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                MDataRow dr = null;
                foreach (XmlNode row in doc.DocumentElement.ChildNodes)
                {
                    dr = dt.NewRow();
                    if (row.ChildNodes.Count > 0)//用子节点处理
                    {
                        foreach (XmlNode cell in row.ChildNodes)
                        {
                            if (!cell.InnerXml.StartsWith("<![CDATA["))
                            {
                                dr.Set(cell.Name, cell.InnerXml.Trim(), 1);
                            }
                            else
                            {
                                dr.Set(cell.Name, cell.InnerText.Trim(), 1);
                            }
                        }
                        dt.Rows.Add(dr);
                    }
                    else if (row.Attributes != null && row.Attributes.Count > 0) //用属性处理
                    {
                        foreach (XmlAttribute cell in row.Attributes)
                        {
                            dr.Set(cell.Name, cell.Value.Trim(), 1);
                        }
                        dt.Rows.Add(dr);
                    }

                }
            }
            return dt;
        }
        #endregion

        #region 列的取值：Min、Max、Sum、Avg
        private T GetMinMaxValue<T>(int index, bool isMin)
        {
            if (Columns != null && index < Columns.Count && Rows != null && Rows.Count > 0)
            {
                List<T> itemList = GetColumnItems<T>(index, BreakOp.NullOrEmpty);
                if (itemList.Count > 0)
                {
                    itemList.Sort();
                    return isMin ? itemList[0] : itemList[itemList.Count - 1];
                }
            }

            return default(T);
        }
        /// <summary>
        /// 获取列的最小值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="columnName">列名</param>
        /// <returns></returns>
        public T Min<T>(string columnName)
        {
            return GetMinMaxValue<T>(Columns.GetIndex(columnName), true);
        }
        /// <summary>
        /// 获取列的最小值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="index">列索引</param>
        /// <returns></returns>
        public T Min<T>(int index)
        {
            return GetMinMaxValue<T>(index, true);
        }

        /// <summary>
        /// 获取列的最大值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="columnName">列名</param>
        /// <returns></returns>
        public T Max<T>(string columnName)
        {
            return GetMinMaxValue<T>(Columns.GetIndex(columnName), false);
        }
        /// <summary>
        /// 获取列的最大值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="index">列索引</param>
        /// <returns></returns>
        public T Max<T>(int index)
        {
            return GetMinMaxValue<T>(index, false);
        }
        /// <summary>
        /// 汇总某列的值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="columnName">列名</param>
        /// <returns></returns>
        public T Sum<T>(string columnName)
        {
            return Sum<T>(Columns.GetIndex(columnName));
        }

        /// <summary>
        /// 汇总某列的值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="index">列索引</param>
        /// <returns></returns>
        public T Sum<T>(int index)
        {
            if (Columns != null && index < Columns.Count && Rows != null && Rows.Count > 0)
            {
                List<Decimal> itemList = GetColumnItems<Decimal>(index, BreakOp.NullOrEmpty);
                if (itemList.Count > 0)
                {
                    Decimal sum = 0;
                    for (int i = 0; i < itemList.Count; i++)
                    {
                        sum += itemList[i];
                    }
                    return (T)ConvertTool.ChangeType(sum, typeof(T));
                }
            }
            return default(T);
        }
        /// <summary>
        /// 记算某列的平均值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="columnName">列名</param>
        /// <returns></returns>
        public T Avg<T>(string columnName)
        {
            return Avg<T>(Columns.GetIndex(columnName));
        }

        /// <summary>
        ///记算某列的平均值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="index">列索引</param>
        /// <returns></returns>
        public T Avg<T>(int index)
        {
            Decimal sum = Sum<Decimal>(index);
            if (sum > 0)
            {
                return (T)ConvertTool.ChangeType(sum / Rows.Count, typeof(T));
            }
            return default(T);
        }

        /// <summary>
        /// 行转换列（不指定时，默认取最后三列处理）
        /// </summary>
        public MDataTable Pivot()
        {
            if (Columns.Count < 3)
            {
                Error.Throw("At least three columns when call Pivot()");
            }
            int count = Columns.Count;
            return Pivot(Columns[count - 3].ColumnName, Columns[count - 2].ColumnName, Columns[count - 1].ColumnName);
        }
        /// <summary>
        /// 行转换列
        /// </summary>
        /// <param name="rowName">用于指定行的列名</param>
        /// <param name="colName">用于分拆成列的列名</param>
        /// <param name="valueName">用于显示值的列名</param>
        /// <returns></returns>
        public MDataTable Pivot(string rowName, string colName, string valueName)
        {
            MDataTable dt = new MDataTable(TableName);

            #region 处理列头
            List<string> colNameItems = GetColumnItems<string>(colName, BreakOp.NullOrEmpty, true);
            if (colNameItems == null || colNameItems.Count == 0 || colNameItems.Count > 255)
            {
                return dt;
            }
            dt.Columns.Add(rowName);
            for (int i = 0; i < colNameItems.Count; i++)
            {
                dt.Columns.Add(colNameItems[i]);
            }

            #endregion

            #region 处理数据
            List<string> rowNameItems = GetColumnItems<string>(rowName, BreakOp.None, true);
            MDataTable splitTable = this;
            for (int i = 0; i < rowNameItems.Count; i++)
            {
                MDataRow nameRow = dt.NewRow(true).Set(0, rowNameItems[i]);//新表的一行
                MDataTable[] dt2 = splitTable.Split(rowName + "='" + rowNameItems[i] + "'");//筛选分隔
                splitTable = dt2[1];//剩下的作为下次分隔

                foreach (MDataRow row in dt2[0].Rows)//填写数据
                {
                    if (!row[colName].IsNullOrEmpty)//列名不能为空或Null
                    {
                        nameRow.Set(row[colName].Value, row[valueName].Value);
                    }
                }
            }
            #endregion
            return dt;
        }
        #endregion

        //给MDataTableBatchAction的批量更新使用。
        internal void Load(MDataTable dt, MCellStruct primary)
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                return;
            }
            //if (Rows.Count == dt.Rows.Count)
            //{
            //    for (int i = 0; i < Rows.Count; i++)
            //    {
            //        Rows[i].LoadFrom(dt.Rows[i], RowOp.IgnoreNull, false);
            //    }
            //}
            //else
            //{
            string pkName = primary != null ? primary.ColumnName : Columns.FirstPrimary.ColumnName;
            int i1 = Columns.GetIndex(pkName);
            MDataRow rowA, rowB;

            for (int i = 0; i < Rows.Count; i++)
            {
                rowA = Rows[i];
                rowB = dt.FindRow(pkName + "='" + rowA[i1].StringValue + "'");
                if (rowB != null)
                {
                    rowA.LoadFrom(rowB, RowOp.Update, false);
                    dt.Rows.Remove(rowB);
                }
            }
            // }
        }

        #region 注释掉代码
        /*
         * 
           /// <summary>
        /// 从List列表里加载成MDataTable
        /// </summary>
        /// <param name="entityList">实体列表</param>
        /// <returns></returns>
        public static MDataTable LoadFromList<T>(List<T> entityList) where T : class
        {
            MDataTable dt = new MDataTable("Default");
            if (entityList != null && entityList.Count > 0)
            {
                dt.Columns = SchemaCreate.GetColumns(entityList[0].GetType());
                //生成表结构。
                foreach (T entity in entityList)
                {
                    MDataRow row = dt.NewRow();
                    row.LoadFrom(entity);
                    dt.Rows.Add(row);
                }
            }
            return dt;
        }
        /// <summary>
        /// 快速创建表架构（无id列名时，系统自动创建自增型的id列到首列。）
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="overwrite">指定的文件存在时，是否允许复盖</param>
        /// <param name="columnNames">创建的列名</param>
        /// <param name="sqlDbTypes">列名对应的数据类型（不指定则默认为nvarchar）</param>
        public static void CreateSchema(string fileName, bool overwrite, string[] columnNames, params SqlDbType[] sqlDbTypes)
        {
            if (columnNames.Length >= 0)
            {
                if (fileName[1] != ':')
                {
                    fileName = AppConfig.WebRootPath + fileName;
                }
                fileName = fileName.Replace(Path.GetExtension(fileName), string.Empty) + ".ts";
                if (!File.Exists(fileName) || overwrite)
                {
                    MDataColumn mdc = new MDataColumn();
                    string columnName = string.Empty;
                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        columnName = columnNames[i];
                        if (sqlDbTypes != null && sqlDbTypes.Length > i)
                        {
                            mdc.Add(columnName, sqlDbTypes[i]);
                        }
                        else
                        {
                            mdc.Add(columnName);
                        }
                    }
                    if (mdc[0].ColumnName.ToLower() != "id")
                    {
                        MCellStruct cellStruct = new MCellStruct("id", SqlDbType.Int, true, false, -1);
                        cellStruct.IsPrimaryKey = true;
                        mdc.Insert(0, cellStruct);
                    }
                    else if (mdc[0].SqlType == SqlDbType.Int)
                    {
                        mdc[0].IsAutoIncrement = true;
                        mdc[0].IsPrimaryKey = true;
                    }
                    mdc.WriteSchema(fileName);
                }
            }
        } 
         * */
        #endregion
    }
}
