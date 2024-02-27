using CYQ.Data.SQL;
using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Table
{
    public partial class MDataTable
    {
        internal int joinOnIndex = -1;
        /// <summary>
        /// 用于关联表的列名，不设置时默认取表主键值
        /// </summary>
        public string JoinOnName
        {
            get
            {
                if (joinOnIndex > -1)
                {
                    return Columns[joinOnIndex].ColumnName;
                }
                return string.Empty;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    joinOnIndex = Columns.GetIndex(value);
                    if (joinOnIndex == -1)
                    {
                        Error.Throw("not exist the column name : " + value);
                    }
                }
            }
        }
        /// <summary>
        /// 两表LeftJoin关联
        /// </summary>
        /// <param name="dt">关联表</param>
        /// <param name="appendColumns">追加显示的列，没有指定则追加关联表的所有列</param>
        /// <returns></returns>
        public MDataTable Join(MDataTable dt, params string[] appendColumns)
        {
            return MDataTableJoin.Join(this, dt, appendColumns);
        }

        /// <summary>
        /// 两表LeftJoin关联
        /// </summary>
        /// <param name="tableName">关联表名</param>
        /// <param name="joinOnName">关联的字段名，设置Null则自动取表主键为关联名</param>
        /// <param name="appendColumns">追加显示的列，没有指定则追加关联表的所有列</param>
        /// <returns></returns>
        public MDataTable Join(object tableName, string joinOnName, params string[] appendColumns)
        {
            return MDataTableJoin.Join(this, Convert.ToString(tableName), joinOnName, appendColumns);
        }
    }
}
