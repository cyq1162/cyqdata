using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;

using System.Data.Common;
using System.Data;
using System.IO;
using System.Data.OleDb;
using CYQ.Data.Cache;
using System.Reflection;
using CYQ.Data.Tool;
using CYQ.Data.Orm;
using System.Configuration;


namespace CYQ.Data.SQL
{
    /// <summary>
    /// 表结构类
    /// </summary>
    internal partial class TableSchema
    {
       
    }
    internal partial class TableSchema
    {
        //internal const string ExistMsSql = "SELECT count(*) FROM sysobjects where id = OBJECT_id(N'{0}') AND xtype in (N'{1}')";
        ////internal const string Exist2005 = "SELECT count(*) FROM sys.objects where object_id = OBJECT_id(N'{0}') AND type in (N'{1}')";
        //internal const string ExistOracle = "Select count(*)  From user_objects where  object_name=upper('{0}') and object_type='{1}'";
        //internal const string ExistMySql = "SELECT count(*)  FROM  `information_schema`.`COLUMNS`  where TABLE_NAME='{0}' and TABLE_SCHEMA='{1}'";
        //internal const string ExistSybase = "SELECT count(*) FROM sysobjects where id = OBJECT_id(N'{0}') AND type in (N'{1}')";
        //internal const string ExistSqlite = "SELECT count(*) FROM sqlite_master where name='{0}' and type='{1}'";
        //internal const string ExistPostgre = "SELECT count(*) FROM information_schema.tables where table_schema = 'public' and table_name='{0}' and table_type='{1}'";
        internal const string ExistOracleSequence = "SELECT count(*) FROM All_Sequences where Sequence_name='{0}'";
        internal const string CreateOracleSequence = "create sequence {0} start with {1} increment by 1";
        internal const string GetOracleMaxID = "select max({0}) from {1}";


    }

}
