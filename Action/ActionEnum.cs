using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data
{
    /// <summary>
    /// MAction的Insert方法的返回值选项
    /// </summary>
    public enum InsertOp
    {
        /// <summary>
        /// 使用此项：插入数据后[MSSQL会返回ID,其它数据库则不会返回ID]
        /// </summary>
        None,
        /// <summary>
        /// 使用此项：插入数据后会返回ID[默认选项]。
        /// </summary>
        ID,
        /// <summary>
        /// 使用此项：插入数据后,会根据返回ID进行查询后填充数据行。
        /// </summary>
        Fill,
    }

}

