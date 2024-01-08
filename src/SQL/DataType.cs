using System;
using System.Data;

using CYQ.Data.Table;


namespace CYQ.Data.SQL
{
    /// <summary>
    /// 数据类型转换类
    /// </summary>
    public static partial class DataType
    {
        /// <summary>
        /// 获得数据类型对应的Type
        /// </summary>
        public static Type GetType(SqlDbType sqlType)
        {
            return GetType(sqlType, DataBaseType.None);
        }
        /// <summary>
        /// 获得数据类型对应的Type
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
                    //case SqlDbType.Udt://这个是顶Numeric类型。
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
        /// 将DbType类型对应映射到SqlDbType类型
        /// </summary>
        /// <param name="type">DbType类型</param>
        /// <returns></returns>
        public static SqlDbType GetSqlType(Type type)
        {
            string name = type.Name.ToString();
            if (type.IsEnum)
            {
                name = "int";//当初为什么要设置为string呢？ "string";//int
            }
            else if (type.IsGenericType)
            {
                if (type.Name.StartsWith("Nullable"))
                {
                    Type nullType = Nullable.GetUnderlyingType(type);
                    if (nullType.IsEnum)
                    {
                        name = "int";
                    }
                    else
                    {
                        name = nullType.Name;
                    }
                }
                else
                {
                    name = "object";
                }
            }
            else if (!type.FullName.StartsWith("System.") || type.FullName.Split('.').Length > 2)//自定义类。
            {
                name = "object";
            }
            return GetSqlType(name);
        }
        /// <summary>
        /// 将DbType类型字符串表达方式对应映射到SqlDbType类型
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <returns></returns>
        public static SqlDbType GetSqlType(string typeName)
        {
            string name = typeName.ToLower().Replace("system.", "").Split('(')[0].Trim('"');
            if (name.Contains("."))
            {
                string[] items = name.Split('.');
                name = items[items.Length - 1];
            }
            if (name.StartsWith("time "))
            {
                return SqlDbType.Time;
            }
            if (name.StartsWith("timestamp ") || name.StartsWith("datetime "))
            {
                return SqlDbType.DateTime;
            }
            if (name.StartsWith("interval "))
            {
                return SqlDbType.VarChar;
            }
            switch (name)
            {
                case "bit":
                case "bit varying":
                case "boolean":
                    //case "tinyint(1)":
                    return SqlDbType.Bit;
                case "tinyint":
                case "tinyint unsigned":
                case "byte":
                case "sbyte":
                    return SqlDbType.TinyInt;
                case "smallint":
                case "int16":
                case "uint16":
                case "short"://firebird
                    return SqlDbType.SmallInt;
                case "int4range"://kingbasees
                case "signtype"://kingbasees
                case "simple_integer"://kingbasees
                case "int":
                case "int32":
                case "uint32":
                case "integer":
                case "mediumint":
                case "rowid"://dameng
                    return SqlDbType.Int;
                case "int8range"://kingbasees
                case "bigint":
                case "int64":
                case "uint64":
                case "varnumeric":
                case "long":
                case "int128"://firbird
                    return SqlDbType.BigInt;
                case "char":
                case "bpchar"://kingbasees
                case "character":
                case "ansistringfixedlength":
                    return SqlDbType.Char;
                case "nchar":
                case "unichar"://Sybase
                case "stringfixedlength":
                    return SqlDbType.NChar;
                case "dsinterval"://kingbasees
                case "yminterval"://kingbasees
                case "dbms_id"://kingbasees
                case "dbms_id_30"://kingbasees
                case "dbms_quoted_id"://kingbasees
                case "dbms_quoted_id_30"://kingbasees
                case "hash16"://kingbasees
                case "hash32"://kingbasees
                case "set"://mysql类型 
                case "enum"://mysql类型
                case "varchar":
                case "ansistring":
                case "varchar2":
                case "character varying":
                case "hierarchyid":
                case "long varchar":
                case "longvarchar"://dameng
                case "graphic":
                case "vargraphic":
                case "long vargraphic":
                case "varying"://firebird
                    return SqlDbType.VarChar;
                case "nvarchar":
                case "nvarchar2":
                case "string":
                case "univarchar":
                case "cstring"://firebird
                    return SqlDbType.NVarChar;
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
                case "uniqueidentifier":
                case "guid":
                case "uuid":
                    return SqlDbType.UniqueIdentifier;
                case "time":
                case "timez"://kingbasees
                case "time_tz_unconstrained"://kingbasees
                case "time_unconstrained"://kingbasees
                case "abstime"://postgresql
                case "reltime":////postgresql
                    return SqlDbType.Time;
                case "date":
                case "ora_date"://kingbasees
                    return SqlDbType.Date;
                case "b_timestamp"://mssql,sybase 为二进制。
                    return SqlDbType.Timestamp;
                case "smalldatetime":
                    return SqlDbType.SmallDateTime;
                case "datetime2":
                    return SqlDbType.DateTime2;
                case "datetimeoffset":
                    return SqlDbType.DateTimeOffset;
                case "datetime":
                case "timestamp":
                case "timestamptz"://kingbasees
                case "timestamp_ltz_unconstrained"://kingbasees
                case "timestamp_unconstrained"://kingbasees
                    return SqlDbType.DateTime;
                case "money":
                case "currency":
                    return SqlDbType.Money;
                case "smallmoney":
                    return SqlDbType.SmallMoney;
                case "numeric":
                //return SqlDbType.Udt;//这个数据类型没有，用这个顶着用。
                case "decimal":
                case "dec"://dameng
                case "number"://可以int，flat,double
                    return SqlDbType.Decimal;
                case "float":
                case "float4"://kingbasees
                case "simple_float"://kingbasees
                case "binary_float"://oracle
                case "single":
                case "decfloat"://firebird
                    return SqlDbType.Float;
                case "real":
                case "float8"://kingbasees
                case "simple_double"://kingbasees
                case "double":
                case "double precision"://dameng,postgre
                case "binary_double"://oracle
                    return SqlDbType.Real;
                case "xml":
                case "xmltype":
                    return SqlDbType.Xml;
                case "raw":
                case "bfile":
                case "binary":
                case "tinyblob":
                case "blob":
                case "blob_id"://firebird
                case "quad"://firebird
                case "mediumblob":
                case "longblob":
                case "byte[]":
                case "oleobject":
                case "bytea"://postgre
                    return SqlDbType.Binary;
                case "varbinary":
                case "longvarbinary"://dameng
                    return SqlDbType.VarBinary;
                case "image":
                    return SqlDbType.Image;
                case "variant":
                case "sql_variant":
                case "object":
                    return SqlDbType.Variant;
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
        /// 将DbType类型字符串表达形式对应映射到DbType类型
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <returns></returns>
        public static DbType GetDbType(string typeName)
        {
            return GetDbType(typeName, DataBaseType.None);
        }
        /// <summary>
        /// 将DbType类型字符串表达形式对应映射到DbType类型
        /// </summary>
        /// <param name="typeName">类型名称</param>
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
                case "timestamp":
                    return DbType.DateTime;
                case "b_timestamp":
                    return DbType.Binary;
                //switch (dalType)
                //{
                //    case DataBaseType.MsSql:
                //    case DataBaseType.Sybase:
                //        return DbType.Binary;
                //    default:
                //        return DbType.DateTime;
                //}
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
        /// 将SqlDbType类型对应映射到DbType类型
        /// </summary>
        /// <param name="type">SqlDbType类型</param>
        /// <returns></returns>
        public static DbType GetDbType(Type type)
        {
            return GetDbType(type.Name.ToString());
        }

        #region 其它方法
        /// <summary>
        /// 字母型返回0；数字型返回1；日期型返回2；bool返回3；guid返回4；其它返回999
        /// </summary>
        /// <param name="sqlDbType"></param>
        /// <returns></returns>
        public static DataGroupType GetGroup(SqlDbType sqlDbType)
        {
            return GetGroup(sqlDbType, DataBaseType.None);
        }
        /// <summary>
        /// 字母型返回0；数字型返回1；日期型返回2；bool返回3；guid返回4；其它返回999
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
        /// 导数据，创建表所用
        /// </summary>
        /// <param name="ms">单元结构</param>
        /// <param name="dalTo">数据库类型</param>
        /// <param name="version">数据库版本号</param>
        /// <returns></returns>
        internal static string GetDataType(MCellStruct ms, DataBaseType dalTo, string version)
        {
            switch (dalTo)
            {
                case DataBaseType.DaMeng:
                    return GetDaMengType(ms);
                case DataBaseType.FireBird:
                    return GetFireBirdType(ms);
                case DataBaseType.KingBaseES:
                    return GetKingBaseESType(ms);
            }
            DataBaseType dalFrom = DataBaseType.None;
            if (ms.MDataColumn != null)
            {
                dalFrom = ms.MDataColumn.DataBaseType;
            }
            bool isSameDalType = dalFrom == dalTo;//dalFrom == DalType.None || 从实体转列结构时DalType为None，不适合这种情况

            SqlDbType sqlType = ms.SqlType;
            int maxSize = ms.MaxSize == 0 ? 255 : ms.MaxSize;
            short scale = ms.Scale;
            version = version ?? string.Empty;
            bool is2000 = version.StartsWith("08") || dalTo == DataBaseType.Sybase;//Sybase 和2000类似。

            switch (sqlType)
            {
                case SqlDbType.BigInt:
                case SqlDbType.Int:
                case SqlDbType.TinyInt:
                case SqlDbType.SmallInt:
                    //if (maxSize == 1 && dalType != DalType.Oracle)//oracle number(1)表示bit
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
                                return "smallint";//postgreSQL没有tinyint
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
                    if (dalTo == DataBaseType.MySql && (ms.SqlTypeName == "datetime" || ms.MaxSize == 23))
                    {
                        return "timestamp";
                    }
                    if (isSameDalType || (dalFrom == DataBaseType.None && dalTo == DataBaseType.MySql)) { return "timestamp"; }//mysql
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
                case SqlDbType.Udt://当Numeric类型用。
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
                    if (dalTo == DataBaseType.Oracle && ms.SqlTypeName == "BINARY_FLOAT")
                    {
                        return ms.SqlTypeName;
                    }
                    return "float";
                case SqlDbType.Real:
                    switch (dalTo)
                    {
                        case DataBaseType.Oracle:
                            if (ms.SqlTypeName == "BINARY_DOUBLE")
                            {
                                return ms.SqlTypeName;
                            }
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
                            else if (maxSize <= 65535) //如果是-1就在这里了。
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
                            if (maxSize > 0 && maxSize <= 16) { maxSize = 255; }//兼容MySql，16的字节存了36的数据
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
                            #region mssql、sybase
                            if (maxSize < 1 || maxSize > 8000)//ntext、text
                            {
                                if (dalTo == DataBaseType.Sybase)
                                {
                                    return t[0] == 'n' ? "unitext" : "text";
                                }
                                else if (is2000 || (t[1] != 'v' && t[1] != 'a'))
                                {
                                    return t[0] == 'n' ? "ntext" : "text";
                                }
                                if (t.EndsWith("text")) return t;
                                return t + "(max)";
                            }
                            else
                            {
                                if (dalTo == DataBaseType.Sybase && t[0] == 'n')
                                {
                                    t = "uni" + t.Substring(1);
                                }
                                if (t.EndsWith("text")) return t;
                                return t + "(" + maxSize + ")";
                            }
                        #endregion
                        case DataBaseType.SQLite:
                            return (maxSize < 1 || maxSize > 65535) ? "TEXT" : "TEXT(" + maxSize + ")";
                        case DataBaseType.MySql://mysql没有nchar之类的。
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
        /// CodeFirst时的默认类型的字段长度。
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

    public static partial class DataType
    {
        private static string GetDaMengType(MCellStruct ms)
        {
            DataBaseType dalFrom = DataBaseType.None;
            if (ms.MDataColumn != null)
            {
                dalFrom = ms.MDataColumn.DataBaseType;
            }
            bool isSameDalType = dalFrom == DataBaseType.DaMeng;
            string typeString = isSameDalType ? ms.SqlTypeName : ms.SqlType.ToString().ToUpper();
            switch (ms.SqlType)
            {
                case SqlDbType.NText:
                case SqlDbType.Xml:
                    return "TEXT";
                case SqlDbType.UniqueIdentifier:
                    return "CHAR(36)";
                case SqlDbType.Real:
                    if (!isSameDalType) return "DOUBLE"; break;
                case SqlDbType.SmallDateTime:
                case SqlDbType.DateTimeOffset:
                case SqlDbType.DateTime2:
                    if (!isSameDalType) return "DATETIME"; break;
                case SqlDbType.Udt://当Numeric类型用。
                case SqlDbType.Money:
                case SqlDbType.Decimal:
                case SqlDbType.SmallMoney:
                    if (!isSameDalType) { typeString = "DECIMAL"; }
                    return typeString + "(" + (ms.MaxSize > 0 ? ms.MaxSize : 22) + "," + (ms.Scale > 0 ? ms.Scale : 6) + ")";
                case SqlDbType.Variant:
                case SqlDbType.Binary:
                case SqlDbType.Timestamp:
                    if (!isSameDalType) { typeString = "BINARY"; }
                    if (typeString == "BINARY")
                    {
                        return typeString + "(" + (ms.MaxSize > 0 ? ms.MaxSize : 10) + ")";
                    }
                    break;
                case SqlDbType.VarBinary:
                    if (typeString == "VARBINARY")
                    {
                        return typeString + "(" + (ms.MaxSize > 0 ? ms.MaxSize : 50) + ")";
                    }
                    break;
                case SqlDbType.Char:
                case SqlDbType.NChar:
                    if (!isSameDalType) { typeString = "CHAR"; }
                    return typeString + "(" + (ms.MaxSize > 0 ? ms.MaxSize : 10) + ")";
                case SqlDbType.NVarChar:
                case SqlDbType.VarChar:
                    if (!isSameDalType) { typeString = "VARCHAR"; }
                    if (typeString.StartsWith("INTERVAL")) { return typeString; }
                    if (typeString != "LONGVARCHAR")
                    {
                        return typeString + "(" + (ms.MaxSize > 0 ? ms.MaxSize : 50) + ")";
                    }
                    break;
            }
            return typeString;
        }
        private static string GetFireBirdType(MCellStruct ms)
        {
            DataBaseType dalFrom = DataBaseType.None;
            if (ms.MDataColumn != null)
            {
                dalFrom = ms.MDataColumn.DataBaseType;
            }
            bool isSameDalType = dalFrom == DataBaseType.FireBird;
            string typeString = isSameDalType ? ms.SqlTypeName : ms.SqlType.ToString().ToUpper();
            switch (ms.SqlType)
            {
                case SqlDbType.Int:
                    return "INTEGER";
                case SqlDbType.TinyInt:
                    return "SMALLINT";
                case SqlDbType.SmallDateTime:
                case SqlDbType.DateTimeOffset:
                case SqlDbType.DateTime2:
                case SqlDbType.DateTime:
                    if (!isSameDalType) { typeString = "TIMESTAMP"; }
                    return typeString;
                case SqlDbType.Bit:
                    return "BOOLEAN";
                case SqlDbType.Udt://当Numeric类型用。
                case SqlDbType.Money:
                case SqlDbType.Decimal:
                case SqlDbType.SmallMoney:
                    if (!isSameDalType) { typeString = "DECIMAL"; }
                    return typeString + "(" + (ms.MaxSize > 0 ? ms.MaxSize : 20) + "," + (ms.Scale > 0 ? ms.Scale : 2) + ")";
                case SqlDbType.Float:
                    if (!isSameDalType) { typeString = "FLOAT"; }
                    return typeString;
                case SqlDbType.Real:
                    return "DOUBLE PRECISION";
                case SqlDbType.Timestamp:
                case SqlDbType.Variant:
                case SqlDbType.Binary:
                case SqlDbType.Image:
                case SqlDbType.VarBinary:
                case SqlDbType.Xml:
                case SqlDbType.Text:
                case SqlDbType.NText:
                    if (isSameDalType && ms.MaxSize > 0)
                    {
                        if (typeString == "BINARY") return "BLOB SUB_TYPE BINARY SEGMENT SIZE " + ms.MaxSize;
                        if (typeString == "TEXT") return "BLOB SUB_TYPE TEXT SEGMENT SIZE " + ms.MaxSize;
                    }
                    return "BLOB";
                case SqlDbType.Char:
                case SqlDbType.NChar:
                    if (!isSameDalType) { typeString = "CHAR"; }
                    return typeString + "(" + (ms.MaxSize > 0 ? ms.MaxSize : 20) + ")";

                case SqlDbType.NVarChar:
                case SqlDbType.VarChar:
                    if (!isSameDalType) { typeString = "VARCHAR"; }
                    return typeString + "(" + (ms.MaxSize > 0 ? ms.MaxSize : 50) + ")";
                case SqlDbType.UniqueIdentifier:
                    return "CHAR(36)";
            }
            return typeString;
        }

        private static string GetKingBaseESType(MCellStruct ms)
        {
            DataBaseType dalFrom = DataBaseType.None;
            if (ms.MDataColumn != null)
            {
                dalFrom = ms.MDataColumn.DataBaseType;
            }
            bool isSameDalType = dalFrom == DataBaseType.KingBaseES;
            string typeString = isSameDalType ? ms.SqlTypeName : ms.SqlType.ToString().ToLower();
            switch (ms.SqlType)
            {
                case SqlDbType.Int:
                    return "integer";
                case SqlDbType.SmallDateTime:
                case SqlDbType.DateTimeOffset:
                case SqlDbType.DateTime2:
                    return "datetime";
                case SqlDbType.Bit:
                    if (!isSameDalType) { typeString = "boolean"; }
                    if (typeString != "boolean" && ms.MaxSize > 0)
                    {
                        return typeString + "(" + ms.MaxSize + ")";
                    }
                    return typeString;
                case SqlDbType.Udt://当Numeric类型用。
                case SqlDbType.Decimal:
                case SqlDbType.SmallMoney:
                    if (!isSameDalType) { typeString = "numeric"; }
                    if (ms.MaxSize > 0)
                    {
                        return typeString + "(" + (ms.MaxSize > 0 ? ms.MaxSize : 20) + "," + (ms.Scale > 0 ? ms.Scale : 2) + ")";
                    }
                    break;
                case SqlDbType.Real:
                    return "double precision";
                case SqlDbType.Timestamp:
                case SqlDbType.Variant:
                case SqlDbType.Binary:
                case SqlDbType.Image:
                case SqlDbType.VarBinary:
                    if (!isSameDalType) { typeString = "bytea"; }
                    return typeString;
                case SqlDbType.NText:
                case SqlDbType.Text:
                    if (!isSameDalType) { typeString = "text"; }
                    return typeString;
                case SqlDbType.Char:
                case SqlDbType.NChar:
                    if (!isSameDalType) { typeString = "char"; }
                    if (ms.MaxSize > 0)
                    {
                        return typeString + "(" + ms.MaxSize + ")";
                    }
                    break;
                case SqlDbType.VarChar:
                case SqlDbType.NVarChar:
                    if (!isSameDalType) { typeString = "varchar"; }
                    if (ms.MaxSize > 0)
                    {
                        return typeString + "(" + ms.MaxSize + ")";
                    }
                    break;
                case SqlDbType.UniqueIdentifier:
                    return "uuid";
            }
            return typeString;
        }
    }
}
