using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;
using System.Reflection;

using CYQ.Data.Tool;
using CYQ.Data.Cache;
using CYQ.Data.SQL;
using System.Data;
using CYQ.Data.Aop;


namespace CYQ.Data.Orm
{
    ///// <summary>
    ///// ORM CodeFirst �ֶ���Դ
    ///// </summary>
    //public enum FieldSource
    //{
    //    /// <summary>
    //    /// ��ʵ��������
    //    /// </summary>
    //    Property,
    //    /// <summary>
    //    /// �����ݿ���ı�������
    //    /// </summary>
    //    Data,
    //    /// <summary>
    //    /// �ۺ���������
    //    /// </summary>
    //    BothOfAll
    //}
    /// <summary>
    /// ��ORM���ࣨ�����ݽ������ܣ�
    /// </summary>
    public class SimpleOrmBase<T> : SimpleOrmBase where T : class
    {
        public new T Get(object where)
        {
            return base.Get<T>(where);
        }
        public new List<T> Select()
        {
            return base.Select<T>();
        }
        public new List<T> Select(int pageIndex, int pageSize)
        {
            return base.Select<T>(pageIndex, pageSize);
        }
        public new List<T> Select(int topN, string where)
        {
            return base.Select<T>(topN, where);
        }
        public new List<T> Select(string where)
        {
            return base.Select<T>(where);
        }
        public new List<T> Select(int pageIndex, int pageSize, string where)
        {
            return base.Select<T>(pageIndex, pageSize, where);
        }
        public new List<T> Select(int pageIndex, int pageSize, string where, out int count)
        {
            return base.Select<T>(pageIndex, pageSize, where, out count);
        }
    }
    /// <summary>
    /// ��ORM���ࣨ�����ݽ������ܣ�
    /// </summary>
    public class SimpleOrmBase : IDisposable
    {
        OrmBaseInfo _BaseInfo;
        /// <summary>
        /// ��ȡ ���� ORM �������Ϣ
        /// </summary>
        [JsonIgnore]
        public OrmBaseInfo BaseInfo
        {
            get
            {
                if (_BaseInfo == null)
                {
                    _BaseInfo = new OrmBaseInfo(Action);
                }
                return _BaseInfo;
            }
        }


        internal MDataColumn Columns = null;
        /// <summary>
        /// ��ʶ�Ƿ�����д��־(Ĭ��true)
        /// </summary>
        internal bool IsWriteLogOnError
        {
            set
            {
                if (Action != null && Action.dalHelper != null)
                {
                    Action.dalHelper.IsWriteLogOnError = value;
                }
            }
        }
        /// <summary>
        /// �Ƿ�������AOP���������ֶ�ֵͬ��(Ĭ��false)
        /// </summary>
        internal bool IsUseAop = false;
        //private static FieldSource _FieldSource = FieldSource.BothOfAll;
        ///// <summary>
        /////  �ֶ���Դ�����ֶα��ʱ���������ô��������л����£�
        ///// </summary>
        //public static FieldSource FieldSource
        //{
        //    get
        //    {
        //        return _FieldSource;
        //    }
        //    set
        //    {
        //        _FieldSource = value;
        //    }
        //}

        Object entity;//ʵ�����
        Type typeInfo;//ʵ���������
        private MAction _Action;
        internal MAction Action
        {
            get
            {
                if (_Action == null)
                {
                    SetDelayInit(_entityInstance, _tableName, _conn);//�ӳټ���
                }
                return _Action;
            }
        }
        /// <summary>
        /// ���캯��
        /// </summary>
        public SimpleOrmBase()
        {

        }
        /// <summary>
        /// ���ò�����
        /// </summary>
        public SimpleOrmBase SetPara(object paraName, object value)
        {
            Action.SetPara(paraName, value);
            return this;
        }

        public SimpleOrmBase SetPara(object paraName, object value, DbType dbType)
        {
            Action.SetPara(paraName, value, dbType);
            return this;
        }
        /// <summary>
        /// ����Aop״̬
        /// </summary>
        /// <param name="op"></param>
        public SimpleOrmBase SetAopState(AopOp op)
        {
            Action.SetAopState(op);
            return this;
        }
        /// <summary>
        /// ��ʼ��״̬[�̳д˻����ʵ���ڹ��캯��������ô˷���]
        /// </summary>
        /// <param name="entityInstance">ʵ�����,һ��д:this</param>
        protected void SetInit(Object entityInstance)
        {
            SetInit(entityInstance, null, AppConfig.DB.DefaultConn);
        }
        /// <summary>
        /// ��ʼ��״̬[�̳д˻����ʵ���ڹ��캯��������ô˷���]
        /// </summary>
        /// <param name="entityInstance">ʵ�����,һ��д:this</param>
        /// <param name="tableName">����,��:Users</param>
        protected void SetInit(Object entityInstance, string tableName)
        {
            SetInit(entityInstance, tableName, AppConfig.DB.DefaultConn);
        }
        /// <summary>
        /// ��ʼ��״̬[�̳д˻����ʵ���ڹ��캯��������ô˷���]
        /// </summary>
        /// <param name="entityInstance">ʵ�����,һ��д:this</param>
        /// <param name="tableName">����,��:Users</param>
        /// <param name="conn">��������,�����ݿ�ʱ��дNull,��дĬ������������:"Conn",��ֱ�����ݿ������ַ���</param>
        protected void SetInit(Object entityInstance, string tableName, string conn)
        {
            _entityInstance = entityInstance;
            _tableName = tableName;
            _conn = conn;
        }
        private object _entityInstance;
        private string _tableName = string.Empty;
        private string _conn = string.Empty;

        /// <summary>
        /// ��ԭ�еĳ�ʼ���������ʱ���ء�
        /// </summary>
        private void SetDelayInit(Object entityInstance, string tableName, string conn)
        {
            if (tableName == AppConfig.Log.LogTableName && string.IsNullOrEmpty(AppConfig.Log.LogConn))
            {
                return;
            }
            //if (string.IsNullOrEmpty(conn))
            //{
            //    //���������ӣ�����ԣ�������ͨ��ʵ���ࣩ
            //    return;
            //}
            if (entityInstance == null) { entityInstance = this; }
            entity = entityInstance;
            typeInfo = entity.GetType();
            try
            {
                if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(conn))
                {
                    string tName, tConn;
                    tName = DBFast.GetTableName(typeInfo, out tConn);
                    if (string.IsNullOrEmpty(tableName)) { tableName = tName; }
                    if (string.IsNullOrEmpty(conn)) { conn = tConn; }
                }
                string errMsg = string.Empty;
                Columns = DBTool.GetColumns(tableName, conn, out errMsg);//�ڲ����Ӵ���ʱ���쳣��
                if (Columns == null || Columns.Count == 0)
                {
                    if (errMsg != string.Empty)
                    {
                        Error.Throw(errMsg);
                    }
                    Columns = TableSchema.GetColumnByType(typeInfo, conn);
                    ConnBean connBean = ConnBean.Create(conn);//����ָ�����ӣ��Ų��������ӱ�ʱ���л��������⡣
                    if (connBean == null)
                    {
                        string err = "SimpleOrmBase<T>.SetDelayInit ConnBean can't create by " + conn;
                        Log.Write(err, LogType.DataBase);
                        Error.Throw(err);
                    }
                    if (!DBTool.Exists(tableName, "U", connBean.ConnString))
                    {
                        DBTool.ErrorMsg = null;
                        if (!DBTool.CreateTable(tableName, Columns, connBean.ConnString))
                        {
                            string err = "SimpleOrmBase ��Create Table " + tableName + " Error:" + DBTool.ErrorMsg;
                            Log.Write(err);
                            Error.Throw(err);
                        }
                    }
                }
                _Action = new MAction(Columns.ToRow(tableName), conn);
                if (typeInfo.Name == "SysLogs")
                {
                    _Action.SetAopState(Aop.AopOp.CloseAll);
                }
            }
            catch (Exception err)
            {
                if (typeInfo.Name != "SysLogs")
                {
                    Log.Write(err, LogType.DataBase);
                }
                Error.Throw(err.Message);
            }
        }
        internal void SetInit2(Object entityInstance, string tableName, string conn)
        {
            SetInit(entityInstance, tableName, conn);
        }
        internal void Set(object key, object value)
        {
            if (Action != null)
            {
                Action.Set(key, value);
            }
        }
        #region ������ɾ�Ĳ� ��Ա

        #region ����
        /// <summary>
        /// ��������
        /// </summary>
        public bool Insert()
        {
            return Insert(InsertOp.ID);
        }
        /// <summary>
        ///  ��������
        /// </summary>
        /// <param name="option">����ѡ��</param>
        public bool Insert(InsertOp option)
        {
            return Insert(false, option, false);
        }
        /// <summary>
        ///  ��������
        /// </summary>
        /// <param name="insertID">��������</param>
        public bool Insert(InsertOp option, bool insertID)
        {
            return Insert(false, option, insertID);
        }
        /*
        /// <summary>
        ///  ��������
        /// </summary>
        /// <param name="autoSetValue">�Զ��ӿ��ƻ�ȡֵ</param>
        internal bool Insert(bool autoSetValue)
        {
            return Insert(autoSetValue, InsertOp.ID);
        }
        internal bool Insert(bool autoSetValue, InsertOp option)
        {
            return Insert(autoSetValue, InsertOp.ID, false);
        }*/
        /// <summary>
        ///  ��������
        /// </summary>
        /// <param name="autoSetValue">�Զ��ӿ��ƻ�ȡֵ</param>
        /// <param name="option">����ѡ��</param>
        /// <param name="insertID">��������</param>
        internal bool Insert(bool autoSetValue, InsertOp option, bool insertID)
        {
            if (autoSetValue)
            {
                Action.UI.GetAll(!insertID);
            }
            GetValueFromEntity();
            Action.AllowInsertID = insertID;
            bool result = Action.Insert(false, option);
            if (autoSetValue || option != InsertOp.None)
            {
                SetValueToEntity();
            }
            return result;
        }
        #endregion

        #region ����
        /// <summary>
        ///  ��������
        /// </summary>
        public bool Update()
        {
            return Update(null, false, null);
        }
        /// <summary>
        ///  ��������
        /// </summary>
        /// <param name="where">where����,��ֱ�Ӵ�id��ֵ��:[88],������where������:[id=88 and name='·������']</param>
        /// <param name="onlyUpdateColumns">ָ�������µ�����������ö��ŷָ�</param>
        /// <returns></returns>
        public bool Update(object where, params string[] updateColumns)
        {
            return Update(where, false, updateColumns);
        }
        /// <summary>
        ///  ��������
        /// </summary>
        /// <param name="where">where����,��ֱ�Ӵ�id��ֵ��:[88],������where������:[id=88 and name='·������']</param>
        /// <param name="autoSetValue">�Ƿ��Զ���ȡֵ[�Զ��ӿؼ���ȡֵ,��Ҫ�ȵ���SetAutoPrefix��SetAutoParentControl�������ÿؼ�ǰ׺]</param>
        internal bool Update(object where, bool autoSetValue, params string[] updateColumns)
        {
            if (autoSetValue)
            {
                Action.UI.GetAll(false);
            }
            GetValueFromEntity();

            if (updateColumns != null && updateColumns.Length > 0)
            {
                Action.Data.SetState(0);
                if (updateColumns.Length == 1)
                {
                    updateColumns = updateColumns[0].Split(',');
                }
                foreach (string item in updateColumns)
                {
                    MDataCell cell = Action.Data[item];
                    if (cell != null)
                    {
                        cell.State = 2;
                    }
                }
            }
            bool result = Action.Update(where);
            if (autoSetValue)
            {
                SetValueToEntity();
            }
            return result;
        }
        #endregion

        #region ɾ��
        /// <summary>
        ///  ɾ������
        /// </summary>
        public bool Delete()
        {
            return Delete(null);
        }
        /// <summary>
        ///  ɾ������
        /// </summary>
        /// <param name="where">where����,��ֱ�Ӵ�id��ֵ��:[88],������where������:[id=88 and name='·������']</param>
        public bool Delete(object where)
        {
            return Delete(where, false);
        }

        /// <param name="isIgnoreDeleteField">�������ɾ���ֶΣ��磺IsDeleted��ʶʱ��Ĭ�ϻ�ת�ɸ��£��˱�ʶ����ǿ������Ϊɾ��</param>
        /// <returns></returns>
        public bool Delete(object where, bool isIgnoreDeleteField)
        {
            GetValueFromEntity();
            return Action.Delete(where, isIgnoreDeleteField);
        }
        #endregion

        #region ��ѯ

        /// <summary>
        /// ��ѯ1������
        /// </summary>
        public bool Fill()
        {
            return Fill(null);
        }
        /// <summary>
        /// ��ѯ1������
        /// </summary>
        public bool Fill(object where)
        {
            bool result = Action.Fill(where);
            if (result)
            {
                SetValueToEntity();
            }
            return result;
        }
        /// <summary>
        /// ��ѯһ�����ݲ�����һ���µ�ʵ��
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T Get<T>(object where) where T : class
        {
            bool result = Action.Fill(where);
            if (result)
            {
                return Action.Data.ToEntity<T>();
            }
            return default(T);
        }
        /// <summary>
        /// �б��ѯ
        /// </summary>
        public virtual List<T> Select<T>() where T : class
        {
            int count = 0;
            return Select<T>(0, 0, null, out count);
        }
        /// <summary>
        /// �б��ѯ
        /// </summary>
        /// <param name="where">��ѯ����[�ɸ��� order by ���]</param>
        /// <returns></returns>
        public virtual List<T> Select<T>(string where) where T : class
        {
            int count = 0;
            return Select<T>(0, 0, where, out count);
        }
        /// <summary>
        /// �б��ѯ
        /// </summary>
        /// <param name="topN">��ѯ����</param>
        /// <param name="where">��ѯ����[�ɸ��� order by ���]</param>
        /// <returns></returns>
        public virtual List<T> Select<T>(int topN, string where) where T : class
        {
            int count = 0;
            return Select<T>(0, topN, where, out count);
        }
        public virtual List<T> Select<T>(int pageIndex, int pageSize) where T : class
        {
            int count = 0;
            return Select<T>(pageIndex, pageSize, null, out count);
        }
        public virtual List<T> Select<T>(int pageIndex, int pageSize, string where) where T : class
        {
            int count = 0;
            return Select<T>(pageIndex, pageSize, where, out count);
        }
        /// <summary>
        /// ���ֲ����ܵ�ѡ��[��������ѯ,ѡ������ʱֻ���PageIndex/PageSize����Ϊ0]
        /// </summary>
        /// <param name="pageIndex">�ڼ�ҳ</param>
        /// <param name="pageSize">ÿҳ����[Ϊ0ʱĬ��ѡ������]</param>
        /// <param name="where"> ��ѯ����[�ɸ��� order by ���]</param>
        /// <param name="count">���صļ�¼����</param>
        /// <param name="selectColumns">ָ�����ص���</param>
        public virtual List<T> Select<T>(int pageIndex, int pageSize, string where, out int count) where T : class
        {
           // return Action.Select(pageIndex, pageSize, where, out count).ToList<T>();
            return Action.Select<T>(pageIndex, pageSize, where, out count);
        }

        internal MDataTable Select(int pageIndex, int pageSize, string where, out int count)
        {
            return Action.Select(pageIndex, pageSize, where, out count);
        }
        public SimpleOrmBase SetSelectColumns(params object[] columnNames)
        {
            Action.SetSelectColumns(columnNames);
            return this;
        }
        /// <summary>
        /// ��ȡ��¼����
        /// </summary>
        public int GetCount()
        {
            return Action.GetCount();
        }
        /// <summary>
        /// ��ȡ��¼����
        /// </summary>
        public int GetCount(object where)
        {
            return Action.GetCount(where);
        }
        /// <summary>
        /// ��ѯ�Ƿ����ָ��������������
        /// </summary>
        public bool Exists(object where)
        {
            return Action.Exists(where);
        }

        #endregion
        /// <summary>
        /// ��������ֵ���ɴ�Json��ʵ�����MDataRow�������ֶε���������ֵ��
        /// </summary>
        /// <param name="jsonOrEntity">json�ַ�����ʵ�����</param>
        public void LoadFrom(object jsonOrEntity)
        {
            LoadFrom(jsonOrEntity, BreakOp.None);
        }
        public void LoadFrom(object jsonOrEntity, BreakOp op)
        {
            MDataRow newValueRow = MDataRow.CreateFrom(jsonOrEntity, null, op);
            Action.Data.LoadFrom(newValueRow);
            List<PropertyInfo> piList = ReflectTool.GetPropertyList(typeInfo);
            foreach (PropertyInfo item in piList)
            {
                if (item.CanWrite)
                {
                    MDataCell cell = newValueRow[item.Name];
                    if (cell != null && !cell.IsNull)
                    {
                        try
                        {
                            item.SetValue(entity, ConvertTool.ChangeType(cell.Value, item.PropertyType), null);
                        }
                        catch (Exception err)
                        {
                            Log.Write(err, LogType.Error);
                        }
                    }
                }
            }
        }
        #endregion
        internal void SetValueToEntity()
        {
            SetValueToEntity(null, RowOp.IgnoreNull);
        }
        internal void SetValueToEntity(RowOp op)
        {
            SetValueToEntity(null, op);
        }
        internal void SetValueToEntity(string propName, RowOp op)
        {
            if (!string.IsNullOrEmpty(propName))
            {
                PropertyInfo pi = typeInfo.GetProperty(propName);
                if (pi != null && pi.CanWrite)
                {
                    MDataCell cell = Action.Data[propName];
                    if (cell != null && !cell.IsNull)
                    {
                        try
                        {
                            pi.SetValue(entity, cell.Value, null);
                        }
                        catch
                        {

                        }

                    }
                }
            }
            else
            {
                Action.Data.SetToEntity(entity, op);
            }
        }
        private void GetValueFromEntity()
        {
            if (!IsUseAop || AppConfig.IsAspNetCore)//ASPNETCore�£���̬�����Aop����Ч��
            {
                MDataRow d = Action.Data;//�ȴ�����ʱ���صġ�
                MDataRow row = MDataRow.CreateFrom(entity);//��ʵ��ԭ��ֵΪ������
                foreach (MDataCell cell in d)
                {
                    MDataCell valueCell = row[cell.ColumnName];
                    if (valueCell.IsNull)
                    {
                        continue;
                    }
                    if (cell.State == 2 && cell.Struct.valueType.IsValueType && (valueCell.StringValue == "0" || valueCell.StringValue == DateTime.MinValue.ToString()))
                    {
                        continue;
                    }
                    cell.Value = valueCell.Value;
                }
            }
        }
        /// <summary>
        /// ���(SetPara���õ�)�Զ������
        /// </summary>
        public void ClearPara()
        {
            if (_Action != null)
            {
                _Action.ClearPara();
            }
        }
        /// <summary>
        /// �������ֵ
        /// </summary>
        public void Clear()
        {
            if (_Action != null)
            {
                _Action.Data.Clear();
            }
        }
        #region IDisposable ��Ա
        /// <summary>
        /// �ͷ���Դ
        /// </summary>
        public void Dispose()
        {
            if (_Action != null && !_Action.IsTransation)//ORM��������ȫ�ֿ��ƣ����������ͷ����ӡ�
            {
                _Action.Dispose();
            }
        }

        #endregion
    }


}
