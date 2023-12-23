using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data
{
    /// <summary>
    /// 操作的数据库类型
    /// </summary>
    public enum DataBaseType
    {
        None,
        /// <summary>
        /// MSSQL[2000/2005/2008/2012/...]
        /// </summary>
        MsSql,
        FoxPro,
        Excel,
        Access,
        Oracle,
        MySql,
        SQLite,
        FireBird,
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
        DB2,
        /// <summary>
        /// 国产达梦数据库
        /// </summary>
        DaMeng
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
    /// 数据类型分组
    /// 字母型返回0；数字型返回1；日期型返回2；bool返回3；guid返回4；其它返回999
    /// </summary>
    public enum DataGroupType
    {
        /// <summary>
        /// 未定义
        /// </summary>
        None=-1,
        /// <summary>
        /// 文本类型0
        /// </summary>
        Text = 0,
        /// <summary>
        /// 数字型返回1
        /// </summary>
        Number = 1,
        /// <summary>
        /// 日期型返回2
        /// </summary>
        Date = 2,
        /// <summary>
        /// bool返回3
        /// </summary>
        Bool = 3,
        /// <summary>
        /// guid返回4
        /// </summary>
        Guid = 4,
        /// <summary>
        /// 其它返回999
        /// </summary>
        Object = 999

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
