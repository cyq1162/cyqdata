using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;

using CYQ.Data.Extension;
using System.Data;
using CYQ.Data.Tool;
using System.Data.Common;
using CYQ.Data.UI;


namespace CYQ.Data.SQL
{
    /// <summary>
    /// 数据操作语句类
    /// </summary>
    internal partial class SqlCreate
    {
        private static List<string> _autoIDItems = new List<string>();
        /// <summary>
        /// oracle序列名称
        /// </summary>
        public string AutoID
        {
            get
            {
                string key = string.Format(AppConfig.DB.AutoID, TableName);
                if (!_autoIDItems.Contains(key))
                {
                    //检测并自动创建。
                    Tool.DBTool.CheckAndCreateOracleSequence(key, _action.dalHelper.conn, _action.Data.PrimaryCell.ColumnName, TableName);
                    _autoIDItems.Add(key);
                }
                return key;
            }
        }
        /// <summary>
        /// 是否允许执行SQL操作（仅对Insert和Update有效；如果SQL语法错误，则拒绝执行）
        /// </summary>
        internal bool isCanDo = true;
        /// <summary>
        /// 更新操作的附加表达式。
        /// </summary>
        internal string updateExpression = null;
        /// <summary>
        /// 指定查询的列。
        /// </summary>
        internal object[] selectColumns = null;
        private string TableName
        {
            get
            {
                return SqlFormat.Keyword(_action.TableName, _action.DalType);
            }
        }
        private MAction _action;
        public SqlCreate(MAction action)
        {
            _action = action;
        }

        #region 组合Sql语句
        #region SQL语句处理
        internal string GetDeleteToUpdateSql(object whereObj)
        {
            string editTime = GetEditTimeSql();
            return "update " + TableName + " set " + editTime + AppConfig.DB.DeleteField + "=[#TRUE] where " + FormatWhere(whereObj);
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
                            return item + "='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',";//如果存在更新列
                        }
                    }
                }
            }
            return string.Empty;
        }
        internal string GetDeleteSql(object whereObj)
        {
            return "delete from " + TableName + " where " + FormatWhere(whereObj);
        }
        /// <summary>
        /// 返回插入的字符串
        /// </summary>
        /// <returns>结果如:insert into tableName(ID,Name,Value) values(@ID,@Name,@Value)</returns>
        internal string GetInsertSql()
        {
            isCanDo = false;
            StringBuilder _TempSql = new StringBuilder();
            StringBuilder _TempSql2 = new StringBuilder();
            _TempSql.Append("insert into " + TableName + "(");
            _TempSql2.Append(") Values(");
            MDataCell primaryCell = _action.Data[_action.Data.Columns.FirstPrimary.ColumnName];
            int groupID = DataType.GetGroup(primaryCell.Struct.SqlType);
            string defaultValue = Convert.ToString(primaryCell.Struct.DefaultValue);
            if (primaryCell.IsNullOrEmpty && (groupID == 4 || (groupID == 0 && (primaryCell.Struct.MaxSize <= 0 || primaryCell.Struct.MaxSize >= 36) &&
                (defaultValue == "" || defaultValue == "newid" || defaultValue == SqlValue.Guid))))//guid类型
            {
                primaryCell.Value = Guid.NewGuid();
            }
            MDataCell cell = null;
            for (int i = 0; i < _action.Data.Count; i++)
            {
                cell = _action.Data[i];
                if (cell.Struct.IsAutoIncrement && !_action.AllowInsertID)
                {
                    continue;//跳过自增列。
                }
                if (cell.IsNull && !cell.Struct.IsCanNull && cell.Struct.DefaultValue == null)
                {
                    _action.dalHelper.debugInfo.Append(AppConst.HR + "error : " + cell.ColumnName + " can't be null" + AppConst.BR);
                    _action.dalHelper.recordsAffected = -2;
                    isCanDo = false;
                    break;
                }
                if (cell.CellValue.State > 0)
                {
                    _TempSql.Append(SqlFormat.Keyword(cell.ColumnName, _action.DalType) + ",");
                    _TempSql2.Append(_action.dalHelper.Pre + cell.ColumnName + ",");
                    object value = cell.Value;
                    DbType dbType = DataType.GetDbType(cell.Struct.SqlType.ToString(), _action.DalType);
                    if (_action.DalType == DalType.Oracle && dbType == DbType.String && cell.strValue == "" && !cell.Struct.IsCanNull)
                    {
                        value = " ";
                    }
                    _action.dalHelper.AddParameters(_action.dalHelper.Pre + cell.ColumnName, value, dbType, cell.Struct.MaxSize, ParameterDirection.Input);
                    isCanDo = true;
                }
            }
            switch (_action.dalHelper.dalType)
            {
                case DalType.Oracle:
                    if (!_action.AllowInsertID && DataType.GetGroup(primaryCell.Struct.SqlType) == 1)
                    {
                        _TempSql.Append(primaryCell.ColumnName + ",");
                        _TempSql2.Append(AutoID + ".nextval,");
                    }
                    break;
            }

            string sql = _TempSql.ToString().TrimEnd(',') + _TempSql2.ToString().TrimEnd(',') + ")";
            switch (_action.dalHelper.dalType)
            {
                case DalType.MsSql:
                case DalType.Sybase:
                    if (primaryCell.Struct.IsAutoIncrement && !_action.AllowInsertID && groupID == 1)
                    {
                        if (_action.dalHelper.dalType == DalType.Sybase)
                        {
                            sql = sql + " select @@IDENTITY as OutPutValue";
                        }
                        else
                        {
                            sql += " select cast(scope_Identity() as int) as OutPutValue";
                        }
                    }
                    else if (!primaryCell.IsNullOrEmpty)
                    {
                        sql += string.Format(" select '{0}' as OutPutValue", primaryCell.Value);
                    }
                    if (_action.AllowInsertID && !_action.dalHelper.isOpenTrans && primaryCell.Struct.IsAutoIncrement)//非批量操作时
                    {
                        sql = "set identity_insert " + SqlFormat.Keyword(TableName, _action.dalHelper.dalType) + " on " + sql + " set identity_insert " + SqlFormat.Keyword(TableName, _action.dalHelper.dalType) + " off";
                    }
                    break;
                //if (!(Parent.AllowInsertID && !primaryCell.IsNull)) // 对于自行插入ID的，跳过，主操作会自动返回ID。
                //{
                //    sql += ((groupID == 1 && (primaryCell.IsNull || primaryCell.ToString() == "0")) ? " select cast(scope_Identity() as int) as OutPutValue" : string.Format(" select '{0}' as OutPutValue", primaryCell.Value));
                //}
                //case DalType.Oracle:
                //    sql += string.Format("BEGIN;select {0}.currval from dual; END;", AutoID);
                //    break;
            }
            return sql;
        }
        /// <summary>
        /// 返回不包括Where条件的字符串
        /// </summary>
        /// <returns>结果如:Update tableName set Name=@Name,Value=@Value</returns>
        internal string GetUpdateSql(object whereObj)
        {
            isCanDo = false;
            StringBuilder _TempSql = new StringBuilder();
            _TempSql.Append("Update " + TableName + " set ");
            if (!string.IsNullOrEmpty(updateExpression))
            {
                _TempSql.Append(SqlCompatible.Format(updateExpression, _action.DalType) + ",");
                updateExpression = null;//取完值后清除值。
                isCanDo = true;
            }
            string editTime = GetEditTimeSql();//内部判断该字段没有值才会更新。
            if (!string.IsNullOrEmpty(editTime))
            {
                _TempSql.Append(editTime);//自带尾,号
            }
            MDataCell cell = null;
            for (int i = 0; i < _action.Data.Count; i++)
            {
                cell = _action.Data[i];
                if (cell.Struct.IsPrimaryKey || cell.Struct.IsAutoIncrement)
                {
                    continue;//跳过自增或主键列。
                }

                if (cell.CellValue.State > 1 && (cell.Struct.IsCanNull || !cell.IsNull))
                {
                    if (cell.Struct.SqlType == SqlDbType.Timestamp && (_action.DalType == DalType.MsSql || _action.DalType == DalType.Sybase))
                    {
                        //更新时间戳不允许更新。
                        continue;
                    }
                    object value = cell.Value;
                    DbType dbType = DataType.GetDbType(cell.Struct.SqlType.ToString(), _action.DalType);
                    if (_action.DalType == DalType.Oracle && dbType == DbType.String && cell.strValue == "" && !cell.Struct.IsCanNull)
                    {
                        value = " ";//Oracle not null 字段，不允许设置空值。
                    }

                    _action.dalHelper.AddParameters(_action.dalHelper.Pre + cell.ColumnName, value, dbType, cell.Struct.MaxSize, ParameterDirection.Input);
                    _TempSql.Append(SqlFormat.Keyword(cell.ColumnName, _action.DalType) + "=" + _action.dalHelper.Pre + cell.ColumnName + ",");
                    isCanDo = true;
                }
            }
            if (!isCanDo)
            {
                _action.dalHelper.debugInfo.Append(AppConst.HR + "Tip : Can not find the data can be updated!");
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

            return "select count(*) from " + TableName + where;
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
            switch (_action.dalHelper.dalType)
            {
                case DalType.Sybase:
                //return "set rowcount 1 select " + columnNames + " from " + TableName + " where " + FormatWhere(whereObj) + " set rowcount 0";
                case DalType.MsSql:
                case DalType.Access:
                    return "select top 1 " + columnNames + " from " + TableName + " where " + FormatWhere(whereObj);
                case DalType.Oracle:
                    return "select " + columnNames + " from " + TableName + " where rownum=1 and " + FormatWhere(whereObj);
                case DalType.SQLite:
                case DalType.MySql:
                    return "select " + columnNames + " from " + TableName + " where " + FormatWhere(whereObj) + " limit 1";
            }
            return (string)Error.Throw(string.Format("GetTopOneSql:{0} No Be Support Now!", _action.dalHelper.dalType.ToString()));
        }
        internal string GetBindSql(object whereObj, object text, object value)
        {
            if (whereObj != null && Convert.ToString(whereObj).Length > 0)
            {
                whereObj = " where " + FormatWhere(whereObj);
            }
            string t = Convert.ToString(text);
            string v = Convert.ToString(value);
            string key = t != v ? (v + "," + t) : t;
            return string.Format("select distinct {0} from {1} {2}", key, TableName, FormatWhere(whereObj));
        }
        internal string GetMaxID()
        {
            switch (_action.dalHelper.dalType)
            {
                case DalType.Oracle:
                    return string.Format("select {0}.currval from dual", AutoID);
                default:
                    //case DalType.MsSql:
                    //case DalType.Sybase:
                    //case DalType.MySql:
                    //case DalType.SQLite:
                    //case DalType.Access:
                    return string.Format("select max({0}) from {1}", _action.Data.Columns.FirstPrimary.ColumnName, TableName);

            }
            // return (string)Error.Throw(string.Format("GetMaxID:{0} No Be Support Now!", _action.dalHelper.dalType.ToString()));
        }
        internal string GetSelectTableName(ref string where)
        {
            string tName = _action.TableName;
            int i = tName.LastIndexOf(')');
            if (i > -1 && _action.DalType == DalType.Oracle)
            {
                tName = tName.Substring(0, i + 1);
            }
            if (selectColumns == null || selectColumns.Length == 0)//没有指定要组合查询。
            {
                return SqlFormat.Keyword(tName, _action.DalType);
            }
            string whereOnly = string.Empty;
            if (!string.IsNullOrEmpty(where))
            {
                int orderbyIndex = where.ToLower().IndexOf("order by");
                if (orderbyIndex > -1)
                {
                    whereOnly = " where " + where.Substring(0, orderbyIndex - 1);//-1是去掉空格
                    where = "1=1 " + where.Substring(orderbyIndex);
                }
                else
                {
                    whereOnly = " where " + where;
                    where = string.Empty;
                }
                whereOnly = SqlFormat.RemoveWhereOneEqualsOne(whereOnly);
            }
            string sql = "(select " + GetColumnsSql() + " from " + TableName + " " + whereOnly + ") v";
            //if (_action.DalType != DalType.Oracle) // Oracle 取消了存储过程。
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
                    int i = _action.Data.Columns.GetIndex(columnName);//兼容字段映射
                    if (i > -1)
                    {
                        v_Columns += SqlFormat.Keyword(_action.Data.Columns[i].ColumnName, _action.dalHelper.dalType) + ",";
                    }
                    else
                    {
                        _action.dalHelper.debugInfo.Append(AppConst.HR + "warn : " + TableName + " no contains column " + columnName + AppConst.BR);
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
        /// 获取主键组合的Where条件。
        /// </summary>
        /// <returns></returns>
        internal string GetPrimaryWhere()
        {
            return GetWhere(_action.DalType, _action.Data.JointPrimaryCell);
        }
        #endregion
        #region 共用函数

        internal string AddOrderByWithCheck(string where, string primaryKey)
        {
            if (SqlFormat.NotKeyword(primaryKey).ToLower() != "id")
            {
                if (string.IsNullOrEmpty(where))
                {
                    where = "1=1";
                }
                where = AddOrderBy(where, primaryKey);
            }
            return where;
        }
        internal string FormatWhere(object whereObj)
        {
            string where = GetWhereFromObj(whereObj);
            return FormatWhere(where, _action.Data.Columns, _action.DalType, _action.dalHelper.Com);
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
            else if (whereObj is IField)
            {
                return SqlFormat.GetIFieldSql(whereObj);
            }
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
                #region 从单元格里取值。
                if (cell.IsNullOrEmpty)
                {
                    isCanDo = false;
                    _action.dalHelper.recordsAffected = -2;
                    _action.dalHelper.debugInfo.Append(AppConst.HR + "error : " + cell.ColumnName + " can't be null" + AppConst.BR);
                    return "1=2 and " + cell.ColumnName + " is null";
                }
                switch (_action.dalHelper.dalType)
                {
                    case DalType.Txt:
                    case DalType.Xml:
                        switch (DataType.GetGroup(cell.Struct.SqlType))
                        {
                            case 1:
                            case 3:
                                where = cell.ColumnName + "=" + cell.Value;
                                break;
                            default:
                                where = cell.ColumnName + "='" + cell.Value + "'";
                                break;
                        }
                        break;
                    default:
                        where = cell.ColumnName + "=" + _action.dalHelper.Pre + cell.ColumnName;
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
        internal static string FormatWhere(string where, MDataColumn mdc, DalType dalType, DbCommand com)
        {
            if (string.IsNullOrEmpty(where))
            {
                return string.Empty;
            }
            where = SqlFormat.Compatible(where.TrimEnd(), dalType, com == null || com.Parameters.Count == 0);
            if (dalType == DalType.MySql)
            {
                where = SqlFormat.FormatMySqlBit(where, mdc);
            }
            else if (dalType == DalType.Oracle)
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
                if (mdc.JointPrimary.Count > 1 && where.Contains(";"))
                {
                    #region 多个主键
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
                    #region 单个主键

                    MCellStruct ms = mdc.FirstPrimary;

                    string[] items = where.Split(',');
                    if (items.Length == 1)
                    {
                        //只处理单个值的情况
                        int primaryGroupID = DataType.GetGroup(ms.SqlType);//优先匹配主键
                        switch (primaryGroupID)
                        {
                            case 4:
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
                            case 1:
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
            }

            return where;
        }
        private static string GetWhereEqual(int groupID, string columnName, string where, DalType dalType)
        {
            if (string.IsNullOrEmpty(where))
            {
                return string.Empty;
            }
            if (groupID != 0)
            {
                where = where.Trim('\'');
            }
            if (groupID == 1)//int 类型的，主键不为bit型。
            {
                where = columnName + "=" + where;
            }
            else
            {
                if (groupID == 4)
                {

                    switch (dalType)
                    {
                        case DalType.Access:// Access的GUID类型的更新，必须带｛｝包含。
                            where = "{" + where.Trim('{', '}') + "}";
                            break;
                        case DalType.SQLite://SQLite 以16字节存储，需要转换才能查询。
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


        internal static string AddOrderBy(string where, string primaryKey)
        {
            if (where.IndexOf("order by", StringComparison.OrdinalIgnoreCase) == -1)
            {
                where += " order by " + primaryKey + " asc";
            }
            else if (where.IndexOf(" asc", StringComparison.OrdinalIgnoreCase) == -1 && where.IndexOf(" desc", StringComparison.OrdinalIgnoreCase) == -1)
            {
                //有order by 但没 asc
                where += " asc";
            }
            return where;
        }
        internal static string GetWhere(DalType dalType, List<MDataCell> cells)
        {
            return GetWhere(dalType, true, cells);
        }
        /// <summary>
        /// 根据元数据列组合where条件。
        /// </summary>
        /// <returns></returns>
        internal static string GetWhere(DalType dalType, bool isAnd, List<MDataCell> cells)
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
                int groupID = DataType.GetGroup(cell.Struct.SqlType);
                string columnName = SqlFormat.Keyword(cell.ColumnName, dalType);
                switch (groupID)
                {
                    case 1:
                        where += columnName + "=" + (cell.IsNullOrEmpty ? -9999 : cell.Value);
                        break;
                    case 3:
                        where += columnName + "=" + (cell.Value.ToString().ToLower() == "true" ? SqlValue.True : SqlValue.False);
                        break;
                    default:

                        if (groupID == 4)
                        {
                            string guid = cell.strValue;
                            if (string.IsNullOrEmpty(guid) || guid.Length != 36)
                            {
                                return "1=2--('" + guid + "' is not guid)";
                            }
                            if (dalType == DalType.Access)
                            {
                                where += columnName + "='{" + guid + "}'";
                            }
                            else if (dalType == DalType.SQLite)
                            {
                                where += columnName + "=x'" + StaticTool.ToGuidByteString(guid) + "'";
                            }
                            else
                            {
                                where += columnName + "='" + guid + "'";
                            }
                        }
                        else if (groupID == 2 && dalType == DalType.Oracle) // Oracle的日期时间要转类型
                        {
                            if (cell.Struct.SqlType == SqlDbType.Timestamp)
                            {
                                where += columnName + "=to_timestamp('" + cell.strValue + "','yyyy-MM-dd HH24:MI:ss.ff')";
                            }
                            else
                            {
                                where += columnName + "=to_date('" + cell.strValue + "','yyyy-mm-dd hh24:mi:ss')";
                            }
                        }
                        else
                        {

                            where += columnName + "='" + cell.Value + "'";
                        }
                        break;
                }
            }
            return where;
        }

        internal static string GetWhereIn(MCellStruct ms, List<string> items, DalType dalType)
        {
            if (items == null || items.Count == 0)
            {
                return "1=2";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(SqlFormat.Keyword(ms.ColumnName, dalType));
            sb.Append(" In (");
            int groupID = DataType.GetGroup(ms.SqlType);
            string item;
            for (int i = 0; i < items.Count; i++)
            {
                item = items[i];
                if (groupID != 0)
                {
                    item = item.Trim('\'');//不是字母都尝试去掉分号
                }
                if (dalType == DalType.Oracle && i > 999 && i % 1000 == 0)//oracle 列表中的最大表达数为1000
                {
                    sb.Remove(sb.Length - 1, 1);//移除最后一个,号。
                    sb.Append(") or " + SqlFormat.Keyword(ms.ColumnName, dalType) + " In (");
                }
                if (!string.IsNullOrEmpty(item))
                {
                    if (groupID == 1)
                    {
                        sb.Append(item + ",");
                    }
                    else
                    {
                        if (groupID == 4 && dalType == DalType.SQLite)
                        {
                            sb.Append("x'" + StaticTool.ToGuidByteString(item) + "',");
                        }
                        else if (groupID == 2 && dalType == DalType.Oracle)
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

        internal static string MySqlBulkCopySql = "LOAD DATA LOCAL INFILE '{0}' INTO TABLE {1} CHARACTER SET utf8 FIELDS TERMINATED BY '{2}' LINES TERMINATED BY '|\r\n|' {3}";
        internal static string OracleBulkCopySql = "LOAD DATA INFILE '{0}' APPEND INTO TABLE {1} FIELDS TERMINATED BY '{2}' OPTIONALLY ENCLOSED BY '\"' {3}";
        internal static string OracleSqlIDR = " userid={0} control='{1}'";//sqlldr   
        internal static string TruncateTable = "truncate table {0}";
        /// <summary>
        /// 获得批量导入的列名。
        /// </summary>
        /// <param name="mdc"></param>
        /// <param name="keepID"></param>
        /// <param name="dalType"></param>
        /// <returns></returns>
        internal static string GetColumnName(MDataColumn mdc, bool keepID, DalType dalType)
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
                if (dalType == DalType.Oracle && DataType.GetGroup(ms.SqlType) == 2)
                {
                    //日期
                    sb.Append(" DATE \"YYYY-MM-DD HH24:MI:SS\" NULLIF (" + ms.ColumnName + "=\"NULL\")");
                }
                sb.Append(",");
            }
            return sb.ToString().TrimEnd(',') + ")";
        }

        internal static object SqlToViewSql(object sqlObj)
        {
            if (sqlObj is String)
            {
                string sql = Convert.ToString(sqlObj).ToLower().Trim();
                if (sql.StartsWith("select ") || (sql.Contains(" ") && sql[0] != '('))
                {
                    sqlObj = "(" + sqlObj + ") v";
                }
            }
            return sqlObj;
        }
    }
}
