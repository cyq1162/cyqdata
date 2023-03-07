using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;
using System.Data;
using CYQ.Data.Tool;

namespace CYQ.Data.SQL
{
    /// <summary>
    /// SQL �ṹ���
    /// </summary>
    internal class SqlCreateForSchema
    {
        #region ��ȡCreateTable���SQL���
        /// <summary>
        /// ��ȡ�����еı��������
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
        /// ��ȡָ���ı�ܹ����ɵ�SQL(Create Table)��˵�����
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
                    if (dalType == DataBaseType.MsSql)//���ӱ������
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
                        sb.AppendFormat("alter table {0} comment = '{1}';\r\n", SqlFormat.Keyword(tableName, DataBaseType.MySql), columns.Description);
                    }
                    result = sb.ToString().TrimEnd(';');
                    break;
            }

            return result;
        }


        /// <summary>
        /// ��ȡָ���ı�ܹ����ɵ�SQL(Create Table)���
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

                    //��ȡ�����ĸ��������������������������������
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
                        createSql = createSql.Replace("Default CURRENT_TIMESTAMP", string.Empty);//mysql����������������ϵ�CURRENT_TIMESTAMP��
                    }
                    primaryKeyList.Clear();
                    return createSql;
            }
        }

        private static string GetKey(MCellStruct column, DataBaseType dalType, ref List<MCellStruct> primaryKeyList, string version)
        {
            string key = SqlFormat.Keyword(column.ColumnName, dalType);//������
            DataGroupType group = DataType.GetGroup(column.SqlType);//���ݿ����͡�
            bool isAutoOrPKey = column.IsPrimaryKey || column.IsAutoIncrement;//�Ƿ������������С�
            if (dalType != DataBaseType.Access || !isAutoOrPKey || !column.IsAutoIncrement)
            {
                SqlDbType sdt = column.SqlType;
                if (sdt == SqlDbType.DateTime && dalType == DataBaseType.MySql && Convert.ToString(column.DefaultValue) == SqlValue.GetDate)
                {
                    column.SqlType= SqlDbType.Timestamp;
                }
                key += " " + DataType.GetDataType(column, dalType, version);
                column.SqlType = sdt;
            }
            if (isAutoOrPKey)
            {
                if (column.IsAutoIncrement)
                {
                    if (primaryKeyList.Count == 0 || (!column.IsPrimaryKey && dalType == DataBaseType.MySql))//MySql ����������������.
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
                        else// ������
                        {
                            if (group == DataGroupType.Guid)//��������GUID
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
                            if (group == DataGroupType.Guid)//��������GUID
                            {
                                key += " Default (newid())";
                            }
                        }
                        break;
                    case DataBaseType.Oracle:
                        if (Convert.ToString(column.DefaultValue) == SqlValue.Guid)//��������GUID
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
                            if (group == DataGroupType.Guid)//��������GUID
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
                    case DataBaseType.SQLite://sqlite��AUTOINCREMENT����д��primarykeyǰ,
                        if (column.IsAutoIncrement)
                        {
                            key += " PRIMARY KEY AUTOINCREMENT";
                            primaryKeyList.Clear();//����������ӣ�ֻ���������һ��������
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
                if (Convert.ToString(column.DefaultValue).Length > 0 && group != DataGroupType.Object)//Ĭ��ֵֻ���ǻ��������С�
                {
                    if (dalType == DataBaseType.MySql)
                    {
                        if ((group ==  DataGroupType.Text && (column.MaxSize < 1 || column.MaxSize > 8000)) || (group == DataGroupType.Date && key.Contains("datetime"))) //ֻ�ܶ�TIMESTAMP���͵ĸ�Ĭ��ֵ��
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
                        key += " NOT NULL";//Sybase bit ������ΪNull
                    }
                    else
                    {
                        if (dalType == DataBaseType.DB2 && column.IsCanNull) { }//db2 ����null
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

        #region ��ȡAlterTable���SQL���

        /// <summary>
        /// ��ȡָ���ı�ܹ����ɵ�SQL(Alter Table)���
        /// </summary>
        public static List<string> AlterTableSql(string tableName, MDataColumn columns, string conn)
        {
            List<string> sql = new List<string>();
            string version = null;
            DataBaseType dalType;
            using (DalBase helper = DalCreate.CreateDal(conn))
            {
                helper.ChangeDatabaseWithCheck(tableName);//���dbname.dbo.tablename�����
                if (!helper.TestConn(AllowConnLevel.Master))
                {
                    helper.Dispose();
                    return sql;
                }
                dalType = helper.DataBaseType;
                version = helper.Version;
            }
            MDataColumn dbColumn = TableSchema.GetColumns(tableName, conn);//��ȡ���ݿ���нṹ
            if (dbColumn == null || dbColumn.Count == 0) { return sql; }

            //��ʼ�Ƚ���ͬ
            List<MCellStruct> primaryKeyList = new List<MCellStruct>();
            string tbName = SqlFormat.Keyword(tableName, dalType);
            string alterTable = "alter table " + tbName;
            foreach (MCellStruct ms in columns)//�����µĽṹ
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
                        //�����ж�
                        if (isContains) // ���ڣ����޸�
                        {
                            string alterSql = SqlFormat.Keyword(ms.ColumnName, dalType) + " " + DataType.GetDataType(ms, dalType, version);
                            //����Ƿ���ͬ
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
                        else //���ڣ������
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
