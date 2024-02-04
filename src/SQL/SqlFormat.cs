using CYQ.Data.Table;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Data;

namespace CYQ.Data.SQL
{
    /// <summary>
    /// Sql ����ʽ���� (�������ֹ���)
    /// </summary>
    internal class SqlFormat
    {
        /// <summary>
        /// Sql�ؼ��ִ���
        /// </summary>
        public static string Keyword(string name, DataBaseType dalType)
        {
            if (!string.IsNullOrEmpty(name))
            {
                name = name.Trim();
                if (name.IndexOfAny(new char[] { ' ', '[', ']', '`', '"', '(', ')' }) == -1)
                {
                    string pre = null;
                    int i = name.LastIndexOf('.');// ���ӿ��֧�֣�demo.dbo.users��
                    if (i > 0)
                    {
                        string[] items = name.Split('.');
                        pre = items[0];
                        name = items[items.Length - 1];
                    }
                    switch (dalType)
                    {
                        case DataBaseType.Access:
                        case DataBaseType.Excel:
                            return "[" + name + "]";
                        case DataBaseType.MsSql:
                        case DataBaseType.Sybase:
                            return (pre == null ? "" : pre + "..") + "[" + name + "]";
                        case DataBaseType.MySql:
                            return (pre == null ? "" : pre + ".") + "`" + name + "`";
                        case DataBaseType.Oracle:
                            if (AppConfig.DB.IsOracleUpper) { name = name.ToUpper(); if (pre != null) { pre = pre.ToUpper(); } }
                            return (pre == null ? "" : pre) + name;
                        case DataBaseType.DB2:
                            if (AppConfig.DB.IsDB2Upper) { name = name.ToUpper(); if (pre != null) { pre = pre.ToUpper(); } }
                            return (pre == null ? "" : pre) + name;
                        case DataBaseType.SQLite:
                            return "\"" + name + "\"";
                        case DataBaseType.FireBird:
                            if (AppConfig.DB.IsFireBirdUpper) { name = name.ToUpper(); if (pre != null) { pre = pre.ToUpper(); } }
                            return (pre == null ? "" : "\"" + pre + "\".") + "\"" + name + "\"";
                        case DataBaseType.DaMeng:
                            if (AppConfig.DB.IsDaMengUpper) { name = name.ToUpper(); if (pre != null) { pre = pre.ToUpper(); } }
                            return (pre == null ? "" : "\"" + pre + "\".") + "\"" + name + "\"";
                        case DataBaseType.KingBaseES:
                            if (AppConfig.DB.IsKingBaseESLower) { name = name.ToLower(); if (pre != null) { pre = pre.ToLower(); } }
                            return (pre == null ? "" : "\"" + pre + "\".") + "\"" + name + "\"";
                        case DataBaseType.PostgreSQL:
                            if (AppConfig.DB.IsPostgreLower) { name = name.ToLower(); }
                            return "\"" + name + "\"";
                        case DataBaseType.Txt:
                        case DataBaseType.Xml:
                        case DataBaseType.FoxPro:
                            return NotKeyword(name);
                    }
                }
            }
            return name;
        }
        /// <summary>
        /// ȥ���ؼ��ַ���
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string NotKeyword(string name)
        {
            name = name.Trim();
            if (name.IndexOfAny(new char[] { '(', ')' }) == -1 && name.Split(' ').Length == 1)
            {
                //string pre = string.Empty;
                int i = name.LastIndexOf('.');// ���ӿ��֧�֣�demo.dbo.users��
                if (i > 0)
                {
                    // pre = name.Substring(0, i + 1);
                    name = name.Substring(i + 1);
                }
                name = name.Trim('[', ']', '`', '"');

            }
            return name;
        }
        /// <summary>
        /// Sql���ݿ���ݺ�Sqlע�봦��
        /// </summary>
        public static string Compatible(object where, DataBaseType dalType, bool isFilterInjection)
        {
            string text = GetIFieldSql(where);
            if (isFilterInjection)
            {
                text = SqlInjection.Filter(text, dalType);
            }
            text = SqlCompatible.Format(text, dalType);

            return RemoveWhereOneEqualsOne(text);
        }

        /// <summary>
        /// �Ƴ�"where 1=1"
        /// </summary>
        internal static string RemoveWhereOneEqualsOne(string sql)
        {
            try
            {
                sql = sql.Trim();
                if (sql == "where 1=1")
                {
                    return string.Empty;
                }
                if (sql.EndsWith(" and 1=1"))
                {
                    return sql.Substring(0, sql.Length - 8);
                }
                int i = sql.IndexOf("where 1=1", StringComparison.OrdinalIgnoreCase);
                //do
                //{
                if (i > 0)
                {
                    if (i == sql.Length - 9)//��where 1=1 ������
                    {
                        sql = sql.Substring(0, sql.Length - 10);
                    }
                    else if (sql.Substring(i + 10, 8).ToLower() == "order by")
                    {
                        sql = sql.Remove(i, 10);//�����ж����
                    }
                    // i = sql.IndexOf("where 1=1", StringComparison.OrdinalIgnoreCase);
                }
                //}
                //while (i > 0);
            }
            catch
            {

            }

            return sql;
        }

        /// <summary>
        /// ��������1=2��SQL���
        /// </summary>
        /// <param name="tableName">����������ͼ���</param>
        /// <returns></returns>
        internal static string BuildSqlWithWhereOneEqualsTow(string tableName)
        {
            tableName = tableName.Trim();
            if (tableName[0] == '(' && tableName.IndexOf(')') > -1)
            {
                int end = tableName.LastIndexOf(')');
                tableName = tableName.Substring(1, end - 1);//.Replace("\r\n", "\n").Replace('\n', ' '); ����ע�͵Ļ��С�
            }
            if (tableName.Contains(" "))
            {
                //�ֳ�����
                string partA = "", partB = "";//�ֳ�������Ϊ�˼����Ӳ�ѯ���
                int fromIndex = tableName.Replace("\n", " ").Replace("\r", " ").LastIndexOf(" from ", StringComparison.OrdinalIgnoreCase);
                if (fromIndex > -1)
                {
                    partA = tableName.Substring(0, fromIndex);
                    partB = tableName.Substring(fromIndex);
                    string[] keys = new string[] { " where ", "\nwhere ", "\nwhere\r", "\nwhere\n" };
                    if (partB.IndexOf("where", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        foreach (string key in keys)
                        {
                            if (partB.IndexOf(key, StringComparison.OrdinalIgnoreCase) > -1)
                            {
                                return partA + Regex.Replace(partB, key, " where 1=2 and ", RegexOptions.IgnoreCase);
                            }
                        }
                    }
                    else if (partA.IndexOf("where", StringComparison.OrdinalIgnoreCase) > -1)//����select * from a where id in(select distinct id from a) �����
                    {
                        foreach (string key in keys)
                        {
                            if (partA.IndexOf(key, StringComparison.OrdinalIgnoreCase) > -1)
                            {
                                return Regex.Replace(partA, key, " where 1=2 and ", RegexOptions.IgnoreCase) + partB;
                            }
                        }
                    }
                    //����Ƿ���group by
                    keys = new string[] { " group by", "\ngroup by" };
                    foreach (string key in keys)
                    {
                        if (partB.IndexOf(key, StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            string newKey = key.Replace("group", "where 1=2 group");
                            return partA + Regex.Replace(partB, key, newKey, RegexOptions.IgnoreCase);
                        }
                    }
                }
                return tableName + " where 1=2";
            }
            return string.Format("select * from {0} where 1=2", tableName);
        }

        /// <summary>
        /// Mysql Bit ���Ͳ��������������� ���ֶ�='0' �����ԣ�
        /// </summary>
        /// <param name="where"></param>
        /// <param name="mdc"></param>
        /// <returns></returns>
        internal static string FormatMySqlBit(string where, MDataColumn mdc)
        {
            if (where.Contains("'0'"))
            {
                foreach (MCellStruct item in mdc)
                {
                    DataGroupType group = DataType.GetGroup(item.SqlType);
                    if (group == DataGroupType.Number || group == DataGroupType.Bool)//��ͼģʽ��ȡ����bit��bigint,��������һ������
                    {
                        if (where.IndexOf(item.ColumnName, StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            string pattern = @"\s?" + item.ColumnName + @"\s*=\s*'0'";
                            where = Regex.Replace(where, pattern, " " + item.ColumnName + "=0", RegexOptions.IgnoreCase);
                        }
                    }
                }
            }
            return where;
        }
        internal static string FormatOracleDateTime(string where, MDataColumn mdc)
        {
            if (where.IndexOf(':') > -1 && where.IndexOfAny(new char[] { '>', '<', '-', '/' }) > -1)//�ж��Ƿ�������ڵ��ж�
            {

                foreach (MCellStruct item in mdc)
                {
                    if (DataType.GetGroup(item.SqlType) == DataGroupType.Date && where.IndexOf(item.ColumnName, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        string pattern = @"(\s?" + item.ColumnName + @"\s*[><]{1}[=]?\s*)('.{19,23}')";
                        Regex reg = new Regex(pattern, RegexOptions.IgnoreCase);
                        if (reg.IsMatch(where))
                        {
                            where = reg.Replace(where, delegate (Match match)
                            {
                                if (item.SqlType == SqlDbType.Timestamp)
                                {
                                    return match.Groups[1].Value + "to_timestamp(" + match.Groups[2].Value + ",'yyyy-MM-dd HH24:MI:ss.ff')";
                                }
                                else
                                {
                                    return match.Groups[1].Value + "to_date(" + match.Groups[2].Value + ",'yyyy-mm-dd hh24:mi:ss')";
                                }
                            });
                        }

                    }
                }
            }
            return where;
        }
        internal static List<string> GetTableNamesFromSql(string sql)
        {
            List<string> nameList = new List<string>();

            //��ȡԭʼ����
            string[] items = sql.Split(new char[] { ' ', ';', '(', ')', ',' });
            if (items.Length == 1) { return nameList; }//������
            if (items.Length > 3) // ���ǰ����ո��select * from xxx
            {
                bool isKeywork = false;
                foreach (string item in items)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        string lowerItem = item.ToLower();
                        switch (lowerItem)
                        {
                            case "from":
                            case "update":
                            case "into":
                            case "join":
                            case "table":
                                isKeywork = true;
                                break;
                            default:
                                if (isKeywork)
                                {
                                    if (item[0] == '(' || item.IndexOf('.') > -1) { isKeywork = false; }
                                    else
                                    {
                                        isKeywork = false;
                                        nameList.Add(NotKeyword(item));
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            return nameList;
        }

        #region IField����

        /// <summary>
        /// ��̬�Ķ�IField�ӿڴ���
        /// </summary>
        public static string GetIFieldSql(object whereObj)
        {
            //if (whereObj is IField)
            //{
            //    IField filed = whereObj as IField;
            //    string where = filed.Sql;
            //    filed.Sql = "";
            //    return where;
            //}
            return Convert.ToString(whereObj);
        }
        #endregion

        /// <summary>
        /// �������ݿ�Ĭ��ֵ��ʽ���ɱ�׼ֵ������׼ֵ��ԭ�ɸ����ݿ�Ĭ��ֵ
        /// </summary>
        /// <param name="flag">[0:ת�ɱ�׼ֵ],[1:ת�ɸ����ݿ�ֵ]</param>
        /// <returns></returns>
        public static string FormatDefaultValue(DataBaseType dalType, object value, int flag, SqlDbType sqlDbType)
        {
            string defaultValue = Convert.ToString(value).Trim().TrimEnd('\n');//oracle���Դ�\n��β
            if (dalType != DataBaseType.Access)
            {
                defaultValue = defaultValue.Replace("GenGUID()", string.Empty);
            }
            if (defaultValue.Length == 0)
            {
                return null;
            }
            DataGroupType group = DataType.GetGroup(sqlDbType);
            if (flag == 0)
            {
                #region ת��׼ֵ - ����˫�����š�
                if (group == DataGroupType.Date)//���ڵı�׼ֵ
                {
                    return SqlValue.GetDate;
                }
                else if (group == DataGroupType.Guid)
                {
                    return SqlValue.Guid;
                }
                switch (dalType)
                {
                    case DataBaseType.MySql://��ת\' \"�����Բ����滻��
                        defaultValue = defaultValue.Replace("\\\"", "\"").Replace("\\\'", "\'");
                        break;
                    case DataBaseType.Access:
                    case DataBaseType.SQLite:
                        defaultValue = defaultValue.Replace("\"\"", "��");
                        break;
                    default:
                        defaultValue = defaultValue.Replace("''", "��");
                        break;
                }
                switch (defaultValue.ToLower().Trim('(', ')'))
                {
                    case "newid":
                    case "guid":
                    case "sys_guid":
                    case "genguid":
                    case "uuid":
                        return SqlValue.Guid;
                }
                #endregion
            }
            else
            {
                if (defaultValue == SqlValue.Guid)
                {
                    switch (dalType)
                    {
                        case DataBaseType.MsSql:
                        case DataBaseType.Oracle:
                        case DataBaseType.Sybase:
                        case DataBaseType.PostgreSQL:
                            return SqlCompatible.FormatGUID(defaultValue, dalType);
                        default:
                            return "";
                    }

                }
            }
            switch (dalType)
            {
                case DataBaseType.Access:
                    if (flag == 0)
                    {
                        if (defaultValue[0] == '"' && defaultValue[defaultValue.Length - 1] == '"')
                        {
                            defaultValue = defaultValue.Substring(1, defaultValue.Length - 2);
                        }
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "Now()").Replace("\"", "\"\"");
                        if (group == DataGroupType.Text)
                        {
                            defaultValue = "\"" + defaultValue + "\"";
                        }
                    }
                    break;
                case DataBaseType.MsSql:
                case DataBaseType.Sybase:
                    if (flag == 0)
                    {
                        if (defaultValue.StartsWith("(") && defaultValue.EndsWith(")"))//���� (newid()) ��ȥ��()
                        {
                            defaultValue = defaultValue.Substring(1, defaultValue.Length - 2);
                        }
                        if (defaultValue.StartsWith("N'"))
                        {
                            defaultValue = defaultValue.TrimStart('N');
                        }
                        defaultValue = defaultValue.Trim('\'');//'(', ')',
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "getdate()").Replace("'", "''");
                        if (group == DataGroupType.Text)
                        {
                            defaultValue = "(N'" + defaultValue + "')";
                        }
                    }
                    break;
                case DataBaseType.Oracle:
                    if (flag == 0)
                    {
                        defaultValue = defaultValue.Trim('\'');
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "sysdate").Replace("'", "''");
                        if (group == DataGroupType.Text)
                        {
                            defaultValue = "'" + defaultValue + "'";
                        }
                    }
                    break;
                case DataBaseType.MySql:
                    if (flag == 0)
                    {
                        defaultValue = defaultValue.Replace("b'0", "0").Replace("b'1", "1").Trim(' ', '\'');
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "CURRENT_TIMESTAMP").Replace("'", "\\'").Replace("\"", "\\\"");
                        if (group == DataGroupType.Text)
                        {
                            defaultValue = "\"" + defaultValue + "\"";
                        }
                    }
                    break;
                case DataBaseType.SQLite:
                    if (flag == 0)
                    {
                        defaultValue = defaultValue.Trim('"');
                        if (group > (int)DataGroupType.Text)//����һЩ���淶��д�����������͵ļ������� '0'
                        {
                            defaultValue = defaultValue.Trim('\'');
                        }
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "CURRENT_TIMESTAMP").Replace("\"", "\"\"");
                        if (group == DataGroupType.Text)
                        {
                            defaultValue = "\"" + defaultValue + "\"";
                        }
                    }
                    break;
                case DataBaseType.PostgreSQL:
                    if (flag == 0)
                    {
                        defaultValue = defaultValue.Trim('"');
                        if (group == DataGroupType.Text)
                        {
                            defaultValue = Regex.Split(defaultValue, "::", RegexOptions.IgnoreCase)[0];
                        }
                        if (group != DataGroupType.Text)//����һЩ���淶��д�����������͵ļ������� '0'
                        {
                            defaultValue = defaultValue.Trim('\'');
                        }
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "now()").Replace("\"", "\"\"");
                        if (group == DataGroupType.Text)
                        {
                            defaultValue = Regex.Split(defaultValue, "::", RegexOptions.IgnoreCase)[0];
                            defaultValue = "'" + defaultValue.Trim('\'') + "'";
                        }
                        else if (group == DataGroupType.Bool) // bool
                        {
                            defaultValue = defaultValue.Replace("1", "true").Replace("0", "false");
                        }
                    }
                    break;
                case DataBaseType.DB2:
                    if (flag == 0)
                    {
                        defaultValue = defaultValue.Trim(' ', '\'');
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "CURRENT TIMESTAMP");
                        if (group == DataGroupType.Text)
                        {
                            defaultValue = "'" + defaultValue.Replace("'", "''") + "'";
                        }
                    }
                    break;
                case DataBaseType.FireBird:
                    if (flag == 0)
                    {
                        defaultValue = defaultValue.Replace("DEFAULT ", "").Trim(' ', '\'');
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "CURRENT_DATE");
                        if (group == DataGroupType.Text)
                        {
                            defaultValue = "'" + defaultValue.Replace("'", "''") + "'";
                        }
                    }
                    break;
                case DataBaseType.DaMeng:
                    if (flag == 0)
                    {
                        defaultValue = defaultValue.Trim(' ', '\'');
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "GETDATE()");
                        if (group == DataGroupType.Text)
                        {
                            defaultValue = "'" + defaultValue.Replace("'", "''") + "'";
                        }
                    }
                    break;
                case DataBaseType.KingBaseES:
                    if (flag == 0)
                    {
                        defaultValue = defaultValue.Trim(' ', '\'');
                    }
                    else
                    {
                        defaultValue = defaultValue.Replace(SqlValue.GetDate, "CURRENT_DATE");
                        if (group == DataGroupType.Text)
                        {
                            defaultValue = "'" + defaultValue.Replace("'", "''") + "'";
                        }
                    }
                    break;
            }
            if (flag == 0)
            {
                return defaultValue.Replace("��", "\"").Replace("��", "'");
            }
            return defaultValue;
        }
    }
}
