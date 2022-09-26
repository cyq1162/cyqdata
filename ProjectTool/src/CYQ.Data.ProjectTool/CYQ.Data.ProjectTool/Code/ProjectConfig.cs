
namespace CYQ.Data.ProjectTool
{
    public class ProjectConfig : CYQ.Data.Orm.OrmBase
    {
        public ProjectConfig()
        {
            base.SetInit(this, "ProjectConfig", "Txt Path={0};ts=0");
        }
        private int _ID;
        /// <summary>
        /// 标识
        /// </summary>
        public int ID
        {
            get
            {
                return _ID;
            }
            set
            {
                _ID = value;
            }
        }
        private string _Name;
        /// <summary>
        /// 配置名称
        /// </summary>
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
            }
        }

        private string _Conn;
        /// <summary>
        /// 链接字符串
        /// </summary>
        public string Conn
        {
            get
            {
                return _Conn;
            }
            set
            {
                _Conn = value;
            }
        }

        private string _DBType;
        /// <summary>
        /// 数据库类型
        /// </summary>
        public string DBType
        {
            get
            {
                return _DBType;
            }
            set
            {
                _DBType = value;
            }
        }

        private bool _MutilDatabase;
        /// <summary>
        /// 支持多数据库模式
        /// </summary>
        public bool MutilDatabase
        {
            get
            {
                return _MutilDatabase;
            }
            set
            {
                _MutilDatabase = value;
            }
        }
        private string _ProjectPath;

        public string ProjectPath
        {
            get
            {
                return _ProjectPath;
            }
            set
            {
                _ProjectPath = value;
            }
        }
        private bool _IsMain;

        public bool IsMain
        {
            get
            {
                return _IsMain;
            }
            set
            {
                _IsMain = value;
            }
        }
        private string _BuildMode;
        /// <summary>
        /// 创建模式（枚举模式；ORM实体类模式）
        /// </summary>
        public string BuildMode
        {
            get
            {
                return _BuildMode;
            }
            set
            {
                _BuildMode = value;
            }
        }
        private string _NameSpace;
        /// <summary>
        /// 默认的名称空间
        /// </summary>
        public string NameSpace
        {
            get
            {
                return _NameSpace;
            }
            set
            {
                _NameSpace = value;
            }
        }

        private bool _ValueTypeNullable;
        /// <summary>
        /// 实体 是否允许值类型为Null，例如：int?
        /// </summary>
        public bool ValueTypeNullable
        {
            get { return _ValueTypeNullable; }
            set { _ValueTypeNullable = value; }
        }
        private bool _ForTwoOnly;
        /// <summary>
        /// For .NET 2.0 （实体不能简写），其它版本实体可以精简写
        /// </summary>
        public bool ForTwoOnly
        {
            get { return _ForTwoOnly; }
            set { _ForTwoOnly = value; }
        }

        private bool _MapName;
        /// <summary>
        /// 去掉'_'符号并修正字段名称
        /// </summary>
        public bool MapName
        {
            get { return _MapName; }
            set { _MapName = value; }
        }

        private string _EntitySuffix;
        /// <summary>
        /// 实体后缀名称
        /// </summary>
        public string EntitySuffix
        {
            get { return _EntitySuffix; }
            set { _EntitySuffix = value; }
        }

    }
}
