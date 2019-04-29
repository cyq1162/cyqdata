using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;
using CYQ.Data.SQL;

using System.IO;
using CYQ.Data.Tool;


namespace CYQ.Data
{
    internal class NoSqlAction : IDisposable
    {
        /// <summary>
        /// 重置静态变量（方便回收内存，避免大量导数据后内存不回收）
        /// </summary>
        internal static void ResetStaticVar()
        {
            _tableList.Clear();
            _tableList = null;//静态字典置为Null才能释放内存。
            _tableList = new MDictionary<string, MDataTable>(5, StringComparer.OrdinalIgnoreCase);//重新初始化。
            //_needToSaveState.Clear();
            //_lockNextidObj.Clear();
            //_maxid.Clear();
        }
        private static MDictionary<string, MDataTable> _tableList = new MDictionary<string, MDataTable>(5, StringComparer.OrdinalIgnoreCase);//内存数据库
        // private static readonly object _lockTableListObj = new object();
        /// <summary>
        ///  是否需要更新：0未更新；1仅插入[往后面插数据]；2更新删除或插入[重新保存],//需要更新[全局的可以有效处理并发]
        /// </summary>
        private static MDictionary<string, int> _needToSaveState = new MDictionary<string, int>(5, StringComparer.OrdinalIgnoreCase);
        private static MDictionary<string, object> _lockNextIDObj = new MDictionary<string, object>(5, StringComparer.OrdinalIgnoreCase);//自增加id锁
        private static MDictionary<string, int> _maxID = new MDictionary<string, int>(5, StringComparer.OrdinalIgnoreCase);//当前表的最大id
        private static MDictionary<string, DateTime> _lastWriteTimeList = new MDictionary<string, DateTime>(5, StringComparer.OrdinalIgnoreCase);//当前表的最大id
        // private static MDictionary<string, object> _lockOperatorObj = new MDictionary<string, object>(5, StringComparer.OrdinalIgnoreCase);//增删改时的互锁
        private List<MDataRow> _insertRows = new List<MDataRow>();//新插入的集合，仅是引用MDataTable的索引
        private static List<string> _isDeleteAll = new List<string>(5);

        /// <summary>
        /// 最后的写入时间
        /// </summary>
        // private static DateTime _lastWriteTimeUtc = DateTime.UtcNow;
        private MDataTable _Table = null;
        private MDataTable Table
        {
            get
            {
                if (_Table != null && _Table.Rows.Count > 0)
                {
                    return _Table;
                }
                else if (_tableList.ContainsKey(_FileFullName))
                {
                    _Table = _tableList[_FileFullName];
                    if (_Table.Rows.Count == 0 && !_isDeleteAll.Contains(_FileFullName))
                    {
                        if (_maxID.ContainsKey(_FileFullName))
                        {
                            _maxID.Remove(_FileFullName);
                        }
                        _tableList.Remove(_FileFullName);// 会引发最后一条数据无法删除的问题(已修正该问题，所以可开放)。
                    }
                    else
                    {
                        return _Table;
                    }
                }

                switch (_DalType)
                {
                    case DataBaseType.Txt:
                        _Table = MDataTable.CreateFrom(_FileFullName, _Row.Columns, EscapeOp.Encode);
                        break;
                    case DataBaseType.Xml:
                        _Table = MDataTable.CreateFromXml(_FileFullName, _Row.Columns);
                        break;
                }
                if (_Table == null || _Table.Columns.Count == 0)
                {
                    Error.Throw("MDataTable can't load data from file : " + _FileFullName);
                }
                //行修正，有可能json的某些列数据为Null
                //                foreach (MCellStruct rowST in _Row.Columns)
                //                {
                //                    foreach (MCellStruct tableST in _Table.Columns)
                //{

                //}
                //                    if (!_Table.Columns.Contains(cst.ColumnName))
                //                    {
                //                        _Table.Columns.Add(cst);
                //                    }
                //                    else if(cst.SqlType!=_Table.col
                //                    {

                //                    }
                //                }
                DateTime _lastWriteTimeUtc = new IOInfo(_FileFullName).LastWriteTimeUtc;
                if (!_lastWriteTimeList.ContainsKey(_FileFullName))
                {
                    _lastWriteTimeList.Add(_FileFullName, _lastWriteTimeUtc);
                }
                if (_Table.Rows.Count > 0)
                {
                    //lock (_lockTableListObj)
                    //{
                    if (!_tableList.ContainsKey(_FileFullName))
                    {
                        _tableList.Add(_FileFullName, _Table);
                    }
                    // }
                }
                return _Table;

            }
        }

        private int maxID
        {
            get
            {
                if (!_maxID.ContainsKey(_FileFullName))
                {
                    _maxID.Add(_FileFullName, 0);
                }
                return _maxID[_FileFullName];
            }
            set
            {
                _maxID[_FileFullName] = value;
            }
        }
        private object lockNextIDObj
        {
            get
            {
                if (!_lockNextIDObj.ContainsKey(_FileFullName))
                {
                    _lockNextIDObj.Add(_FileFullName, new object());
                }
                return _lockNextIDObj[_FileFullName];
            }
        }
        private int needToSaveState
        {
            get
            {
                if (!_needToSaveState.ContainsKey(_FileFullName))
                {
                    _needToSaveState.Add(_FileFullName, 0);
                }
                return _needToSaveState[_FileFullName];
            }

            set
            {
                _needToSaveState[_FileFullName] = value;
            }
        }

        /// <summary>
        /// 下一个自增加id
        /// </summary>
        private int NextID
        {
            get
            {
                lock (lockNextIDObj)
                {
                    if (maxID > 0)
                    {
                        maxID++;
                    }
                    else if (DataType.GetGroup(Table.Columns.FirstPrimary.SqlType) == 1)//自增id仅对int有效
                    {
                        try
                        {
                            if (Table.Rows.Count > 0)
                            {
                                #region 读取索引
                                int lastIndex = _Table.Rows.Count - 1;
                                do
                                {
                                    if (lastIndex >= 0)
                                    {
                                        if (_Table.Rows[lastIndex][0].IsNull)
                                        {
                                            lastIndex--;
                                        }
                                        else
                                        {
                                            maxID = Convert.ToInt32(_Table.Rows[lastIndex][0].Value) + 1;
                                        }
                                    }
                                    else
                                    {
                                        maxID = 1;
                                    }
                                }
                                while (maxID == 0);
                                #endregion

                            }
                            else
                            {
                                maxID = 1;
                            }
                        }
                        catch
                        {
                        }
                    }
                    else
                    {
                        Error.Throw("Increment id only allow use for int type");
                    }

                }
                return maxID;
            }
        }
        /// <summary>
        /// 包含路径的完整文件名称
        /// </summary>
        string _FileFullName = string.Empty;
        /// <summary>
        /// （文件名）：不包含路径的文件名称(带扩展名)
        /// </summary>
        public string _FileName = string.Empty;
        internal MDataRow _Row;//MAction中的Row
        DataBaseType _DalType = DataBaseType.None;
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName;
        public NoSqlAction(ref MDataRow row, string fileName, string filePath, DataBaseType dalType)
        {
            Reset(ref row, fileName, filePath, dalType);
        }
        /// <summary>
        /// 切换表
        /// </summary>
        /// <param name="row">数据行结构</param>
        /// <param name="fileName">文件名称</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="dalType">数据类型</param>
        public void Reset(ref MDataRow row, string fileName, string filePath, DataBaseType dalType)
        {
            TableName = Path.GetFileNameWithoutExtension(fileName);
            string exName = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(exName))
            {
                switch (dalType)
                {
                    case DataBaseType.Txt:
                        fileName = fileName + ".txt";
                        break;
                    case DataBaseType.Xml:
                        fileName = fileName + ".xml";
                        break;
                }
            }
            if (fileName != _FileName)
            {
                _insertRows.Clear();//切换表的时候重置。
                Dispose();//先保存
            }
            _Row = row;
            _FileName = fileName;
            _FileFullName = filePath + _FileName;
            _DalType = dalType;
            _Table = null;
        }
        public bool Delete(object where)
        {
            int count = 0;
            return Delete(where, out count);
        }

        internal bool Delete(object where, out int count)
        {
            count = -1;
            if (!string.IsNullOrEmpty(Convert.ToString(where)))
            {
                //lock (lockOperatorObj) // 删除条件会影响到Insert。
                //{
                IList<MDataRow> rowList = Table.FindAll(where);
                if (rowList != null)
                {
                    count = rowList.Count;
                    if (count > 0)
                    {
                        bool isDeleteAll = count == Table.Rows.Count;
                        if (isDeleteAll)
                        {
                            _Table.Rows.Clear();//清空竟然无效，暂时未找到原因
                            _insertRows.Clear();
                            if (!_isDeleteAll.Contains(_FileFullName))
                            {
                                _isDeleteAll.Add(_FileFullName);
                            }
                        }
                        else
                        {
                            for (int i = rowList.Count - 1; i >= 0; i--)
                            {
                                try
                                {
                                    MDataRow row = rowList[i];
                                    if (row != null)
                                    {
                                        if (Table.Rows.Contains(row)) //线程多时，有时候会重复删
                                        {
                                            Table.Rows.Remove(row);
                                        }
                                        if (_insertRows.Count > 0 && _insertRows.Contains(row))
                                        {
                                            _insertRows.Remove(row);
                                        }

                                    }
                                }
                                catch { }
                            }
                        }
                        // if (isDeleteAll) { Save(); }
                        needToSaveState = 2;
                        return true;
                    }
                }
                //}
            }
            return false;
        }
        public bool Insert(bool isOpenTrans)
        {
            MDataCell cell = _Row.PrimaryCell;
            if (IsCanDoInsertCheck((cell.IsNullOrEmpty || cell.Struct.IsAutoIncrement || cell.Struct.IsPrimaryKey) ? 1 : 0))
            {
                //判断是否需要增加自增加id
                if (!cell.Struct.IsCanNull && (cell.Struct.IsAutoIncrement || cell.Struct.IsPrimaryKey))
                {
                    #region 给主键赋值
                    int groupID = DataType.GetGroup(cell.Struct.SqlType);
                    string existWhere = cell.ColumnName + (groupID == 1 ? "={0}" : "='{0}'");
                    if (cell.IsNull || cell.State == 0 || cell.StringValue == "0" || Exists(string.Format(existWhere, cell.Value)))//这里检测存在，避免id重复
                    {
                        switch (groupID)
                        {
                            case 1:
                                cell.Value = NextID;
                                break;
                            case 4:
                                cell.Value = Guid.NewGuid();
                                break;
                            case 0:
                                cell.Value = Guid.NewGuid().ToString();
                                break;
                            default:
                                return (bool)Error.Throw("first column value can't be null");
                        }
                    }
                    if (groupID == 1 || groupID == 4)//再检测是否已存在
                    {
                        if (!isOpenTrans && Exists(string.Format(existWhere, cell.Value)))//事务时，由于自动补id，避开检测，提升性能
                        {
                            Error.Throw("first column value must be unique:(" + cell.ColumnName + ":" + cell.Value + ")");
                        }
                        else if (groupID == 1)
                        {
                            maxID = (int)cell.Value;
                        }
                    }
                    #endregion
                }

                CheckFileChanged(true);
                _Row.SetState(0);//状态重置，避免重复使用插入！
                MDataRow newRow = Table.NewRow(true);
                newRow.LoadFrom(_Row);
                _insertRows.Add(newRow);//插入引用，内存表有数据，还没写文章！
                needToSaveState = needToSaveState > 1 ? 2 : 1;
                return true;
            }
            return false;
        }
        public bool Update(object where)
        {
            int count = 0;
            return Update(where, out count);
        }
        public bool Update(object where, out int count)
        {
            count = -1;
            CheckFileChanged(true);
            IList<MDataRow> rowList = Table.FindAll(where);
            if (rowList != null)
            {
                count = rowList.Count;
                if (count > 0)
                {
                    for (int i = rowList.Count - 1; i >= 0; i--)
                    {
                        rowList[i].LoadFrom(_Row, RowOp.Update, false);
                        rowList[i].SetState(0);//状态重置
                    }
                    _Row.SetState(0);
                    needToSaveState = 2;
                    return true;
                }
            }
            return false;
        }
        public bool Fill(object where)
        {
            CheckFileChanged(true);
            MDataRow row = Table.FindRow(where);
            if (row != null)
            {
                _Row.LoadFrom(row);
                _Row.SetState(0);//查询时，后续会定位状态为1
                return true;
            }
            return false;
        }
        public int GetCount(object where)
        {
            CheckFileChanged(true);
            return Table.GetCount(where);
        }
        public bool Exists(object where)
        {
            CheckFileChanged(true);
            return Table.FindRow(where) != null;
        }
        public MDataTable Select(int pageIndex, int pageSize, object where, out int rowCount, params object[] selectColumns)
        {
            CheckFileChanged(true);
            MDataTable dt = Table.Select(pageIndex, pageSize, where, selectColumns);
            rowCount = dt.RecordsAffected;
            return dt;
        }

        #region 其它方法
        private bool IsCanDoInsertCheck(int start)
        {
            bool isCanDo = false;
            for (int i = start; i < _Row.Count; i++)
            {
                if (_Row[i].State == 0 && !_Row[i].IsNull)
                {
                    _Row[i].Value = null;
                }
                if (!_Row[i].IsNullOrEmpty)
                {
                    isCanDo = true;
                }
                else if (Convert.ToString(_Row[i].Struct.DefaultValue).Length > 0)
                {
                    _Row[i].SetDefaultValueToValue();
                    if (!_Row[i].IsNullOrEmpty)
                    {
                        isCanDo = true;
                    }
                }
                else if (!_Row[i].Struct.IsCanNull)
                {
                    Error.Throw("Column [" + _Row[i].ColumnName + "] 's value can't be null or empty ! (tip:column property:iscannull=false)");
                }
            }
            if (!isCanDo && start == 1) // 允许插入空的主键(非自增）。
            {
                return !_Row[0].IsNullOrEmpty && _Row[0].Struct.IsPrimaryKey && !_Row[0].Struct.IsAutoIncrement;
            }
            return isCanDo;
        }
        #endregion

        #region IDisposable 成员

        public void Dispose()
        {
            if (string.IsNullOrEmpty(_FileFullName))
            {
                return;
            }
            int state = needToSaveState;
            if (state > 0)
            {
                bool isFirstAddRow = (Table.Rows.Count - _insertRows.Count) == 0;//如果是首次新增加数据。
                if (state > 1 || isFirstAddRow || _DalType == DataBaseType.Xml || _insertRows.Count == 0)
                {
                    Save();
                }
                else//文本仅有插入
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < _insertRows.Count; i++)
                    {
                        sb.Append("," + AppConst.NewLine + _insertRows[i].ToJson(RowOp.None, false, EscapeOp.Encode));
                    }
                    _insertRows.Clear();//重置
                    if (!Tool.IOHelper.Append(_FileFullName, sb.ToString()))
                    {
                        Save();//失败，则重新尝试写入！
                    }
                }
                needToSaveState = 0;//重置为0
                CheckFileChanged(false);//通过检测重置最后修改时间。
            }
        }
        /// <summary>
        /// 检测文件是否已被修改过
        /// </summary>
        /// <param name="isNeedToReloadTable"></param>
        private void CheckFileChanged(bool isNeedToReloadTable)
        {
            if (isNeedToReloadTable && !_isDeleteAll.Contains(_FileFullName))
            {
                DateTime _lastWriteTimeUtc = _lastWriteTimeList[_FileFullName];
                if (IOHelper.IsLastFileWriteTimeChanged(_FileFullName, ref _lastWriteTimeUtc))
                {
                    _lastWriteTimeList[_FileFullName] = _lastWriteTimeUtc;
                    if (_tableList.ContainsKey(_FileFullName))
                    {
                        try
                        {
                            _tableList[_FileFullName].Rows.Clear();
                            _tableList.Remove(_FileFullName);
                        }
                        catch// (Exception err)
                        {

                        }
                    }
                    _Table = null;//需要重新加载数据。
                }
            }

        }
        private void Save()
        {
            try
            {
                string text = string.Empty;
                if (string.IsNullOrEmpty(text))
                {
                    text = _DalType == DataBaseType.Txt ? Table.ToJson(false, true, RowOp.None, false, EscapeOp.Encode).Replace("},{", "},\r\n{").Trim('[', ']') : Table.ToXml();
                }
                int tryAgainCount = 3;
                bool isError = false;
                do
                {
                    try
                    {

                        IOHelper.Write(_FileFullName, text, _DalType == DataBaseType.Txt ? IOHelper.DefaultEncoding : Encoding.UTF8);
                        tryAgainCount = 0;
                        if (_isDeleteAll.Contains(_FileFullName))
                        {
                            _isDeleteAll.Remove(_FileFullName);
                        }
                    }
                    catch
                    {
                        tryAgainCount--;
                        isError = true;
                    }

                    if (isError)
                    {
                        System.Threading.Thread.Sleep(20 * (4 - tryAgainCount));
                    }

                }
                while (tryAgainCount > 0);
            }
            catch (Exception err)
            {
                Log.Write(err, LogType.DataBase);
            }
        }
        #endregion
    }
}
