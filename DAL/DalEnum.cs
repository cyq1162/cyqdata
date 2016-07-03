using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data
{ /// <summary>
    /// 操作的数据库类型
    /// </summary>
    public enum DalType
    {
        /// <summary>
        /// 未知
        /// </summary>
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
        /// No Support Now
        /// </summary>
        // PostgreSQL,
        /// <summary>
        /// Txt文本数据库
        /// </summary>
        Txt,
        /// <summary>
        /// Xml数据库
        /// </summary>
        Xml,
        Sybase
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
        NCLOB
    }

    /// <summary>
    /// 重置数据库的结果
    /// </summary>
    internal enum DbResetResult
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
}
