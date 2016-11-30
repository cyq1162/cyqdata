using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data
{
    /// <summary>
    /// MAction Insert Options
    /// <para>MAction的Insert方法的返回值选项</para>
    /// </summary>
    public enum InsertOp
    {
        /// <summary>
        /// only insert,no return autoIncrement id
        /// <para>使用此项：插入数据后[MSSQL会返回ID,其它数据库则不会返回ID]</para>
        /// </summary>
        None,
        /// <summary>
        /// insert and return autoincrement id (default option)
        /// <para>使用此项：插入数据后会返回ID[默认选项]。</para>
        /// </summary>
        ID,
        /// <summary>
        /// insert and select top 1 data to fill row
        /// <para>使用此项：插入数据后,会根据返回ID进行查询后填充数据行。</para>
        /// </summary>
        Fill,
    }

}

