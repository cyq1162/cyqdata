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
    /// ���
    /// </summary>
    public partial class MDataTable
    {
        private const string DefaultTableName = "SysDefault";
        #region ��ʽת��

        /// <summary>
        /// ��DataReader��ʽת����MDataTable
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
        /// ��DataTable��ʽת����MDataTable
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
        /// ���м�����ʽת����MDataTable
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static implicit operator MDataTable(List<MDataRow> rows)
        {
            return (MDataRowCollection)rows;
        }
        /// <summary>
        /// ��һ������װ�س�һ����
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
        /// ��һ������װ�س�һ����
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

        #region ����
        private MDataRowCollection _Rows;
        /// <summary>
        /// �����
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
        /// ��̬�洢����(�磺AcceptChanges �������쳣Ĭ���ɱ������洢)
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
        /// ����
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
        /// ��������
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
        /// ���ļܹ���
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
        /// �ñ���������ݿ����ӡ�
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

        #region ����
        /// <summary>
        /// �½�һ��
        /// </summary>
        /// <returns></returns>
        public MDataRow NewRow()
        {
            return NewRow(false);
        }
        /// <summary>
        /// �½�һ��
        /// </summary>
        /// <param name="isAddToTable">�Ƿ�˳����ӵ�����</param>
        /// <returns></returns>
        public MDataRow NewRow(bool isAddToTable)
        {
            return NewRow(isAddToTable, -1);
        }
        /// <summary>
        /// �½�һ��
        /// </summary>
        /// <param name="index">���������</param>
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
        #region ׼���¿�ʼ�ķ���
        /// <summary>
        /// ʹ�ñ���ѯ���õ���¡�������
        /// </summary>
        public MDataTable Select(object where)
        {
            return Select(0, 0, where);
        }
        /// <summary>
        /// ʹ�ñ���ѯ���õ���¡�������
        /// </summary>
        public MDataTable Select(int topN, object where)
        {
            return Select(1, topN, where);
        }
        /// <summary>
        /// ʹ�ñ���ѯ���õ���¡�������
        /// </summary>
        public MDataTable Select(int pageIndex, int pageSize, object where, params object[] selectColumns)
        {
            return MDataTableFilter.Select(this, pageIndex, pageSize, where, selectColumns);
        }
        /// <summary>
        /// ʹ�ñ���ѯ���õ�ԭ���ݵ����á�
        /// </summary>
        public MDataRow FindRow(object where)
        {
            return MDataTableFilter.FindRow(this, where);
        }
        /// <summary>
        /// ʹ�ñ���ѯ���õ�ԭ���ݵ����á�
        /// </summary>
        public MDataRowCollection FindAll(object where)
        {
            return MDataTableFilter.FindAll(this, where);
        }
        /// <summary>
        /// ͳ�����������������ڵ�����
        /// </summary>
        public int GetIndex(object where)
        {
            return MDataTableFilter.GetIndex(this, where);
        }
        /// <summary>
        /// ͳ����������������
        /// </summary>
        public int GetCount(object where)
        {
            return MDataTableFilter.GetCount(this, where);
        }
        /// <summary>
        /// ���������ֲ�������������������ͷ����������ġ����ֳ����������к�ԭʼ������ͬһ������
        /// </summary>
        public MDataTable[] Split(object where)
        {
            return MDataTableFilter.Split(this, where);
        }
        #endregion

        /// <summary>
        /// ������(�����мܹ�)[��ʾ��������Ϊ�ռܹ�ʱ��Ч]
        /// </summary>
        /// <param name="row"></param>
        internal void LoadRow(MDataRow row) //�Ƿ�ֱ������Row.Table�أ�����
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
        /// ת����DataTable
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
                    if (!checkDuplicate && dt.Columns.Contains(item.ColumnName))//ȥ�ء�
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
        /// ���Xml�ĵ�
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
        /// ���Xml�ĵ�
        /// </summary>
        /// <param name="isConvertNameToLower">����תСд</param>
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
        /// ����Xml
        /// </summary>
        public bool WriteXml(string fileName, bool isConvertNameToLower)
        {
            return IOHelper.Write(fileName, ToXml(isConvertNameToLower), Encoding.UTF8);
        }

        /// <summary>
        /// ���Json
        /// </summary>
        public string ToJson()
        {
            return ToJson(true);
        }
        public string ToJson(bool addHead)
        {
            return ToJson(addHead, false);
        }
        /// <param name="addHead">���ͷ����Ϣ[��count��Success��ErrorMsg](Ĭ��true)</param>
        /// <param name="addSchema">���������ܹ���Ϣ,������ʱ�ɻ�ԭ�ܹ�(Ĭ��false)</param>
        public string ToJson(bool addHead, bool addSchema)
        {
            return ToJson(addHead, addSchema, RowOp.None);
        }
        /// <param name="rowOp">����ѡ��</param>
        public string ToJson(bool addHead, bool addSchema, RowOp rowOp)
        {
            return ToJson(addHead, addSchema, rowOp, false);
        }

        public string ToJson(bool addHead, bool addSchema, RowOp rowOp, bool isConvertNameToLower)
        {
            return ToJson(addHead, addSchema, rowOp, isConvertNameToLower, JsonHelper.DefaultEscape);
        }
        /// <param name="op">����ת��ѡ��</param>
        public string ToJson(bool addHead, bool addSchema, RowOp rowOp, bool isConvertNameToLower, EscapeOp escapeOp)
        {
            JsonHelper helper = new JsonHelper(addHead, addSchema);
            if (DynamicData != null && DynamicData is MDictionary<int, int>)
            {
                helper.LoopCheckList = DynamicData as MDictionary<int, int>;//�̳и������ݣ�����ѭ�����ø�
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
        /// ���Json[��ָ������·��]
        /// </summary>
        public bool WriteJson(bool addHead, bool addSchema, string fileName)
        {
            return IOHelper.Write(fileName, ToJson(addHead, addSchema));
        }


        /// <summary>
        /// �����ݱ�󶨵��б�ؼ�
        /// </summary>
        /// <param name="control">�б�ؼ�[����Repeater/DataList/GridView/DataGrid��]</param>
        public void Bind(object control)
        {
            Bind(control, null);
        }
        /// <summary>
        /// �����ݱ�󶨵��б�ؼ�
        /// </summary>
        /// <param name="control">�б�ؼ�[����Repeater/DataList/GridView/DataGrid��]</param>
        /// <param name="nodeid">��ControlΪXHtmlAction����ʱ����Ҫָ���󶨵Ľڵ�id</param>
        public void Bind(object control, string nodeid)
        {
            MBindUI.Bind(control, this, nodeid);
        }
        /// <summary>
        /// ���±���зŵ�ԭ������档
        /// </summary>
        /// <param name="newTable"></param>
        public void Merge(MDataTable newTable)
        {
            if (newTable != null && newTable.Rows.Count > 0)
            {
                int count = newTable.Rows.Count;//��ǰ��ȡ��������Ϊ�˱���dt.Merge(dt);//���������µ���ѭ����
                for (int i = 0; i < count; i++)
                {
                    // _Rows.Add(newTable.Rows[i]);
                    NewRow(true).LoadFrom(newTable.Rows[i]);
                }
            }
        }
        /// <summary>
        /// �����������е������е�״̬ȫ������
        /// </summary>
        /// <param name="state">״̬[0:δ���ģ�1:�Ѹ�ֵ,ֵ��ͬ[�ɲ���]��2:�Ѹ�ֵ,ֵ��ͬ[�ɸ���]]</param>
        public MDataTable SetState(int state)
        {
            SetState(state, BreakOp.None); return this;
        }
        /// <summary>
        /// �����������е������е�״̬ȫ������
        /// </summary>
        /// <param name="state">״̬[0:δ���ģ�1:�Ѹ�ֵ,ֵ��ͬ[�ɲ���]��2:�Ѹ�ֵ,ֵ��ͬ[�ɸ���]]</param>
        /// <param name="op">״̬����ѡ��</param>
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
        /// תʵ���б�
        /// </summary>
        /// <param name="useEmit">�Ƿ�ʹ��Emit��ʽת��[����Խ��[����500��]����Խ��],��дĬ������Ӧ�ж�</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> ToList<T>(params bool[] useEmit)
        {
            List<T> list = new List<T>();
            if (Rows != null && Rows.Count > 0)
            {
                if (((Rows.Count > 500 && useEmit.Length == 0) || (useEmit.Length > 0 && useEmit[0])) && typeof(T).BaseType.Name != "OrmBase")//Զ�̴���ʵ������Ի�䣬�޷���Emit
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
            object listObj = Activator.CreateInstance(t);//����ʵ��
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
        /// ������������ [��ʾ�������͵�ǰ�����йأ��統ǰ��������Ҫ�ύ���ı���,���TableName�������¸�ֵ]
        /// </summary>
        /// <param name="op">����ѡ��[����|����]</param>
        /// <returns>����falseʱ�������쳣�����ڣ�DynamicData ������</returns>
        public bool AcceptChanges(AcceptOp op)
        {
            return AcceptChanges(op, string.Empty);
        }

        /// <param name="op">����ѡ��[����|����]</param>
        /// <param name="newConn">ָ���µ����ݿ�����</param>
        /// <param name="jointPrimaryKeys">AcceptOpΪUpdate��Autoʱ������Ҫ������������ΪΨһ�������������������ö���ֶ���</param>
        /// <returns>����falseʱ�������쳣�����ڣ�DynamicData ������</returns>
        public bool AcceptChanges(AcceptOp op, string newConn, params object[] jointPrimaryKeys)
        {
            bool result = false;
            if (Columns.Count == 0 || Rows.Count == 0)
            {
                return false;//ľ�пɸ��µġ�
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
                //ȡ��AOP���档
                AutoCache.ReadyForRemove(AutoCache.GetBaseKey(TableName, newConn));
            }
            return result;
        }
        /// <summary>
        /// ��ȡ�޸Ĺ�������
        /// </summary>
        /// <returns></returns>
        public MDataTable GetChanges()
        {
            return GetChanges(RowOp.Update);
        }
        /// <summary>
        /// ��ȡ�޸Ĺ�������(�����޸ģ��򷵻�Null��
        /// </summary>
        /// <param name="rowOp">��Insert��Updateѡ�����</param>
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
        /// ����ĳ�еļ���
        /// <param name="columnName">����</param>
        /// </summary>
        public List<T> GetColumnItems<T>(string columnName)
        {
            return GetColumnItems<T>(columnName, BreakOp.None, false);
        }
        /// <summary>
        /// ����ĳ�еļ���
        /// <param name="columnName">����</param>
        /// <param name="op">����ѡ��</param>
        /// </summary>
        public List<T> GetColumnItems<T>(string columnName, BreakOp op)
        {
            return GetColumnItems<T>(columnName, op, false);
        }
        /// <summary>
        /// ����ĳ�еļ���
        /// </summary>
        /// <typeparam name="T">�е�����</typeparam>
        /// <param name="columnIndex">����</param>
        /// <param name="op">����ѡ��</param>
        /// <param name="isDistinct">�Ƿ�ȥ���ظ�����</param>
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
        /// ����ĳ�еļ���
        /// </summary>
        /// <typeparam name="T">�е�����</typeparam>
        /// <param name="columnIndex">��N��</param>
        public List<T> GetColumnItems<T>(int columnIndex)
        {
            return GetColumnItems<T>(columnIndex, BreakOp.None);
        }

        /// <summary>
        /// ����ĳ�еļ���
        /// </summary>
        /// <typeparam name="T">�е�����</typeparam>
        /// <param name="columnIndex">��N��</param>
        /// <param name="op">����ѡ��</param>
        /// <returns></returns>
        public List<T> GetColumnItems<T>(int columnIndex, BreakOp op)
        {
            return GetColumnItems<T>(columnIndex, op, false);
        }
        /// <summary>
        /// ����ĳ�еļ���
        /// </summary>
        /// <typeparam name="T">�е�����</typeparam>
        /// <param name="columnIndex">��N��</param>
        /// <param name="op">����ѡ��</param>
        /// <param name="isDistinct">�Ƿ�ȥ���ظ�����</param>
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
        /// ���Ʊ�
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
        /// ���Ʊ�Ľṹ
        /// </summary>
        /// <param name="clone">�Ƿ��¡��ṹ</param>
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
        /// ���˵��ظ��������У����Ƚϻ������͡������������ͽ��Ƚ��������ƣ���
        /// <param name="filterRows">�����˵����ݼ���</param>
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
                    for (int j = Rows.Count - 1; j >= 0 && j != i; j--)//�����⡣
                    {
                        int eqCount = 0;
                        for (int k = 0; k < cCount; k++)//�Ƚ���
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
        /// ���˵��ظ��������У����Ƚϻ������͡������������ͽ��Ƚ��������ƣ���
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
        private int _Ptr = -1;//������
        #region IDataRecord ��Ա
        /// <summary>
        /// ��ȡ�е�����
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
            return "";//�󶨵Ŀ��Բ���Ҫ����
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

        #region IDataReader ��Ա
        /// <summary>
        /// ���������
        /// </summary>
        void IDataReader.Close()
        {
            _Rows.Clear();
        }
        /// <summary>
        /// ��ȡ����������
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
        /// �Ƿ��Ѷ�ȡ�����������ݣ�������˼�¼��
        /// </summary>
        bool IDataReader.IsClosed
        {
            get
            {
                return _Rows.Count == 0 && _Ptr >= _Rows.Count - 1;
            }
        }

        /// <summary>
        /// �Ƿ�����һ������
        /// </summary>
        /// <returns></returns>
        bool IDataReader.NextResult()
        {
            return _Ptr < _Rows.Count - 1;
        }
        /// <summary>
        /// �����Ƶ���һ����׼�����ж�ȡ��
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
        /// ���أ���ѯʱ����¼������
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

        #region IDisposable ��Ա

        void IDisposable.Dispose()
        {
            _Rows.Clear();
            _Rows = null;
        }

        #endregion

        #region IEnumerable ��Ա

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
        #region ��̬���� CreateFrom

        /// <summary>
        /// ���ر�Sdr����Ϊ�ⲿMProc.ExeMDataTableList����Ҫʹ�ã�
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
                #region ����ṹ
                if (noSchema) //OracleClient ��֧���Ӳ�ѯ��GetSchemaTable����ODP.NET��֧�ֵġ�
                {
                    //��DataReader��ȡ��ṹ��������û�����ݡ�
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
                    List<int> errIndex = new List<int>();//SQLite�ṩ��dll�����ף�sdr[x]����ת����ʱ����ֱ�����쳣
                    while (sdr.Read())
                    {
                        #region ��������
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
                                mRecord[i].Value = value; //sdr.GetValue(i); �ô˸�ֵ���ڲ����������ת����
                                mRecord[i].State = 1;//��ʼʼ״̬Ϊ1
                            }

                        }
                        #endregion
                    }
                }
            }
            return mTable;
        }
        /// <summary>
        /// ��List�б�����س�MDataTable
        /// </summary>
        /// <param name="entityList">ʵ���б����</param>
        /// <returns></returns>
        public static MDataTable CreateFrom(object entityList, BreakOp op)
        {
            if (entityList is MDataView)//Ϊ�˴���UI�󶨵ı��˫���ٵ����
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
                        #region ������ͷ
                        Type[] types;
                        int len = ReflectTool.GetArgumentLength(ref t, out types);
                        if (len == 2)//�ֵ�
                        {
                            dt.Columns.Add("Key", DataType.GetSqlType(types[0]));
                            dt.Columns.Add("Value", DataType.GetSqlType(types[1]));
                        }
                        else
                        {
                            Type objType = types[0];
                            if ((objType.FullName.StartsWith("System.") && objType.FullName.Split('.').Length == 2) || objType.IsEnum)//ϵͳ���͡�
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
                            row.LoadFrom(o, op);//��ʼֵ״̬Ϊ1
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
        /// ��Json��Xml�ַ��������س�MDataTable
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
        /// ��Json��Xml�ַ��������س�MDataTable
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
            if (!xmlOrFileName.StartsWith("<"))//�������ļ�·��
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
            else  // xml �ַ���
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
                        //���绯��ܹ�
                        bool useChildToGetSchema = doc.DocumentElement.ChildNodes[0].ChildNodes.Count > 0;
                        foreach (XmlNode item in doc.DocumentElement.ChildNodes)
                        {
                            if (useChildToGetSchema)
                            {
                                if (item.ChildNodes.Count > 0)//���ӽڵ�,���ӽڵ�����Ƶ��ֶ�
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
                            else//�����ӽڵ㣬�õ�ǰ�ڵ�����Ե��ֶ�
                            {
                                if (item.Attributes != null && item.Attributes.Count > 0)//���ӽڵ�,���ӽڵ�����Ƶ��ֶ�
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
                    if (row.ChildNodes.Count > 0)//���ӽڵ㴦��
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
                    else if (row.Attributes != null && row.Attributes.Count > 0) //�����Դ���
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

        #region �е�ȡֵ��Min��Max��Sum��Avg
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
        /// ��ȡ�е���Сֵ
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <param name="columnName">����</param>
        /// <returns></returns>
        public T Min<T>(string columnName)
        {
            return GetMinMaxValue<T>(Columns.GetIndex(columnName), true);
        }
        /// <summary>
        /// ��ȡ�е���Сֵ
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <param name="index">������</param>
        /// <returns></returns>
        public T Min<T>(int index)
        {
            return GetMinMaxValue<T>(index, true);
        }

        /// <summary>
        /// ��ȡ�е����ֵ
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <param name="columnName">����</param>
        /// <returns></returns>
        public T Max<T>(string columnName)
        {
            return GetMinMaxValue<T>(Columns.GetIndex(columnName), false);
        }
        /// <summary>
        /// ��ȡ�е����ֵ
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <param name="index">������</param>
        /// <returns></returns>
        public T Max<T>(int index)
        {
            return GetMinMaxValue<T>(index, false);
        }
        /// <summary>
        /// ����ĳ�е�ֵ
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <param name="columnName">����</param>
        /// <returns></returns>
        public T Sum<T>(string columnName)
        {
            return Sum<T>(Columns.GetIndex(columnName));
        }

        /// <summary>
        /// ����ĳ�е�ֵ
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <param name="index">������</param>
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
        /// ����ĳ�е�ƽ��ֵ
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <param name="columnName">����</param>
        /// <returns></returns>
        public T Avg<T>(string columnName)
        {
            return Avg<T>(Columns.GetIndex(columnName));
        }

        /// <summary>
        ///����ĳ�е�ƽ��ֵ
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <param name="index">������</param>
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
        /// ��ת���У���ָ��ʱ��Ĭ��ȡ������д���
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
        /// ��ת����
        /// </summary>
        /// <param name="rowName">����ָ���е�����</param>
        /// <param name="colName">���ڷֲ���е�����</param>
        /// <param name="valueName">������ʾֵ������</param>
        /// <returns></returns>
        public MDataTable Pivot(string rowName, string colName, string valueName)
        {
            MDataTable dt = new MDataTable(TableName);

            #region ������ͷ
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

            #region ��������
            List<string> rowNameItems = GetColumnItems<string>(rowName, BreakOp.None, true);
            MDataTable splitTable = this;
            for (int i = 0; i < rowNameItems.Count; i++)
            {
                MDataRow nameRow = dt.NewRow(true).Set(0, rowNameItems[i]);//�±��һ��
                MDataTable[] dt2 = splitTable.Split(rowName + "='" + rowNameItems[i] + "'");//ɸѡ�ָ�
                splitTable = dt2[1];//ʣ�µ���Ϊ�´ηָ�

                foreach (MDataRow row in dt2[0].Rows)//��д����
                {
                    if (!row[colName].IsNullOrEmpty)//��������Ϊ�ջ�Null
                    {
                        nameRow.Set(row[colName].Value, row[valueName].Value);
                    }
                }
            }
            #endregion
            return dt;
        }
        #endregion

        //��MDataTableBatchAction����������ʹ�á�
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

        #region ע�͵�����
        /*
         * 
           /// <summary>
        /// ��List�б�����س�MDataTable
        /// </summary>
        /// <param name="entityList">ʵ���б�</param>
        /// <returns></returns>
        public static MDataTable LoadFromList<T>(List<T> entityList) where T : class
        {
            MDataTable dt = new MDataTable("Default");
            if (entityList != null && entityList.Count > 0)
            {
                dt.Columns = SchemaCreate.GetColumns(entityList[0].GetType());
                //���ɱ�ṹ��
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
        /// ���ٴ�����ܹ�����id����ʱ��ϵͳ�Զ����������͵�id�е����С���
        /// </summary>
        /// <param name="fileName">�ļ���</param>
        /// <param name="overwrite">ָ�����ļ�����ʱ���Ƿ�������</param>
        /// <param name="columnNames">����������</param>
        /// <param name="sqlDbTypes">������Ӧ���������ͣ���ָ����Ĭ��Ϊnvarchar��</param>
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
