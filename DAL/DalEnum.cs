using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data
{ /// <summary>
    /// 操作的数据库类型
    /// </summary>
    public enum DataBaseType
    {
        None,
        /// <summary>
        /// MSSQL[2000/2005/2008/2012/...]
        /// </summary>
        MsSql,
        // Excel,
        Access,
        Oracle,
        MySql,
        SQLite,
        /// <summary>
        /// No Support Now
        /// </summary>
        // FireBird,
        /// <summary>
        /// PostgreSQL 
        /// </summary>
        PostgreSQL,
        /// <summary>
        /// Txt DataBase
        /// </summary>
        Txt,
        /// <summary>
        /// Xml DataBase
        /// </summary>
        Xml,
        Sybase,
        DB2
    }
    /// <summary>
    /// 特殊参数类型[MProc SetCustom方法所使用的参数]
    /// </summary>
    public enum ParaType
    {
        /// <summary>
        /// Oracle 游标类型
        /// </summary>
        Cursor,
        /// <summary>
        /// 输出类型
        /// </summary>
        OutPut,
        /// <summary>
        /// 输入输出类型
        /// </summary>
        InputOutput,
        /// <summary>
        /// 返回值类型
        /// </summary>
        ReturnValue,
        /// <summary>
        /// Oracle CLOB类型
        /// </summary>
        CLOB,
        /// <summary>
        ///  Oracle NCLOB类型
        /// </summary>
        NCLOB,
        /// <summary>
        ///  MSSQL 用户定义表类型
        /// </summary>
        Structured
    }

    /// <summary>
    /// 重置数据库的结果
    /// </summary>
    internal enum DBResetResult
    {
        /// <summary>
        ///  成功切换 数据库链接
        /// </summary>
        Yes,
        /// <summary>
        /// 未切换 - 相同数据库名。
        /// </summary>
        No_SaveDbName,
        /// <summary>
        /// 未切换 - 事务中。
        /// </summary>
        No_Transationing,
        /// <summary>
        /// 未切换 - 新数据库名不存在。
        /// </summary>
        No_DBNoExists,
    }
    /// <summary>
    /// 测试链接的级别
    /// </summary>
    internal enum AllowConnLevel
    {
        Master = 1,
        MasterBackup = 2,
        MaterBackupSlave = 3
    }
}
