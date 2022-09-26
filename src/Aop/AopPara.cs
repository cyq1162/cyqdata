using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;
using System.Data.Common;
using System.Data;

namespace CYQ.Data.Aop
{
    /// <summary>
    /// Aop������Ϣ
    /// </summary>
    public partial class AopInfo
    {
        // internal bool IsCustomAop = false;

        private string _TableName;
        /// <summary>
        /// ����
        /// </summary>
        public string TableName
        {
            get { return _TableName; }
            set { _TableName = value; }
        }
        //private bool _IsView;
        ///// <summary>
        ///// �Ƿ���ͼ������ͼ��䣩
        ///// </summary>
        //public bool IsView
        //{
        //    get { return _IsView; }
        //    set { _IsView = value; }
        //}
        // private DalType _DalType = DalType.None;
        /// <summary>
        /// ��������(ֻ��)
        /// </summary>
        public DataBaseType DalType
        {
            get
            {
                if (MAction != null)
                {
                    return MAction.DataBaseType;
                }
                else if (MProc != null)
                {
                    return MProc.DataBaseType;
                }
                return DataBaseType.None;
                //if (_DalType == DalType.None)
                //{
                //    if (MAction != null)
                //    {
                //        _DalType = MAction.DalType;
                //    }
                //    else if (MProc != null)
                //    {
                //        _DalType = MProc.DalType;
                //    }
                //}
                //return _DalType;
            }
            //set { _DalType = value; }
        }
        // private string _DataBase;
        /// <summary>
        /// ���ݿ�����(ֻ��)
        /// </summary>
        public string DataBase
        {
            get
            {
                if (MAction != null)
                {
                    return MAction.DataBaseName;
                }
                else if (MProc != null)
                {
                    return MProc.DataBaseName;
                }
                return string.Empty;
                //if (string.IsNullOrEmpty(_DataBase))
                //{
                //    if (MAction != null)
                //    {
                //        _DataBase = MAction.dalHelper.DataBase;
                //    }
                //    else if (MProc != null)
                //    {
                //        _DataBase = MProc.dalHelper.DataBase;
                //    }
                //}
                //return _DataBase;
            }
            // set { _DataBase = value; }
        }
        /// <summary>
        /// ���ݿ����ӣ�ֻ����
        /// </summary>
        public string ConnectionString
        {
            get
            {
                if (MAction != null)
                {
                    return MAction.ConnString;
                }
                else if (MProc != null)
                {
                    return MProc.ConnString;
                }
                return string.Empty;

            }
        }

        private object _AopPara;
        /// <summary>
        /// AOP���Զ������
        /// </summary>
        public object AopPara
        {
            get { return _AopPara; }
            set { _AopPara = value; }
        }
        private bool _IsTransaction;
        /// <summary>
        /// �Ƿ�������
        /// </summary>
        public bool IsTransaction
        {
            get { return _IsTransaction; }
            set { _IsTransaction = value; }
        }

    }
    public partial class AopInfo
    {
        private MAction _MAction;
        /// <summary>
        /// ��ǰ�����ĵĴ������
        /// </summary>
        public MAction MAction
        {
            get { return _MAction; }
            set { _MAction = value; }
        }
        private List<MDataTable> _TableList;
        /// <summary>
        /// �����б�
        /// </summary>
        public List<MDataTable> TableList
        {
            get { return _TableList; }
            set { _TableList = value; }
        }

        private MDataTable _Table;
        /// <summary>
        /// ���ݱ�
        /// </summary>
        public MDataTable Table
        {
            get { return _Table; }
            set { _Table = value; }
        }

        private MDataRow _Row;
        /// <summary>
        /// ������
        /// </summary>
        public MDataRow Row
        {
            get { return _Row; }
            set { _Row = value; }
        }
        private object _Where;
        /// <summary>
        /// ��ѯ����
        /// </summary>
        public object Where
        {
            get { return _Where; }
            set { _Where = value; }
        }
        private bool _AutoSetValue;
        /// <summary>
        /// �Զ���Form����ȡֵ[��������ʱʹ��]
        /// </summary>
        public bool AutoSetValue
        {
            get { return _AutoSetValue; }
            set { _AutoSetValue = value; }
        }
        private InsertOp _InsertOp;
        /// <summary>
        /// ���ݲ���ѡ��
        /// </summary>
        public InsertOp InsertOp
        {
            get { return _InsertOp; }
            set { _InsertOp = value; }
        }

        private int _PageIndex;
        /// <summary>
        /// ��ҳ��ʼҳ
        /// </summary>
        public int PageIndex
        {
            get { return _PageIndex; }
            set { _PageIndex = value; }
        }
        private int _PageSize;
        /// <summary>
        /// ��ҳÿҳ����
        /// </summary>
        public int PageSize
        {
            get { return _PageSize; }
            set { _PageSize = value; }
        }
        private string _UpdateExpression;
        /// <summary>
        /// ���²����ĸ��ӱ��ʽ��
        /// </summary>
        public string UpdateExpression
        {
            get
            {
                return _UpdateExpression;
            }
            set
            {
                _UpdateExpression = value;
            }
        }

        private object[] _SelectColumns;
        /// <summary>
        /// ָ���Ĳ�ѯ��
        /// </summary>
        public object[] SelectColumns
        {
            get { return _SelectColumns; }
            set { _SelectColumns = value; }
        }
        private bool _IsSuccess;
        /// <summary>
        /// Begin�����Ƿ���óɹ�[��End��ʹ��]
        /// </summary>
        public bool IsSuccess
        {
            get { return _IsSuccess; }
            set { _IsSuccess = value; }
        }
        private int _RowCount;
        /// <summary>
        /// ��ѯʱ���صļ�¼������ExeNonQuery�ķ���ֵ
        /// </summary>
        public int RowCount
        {
            get { return _RowCount; }
            set { _RowCount = value; }
        }
        private List<AopCustomDbPara> _CustomDbPara;
        /// <summary>
        /// �û�����SetPara�����ӵ��Զ������
        /// </summary>
        public List<AopCustomDbPara> CustomDbPara
        {
            get { return _CustomDbPara; }
            set { _CustomDbPara = value; }
        }


    }
    public partial class AopInfo
    {
        private MProc _MProc;
        /// <summary>
        /// ��ǰ�����ĵĴ������
        /// </summary>
        public MProc MProc
        {
            get { return _MProc; }
            set { _MProc = value; }
        }

        private string _ProcName;
        /// <summary>
        /// �洢��������SQL���
        /// </summary>
        public string ProcName
        {
            get { return _ProcName; }
            set { _ProcName = value; }
        }
        private bool _IsProc;
        /// <summary>
        /// �Ƿ�洢����
        /// </summary>
        public bool IsProc
        {
            get { return _IsProc; }
            set { _IsProc = value; }
        }
        private DbParameterCollection _DbParameters;
        /// <summary>
        /// �������
        /// </summary>
        public DbParameterCollection DBParameters
        {
            get { return _DbParameters; }
            set { _DbParameters = value; }
        }
        private object _ExeResult;
        /// <summary>
        /// ִ�к󷵻صĽ��
        /// </summary>
        public object ExeResult
        {
            get { return _ExeResult; }
            set { _ExeResult = value; }
        }


    }
    /// <summary>
    /// �����Զ������[ʹ��MActionʱ����Where�������еĲ���������ʹ��]
    /// </summary>
    public class AopCustomDbPara
    {
        private string _ParaName;
        /// <summary>
        /// ��������
        /// </summary>
        public string ParaName
        {
            get { return _ParaName; }
            set { _ParaName = value; }
        }
        private object _Value;
        /// <summary>
        /// ����ֵ
        /// </summary>
        public object Value
        {
            get { return _Value; }
            set { _Value = value; }
        }
        private DbType _DbType;
        /// <summary>
        /// ��������
        /// </summary>
        public DbType ParaDbType
        {
            get { return _DbType; }
            set { _DbType = value; }
        }
        //private bool _IsSysPara;
        ///// <summary>
        ///// �Ƿ�ϵͳ�ڲ������Ĳ���
        ///// </summary>
        //internal bool IsSysPara
        //{
        //    get { return _IsSysPara; }
        //    set { _IsSysPara = value; }
        //}

    }
}
