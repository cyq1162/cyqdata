using System;
using System.Data;

using CYQ.Data.Table;


namespace CYQ.Data.SQL
{
    /// <summary>
    /// ��������ת����
    /// </summary>
    public static partial class DataType
    {
        /// <summary>
        /// ����������Ͷ�Ӧ��Type
        /// </summary>
        public static Type GetType(SqlDbType sqlType)
        {
            return GetType(sqlType, DataBaseType.None);
        }
        /// <summary>
        /// ����������Ͷ�Ӧ��Type
        /// </summary>
        public static Type GetType(SqlDbType sqlType, DataBaseType dalType)
        {
            return GetType(sqlType, dalType, null);
        }
        internal static Type GetType(SqlDbType sqlType, DataBaseType dalType, string sqlTypeName)
        {
            switch (sqlType)
            {
                case SqlDbType.BigInt:
                    return typeof(Int64);
                case SqlDbType.Binary:
                case SqlDbType.Image:
                case SqlDbType.VarBinary:
                    return typeof(byte[]);
                case SqlDbType.Bit:
                    return typeof(Boolean);
                case SqlDbType.Text:
                case SqlDbType.Char:
                case SqlDbType.NChar:
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.VarChar:
                case SqlDbType.Time:
                case SqlDbType.Xml:
                    return typeof(String);
                case SqlDbType.SmallDateTime:
                case SqlDbType.DateTimeOffset:
                case SqlDbType.DateTime2:
                case SqlDbType.DateTime:
                case SqlDbType.Date:
                    return typeof(DateTime);
                case SqlDbType.Timestamp:
                    switch (dalType)
                    {
                        case DataBaseType.MsSql:
                        case DataBaseType.Sybase:
                            return typeof(byte[]);
                        default:
                            return typeof(DateTime);
                    }
                case SqlDbType.Money:
                case SqlDbType.Decimal:
                case SqlDbType.SmallMoney:
                    //case SqlDbType.Udt://����Ƕ�Numeric���͡�
                    return typeof(Decimal);
                case SqlDbType.Float:
                    return typeof(Single);
                case SqlDbType.Int:
                    return typeof(int);
                case SqlDbType.Real:
                    return typeof(double);
                case SqlDbType.TinyInt:
                    if (dalType == DataBaseType.MySql && !string.IsNullOrEmpty(sqlTypeName) && !sqlTypeName.EndsWith("unsigned"))
                    {
                        return typeof(SByte);
                    }
                    return typeof(Byte);
                case SqlDbType.SmallInt:
                    return typeof(Int16);
                case SqlDbType.UniqueIdentifier:
                    return typeof(Guid);
                default:
                    return typeof(object);
            }
        }
        /// <summary>
        /// ��DbType���Ͷ�Ӧӳ�䵽SqlDbType����
        /// </summary>
        /// <param name="type">DbType����</param>
        /// <returns></returns>
        public static SqlDbType GetSqlType(Type type)
        {
            string name = type.Name.ToString();
            if (type.IsEnum)
            {
                name = "int";//����ΪʲôҪ����Ϊstring�أ� "string";//int
            }
            else if (type.IsGenericType)
            {
                if (type.Name.StartsWith("Nullable"))
                {
                    name = Nullable.GetUnderlyingType(type).Name;
                }
                else
                {
                    name = "object";
                }
            }
            else if (!type.FullName.StartsWith("System.") || type.FullName.Split('.').Length > 2)//�Զ����ࡣ
            {
                name = "object";
            }
            return GetSqlType(name);
        }
        /// <summary>
        /// ��DbType�����ַ�����﷽ʽ��Ӧӳ�䵽SqlDbType����
        /// </summary>
        /// <param name="typeName">��������</param>
        /// <returns></returns>
        public static SqlDbType GetSqlType(string typeName)
        {
            string name = typeName.ToLower().Replace("system.", "").Split('(')[0].Trim('"');
            if (name.Contains("."))
            {
                string[] items = name.Split('.');
                name = items[items.Length - 1];
            }
            switch (name)
            {
                case "char":
                case "character":
                case "ansistringfixedlength":
                    return SqlDbType.Char;
                case "nchar":
                case "unichar"://Sybase
                case "stringfixedlength":
                    return SqlDbType.NChar;
                case "set"://mysql���� 
                case "enum"://mysql����
                case "varchar":
                case "ansistring":
                case "varchar2":
                case "character varying":
                case "hierarchyid":
                case "long varchar":
                case "graphic":
                case "vargraphic":
                case "long vargraphic":
                    return SqlDbType.VarChar;
                case "nvarchar":
                case "nvarchar2":
                case "string":
                case "univarchar":
                    return SqlDbType.NVarChar;
                case "timestamp":
                case "timestamp(6)"://oracle
                    return SqlDbType.Timestamp;
                case "raw":
                case "bfile":
                case "binary":
                case "tinyblob":
                case "blob":
                case "mediumblob":
                case "longblob":


                case "byte[]":
                case "oleobject":
                case "bytea"://postgre
                    return SqlDbType.Binary;
                case "varbinary":
                    return SqlDbType.VarBinary;
                case "image":
                    return SqlDbType.Image;
                case "bit":
                case "bit varying":
                case "boolean":
                case "tinyint(1)":
                    return SqlDbType.Bit;
                case "tinyint":
                case "tinyint unsigned":
                case "byte":
                case "sbyte":
                    return SqlDbType.TinyInt;
                case "money":
                case "currency":
                    return SqlDbType.Money;
                case "smallmoney":
                    return SqlDbType.SmallMoney;
                case "smalldatetime":
                    return SqlDbType.SmallDateTime;
                case "datetime2":
                    return SqlDbType.DateTime2;
                case "datetimeoffset":
                    return SqlDbType.DateTimeOffset;
                case "datetime":
                case "timestamp with time zone":
                case "timestamp without time zone":
                    return SqlDbType.DateTime;
                case "time":
                case "abstime"://postgresql
                case "reltime":////postgresql
                case "time with time zone":
                    return SqlDbType.Time;
                case "date":
                    return SqlDbType.Date;
                case "numeric":
                //return SqlDbType.Udt;//�����������û�У�����������á�
                case "decimal":
                    return SqlDbType.Decimal;
                case "real":
                case "double":
                case "binary_double"://oracle
                    return SqlDbType.Real;
                case "uniqueidentifier":
                case "guid":
                case "uuid":
                    return SqlDbType.UniqueIdentifier;
                case "smallint":
                case "int16":
                case "uint16":
                    return SqlDbType.SmallInt;
                case "int":
                case "int32":
                case "uint32":
                case "number":
                case "integer":
                case "mediumint":
                    return SqlDbType.Int;
                case "bigint":
                case "int64":
                case "uint64":
                case "varnumeric":
                case "long":
                    return SqlDbType.BigInt;
                case "variant":
                case "sql_variant":
                case "object":
                    return SqlDbType.Variant;
                case "float":
                case "binary_float"://oracle
                case "single":
                case "double precision"://postgresql
                    return SqlDbType.Float;
                case "xml":
                case "xmltype":
                    return SqlDbType.Xml;
                case "ntext":
                case "nclob":
                case "unitext":
                    return SqlDbType.NText;
                case "tinytext":
                case "text":
                case "mediumtext":
                case "longtext":
                case "clob":
                case "json"://postgre
                case "jsonb":
                    return SqlDbType.Text;
                default:
                    if (name.EndsWith("[]"))
                    {
                        return SqlDbType.Variant;
                    }
                    break;
            }

            return SqlDbType.Variant;
        }

        /// <summary>
        /// ��DbType�����ַ��������ʽ��Ӧӳ�䵽DbType����
        /// </summary>
        /// <param name="typeName">��������</param>
        /// <returns></returns>
        public static DbType GetDbType(string typeName)
        {
            return GetDbType(typeName, DataBaseType.None);
        }
        /// <summary>
        /// ��DbType�����ַ��������ʽ��Ӧӳ�䵽DbType����
        /// </summary>
        /// <param name="typeName">��������</param>
        /// <returns></returns>
        public static DbType GetDbType(string typeName, DataBaseType dalType)
        {
            string name = typeName.ToLower().Replace("system.", "").Split('(')[0].Trim('"');
            if (name.Contains("."))
            {
                string[] items = name.Split('.');
                name = items[items.Length - 1];
            }
            switch (name)
            {
                case "ansistring":
                    return DbType.AnsiString;
                case "ansistringfixedlength":
                    return DbType.AnsiStringFixedLength;
                case "text":
                case "ntext":
                case "unitext":
                case "string":
                case "nvarchar":
                case "hierarchyid":
                    return DbType.String;
                case "char":
                    return DbType.AnsiStringFixedLength;
                case "unichar":
                case "nchar":
                case "stringfixedlength":
                    return DbType.StringFixedLength;
                case "varchar":
                    return DbType.AnsiString;
                case "varbinary":
                case "binary":
                case "image":
                case "byte[]":
                    return DbType.Binary;
                case "bit":
                case "boolean":
                    return DbType.Boolean;
                case "tinyint":
                case "byte":
                    return DbType.Byte;
                case "smallmoney":
                case "currency":
                    return DbType.Currency;
                case "date":
                    return DbType.Date;
                case "time":
                    return DbType.Time;
                case "smalldatetime":
                case "datetime":
                    return DbType.DateTime;
                case "timestamp":
                    switch (dalType)
                    {
                        case DataBaseType.MsSql:
                        case DataBaseType.Sybase:
                            return DbType.Binary;
                        default:
                            return DbType.DateTime;
                    }
                case "udt":
                case "numeric":
                case "decimal":
                    return DbType.Decimal;
                case "real":
                case "money":
                case "double":
                case "binary_double":
                    return DbType.Double;
                case "uniqueidentifier":
                case "guid":
                    return DbType.Guid;
                case "smallint":
                case "int16":
                case "uint16":
                    return DbType.Int16;
                case "int":
                case "int32":
                case "uint32":
                    return DbType.Int32;
                case "bigint":
                case "int64":
                case "uint64":
                    return DbType.Int64;
                case "variant":
                case "object":
                    return DbType.Object;
                case "sbyte":
                    return DbType.SByte;
                case "float":
                case "single":
                case "binary_float":
                    return DbType.Single;
                case "varnumeric":
                    return DbType.VarNumeric;
                case "xml":
                    return DbType.Xml;
            }

            return DbType.String;
        }
        /// <summary>
        /// ��SqlDbType���Ͷ�Ӧӳ�䵽DbType����
        /// </summary>
        /// <param name="type">SqlDbType����</param>
        /// <returns></returns>
        public static DbType GetDbType(Type type)
        {
            return GetDbType(type.Name.ToString());
        }

        #region ��������
        /// <summary>
        /// ��ĸ�ͷ���0�������ͷ���1�������ͷ���2��bool����3��guid����4����������999
        /// </summary>
        /// <param name="sqlDbType"></param>
        /// <returns></returns>
        public static DataGroupType GetGroup(SqlDbType sqlDbType)
        {
            return GetGroup(sqlDbType, DataBaseType.None);
        }
        /// <summary>
        /// ��ĸ�ͷ���0�������ͷ���1�������ͷ���2��bool����3��guid����4����������999
        /// </summary>
        /// <param name="sqlDbType"></param>
        /// <returns></returns>
        public static DataGroupType GetGroup(SqlDbType sqlDbType, DataBaseType dalType)
        {
            switch (sqlDbType)
            {
                case SqlDbType.Xml:
                case SqlDbType.NVarChar:
                case SqlDbType.VarChar:
                case SqlDbType.NChar:
                case SqlDbType.Char:
                case SqlDbType.Text:
                case SqlDbType.NText:
                case SqlDbType.Time:
                    return DataGroupType.Text;
                case SqlDbType.Int:
                case SqlDbType.TinyInt:
                case SqlDbType.BigInt:
                case SqlDbType.SmallInt:
                case SqlDbType.Float:
                case SqlDbType.Real:
                case SqlDbType.Decimal:
                case SqlDbType.Money:
                case SqlDbType.SmallMoney:
                    return DataGroupType.Number;

                case SqlDbType.Date:
                case SqlDbType.DateTimeOffset:
                case SqlDbType.DateTime:
                case SqlDbType.SmallDateTime:
                case SqlDbType.DateTime2:
                    return DataGroupType.Date;
                case SqlDbType.Bit:
                    return DataGroupType.Bool;
                case SqlDbType.UniqueIdentifier:
                    return DataGroupType.Guid;
                default:
                    if (sqlDbType == SqlDbType.Timestamp)
                    {
                        if (dalType != DataBaseType.MsSql && dalType != DataBaseType.Sybase)
                        {
                            return DataGroupType.Date;
                        }
                    }
                    return DataGroupType.Object;
            }
        }


        #endregion
    }

    public static partial class DataType
    {
        /// <summary>
        /// �����ݣ�����������
        /// </summary>
        /// <param name="ms">��Ԫ�ṹ</param>
        /// <param name="dalTo">���ݿ�����</param>
        /// <param name="version">���ݿ�汾��</param>
        /// <returns></returns>
        internal static string GetDataType(MCellStruct ms, DataBaseType dalTo, string version)
        {
            DataBaseType dalFrom = DataBaseType.None;
            if (ms.MDataColumn != null)
            {
                dalFrom = ms.MDataColumn.DataBaseType;
            }
            bool isSameDalType = dalFrom == dalTo;//dalFrom == DalType.None || ��ʵ��ת�нṹʱDalTypeΪNone�����ʺ��������

            SqlDbType sqlType = ms.SqlType;
            int maxSize = ms.MaxSize == 0 ? 255 : ms.MaxSize;
            short scale = ms.Scale;
            version = version ?? string.Empty;
            bool is2000 = version.StartsWith("08") || dalTo == DataBaseType.Sybase;//Sybase ��2000���ơ�

            switch (sqlType)
            {
                case SqlDbType.BigInt:
                case SqlDbType.Int:
                case SqlDbType.TinyInt:
                case SqlDbType.SmallInt:
                    //if (maxSize == 1 && dalType != DalType.Oracle)//oracle number(1)��ʾbit
                    //{
                    //    return "bit";
                    //}
                    switch (dalTo)
                    {
                        case DataBaseType.Access:
                            if (sqlType == SqlDbType.BigInt)
                            {
                                return "long";
                            }
                            break;
                        case DataBaseType.SQLite:
                            if (sqlType == SqlDbType.BigInt)
                            {
                                return "INT64";
                            }
                            return "INTEGER";
                        case DataBaseType.Oracle:
                            if (sqlType == SqlDbType.BigInt)
                            {
                                if (maxSize > 10)
                                {
                                    return "NUMBER(" + maxSize + ")";
                                }
                                return "LONG";
                            }
                            return maxSize < 1 ? "NUMBER" : "NUMBER(" + maxSize + ")";
                        case DataBaseType.MySql:
                            if (sqlType == SqlDbType.TinyInt)
                            {
                                return "tinyint(" + (maxSize > 0 ? maxSize : 4) + ") UNSIGNED";
                            }
                            break;
                        case DataBaseType.PostgreSQL:
                            if (sqlType == SqlDbType.TinyInt)
                            {
                                return "smallint";//postgreSQLû��tinyint
                            }
                            break;

                    }
                    return sqlType.ToString().ToLower();
                case SqlDbType.Time:
                    if (dalTo == DataBaseType.PostgreSQL)
                    {
                        return isSameDalType ? ms.SqlTypeName : "time without time zone";
                    }
                    if (dalTo == DataBaseType.MySql || dalTo == DataBaseType.SQLite || isSameDalType)
                    {
                        return sqlType.ToString().ToLower();
                    }
                    return "char(12)";
                case SqlDbType.Date:
                    if (isSameDalType) { return sqlType.ToString().ToLower(); }
                    switch (dalTo)
                    {
                        case DataBaseType.MySql:
                        case DataBaseType.SQLite:
                        case DataBaseType.Oracle:
                        case DataBaseType.PostgreSQL:
                        case DataBaseType.DB2:
                            return sqlType.ToString().ToLower();
                    }
                    return "datetime";
                case SqlDbType.Timestamp:
                    if (isSameDalType) { return "timestamp"; }
                    if (dalFrom == DataBaseType.MySql || dalFrom == DataBaseType.Oracle || dalFrom == DataBaseType.PostgreSQL || dalFrom == DataBaseType.DB2)
                    {
                        if (dalTo == DataBaseType.MySql || dalTo == DataBaseType.Oracle || dalTo == DataBaseType.PostgreSQL || dalTo == DataBaseType.DB2)
                        {
                            return "timestamp";
                        }
                        return "datetime";
                    }
                    else if (dalFrom == DataBaseType.MsSql || dalFrom == DataBaseType.Sybase)
                    {
                        if (dalTo == DataBaseType.MsSql || dalTo == DataBaseType.Sybase)
                        {
                            return "timestamp";
                        }
                        else if (dalTo == DataBaseType.PostgreSQL)
                        {
                            return "bytea";
                        }
                        return "binary(8)";
                    }
                    return "datetime";
                case SqlDbType.SmallDateTime:
                case SqlDbType.DateTimeOffset:
                case SqlDbType.DateTime2:
                case SqlDbType.DateTime:
                    if (dalTo == DataBaseType.PostgreSQL)
                    {
                        return isSameDalType ? ms.SqlTypeName : "timestamp";
                    }
                    if (isSameDalType) { return sqlType.ToString().ToLower(); }
                    switch (dalTo)
                    {
                        case DataBaseType.MySql:
                        case DataBaseType.Oracle:
                            if (dalTo == DataBaseType.Oracle)
                            {
                                return "date";
                            }
                            break;
                        case DataBaseType.MsSql:
                        case DataBaseType.Sybase:
                            if (sqlType == SqlDbType.SmallDateTime)
                            {
                                return sqlType.ToString().ToLower();
                            }
                            break;
                    }
                    return "datetime";
                case SqlDbType.Bit:
                    switch (dalTo)
                    {
                        case DataBaseType.Oracle:
                            return "NUMBER(" + (maxSize == -1 ? 1 : maxSize) + ")";
                        case DataBaseType.PostgreSQL:
                            if (maxSize <= 1) { return "boolean"; }
                            string name = isSameDalType ? ms.SqlTypeName : "bit";
                            return name + "(" + maxSize + ")";
                        case DataBaseType.DB2:
                            return "char(1)";

                    }
                    if (maxSize > 1)
                    {
                        return "char(" + maxSize + ")";
                    }
                    return "bit";
                case SqlDbType.Udt://��Numeric�����á�
                case SqlDbType.Money:
                case SqlDbType.Decimal:
                case SqlDbType.SmallMoney:
                    switch (dalTo)
                    {
                        case DataBaseType.Access:
                            return "Currency";
                        case DataBaseType.Oracle:
                            return maxSize == -1 ? "NUMBER" : "NUMBER(" + maxSize + "," + scale + ")";
                        case DataBaseType.MsSql:
                        case DataBaseType.Sybase:
                            if (sqlType == SqlDbType.Money || sqlType == SqlDbType.SmallMoney)
                            {
                                return sqlType.ToString().ToLower();
                            }
                            else if (sqlType == SqlDbType.Udt)
                            {
                                return "numeric(" + maxSize + "," + scale + ")";
                            }
                            break;
                        case DataBaseType.PostgreSQL:
                            if (sqlType == SqlDbType.Decimal)
                            {
                                return "numeric(" + maxSize + "," + scale + ")";
                            }
                            return "money";
                    }
                    return "decimal(" + maxSize + "," + scale + ")";
                case SqlDbType.Float:
                    return "float";
                case SqlDbType.Real:
                    switch (dalTo)
                    {
                        case DataBaseType.Oracle:
                            return maxSize == -1 ? "NUMBER" : "NUMBER(" + maxSize + "," + scale + ")";
                        case DataBaseType.Access:
                            return "double";
                        case DataBaseType.MsSql:
                            return ms.SqlType.ToString();
                    }
                    return ms.SqlTypeName;
                case SqlDbType.Variant:
                case SqlDbType.Binary:
                case SqlDbType.Image:
                case SqlDbType.VarBinary:
                    if (sqlType == SqlDbType.Variant && isSameDalType && !string.IsNullOrEmpty(ms.SqlTypeName))
                    {
                        return ms.SqlTypeName;
                    }
                    switch (dalTo)
                    {
                        case DataBaseType.SQLite:
                        case DataBaseType.Oracle:
                            return "BLOB";
                        case DataBaseType.Access:
                            return "oleobject";
                        case DataBaseType.MySql:
                            if (ms.SqlTypeName.ToLower() == "binary" && maxSize > 0 && maxSize <= 255)
                            {
                                if (maxSize <= 16) { maxSize = 255; }
                                return "binary(" + maxSize + ")";
                            }
                            if (maxSize < 0 || maxSize > 16777215)
                            {
                                return "longblob";
                            }
                            else if (maxSize <= 255)
                            {
                                return "tinyblob";
                            }
                            else if (maxSize <= 65535) //�����-1���������ˡ�
                            {
                                return "blob";
                            }
                            else
                            {
                                return "mediumblob";
                            }
                        case DataBaseType.MsSql:
                        case DataBaseType.Sybase:
                            string key = sqlType.ToString().ToLower();
                            bool isSybase = dalTo == DataBaseType.Sybase;
                            if (key == "image" || (maxSize < 0 && is2000) || (isSybase && maxSize > 1962))
                            {
                                return "image";
                            }
                            else if (key == "variant")
                            {
                                if (isSybase) { return "text"; }
                                return "sql_variant";
                            }
                            if (maxSize > 0 && maxSize <= 16) { maxSize = 255; }//����MySql��16���ֽڴ���36������
                            return key + "(" + (maxSize < 0 ? "max" : maxSize.ToString()) + ")";
                        case DataBaseType.PostgreSQL:
                            return "bytea";
                    }
                    return "binary";

                case SqlDbType.Text:
                case SqlDbType.Char:
                case SqlDbType.NChar:
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.VarChar:
                    string t = sqlType.ToString().ToLower();
                    switch (dalTo)
                    {
                        case DataBaseType.Access:
                            return (maxSize < 1 || maxSize > 255) ? "memo" : "text(" + maxSize + ")";
                        case DataBaseType.MsSql:
                        case DataBaseType.Sybase:
                            #region mssql��sybase
                            if (maxSize < 1 || maxSize > 8000)//ntext��text
                            {
                                if (dalTo == DataBaseType.Sybase)
                                {
                                    return t[0] == 'n' ? "unitext" : "text";
                                }
                                else if (is2000 || (t[1] != 'v' && t[1] != 'a'))
                                {
                                    return t[0] == 'n' ? "ntext" : "text";
                                }
                                return t + "(max)";
                            }
                            else
                            {
                                if (dalTo == DataBaseType.Sybase && t[0] == 'n')
                                {
                                    t = "uni" + t.Substring(1);
                                }
                                return t + "(" + maxSize + ")";
                            }
                        #endregion
                        case DataBaseType.SQLite:
                            return (maxSize < 1 || maxSize > 65535) ? "TEXT" : "TEXT(" + maxSize + ")";
                        case DataBaseType.MySql://mysqlû��nchar֮��ġ�
                            #region mysql
                            if (ms.IsPrimaryKey && t.EndsWith("text"))
                            {
                                return "varchar(255)";
                            }
                            if (t[0] == 'n')
                            {
                                t = t.Substring(1);
                            }
                            if (t.ToLower() == "text" || maxSize == -1 || maxSize > 21000)
                            {
                                if (maxSize == -1 || maxSize > 16777215)
                                {
                                    return "longtext";

                                }
                                else if (maxSize <= 255)
                                {
                                    return "tinytext";
                                }
                                else if (maxSize <= 65535)
                                {
                                    return "text";
                                }
                                else
                                {
                                    return "mediumtext";
                                }
                            }
                            else
                            {
                                return t + "(" + maxSize + ")";
                            }
                        #endregion
                        //return (maxSize < 1 || maxSize > 8000) ? "longtext" : ();
                        case DataBaseType.Oracle:
                            if (maxSize < 1 || maxSize > 4000 || (maxSize > 2000 && (sqlType == SqlDbType.NVarChar || sqlType == SqlDbType.Char))
                                || sqlType == SqlDbType.Text || sqlType == SqlDbType.NText)
                            {
                                return (t[0] == 'n' ? "N" : "") + "CLOB";
                            }
                            else if (sqlType == SqlDbType.Char || sqlType == SqlDbType.NChar)
                            {
                                return "char(" + maxSize + ")";
                            }
                            return t + "2(" + maxSize + ")";
                        case DataBaseType.PostgreSQL:
                            string name = "varchar";
                            if (isSameDalType)
                            {
                                name = ms.SqlTypeName;
                            }
                            else
                            {
                                if (t.EndsWith("text")) { return "text"; }
                                if (!t.EndsWith(name)) { name = "char"; }
                            }
                            return name + (maxSize > -1 ? "(" + maxSize + ")" : "");
                        case DataBaseType.DB2:
                            name = "varchar";
                            if (isSameDalType)
                            {
                                name = ms.SqlTypeName;
                            }
                            else
                            {
                                if (t.EndsWith("text")) { return "text"; }
                                if (!t.EndsWith(name)) { name = "char"; }
                            }
                            return name + (maxSize > -1 && !name.StartsWith("long ", StringComparison.OrdinalIgnoreCase) ? "(" + maxSize + ")" : "");
                    }
                    break;
                case SqlDbType.UniqueIdentifier:
                    switch (dalTo)
                    {
                        case DataBaseType.Access:
                            return "GUID";
                        case DataBaseType.MySql:
                        case DataBaseType.Oracle:
                        case DataBaseType.Sybase:
                        case DataBaseType.DB2:
                            return "char(36)";
                        case DataBaseType.PostgreSQL:
                            return "uuid";
                    }
                    return "uniqueidentifier";
                case SqlDbType.Xml:
                    switch (dalTo)
                    {
                        case DataBaseType.MsSql:
                            if (is2000)
                            {
                                return "ntext";
                            }
                            else
                            {
                                return "xml";
                            }
                        case DataBaseType.Sybase:
                        case DataBaseType.SQLite:
                            return "text";
                        case DataBaseType.MySql:
                            return "mediumtext";
                        case DataBaseType.Oracle:
                            return "CLOB";
                        case DataBaseType.Access:
                            return "memo";
                        case DataBaseType.PostgreSQL:
                        case DataBaseType.DB2:
                            return "Xml";
                    }
                    break;
            }
            return "varchar";
        }
        /// <summary>
        /// CodeFirstʱ��Ĭ�����͵��ֶγ��ȡ�
        /// </summary>
        /// <param name="sqlDbType"></param>
        /// <returns></returns>
        internal static int GetMaxSize(SqlDbType sqlDbType)
        {
            switch (sqlDbType)
            {
                case SqlDbType.Int:
                    return int.MaxValue.ToString().Length;
                case SqlDbType.TinyInt:
                    return 3;
                case SqlDbType.BigInt:
                    return Int64.MaxValue.ToString().Length;
                case SqlDbType.SmallInt:
                    return Int16.MaxValue.ToString().Length;
                case SqlDbType.Float:
                    return Single.MaxValue.ToString().Length;
                case SqlDbType.Real:
                    return Double.MaxValue.ToString().Length;
                case SqlDbType.Decimal:
                    return Decimal.MaxValue.ToString().Length;
                case SqlDbType.Money:
                    return 19;
                case SqlDbType.NVarChar:
                    return 2000;
                case SqlDbType.VarChar:
                    return 4000;
                case SqlDbType.NChar:
                    return 2000;
                case SqlDbType.Char:
                    return 4000;
                case SqlDbType.Text:
                    return 2147483647;
                case SqlDbType.NText:
                    return 1073741823;
                case SqlDbType.Bit:
                    return 1;
                case SqlDbType.UniqueIdentifier:
                    return Guid.Empty.ToString().Length;
                case SqlDbType.Time:
                    return 18;
                case SqlDbType.DateTime:
                case SqlDbType.SmallDateTime:
                case SqlDbType.Timestamp:
                case SqlDbType.DateTime2:
                default:
                    return -1;
            }
        }
    }
}
