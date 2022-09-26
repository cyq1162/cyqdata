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
        /// �����������
        /// </summary>
        internal static void Clear()
        {
            ResetStaticVar();
            _needToSaveState.Clear();
            _lockNextIDObj.Clear();
            _maxID.Clear();
            _lastWriteTimeList.Clear();
            _isDeleteAll.Clear();
        }
        /// <summary>
        /// ���þ�̬��������������ڴ棬������������ݺ��ڴ治���գ�
        /// </summary>
        internal static void ResetStaticVar()
        {
            _tableList.Clear();
            _tableList = null;//��̬�ֵ���ΪNull�����ͷ��ڴ档
            _tableList = new MDictionary<string, MDataTable>(5, StringComparer.OrdinalIgnoreCase);//���³�ʼ����
            //_needToSaveState.Clear();
            //_lockNextidObj.Clear();
            //_maxid.Clear();
        }
        private static MDictionary<string, MDataTable> _tableList = new MDictionary<string, MDataTable>(5, StringComparer.OrdinalIgnoreCase);//�ڴ����ݿ�
        // private static readonly object _lockTableListObj = new object();
        /// <summary>
        ///  �Ƿ���Ҫ���£�0δ���£�1������[�����������]��2����ɾ�������[���±���],//��Ҫ����[ȫ�ֵĿ�����Ч������]
        /// </summary>
        private static MDictionary<string, int> _needToSaveState = new MDictionary<string, int>(5, StringComparer.OrdinalIgnoreCase);
        private static MDictionary<string, object> _lockNextIDObj = new MDictionary<string, object>(5, StringComparer.OrdinalIgnoreCase);//������id��
        private static MDictionary<string, int> _maxID = new MDictionary<string, int>(5, StringComparer.OrdinalIgnoreCase);//��ǰ������id
        private static MDictionary<string, DateTime> _lastWriteTimeList = new MDictionary<string, DateTime>(5, StringComparer.OrdinalIgnoreCase);//��ǰ������id
        // private static MDictionary<string, object> _lockOperatorObj = new MDictionary<string, object>(5, StringComparer.OrdinalIgnoreCase);//��ɾ��ʱ�Ļ���
        private List<MDataRow> _insertRows = new List<MDataRow>();//�²���ļ��ϣ���������MDataTable������
        private static List<string> _isDeleteAll = new List<string>(5);

        /// <summary>
        /// ����д��ʱ��
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
                        _tableList.Remove(_FileFullName);// ���������һ�������޷�ɾ��������(�����������⣬���Կɿ���)��
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

                DateTime lastWriteTime = new IOInfo(_FileFullName).LastWriteTime;
                if (!_lastWriteTimeList.ContainsKey(_FileFullName))
                {
                    _lastWriteTimeList.Add(_FileFullName, lastWriteTime);
                }
                if (_Table.Rows.Count > 0)
                {
                    if (!_tableList.ContainsKey(_FileFullName))
                    {
                        _tableList.Add(_FileFullName, _Table);
                    }
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
        /// ��һ��������id
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
                    else if (DataType.GetGroup(Table.Columns.FirstPrimary.SqlType) == DataGroupType.Number)//����id����int��Ч
                    {
                        try
                        {
                            if (Table.Rows.Count > 0)
                            {
                                #region ��ȡ����
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
        /// ����·���������ļ�����
        /// </summary>
        string _FileFullName = string.Empty;
        /// <summary>
        /// ���ļ�������������·�����ļ�����(����չ��)
        /// </summary>
        public string _FileName = string.Empty;
        internal MDataRow _Row;//MAction�е�Row
        DataBaseType _DalType = DataBaseType.None;
        /// <summary>
        /// ����
        /// </summary>
        public string TableName;
        public NoSqlAction(ref MDataRow row, string fileName, string filePath, DataBaseType dalType)
        {
            Reset(ref row, fileName, filePath, dalType);
        }
        /// <summary>
        /// �л���
        /// </summary>
        /// <param name="row">�����нṹ</param>
        /// <param name="fileName">�ļ�����</param>
        /// <param name="filePath">�ļ�·��</param>
        /// <param name="dalType">��������</param>
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
                _insertRows.Clear();//�л����ʱ�����á�
                Dispose();//�ȱ���
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
                //lock (lockOperatorObj) // ɾ��������Ӱ�쵽Insert��
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
                            _Table.Rows.Clear();//��վ�Ȼ��Ч����ʱδ�ҵ�ԭ��
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
                                        if (Table.Rows.Contains(row)) //�̶߳�ʱ����ʱ����ظ�ɾ
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
                //�ж��Ƿ���Ҫ����������id
                if (!cell.Struct.IsCanNull && (cell.Struct.IsAutoIncrement || cell.Struct.IsPrimaryKey))
                {
                    #region ��������ֵ
                    DataGroupType group = DataType.GetGroup(cell.Struct.SqlType);
                    string existWhere = cell.ColumnName + (group == DataGroupType.Number ? "={0}" : "='{0}'");
                    if (cell.IsNull || cell.State == 0 || cell.StringValue == "0" || Exists(string.Format(existWhere, cell.Value)))//��������ڣ�����id�ظ�
                    {
                        switch (group)
                        {
                            case DataGroupType.Number:
                                cell.Value = NextID;
                                break;
                            case DataGroupType.Guid:
                                cell.Value = Guid.NewGuid();
                                break;
                            case DataGroupType.Text:
                                cell.Value = Guid.NewGuid().ToString();
                                break;
                            default:
                                return (bool)Error.Throw("first column value can't be null");
                        }
                    }
                    if (group == DataGroupType.Number || group == DataGroupType.Guid)//�ټ���Ƿ��Ѵ���
                    {
                        if (!isOpenTrans && Exists(string.Format(existWhere, cell.Value)))//����ʱ�������Զ���id���ܿ���⣬��������
                        {
                            Error.Throw("first column value must be unique:(" + cell.ColumnName + ":" + cell.Value + ")");
                        }
                        else if (group == DataGroupType.Number)
                        {
                            maxID = (int)cell.Value;
                        }
                    }
                    #endregion
                }

                CheckFileChanged(true);
                _Row.SetState(0);//״̬���ã������ظ�ʹ�ò��룡
                MDataRow newRow = Table.NewRow(true);
                newRow.LoadFrom(_Row);
                _insertRows.Add(newRow);//�������ã��ڴ�������ݣ���ûд���£�
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
                        rowList[i].SetState(0);//״̬����
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
                _Row.SetState(0);//��ѯʱ�������ᶨλ״̬Ϊ1
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

        #region ��������
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
            if (!isCanDo && start == 1) // �������յ�����(����������
            {
                return !_Row[0].IsNullOrEmpty && _Row[0].Struct.IsPrimaryKey && !_Row[0].Struct.IsAutoIncrement;
            }
            return isCanDo;
        }
        #endregion

        #region IDisposable ��Ա
        public void Dispose()
        {
            if (string.IsNullOrEmpty(_FileFullName) || AppConfig.DB.IsTxtReadOnly)
            {
                return;
            }
            int state = needToSaveState;
            if (state > 0)
            {
                bool isFirstAddRow = (Table.Rows.Count - _insertRows.Count) == 0;//������״����������ݡ�
                if (state > 1 || isFirstAddRow || _DalType == DataBaseType.Xml || _insertRows.Count == 0)
                {
                    Save();
                }
                else//�ı����в���
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < _insertRows.Count; i++)
                    {
                        sb.Append("," + AppConst.NewLine + _insertRows[i].ToJson(RowOp.None, false, EscapeOp.Encode));
                    }
                    _insertRows.Clear();//����
                    if (!Tool.IOHelper.Append(_FileFullName, sb.ToString()))
                    {
                        Save();//ʧ�ܣ������³���д�룡
                    }
                    else
                    {
                        needToSaveState = 0;
                    }
                }
            }
        }
        /// <summary>
        /// ����ļ��Ƿ��ѱ��޸Ĺ�
        /// </summary>
        /// <param name="isNeedToReloadTable"></param>
        private void CheckFileChanged(bool isNeedToReloadTable)
        {
            if (!_isDeleteAll.Contains(_FileFullName))
            {
                DateTime lastWriteTime = _lastWriteTimeList[_FileFullName];
                if (IOHelper.IsLastFileWriteTimeChanged(_FileFullName, ref lastWriteTime))
                {
                    _lastWriteTimeList[_FileFullName] = lastWriteTime;
                    if (isNeedToReloadTable && _tableList.ContainsKey(_FileFullName))
                    {
                        try
                        {
                            _tableList[_FileFullName].Rows.Clear();
                            _tableList.Remove(_FileFullName);
                        }
                        catch// (Exception err)
                        {

                        }
                        _Table = null;//��Ҫ���¼������ݡ�
                    }

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
                        needToSaveState = 0;//����Ϊ0
                        DateTime lastWriteTime = _lastWriteTimeList[_FileFullName];
                        if (IOHelper.IsLastFileWriteTimeChanged(_FileFullName, ref lastWriteTime))
                        {
                            //��������ļ�ʱ��
                            _lastWriteTimeList[_FileFullName] = lastWriteTime;
                        }
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
