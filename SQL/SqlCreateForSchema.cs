using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;
using System.Data;
using CYQ.Data.Tool;

namespace CYQ.Data.SQL
{
    /// <summary>
    /// SQL 结构语句
    /// </summary>
    internal class SqlCreateForSchema
    {
        #region 获取CreateTable相关SQL语句
        /// <summary>
        /// 获取单个列的表描述语句
        /// </summary>
        internal static string GetTableDescriptionSql(string tableName, MCellStruct mcs, DataBaseType dalType, bool isAdd)
        {
            switch (dalType)
            {
                case DataBaseType.MsSql:
                    string spName = isAdd ? "sp_addextendedproperty" : "sp_updateextendedproperty";
                    return string.Format("exec {3} N'MS_Description', N'{0}', N'user', N'dbo', N'table', N'{1}', N'column', N'{2}'", mcs.Description, tableName, mcs.ColumnName, spName);
                case DataBaseType.Oracle:
                    return string.Format("comment on column {0}.{1}  is '{2}'", tableName.ToUpper(), mcs.ColumnName.ToUpper(), mcs.Description);
            }

            return string.Empty;
        }


        /// <summary>
        /// 获取指定的表架构生成的SQL(Create Table)的说明语句
        /// </summary>
        internal static string CreateTableDescriptionSql(string tableName, MDataColumn columns, DataBaseType dalType)
        {
            string result = string.Empty;
            switch (dalType)
            {
                case DataBaseType.MsSql:
                case DataBaseType.Oracle:
                case DataBaseType.PostgreSQL:
                case DataBaseType.MySql:
                case DataBaseType.DB2:
                    StringBuilder sb = new StringBuilder();
                    foreach (MCellStruct mcs in columns)
                    {
                        if (!string.IsNullOrEmpty(mcs.Description))
                        {
                            if (dalType == DataBaseType.MsSql)
                            {
                                sb.AppendFormat("exec sp_addextendedproperty N'MS_Description', N'{0}', N'user', N'dbo', N'table', N'{1}', N'column', N'{2}';\r\n", mcs.Description, tableName, mcs.ColumnName);
                            }
                            else if (dalType == DataBaseType.Oracle || dalType == DataBaseType.DB2)
                            {
                                sb.AppendFormat("comment on column {0}.{1}  is '{2}';\r\n", tableName.ToUpper(), mcs.ColumnName.ToUpper(), mcs.Description);
                            }
                            else if (dalType == DataBaseType.PostgreSQL)
                            {
                                sb.AppendFormat("comment on column {0}.{1}  is '{2}';\r\n",
                                   SqlFormat.Keyword(tableName, DataBaseType.PostgreSQL), SqlFormat.Keyword(mcs.ColumnName, DataBaseType.PostgreSQL), mcs.Description);
                            }
                        }
                    }
                    if (dalType == DataBaseType.MsSql)//增加表的描述
                    {
                        sb.AppendFormat("exec sp_addextendedproperty N'MS_Description', N'{0}', N'user', N'dbo', N'table', N'{1}';\r\n", columns.Description, tableName);
                    }
                    else if (dalType == DataBaseType.Oracle || dalType == DataBaseType.DB2)
                    {
                        sb.AppendFormat("comment on table {0}  is '{1}';\r\n",
                                tableName.ToUpper(), columns.Description);
                    }
                    else if (dalType == DataBaseType.PostgreSQL)
                    {
                        sb.AppendFormat("comment on table {0}  is '{1}';\r\n",
                                SqlFormat.Keyword(tableName, DataBaseType.PostgreSQL), columns.Description);
                    }
                    else if (dalType == DataBaseType.MySql)
                    {
                        sb.AppendFormat("comment = '{0}';\r\n", columns.Description);
                    }
                    result = sb.ToString().TrimEnd(';');
                    break;
            }

            return result;
        }


        /// <summary>
        /// 获取指定的表架构生成的SQL(Create Table)语句
        /// </summary>
        internal static string CreateTableSql(string tableName, MDataColumn columns, DataBaseType dalType, string version)
        {
            switch (dalType)
            {
                case DataBaseType.Txt:
                case DataBaseType.Xml:
                    return columns.ToJson(true);
                default:
                    string createSql = string.Empty;
                    createSql = "CREATE TABLE " + SqlFormat.Keyword(tableName, dalType) + " \n(";

                    //读取主键的个数，如果是联合主键，则不设置主键。
                    List<MCellStruct> primaryKeyList = new List<MCellStruct>();
                    foreach (MCellStruct column in columns)
                    {
                        if (column.IsPrimaryKey)
                        {
                            primaryKeyList.Add(column);
                        }
                    }
                    foreach (MCellStruct column in columns)
                    {
                        createSql += "\n    " + GetKey(column, dalType, ref primaryKeyList, version);
                    }
                    if (primaryKeyList.Count > 0)
                    {
                        createSql += GetUnionPrimaryKey(dalType, primaryKeyList);
                    }
                    createSql = createSql.TrimEnd(',') + " \n)";
                    // createSql += GetSuffix(dalType);
                    if (dalType == DataBaseType.MySql && createSql.IndexOf("CURRENT_TIMESTAMP") != createSql.LastIndexOf("CURRENT_TIMESTAMP"))
                    {
                        createSql = createSql.Replace("Default CURRENT_TIMESTAMP", string.Empty);//mysql不允许存在两个以上的CURRENT_TIMESTAMP。
                    }
                    primaryKeyList.Clear();
                    return createSql;
            }
        }

        private static string GetKey(MCellStruct column, DataBaseType dalType, ref List<MCellStruct> primaryKeyList, string version)
        {
            string key = SqlFormat.Keyword(column.ColumnName, dalType);//列名。
            int groupID = DataType.GetGroup(column.SqlType);//数据库类型。
            bool isAutoOrPKey = column.IsPrimaryKey || column.IsAutoIncrement;//是否主键或自增列。
            if (dalType != DataBaseType.Access || !isAutoOrPKey || !column.IsAutoIncrement)
            {
                SqlDbType sdt = column.SqlType;
                if (sdt == SqlDbType.DateTime && dalType == DataBaseType.MySql && Convert.ToString(column.DefaultValue) == SqlValue.GetDate)
                {
                    sdt = SqlDbType.Timestamp;
                }
                key += " " + DataType.GetDataType(column, dalType, version);
            }
            if (isAutoOrPKey)
            {
                if (column.IsAutoIncrement)
                {
                    if (primaryKeyList.Count == 0 || (!column.IsPrimaryKey && dalType == DataBaseType.MySql))//MySql 的自增必须是主键.
                    {
                        column.IsPrimaryKey = true;
                        primaryKeyList.Insert(0, column);
                    }
                }
                switch (dalType)
                {
                    case DataBaseType.Access:
                        if (column.IsAutoIncrement)
                        {
                            key += " autoincrement(1,1)";
                        }
                        else// 主键。
                        {
                            if (groupID == 4)//主键又是GUID
                            {
                                key += " default GenGUID()";
                            }
                        }
                        break;
                    case DataBaseType.MsSql:
                        if (column.IsAutoIncrement)
                        {
                            key += " IDENTITY(1,1)";
                        }
                        else
                        {
                            if (groupID == 4)//主键又是GUID
                            {
                                key += " Default (newid())";
                            }
                        }
                        break;
                    case DataBaseType.Oracle:
                        if (Convert.ToString(column.DefaultValue) == SqlValue.Guid)//主键又是GUID
                        {
                            key += " Default (SYS_GUID())";
                        }
                        break;
                    case DataBaseType.Sybase:
                        if (column.IsAutoIncrement)
                        {
                            key += " IDENTITY";
                        }
                        else
                        {
                            if (groupID == 4)//主键又是GUID
                            {
                                key += " Default (newid())";
                            }
                        }
                        break;
                    case DataBaseType.MySql:
                        if (column.IsAutoIncrement)
                        {
                            key += " AUTO_INCREMENT";
                            if (!column.IsPrimaryKey)
                            {
                                primaryKeyList.Add(column);
                            }
                        }
                        break;
                    case DataBaseType.SQLite://sqlite的AUTOINCREMENT不能写在primarykey前,
                        if (column.IsAutoIncrement)
                        {
                            key += " PRIMARY KEY AUTOINCREMENT";
                            primaryKeyList.Clear();//如果有自增加，只允许存在这一个主键。
                        }
                        break;
                    case DataBaseType.PostgreSQL:
                        if (column.IsAutoIncrement && key.EndsWith("int"))
                        {
                            key = key.Substring(0, key.Length - 3) + "serial";
                        }
                        break;
                    case DataBaseType.DB2:
                        if (column.IsAutoIncrement)
                        {
                            key += " GENERATED ALWAYS AS IDENTITY";
                        }
                        break;
                }
                key += " NOT NULL";
            }
            else
            {
                string defaultValue = string.Empty;
                if (Convert.ToString(column.DefaultValue).Length > 0 && groupID < 5)//默认值只能是基础类型有。
                {
                    if (dalType == DataBaseType.MySql)
                    {
                        if ((groupID == 0 && (column.MaxSize < 1 || column.MaxSize > 8000)) || (groupID == 2 && key.Contains("datetime"))) //只能对TIMESTAMP类型的赋默认值。
                        {
                            goto er;
                        }
                    }
                    defaultValue = SqlFormat.FormatDefaultValue(dalType, column.DefaultValue, 1, column.SqlType);
                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        if (dalType == DataBaseType.MySql) { defaultValue = defaultValue.Trim('(', ')'); }
                        key += " Default " + defaultValue;
                    }
                }

            er:
                if (dalType != DataBaseType.Access)
                {
                    if (dalType == DataBaseType.Sybase && column.SqlType == SqlDbType.Bit)
                    {
                        if (string.IsNullOrEmpty(defaultValue))
                        {
                            key += " Default 0";
                        }
                        key += " NOT NULL";//Sybase bit 不允许为Null
                    }
                    else
                    {
                        if (dalType == DataBaseType.DB2 && column.IsCanNull) { }//db2 不用null
                        else
                        {
                            key += column.IsCanNull ? " NULL" : " NOT NULL";
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(column.Description))
            {
                switch (dalType)
                {
                    case DataBaseType.MySql:
                        key += string.Format(" COMMENT '{0}'", column.Description.Replace("'", "''"));
                        break;
                }
            }
            return key + ",";
        }
        private static string GetUnionPrimaryKey(DataBaseType dalType, List<MCellStruct> primaryKeyList)
        {
            string suffix = "\n    ";

            switch (dalType)
            {
                //case DalType.Access:
                //case DalType.SQLite:
                //case DalType.MySql:
                //case DalType.Oracle:
                //case DalType.MsSql:
                //case DalType.Sybase:
                default:
                    suffix += "PRIMARY KEY (";
                    foreach (MCellStruct st in primaryKeyList)
                    {
                        suffix += SqlFormat.Keyword(st.ColumnName, dalType) + ",";
                    }
                    suffix = suffix.TrimEnd(',') + ")";
                    break;
            }
            return suffix;
        }
        #endregion

        #region 获取AlterTable相关SQL语句

        /// <summary>
        /// 获取指定的表架构生成的SQL(Alter Table)语句
        /// </summary>
        public static List<string> AlterTableSql(string tableName, MDataColumn columns, string conn)
        {
            List<string> sql = new List<string>();
            string version = null;
            DataBaseType dalType;
            using (DalBase helper = DalCreate.CreateDal(conn))
            {
                helper.ChangeDatabaseWithCheck(tableName);//检测dbname.dbo.tablename的情况
                if (!helper.TestConn(AllowConnLevel.Master))
                {
                    helper.Dispose();
                    return sql;
                }
                dalType = helper.DataBaseType;
                version = helper.Version;
            }
            MDataColumn dbColumn = TableSchema.GetColumns(tableName, conn);//获取数据库的列结构
            if (dbColumn == null || dbColumn.Count == 0) { return sql; }

            //开始比较异同
            List<MCellStruct> primaryKeyList = new List<MCellStruct>();
            string tbName = SqlFormat.Keyword(tableName, dalType);
            string alterTable = "alter table " + tbName;
            foreach (MCellStruct ms in columns)//遍历新的结构
            {
                string cName = SqlFormat.Keyword(ms.ColumnName, dalType);
                if (ms.AlterOp != AlterOp.None)
                {
                    bool isContains = dbColumn.Contains(ms.ColumnName);
                    AlterOp op = ms.AlterOp;
                    if ((op & AlterOp.Rename) != 0)
                    {
                        op = (AlterOp)(op - AlterOp.Rename);
                        #region MyRegion Rename
                        if (!string.IsNullOrEmpty(ms.OldName) && ms.OldName != ms.ColumnName && !isContains)
                        {
                            string oName = SqlFormat.Keyword(ms.OldName, dalType);
                            switch (dalType)
                            {
                                case DataBaseType.MsSql:
                                    sql.Add("exec sp_rename '" + tbName + "." + oName + "', '" + ms.ColumnName + "', 'column'");
                                    break;
                                case DataBaseType.Sybase:
                                    sql.Add("exec sp_rename \"" + tableName + "." + ms.OldName + "\", " + ms.ColumnName);
                                    break;
                                case DataBaseType.MySql:
                                    sql.Add(alterTable + " change " + oName + " " + GetKey(ms, dalType, ref primaryKeyList, version).TrimEnd(','));
                                    break;
                                case DataBaseType.Oracle:

                                    sql.Add(alterTable + " rename column " + oName + " to " + cName);
                                    break;
                            }
                            isContains = isContains || dbColumn.Contains(ms.OldName);
                        }
                        #endregion
                    }

                    if (op == AlterOp.Drop)
                    {
                        #region MyRegion
                        if (isContains)
                        {
                            switch (dalType)
                            {
                                case DataBaseType.MsSql:
                                case DataBaseType.Access:
                                case DataBaseType.MySql:
                                case DataBaseType.Oracle:
                                    if (dalType == DataBaseType.MsSql)
                                    {
                                        sql.Add(@"declare @name varchar(50) select  @name =b.name from sysobjects b join syscolumns a on b.id = a.cdefault 
where a.id = object_id('" + tableName + "') and a.name ='" + ms.ColumnName + "'if(@name!='') begin   EXEC('alter table " + tableName + " drop constraint '+ @name) end");
                                    }
                                    sql.Add(alterTable + " drop column " + cName);
                                    break;
                                case DataBaseType.Sybase:
                                    sql.Add(alterTable + " drop " + cName);
                                    break;
                            }
                        }
                        #endregion
                    }
                    //else if (ms.AlterOp == AlterOp.Rename)
                    //{

                    //}
                    else if (op == AlterOp.AddOrModify)
                    {
                        string alterSql = SqlFormat.Keyword(ms.ColumnName, dalType) + " " + DataType.GetDataType(ms, dalType, version);
                        //智能判断
                        if (isContains) // 存在，则修改
                        {
                            //检测是否相同
                            MCellStruct dbStruct = dbColumn[ms.ColumnName] ?? dbColumn[ms.OldName];
                            if (dbStruct.IsCanNull != ms.IsCanNull || dbStruct.SqlType != ms.SqlType || dbStruct.MaxSize != ms.MaxSize || dbStruct.Scale != ms.Scale)
                            {
                                string modify = "";
                                switch (dalType)
                                {
                                    case DataBaseType.Oracle:
                                    case DataBaseType.Sybase:
                                        modify = " modify ";
                                        break;
                                    case DataBaseType.MySql:
                                        modify = " change " + cName + " ";
                                        break;
                                    case DataBaseType.MsSql:
                                    case DataBaseType.Access:
                                    case DataBaseType.PostgreSQL:
                                        modify = " alter column ";
                                        break;
                                }
                                if (ms.IsCanNull != dbStruct.IsCanNull)
                                {
                                    alterSql += (ms.IsCanNull ? " NULL" : " NOT NULL");
                                }
                                sql.Add(alterTable + modify + alterSql);
                            }
                        }
                        else //不在，则添加
                        {
                            sql.Add(alterTable + " add " + GetKey(ms, dalType, ref primaryKeyList, version).TrimEnd(','));
                            if (!string.IsNullOrEmpty(ms.Description))
                            {
                                string description = SqlCreateForSchema.GetTableDescriptionSql(tableName, ms, dalType, true);
                                if (!string.IsNullOrEmpty(description))
                                {
                                    sql.Add(description);
                                }
                            }
                        }
                    }
                }

            }
            return sql;
        }
        #endregion
    }
}
