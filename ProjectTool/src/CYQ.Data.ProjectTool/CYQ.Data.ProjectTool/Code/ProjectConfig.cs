
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
        /// ��ʶ
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
        /// ��������
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
        /// �����ַ���
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
        /// ���ݿ�����
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
        /// ֧�ֶ����ݿ�ģʽ
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
        /// ����ģʽ��ö��ģʽ��ORMʵ����ģʽ��
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
        /// Ĭ�ϵ����ƿռ�
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
        /// ʵ�� �Ƿ�����ֵ����ΪNull�����磺int?
        /// </summary>
        public bool ValueTypeNullable
        {
            get { return _ValueTypeNullable; }
            set { _ValueTypeNullable = value; }
        }
        private bool _ForTwoOnly;
        /// <summary>
        /// For .NET 2.0 ��ʵ�岻�ܼ�д���������汾ʵ����Ծ���д
        /// </summary>
        public bool ForTwoOnly
        {
            get { return _ForTwoOnly; }
            set { _ForTwoOnly = value; }
        }

        private bool _MapName;
        /// <summary>
        /// ȥ��'_'���Ų������ֶ�����
        /// </summary>
        public bool MapName
        {
            get { return _MapName; }
            set { _MapName = value; }
        }

        private string _EntitySuffix;
        /// <summary>
        /// ʵ���׺����
        /// </summary>
        public string EntitySuffix
        {
            get { return _EntitySuffix; }
            set { _EntitySuffix = value; }
        }

    }
}
