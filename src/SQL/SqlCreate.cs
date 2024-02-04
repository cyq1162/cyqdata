using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;
using System.Data;
using CYQ.Data.Tool;
using System.Data.Common;
using CYQ.Data.UI;


namespace CYQ.Data.SQL
{
    /// <summary>
    /// ���ݲ��������
    /// </summary>
    internal partial class SqlCreate
    {
        private static List<string> _autoidItems = new List<string>();
        /// <summary>
        /// oracle��������
        /// </summary>
        internal string AutoID
        {
            get
            {
                string key = string.Format(AppConfig.DB.AutoID, TableName);
                if (!_autoidItems.Contains(key))
                {
                    //��Ⲣ�Զ�������
                    Tool.DBTool.CheckAndCreateOracleSequence(key, _action.dalHelper.ConnName, _action.Data.PrimaryCell.ColumnName, TableName);
                    _autoidItems.Add(key);
                }
                return key;
            }
        }
        /// <summary>
        /// �Ƿ�����ִ��SQL����������Insert��Update��Ч�����SQL�﷨������ܾ�ִ�У�
        /// </summary>
        internal bool isCanDo = true;
        /// <summary>
        /// ���²����ĸ��ӱ��ʽ��
        /// </summary>
        internal string updateExpression = null;
        /// <summary>
        /// ָ����ѯ���С�
        /// </summary>
        internal object[] selectColumns = null;
        private string TableName
        {
            get
            {
                return SqlFormat.Keyword(_action.TableName, _action.DataBaseType);
            }
        }
        private MAction _action;
        internal SqlCreate(MAction action)
        {
            _action = action;
        }

        #region ���Sql���
        #region SQL��䴦��
        internal string GetDeleteToUpdateSql(object whereObj)
        {
            string editTime = GetEditTimeSql();
            return "update " + SqlFormat.Keyword(TableName, _action.dalHelper.DataBaseType) + " set " + editTime + SqlFormat.Keyword(AppConfig.DB.DeleteField, _action.dalHelper.DataBaseType) + "=[#TRUE] where " + FormatWhere(whereObj);
        }
        private string GetEditTimeSql()
        {
            string editTime = AppConfig.DB.EditTimeFields;
            if (!string.IsNullOrEmpty(editTime))
            {
                foreach (string item in editTime.Split(','))
                {
                    if (!string.IsNullOrEmpty(item.Trim()))
                    {
                        if (_action.Data.Columns.Contains(item) && (_action.Data[item].IsNullOrEmpty || _action.Data[item].State < 2))
                        {
                            return SqlFormat.Keyword(item, _action.dalHelper.DataBaseType) + "='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',";//������ڸ�����
                        }
                    }
                }
            }
            return string.Empty;
        }
        internal string GetDeleteSql(object whereObj)
        {
            return "delete from " + SqlFormat.Keyword(TableName, _action.dalHelper.DataBaseType) + " where " + FormatWhere(whereObj);
        }
        /// <summary>
        /// ���ز�����ַ���
        /// </summary>
        /// <returns>�����:insert into tableName(id,Name,Value) values(@id,@Name,@Value)</returns>
        internal string GetInsertSql()
        {
            isCanDo = false;
            StringBuilder _TempSql = new StringBuilder();
            StringBuilder _TempSql2 = new StringBuilder();
            _TempSql.Append("insert into " + SqlFormat.Keyword(TableName, _action.dalHelper.DataBaseType) + "(");
            _TempSql2.Append(" Values(");
            string endChar = ")";
            MDataCell primaryCell = _action.Data[_action.Data.Columns.FirstPrimary.ColumnName];
            DataGroupType group = DataType.GetGroup(primaryCell.Struct.SqlType);
            string defaultValue = Convert.ToString(primaryCell.Struct.DefaultValue);
            if (primaryCell.IsNullOrEmpty && (group == DataGroupType.Guid || (group == 0 && (primaryCell.Struct.MaxSize <= 0 || primaryCell.Struct.MaxSize >= 36) &&
                (defaultValue == "" || defaultValue == "newid" || defaultValue == SqlValue.Guid))))//guid����
            {
                primaryCell.Value = Guid.NewGuid();
            }
            MDataCell cell = null;
            bool insertAutoIncrement = false;
            for (int i = 0; i < _action.Data.Count; i++)
            {
                cell = _action.Data[i];
                if (cell.Struct.IsAutoIncrement)
                {
                    if (!_action.AllowInsertID) continue;//���������С�
                    else insertAutoIncrement = true;
                }
                if (cell.IsNull && !cell.Struct.IsCanNull && cell.Struct.DefaultValue == null)
                {
                    string err = AppConst.HR + string.Format("error : {0} {1} can't be insert null", TableName, cell.ColumnName) + AppConst.BR;
                    Log.Write(err, LogType.DataBase);
                    _action.dalHelper.DebugInfo.Append(err);
                    _action.dalHelper.RecordsAffected = -2;
                    isCanDo = false;
                    break;
                }
                if (cell.State > 0)
                {
                    _TempSql.Append(SqlFormat.Keyword(cell.ColumnName, _action.DataBaseType) + ",");
                    if (_action.DataBaseType == DataBaseType.MsSql && cell.Struct.SqlTypeName != null && cell.Struct.SqlTypeName.EndsWith("hierarchyId"))
                    {
                        _TempSql2.Append("HierarchyID::Parse('" + cell.StringValue + "')");
                    }
                    else
                    {
                        if (_action.DataBaseType == DataBaseType.FoxPro)
                        {
                            string value = "\"" + cell.StringValue + "\",";
                            if (cell.Struct.SqlType == SqlDbType.DateTime)
                            {
                                value = "{^" + cell.StringValue + "},";
                            }
                            //��֧�ֲ�����
                            _TempSql2.Append(value);
                        }
                        else
                        {
                            object value = cell.Value;
                            DbType dbType = DataType.GetDbType(cell.Struct.SqlType.ToString(), _action.DataBaseType);
                            if (dbType == DbType.String && cell.StringValue == "")
                            {
                                if (_action.DataBaseType == DataBaseType.Oracle && !cell.Struct.IsCanNull)
                                {
                                    value = " ";//Oracle not null �ֶΣ����������ÿ�ֵ��
                                }
                                if (_action.DataBaseType == DataBaseType.MySql && cell.Struct.MaxSize == 36)
                                {
                                    value = DBNull.Value;//MySql ��char36 �ᵱ��guid��������Ϊ�գ�ֻ��Ϊnull��
                                }
                            }
                            _TempSql2.Append(_action.dalHelper.Pre + cell.ColumnName + ",");
                            _action.dalHelper.AddParameters(_action.dalHelper.Pre + cell.ColumnName, value, dbType, cell.Struct.MaxSize, ParameterDirection.Input);
                        }
                    }
                    isCanDo = true;
                }
            }
            switch (_action.dalHelper.DataBaseType)
            {
                case DataBaseType.Oracle:
                    if (!_action.AllowInsertID && DataType.GetGroup(primaryCell.Struct.SqlType) == DataGroupType.Number)
                    {
                        _TempSql.Append(primaryCell.ColumnName + ",");
                        _TempSql2.Append(AutoID + ".nextval,");
                    }
                    break;
                case DataBaseType.PostgreSQL:
                    if (_action.AllowInsertID && primaryCell.Struct.IsAutoIncrement && !primaryCell.IsNullOrEmpty)
                    {
                        endChar = ") OVERRIDING SYSTEM  VALUE ";
                    }
                    break;
            }

            string sql = _TempSql.ToString().TrimEnd(',') + endChar + _TempSql2.ToString().TrimEnd(',') + ")";
            switch (_action.dalHelper.DataBaseType)
            {
                case DataBaseType.PostgreSQL:
                    if (primaryCell.Struct.IsAutoIncrement && !_action.AllowInsertID && group == DataGroupType.Number)
                    {
                        string key = Convert.ToString(primaryCell.Struct.DefaultValue);
                        if (!string.IsNullOrEmpty(key))
                        {
                            key = key.Replace("nextval", "currval");//nextval('sequence_name'::regclass);||nextval('"sequence_name"'::regclass);
                            string[] items = key.Split('\'');
                            key = items[0] + "'" + SqlFormat.Keyword(items[1].Trim('\"'), DataBaseType.PostgreSQL) + "'" + items[2];
                            sql = sql + "; select " + key + " as OutPutValue";
                        }
                        else
                        {
                            sql = sql + " RETURNING " + SqlFormat.Keyword(primaryCell.ColumnName, DataBaseType.PostgreSQL);
                        }
                    }
                    else if (!primaryCell.IsNullOrEmpty)
                    {
                        sql += string.Format("; select '{0}' as OutPutValue", SqlFormat.Keyword(primaryCell.StringValue, DataBaseType.PostgreSQL));
                    }
                    break;
                case DataBaseType.DaMeng:
                    if (primaryCell.Struct.IsAutoIncrement && !_action.AllowInsertID && group == DataGroupType.Number)
                    {
                        sql = sql + " ;select @@IDENTITY as OutPutValue;";
                    }
                    break;
                case DataBaseType.MsSql:
                case DataBaseType.Sybase:
                    if (primaryCell.Struct.IsAutoIncrement && !_action.AllowInsertID && group == DataGroupType.Number)
                    {
                        if (_action.dalHelper.DataBaseType == DataBaseType.Sybase)
                        {
                            sql = sql + " select @@IDENTITY as OutPutValue";
                        }
                        else if (_action.dalHelper.DataBaseType == DataBaseType.MsSql)
                        {
                            sql += " select cast(scope_identity() as bigint) as OutPutValue";//�ĳ�bigint����ת���������
                        }
                    }
                    else if (!primaryCell.IsNullOrEmpty)
                    {
                        sql += string.Format(" select '{0}' as OutPutValue", primaryCell.Value);
                    }
                    if (insertAutoIncrement)//_action.AllowInsertID && !_action.dalHelper.IsOpenTrans && primaryCell.Struct.IsAutoIncrement)//����������ʱ
                    {
                        sql = "set identity_insert " + SqlFormat.Keyword(TableName, _action.dalHelper.DataBaseType) + " on " + sql + " set identity_insert " + SqlFormat.Keyword(TableName, _action.dalHelper.DataBaseType) + " off";
                    }
                    break;
                    //if (!(Parent.AllowInsertID && !primaryCell.IsNull)) // �������в���id�ģ����������������Զ�����id��
                    //{
                    //    sql += ((groupID == 1 && (primaryCell.IsNull || primaryCell.ToString() == "0")) ? " select cast(scope_identity() as int) as OutPutValue" : string.Format(" select '{0}' as OutPutValue", primaryCell.Value));
                    //}
                    //case DalType.Oracle:
                    //    sql += string.Format("BEGIN;select {0}.currval from dual; END;", Autoid);
                    //    break;
            }
            return sql;
        }
        /// <summary>
        /// ���ز�����Where�������ַ���
        /// </summary>
        /// <returns>�����:Update tableName set Name=@Name,Value=@Value</returns>
        internal string GetUpdateSql(object whereObj)
        {
            isCanDo = false;
            StringBuilder _TempSql = new StringBuilder();
            _TempSql.Append("Update " + SqlFormat.Keyword(TableName, _action.dalHelper.DataBaseType) + " set ");
            if (!string.IsNullOrEmpty(updateExpression))
            {
                _TempSql.Append(SqlCompatible.Format(updateExpression, _action.DataBaseType) + ",");
                updateExpression = null;//ȡ��ֵ�����ֵ��
                isCanDo = true;
            }
            string editTime = GetEditTimeSql();//�ڲ��жϸ��ֶ�û��ֵ�Ż���¡�
            if (!string.IsNullOrEmpty(editTime))
            {
                _TempSql.Append(editTime);//�Դ�β,��
            }
            MDataCell cell = null;
            for (int i = 0; i < _action.Data.Count; i++)
            {
                cell = _action.Data[i];
                if (cell.Struct.IsPrimaryKey || cell.Struct.IsAutoIncrement)
                {
                    continue;//���������������С�
                }

                if (cell.State > 1 && (cell.Struct.IsCanNull || !cell.IsNull))
                {
                    if (cell.Struct.SqlType == SqlDbType.Timestamp && (_action.DataBaseType == DataBaseType.MsSql || _action.DataBaseType == DataBaseType.Sybase))
                    {
                        //����ʱ�����������¡�
                        continue;
                    }
                    if (_action.DataBaseType == DataBaseType.MsSql && cell.Struct.SqlTypeName != null && cell.Struct.SqlTypeName.EndsWith("hierarchyId"))
                    {
                        _TempSql.Append(SqlFormat.Keyword(cell.ColumnName, _action.DataBaseType) + "=HierarchyID::Parse('" + cell.StringValue + "')" + ",");
                    }
                    else
                    {
                        if (_action.DataBaseType == DataBaseType.FoxPro)
                        {
                            string value = "\"" + cell.StringValue + "\",";
                            if (cell.Struct.SqlType == SqlDbType.DateTime)
                            {
                                value = "{^" + cell.StringValue + "},";
                            }
                            //��֧�ֲ�����
                            _TempSql.Append(SqlFormat.Keyword(cell.ColumnName, _action.DataBaseType) + "=" + value);
                        }
                        else
                        {
                            object value = cell.Value;
                            DbType dbType = DataType.GetDbType(cell.Struct.SqlType.ToString(), _action.DataBaseType);
                            if (dbType == DbType.String && cell.StringValue == "")
                            {
                                if (_action.DataBaseType == DataBaseType.Oracle && !cell.Struct.IsCanNull)
                                {
                                    value = " ";//Oracle not null �ֶΣ����������ÿ�ֵ��
                                }
                                if (_action.DataBaseType == DataBaseType.MySql && cell.Struct.MaxSize == 36)
                                {
                                    value = DBNull.Value;//MySql ��char36 �ᵱ��guid��������Ϊ�գ�ֻ��Ϊnull��
                                }
                            }
                            _action.dalHelper.AddParameters(_action.dalHelper.Pre + cell.ColumnName, value, dbType, cell.Struct.MaxSize, ParameterDirection.Input);
                            _TempSql.Append(SqlFormat.Keyword(cell.ColumnName, _action.DataBaseType) + "=" + _action.dalHelper.Pre + cell.ColumnName + ",");
                        }
                    }
                    isCanDo = true;
                }
            }
            if (!isCanDo)
            {
                string err = AppConst.HR + "warn : " + TableName + " can't find the data can be updated!";
                Log.Write(err, LogType.Warn);
                _action.dalHelper.DebugInfo.Append(err);
            }
            //switch (_action.dalHelper.dalType)
            //{
            //    case DalType.Oracle:
            //    case DalType.SQLite:
            //        _TempSql = _TempSql.Replace("[", "").Replace("]", "");
            //        break;
            //    case DalType.MySql:
            //        _TempSql = _TempSql.Replace("[", "`").Replace("]", "`");
            //        break;
            //}
            _TempSql = _TempSql.Remove(_TempSql.Length - 1, 1);
            _TempSql.Append(" where " + FormatWhere(whereObj));
            return _TempSql.ToString();
        }
        internal string GetCountSql(object whereObj)
        {
            string where = FormatWhere(whereObj);
            if (!string.IsNullOrEmpty(where))
            {
                where = " where " + where;
                where = RemoveOrderBy(where);
            }

            return "select count(*) from " + SqlFormat.Keyword(TableName, _action.dalHelper.DataBaseType) + where;
        }
        internal string GetExistsSql(object whereObj)
        {
            return GetTopOneSql(whereObj, "1");
        }
        internal string GetTopOneSql(object whereObj)
        {
            return GetTopOneSql(whereObj, null);
        }
        private string GetTopOneSql(object whereObj, string customColumn)
        {
            string columnNames = !string.IsNullOrEmpty(customColumn) ? customColumn : GetColumnsSql();
            switch (_action.dalHelper.DataBaseType)
            {
                case DataBaseType.Txt:
                case DataBaseType.Xml:
                case DataBaseType.Sybase:
                //return "set rowcount 1 select " + columnNames + " from " + TableName + " where " + FormatWhere(whereObj) + " set rowcount 0";
                case DataBaseType.MsSql:
                case DataBaseType.Access:
                case DataBaseType.Excel:
                case DataBaseType.DaMeng:
                case DataBaseType.KingBaseES:
                    return "select top 1 " + columnNames + " from " + SqlFormat.Keyword(TableName, _action.dalHelper.DataBaseType) + " where " + FormatWhere(whereObj);
                case DataBaseType.FoxPro://��Ҫ��order by 
                    return "select top 1 " + columnNames + " from " + SqlFormat.Keyword(TableName, _action.dalHelper.DataBaseType) + " where " + AddOrderByWithCheck(FormatWhere(whereObj), _action.Data.PrimaryCell.ColumnName);
                case DataBaseType.Oracle:
                    return "select " + columnNames + " from " + SqlFormat.Keyword(TableName, _action.dalHelper.DataBaseType) + " where rownum=1 and " + FormatWhere(whereObj);
                case DataBaseType.SQLite:
                case DataBaseType.MySql:
                case DataBaseType.PostgreSQL:
                    return "select " + columnNames + " from " + SqlFormat.Keyword(TableName, _action.dalHelper.DataBaseType) + " where " + FormatWhere(whereObj) + " limit 1";
                case DataBaseType.FireBird:
                    return "select first 1 " + columnNames + " from " + SqlFormat.Keyword(TableName, _action.dalHelper.DataBaseType) + " where " + FormatWhere(whereObj);
            }
            return (string)Error.Throw(string.Format("GetTopOneSql:{0} No Be Support Now!", _action.dalHelper.DataBaseType.ToString()));
        }
        internal string GetBindSql(object whereObj, object text, object value)
        {
            if (whereObj != null && Convert.ToString(whereObj).Length > 0)
            {
                whereObj = " where " + FormatWhere(whereObj);
            }
            string t = SqlFormat.Keyword(Convert.ToString(text), _action.dalHelper.DataBaseType);
            string v = SqlFormat.Keyword(Convert.ToString(value), _action.dalHelper.DataBaseType);
            string key = t != v ? (v + "," + t) : t;
            return string.Format("select distinct {0} from {1} {2}", key, SqlFormat.Keyword(TableName, _action.dalHelper.DataBaseType), FormatWhere(whereObj));
        }
        internal string GetMaxID()
        {
            switch (_action.dalHelper.DataBaseType)
            {
                case DataBaseType.Oracle:
                    return string.Format("select {0}.currval from dual", AutoID);
                default:
                    //case DalType.MsSql:
                    //case DalType.Sybase:
                    //case DalType.MySql:
                    //case DalType.SQLite:
                    //case DalType.Access:
                    //string columnName = _action.Data.Columns.FirstPrimary.ColumnName;
                    //string tableName = TableName;
                    ////if (_action.dalHelper.DataBaseType == DataBaseType.PostgreSQL)
                    ////{
                    //columnName = SqlFormat.Keyword(columnName, _action.dalHelper.DataBaseType);
                    //tableName = SqlFormat.Keyword(tableName, _action.dalHelper.DataBaseType);
                    // }
                    return string.Format("select max({0}) from {1}", SqlFormat.Keyword(_action.Data.Columns.FirstPrimary.ColumnName, _action.dalHelper.DataBaseType), SqlFormat.Keyword(TableName, _action.dalHelper.DataBaseType));

            }
            // return (string)Error.Throw(string.Format("GetMaxid:{0} No Be Support Now!", _action.dalHelper.dalType.ToString()));
        }
        internal string GetSelectTableName(ref string where)
        {
            string tName = _action.TableName;
            int i = tName.LastIndexOf(')');
            if (i > -1 && _action.DataBaseType == DataBaseType.Oracle)
            {
                tName = tName.Substring(0, i + 1);
            }
            if (selectColumns == null || selectColumns.Length == 0)//û��ָ��Ҫ��ϲ�ѯ��
            {
                return SqlFormat.Keyword(tName, _action.DataBaseType);
            }
            string whereOnly = string.Empty;
            if (!string.IsNullOrEmpty(where))
            {
                int orderbyIndex = where.ToLower().IndexOf("order by");
                if (orderbyIndex > -1)
                {
                    whereOnly = " where " + where.Substring(0, orderbyIndex - 1);//-1��ȥ���ո�
                    where = "1=1 " + where.Substring(orderbyIndex);
                }
                else
                {
                    whereOnly = " where " + where;
                    where = string.Empty;
                }
                whereOnly = SqlFormat.RemoveWhereOneEqualsOne(whereOnly);
            }
            string sql = "(select " + GetColumnsSql() + " from " + SqlFormat.Keyword(TableName, _action.DataBaseType) + " " + whereOnly + ") v";
            //if (_action.DalType != DalType.Oracle) // Oracle ȡ���˴洢���̡�
            //{
            //    sql += "v";
            //}
            return sql;
        }

        internal string GetColumnsSql()
        {
            if (selectColumns == null || selectColumns.Length == 0)
            {
                return "*";
            }
            string v_Columns = string.Empty;
            string columnName = string.Empty;
            foreach (object column in selectColumns)
            {

                columnName = column.ToString().Trim();
                if (columnName.IndexOf(' ') > -1 || columnName == "*")
                {
                    v_Columns += columnName + ",";
                }
                else
                {
                    int i = _action.Data.Columns.GetIndex(columnName);//�����ֶ�ӳ��
                    if (i > -1)
                    {
                        v_Columns += SqlFormat.Keyword(_action.Data.Columns[i].ColumnName, _action.dalHelper.DataBaseType) + ",";
                    }
                    else
                    {
                        _action.dalHelper.DebugInfo.Append(AppConst.HR + "warn : " + TableName + " no contains column " + columnName + AppConst.BR);
                    }
                }
            }
            if (v_Columns == string.Empty)
            {
                return "*";
            }
            return v_Columns.TrimEnd(',');
        }
        /// <summary>
        /// ��ȡ������ϵ�Where������
        /// </summary>
        /// <returns></returns>
        internal string GetPrimaryWhere()
        {
            return GetWhere(_action.DataBaseType, _action.Data.JointPrimaryCell);
        }
        #endregion
        #region ���ú���

        internal string AddOrderByWithCheck(string where, string primaryKey)
        {
            if (SqlFormat.NotKeyword(primaryKey).ToLower() != "id")
            {
                if (string.IsNullOrEmpty(where))
                {
                    where = "1=1";
                }
                where = AddOrderBy(where, primaryKey, _action.DataBaseType);
            }
            return where;
        }
        internal string FormatWhere(object whereObj)
        {
            string where = GetWhereFromObj(whereObj);
            return FormatWhere(where, _action.Data.Columns, _action.DataBaseType, _action.dalHelper.Com);
        }
        private string GetWhereFromObj(object whereObj)
        {
            if (whereObj == null)
            {
                return string.Empty;
            }
            else if (whereObj is String || (whereObj is ValueType && !(whereObj is Enum)))
            {
                return Convert.ToString(whereObj);
            }
            //else if (whereObj is IField)
            //{
            //    return SqlFormat.GetIFieldSql(whereObj);
            //}
            MDataCell cell = null;
            if (whereObj is Enum)
            {
                cell = _action.Data[(int)whereObj];
            }
            else if (whereObj is MDataCell)
            {
                cell = whereObj as MDataCell;
            }
            else
            {
                string propName = MBindUI.GetID(whereObj);
                if (!string.IsNullOrEmpty(propName))
                {
                    _action.UI.Get(whereObj, null, null);
                    cell = _action.Data[propName];
                }
            }
            string where = string.Empty;
            if (cell != null)
            {
                #region �ӵ�Ԫ����ȡֵ��
                if (cell.IsNullOrEmpty)
                {
                    isCanDo = false;
                    _action.dalHelper.RecordsAffected = -2;
                    _action.dalHelper.DebugInfo.Append(AppConst.HR + "error : " + cell.ColumnName + " can't be null" + AppConst.BR);
                    return "1=2 and " + cell.ColumnName + " is null";
                }
                switch (_action.dalHelper.DataBaseType)
                {
                    case DataBaseType.Txt:
                    case DataBaseType.Xml:
                        switch (DataType.GetGroup(cell.Struct.SqlType))
                        {
                            case DataGroupType.Number:
                            case DataGroupType.Bool:
                                where = cell.ColumnName + "=" + cell.Value;
                                break;
                            default:
                                where = cell.ColumnName + "='" + cell.Value + "'";
                                break;
                        }
                        break;
                    default:
                        string cName = cell.ColumnName;
                        if (cell.Struct.MDataColumn != null)
                        {
                            cName = SqlFormat.Keyword(cell.ColumnName, cell.Struct.MDataColumn.DataBaseType);
                        }
                        where = cName + "=" + _action.dalHelper.Pre + cell.ColumnName;
                        _action.dalHelper.AddParameters(cell.ColumnName, cell.Value, DataType.GetDbType(cell.Struct.ValueType), cell.Struct.MaxSize, ParameterDirection.Input);
                        break;
                }
                #endregion
            }
            return where;
        }
        #endregion

        #endregion
    }
    internal partial class SqlCreate
    {
        internal static string FormatWhere(string where, MDataColumn mdc, DataBaseType dalType, DbCommand com)
        {
            if (string.IsNullOrEmpty(where))
            {
                return string.Empty;
            }
            if (mdc == null || mdc.Count == 0) { return "error : Column.Count=0"; }
            where = SqlFormat.Compatible(where.TrimEnd(), dalType, com == null || com.Parameters.Count == 0);
            if (dalType == DataBaseType.MySql)
            {
                where = SqlFormat.FormatMySqlBit(where, mdc);
            }
            else if (dalType == DataBaseType.Oracle)
            {
                where = SqlFormat.FormatOracleDateTime(where, mdc);
            }
            string lowerWhere = where.ToLower().TrimStart();
            if (lowerWhere.StartsWith("order by"))
            {
                where = "1=1 " + where;
            }
            else if (lowerWhere.IndexOfAny(new char[] { '=', '>', '<' }) == -1 && !lowerWhere.Contains(" like ") && !lowerWhere.Contains(" between ")
                && !lowerWhere.Contains(" in ") && !lowerWhere.Contains(" in(") && !lowerWhere.Contains(" is "))
            {
                //����Ƿ��order by 'xxxx order by xxx desc'
                int index = where.Replace("\n", " ").IndexOf(" order by ", StringComparison.OrdinalIgnoreCase);//���ǿ��ܻ��е����xxxx\r\norder by xxx
                string orderBy = string.Empty;
                if (index > -1)
                {
                    orderBy = where.Substring(index + 1, where.Length - index - 1);
                    where = where.Substring(0, index);
                }
                if (mdc.JointPrimary.Count > 1 && where.Contains(";"))
                {
                    #region �������
                    StringBuilder sb = new StringBuilder();
                    string[] items = where.Split(',');
                    MDataRow row = mdc.ToRow("row");
                    for (int i = 0; i < items.Length; i++)
                    {
                        string item = items[i];
                        if (!string.IsNullOrEmpty(item))
                        {
                            string[] values = item.Split(';');
                            for (int j = 0; j < row.JointPrimaryCell.Count; j++)
                            {
                                if (j < values.Length)
                                {
                                    row.JointPrimaryCell[j].Value = values[j];
                                }
                            }
                            if (i != 0)
                            {
                                sb.Append(" or ");
                            }
                            sb.Append("(" + GetWhere(dalType, row.JointPrimaryCell) + ")");
                        }
                    }
                    where = sb.ToString();
                    if (items.Length == 1)
                    {
                        where = where.Trim('(', ')');
                    }
                    items = null;
                    #endregion
                }
                else
                {
                    #region ��������

                    MCellStruct ms = mdc.FirstPrimary;

                    string[] items = where.Split(',');
                    if (items.Length == 1)
                    {
                        //ֻ������ֵ�����
                        DataGroupType primaryGroup = DataType.GetGroup(ms.SqlType);//����ƥ������
                        switch (primaryGroup)
                        {
                            case DataGroupType.Guid:
                                bool isOK = false;
                                if (where.Length == 36)
                                {
                                    try
                                    {
                                        new Guid(where);
                                        isOK = true;
                                    }
                                    catch
                                    {
                                    }
                                }
                                if (!isOK)
                                {
                                    ms = mdc.FirstUnique;
                                }
                                break;
                            case DataGroupType.Number:
                                long v;
                                if (!long.TryParse(where.Trim('\''), out v))
                                {
                                    ms = mdc.FirstUnique;
                                }
                                break;
                        }

                        string columnName = SqlFormat.Keyword(ms.ColumnName, dalType);
                        where = GetWhereEqual(DataType.GetGroup(ms.SqlType), columnName, where, dalType);
                    }
                    else
                    {
                        List<string> lists = new List<string>(items.Length);
                        lists.AddRange(items);
                        where = GetWhereIn(ms, lists, dalType);
                    }
                    #endregion
                }
                if (!string.IsNullOrEmpty(orderBy))
                {
                    where = where + " " + orderBy;
                }
            }

            return where;
        }
        private static string GetWhereEqual(DataGroupType group, string columnName, string where, DataBaseType dalType)
        {
            if (string.IsNullOrEmpty(where))
            {
                return string.Empty;
            }
            if (group != 0)
            {
                where = where.Trim('\'');
            }
            columnName = SqlFormat.Keyword(columnName, dalType);
            if (group == DataGroupType.Number)//int ���͵ģ�������Ϊbit�͡�
            {
                where = columnName + "=" + where;
            }
            else
            {
                if (group == DataGroupType.Guid)
                {

                    switch (dalType)
                    {
                        case DataBaseType.Access:// Access��GUID���͵ĸ��£����������������
                            where = "{" + where.Trim('{', '}') + "}";
                            break;
                        case DataBaseType.SQLite://SQLite ��16�ֽڴ洢����Ҫת�����ܲ�ѯ��
                            return columnName + "=x'" + StaticTool.ToGuidByteString(where) + "'";
                    }
                }
                where = columnName + "='" + where + "'";
            }
            return where;
        }



        internal static string RemoveOrderBy(string where)
        {
            int index = where.ToLower().IndexOf("order by");
            if (index > 0)
            {
                where = where.Substring(0, index);
            }
            return where;
        }


        internal static string AddOrderBy(string where, string primaryKey, DataBaseType dalType)
        {
            if (where.IndexOf("order by", StringComparison.OrdinalIgnoreCase) == -1)
            {
                where += " order by " + SqlFormat.Keyword(primaryKey, dalType) + " asc";
            }
            else if (where.IndexOf(" asc", StringComparison.OrdinalIgnoreCase) == -1 && where.IndexOf(" desc", StringComparison.OrdinalIgnoreCase) == -1)
            {
                //��order by ��û asc
                where += " asc";
            }
            return where;
        }
        internal static string GetWhere(DataBaseType dalType, List<MDataCell> cells)
        {
            return GetWhere(dalType, true, cells);
        }
        /// <summary>
        /// ����Ԫ���������where������
        /// </summary>
        /// <returns></returns>
        internal static string GetWhere(DataBaseType dalType, bool isAnd, List<MDataCell> cells)
        {
            string where = "";
            MDataCell cell;
            for (int i = 0; i < cells.Count; i++)
            {
                cell = cells[i];
                if (i > 0)
                {
                    where += (isAnd ? " and " : " or ");
                }
                DataGroupType group = DataType.GetGroup(cell.Struct.SqlType);
                string columnName = SqlFormat.Keyword(cell.ColumnName, dalType);
                switch (group)
                {
                    case DataGroupType.Number:
                        where += columnName + "=" + (cell.IsNullOrEmpty ? -9999 : cell.Value);
                        break;
                    case DataGroupType.Bool:
                        where += columnName + "=" + (cell.Value.ToString().ToLower() == "true" ? SqlValue.True : SqlValue.False);
                        break;
                    default:

                        if (group == DataGroupType.Guid)
                        {
                            string guid = cell.StringValue;
                            if (string.IsNullOrEmpty(guid) || guid.Length != 36)
                            {
                                return "1=2--('" + guid + "' is not guid)";
                            }
                            if (dalType == DataBaseType.Access)
                            {
                                where += columnName + "='{" + guid + "}'";
                            }
                            else if (dalType == DataBaseType.SQLite)
                            {
                                where += columnName + "=x'" + StaticTool.ToGuidByteString(guid) + "'";
                            }
                            else
                            {
                                where += columnName + "='" + guid + "'";
                            }
                        }
                        else if (group == DataGroupType.Date && dalType == DataBaseType.Oracle) // Oracle������ʱ��Ҫת����
                        {
                            if (cell.Struct.SqlType == SqlDbType.Timestamp)
                            {
                                where += columnName + "=to_timestamp('" + cell.StringValue + "','yyyy-MM-dd HH24:MI:ss.ff')";
                            }
                            else
                            {
                                where += columnName + "=to_date('" + cell.StringValue + "','yyyy-mm-dd hh24:mi:ss')";
                            }
                        }
                        else if (group == 0 && dalType == DataBaseType.MsSql && cell.Struct.SqlTypeName != null && cell.Struct.SqlTypeName.EndsWith("hierarchyId"))
                        {
                            where += columnName + "=HierarchyID::Parse('" + cell.StringValue + "')";
                        }
                        else
                        {
                            //����HID=HierarchyID::Parse('/1/2/3/')
                            where += columnName + "='" + cell.Value + "'";
                        }
                        break;
                }
            }
            return where;
        }

        internal static string GetWhereIn(MCellStruct ms, List<string> items, DataBaseType dalType)
        {
            if (items == null || items.Count == 0)
            {
                return "1=2";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(SqlFormat.Keyword(ms.ColumnName, dalType));
            sb.Append(" In (");
            DataGroupType group = DataType.GetGroup(ms.SqlType);
            string item;
            for (int i = 0; i < items.Count; i++)
            {
                item = items[i];
                if (string.IsNullOrEmpty(item))
                {
                    continue;
                }
                if (group != DataGroupType.Text)
                {
                    item = item.Trim('\'');//������ĸ������ȥ���ֺ�
                }
                if (dalType == DataBaseType.Oracle && i > 999 && i % 1000 == 0)//oracle �б��е��������Ϊ1000
                {
                    sb.Remove(sb.Length - 1, 1);//�Ƴ����һ��,�š�
                    sb.Append(") or " + SqlFormat.Keyword(ms.ColumnName, dalType) + " In (");
                }
                if (!string.IsNullOrEmpty(item))
                {
                    if (group == DataGroupType.Number)
                    {
                        sb.Append(item + ",");
                    }
                    else
                    {
                        if (group == DataGroupType.Guid && dalType == DataBaseType.SQLite)
                        {
                            sb.Append("x'" + StaticTool.ToGuidByteString(item) + "',");
                        }
                        else if (group == DataGroupType.Date && dalType == DataBaseType.Oracle)
                        {
                            if (ms.SqlType == SqlDbType.Timestamp)
                            {
                                sb.Append("to_timestamp('" + item + "','yyyy-MM-dd HH24:MI:ss.ff'),");
                            }
                            else
                            {
                                sb.Append("to_date('" + item + "','yyyy-mm-dd hh24:mi:ss'),");
                            }
                        }
                        else
                        {
                            sb.Append("'" + item + "',");
                        }
                    }
                }
            }
            return sb.ToString().TrimEnd(',') + ")";
        }

        internal static string MySqlBulkCopySql = "SET GLOBAL local_infile=1;LOAD DATA LOCAL INFILE '{0}' INTO TABLE {1} CHARACTER SET utf8 FIELDS TERMINATED BY '{2}' LINES TERMINATED BY '|\r\n|' {3}";
        internal static string OracleBulkCopySql = "LOAD DATA INFILE '{0}' APPEND INTO TABLE {1} FIELDS TERMINATED BY '{2}' OPTIONALLY ENCLOSED BY '\"' {3}";
        internal static string OracleSqlldr = " userid={0} control='{1}'";//sqlldr   
        internal static string TruncateTable = "truncate table {0}";
        /// <summary>
        /// ������������������
        /// </summary>
        /// <param name="mdc"></param>
        /// <param name="keepID"></param>
        /// <param name="dalType"></param>
        /// <returns></returns>
        internal static string GetColumnName(MDataColumn mdc, bool keepID, DataBaseType dalType)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            foreach (MCellStruct ms in mdc)
            {
                if (!keepID && ms.IsAutoIncrement)
                {
                    continue;
                }
                sb.Append(SqlFormat.Keyword(ms.ColumnName, dalType));
                if (dalType == DataBaseType.Oracle && DataType.GetGroup(ms.SqlType) == DataGroupType.Date)
                {
                    //����
                    sb.Append(" DATE \"YYYY-MM-DD HH24:MI:SS\" NULLIF (" + ms.ColumnName + "=\"NULL\")");
                }
                sb.Append(",");
            }
            return sb.ToString().TrimEnd(',') + ")";
        }

        internal static string SqlToViewSql(string sqlObj)
        {
            string sql = sqlObj.ToLower().Trim();
            if (sql.StartsWith("select ") || (sql.Contains(" ") && sql[0] != '('))
            {
                sqlObj = "(" + sqlObj + ") v";
            }
            return sqlObj.Trim();
        }
    }
}
