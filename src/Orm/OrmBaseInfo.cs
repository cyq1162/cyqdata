using CYQ.Data.Table;

namespace CYQ.Data.Orm
{
    /// <summary>
    /// 数据库 - 元数据基础信息
    /// </summary>
    public class OrmBaseInfo
    {
        MAction _Action;
        internal OrmBaseInfo(MAction action)
        {
            _Action = action;
        }
        /// <summary>
        /// Get 数据库 - 更新、删除、插入，查询操作后，受影响的行数
        /// </summary>
        public int RecordsAffected
        {
            get
            {
                if (_Action != null)
                {
                    return _Action.RecordsAffected;
                }
                return -1;
            }
        }
        /// <summary>
        /// Get 数据库 - 是否事务进行中
        /// </summary>
        public bool IsTransation
        {
            get
            {
                if (_Action != null)
                {
                    return _Action.IsTransation;
                }
                return false;
            }
        }
        /// <summary>
        /// Get 数据库 - 表名
        /// </summary>
        public string TableName
        {
            get
            {
                if (_Action != null)
                {
                    return _Action.TableName;
                }
                return "";
            }
        }
        /// <summary>
        /// Get 数据库 - 名称
        /// </summary>
        public string DataBaseName
        {
            get
            {
                if (_Action != null)
                {
                    return _Action.DataBaseName;
                }
                return "";
            }
        }
        /// <summary>
        /// Get 数据库 - 版本信息
        /// </summary>
        public string DataBaseVersion
        {
            get
            {
                if (_Action != null)
                {
                    return _Action.DataBaseVersion;
                }
                return "";
            }
        }
        /// <summary>
        /// Get 数据库 - 的列结构
        /// </summary>
        public MDataColumn MDataColumn
        {
            get
            {
                if (_Action != null)
                {
                    return _Action.Data.Columns;
                }
                return null;
            }
        }
        /// <summary>
        /// Get 数据库 - 链接配置名称
        /// </summary>
        public string ConnName
        {
            get
            {
                if (_Action != null)
                {
                    return _Action.ConnName;
                }
                return null;
            }
        }
        /// <summary>
        /// Get 数据库 - 链接完整字符串
        /// </summary>
        public string ConnString
        {
            get
            {
                if (_Action != null)
                {
                    return _Action.ConnString;
                }
                return null;
            }
        }
        /// <summary>
        /// Get 数据库 - 操作的调试信息
        /// </summary>
        public string DebugInfo
        {
            get
            {
                if (_Action != null)
                {
                    return _Action.DebugInfo;
                }
                return null;
            }
        }
        /// <summary>
        /// Get 数据库 - 类型
        /// </summary>
        public DataBaseType DataBaseType
        {
            get
            {
                if (_Action != null)
                {
                    return _Action.DataBaseType;
                }
                return DataBaseType.None;
            }
        }

    }
}
