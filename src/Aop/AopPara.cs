using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Table;
using System.Data.Common;
using System.Data;

namespace CYQ.Data.Aop
{
    /// <summary>
    /// Aop参数信息
    /// </summary>
    public partial class AopInfo
    {
        // internal bool IsCustomAop = false;

        private string _TableName;
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName
        {
            get { return _TableName; }
            set { _TableName = value; }
        }
        //private bool _IsView;
        ///// <summary>
        ///// 是否视图（或视图语句）
        ///// </summary>
        //public bool IsView
        //{
        //    get { return _IsView; }
        //    set { _IsView = value; }
        //}
        // private DalType _DalType = DalType.None;
        /// <summary>
        /// 数据类型(只读)
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
        /// 数据库名称(只读)
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
        /// 数据库链接（只读）
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
        /// AOP的自定义参数
        /// </summary>
        public object AopPara
        {
            get { return _AopPara; }
            set { _AopPara = value; }
        }
        private bool _IsTransaction;
        /// <summary>
        /// 是否事务中
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
        /// 当前上下文的处理程序。
        /// </summary>
        public MAction MAction
        {
            get { return _MAction; }
            set { _MAction = value; }
        }
        private List<MDataTable> _TableList;
        /// <summary>
        /// 数据列表
        /// </summary>
        public List<MDataTable> TableList
        {
            get { return _TableList; }
            set { _TableList = value; }
        }

        private MDataTable _Table;
        /// <summary>
        /// 数据表
        /// </summary>
        public MDataTable Table
        {
            get { return _Table; }
            set { _Table = value; }
        }

        private MDataRow _Row;
        /// <summary>
        /// 数据行
        /// </summary>
        public MDataRow Row
        {
            get { return _Row; }
            set { _Row = value; }
        }
        private object _Where;
        /// <summary>
        /// 查询条件
        /// </summary>
        public object Where
        {
            get { return _Where; }
            set { _Where = value; }
        }
        private bool _AutoSetValue;
        /// <summary>
        /// 自动从Form表单提取值[插入或更新时使用]
        /// </summary>
        public bool AutoSetValue
        {
            get { return _AutoSetValue; }
            set { _AutoSetValue = value; }
        }
        private InsertOp _InsertOp;
        /// <summary>
        /// 数据插入选项
        /// </summary>
        public InsertOp InsertOp
        {
            get { return _InsertOp; }
            set { _InsertOp = value; }
        }

        private int _PageIndex;
        /// <summary>
        /// 分页起始页
        /// </summary>
        public int PageIndex
        {
            get { return _PageIndex; }
            set { _PageIndex = value; }
        }
        private int _PageSize;
        /// <summary>
        /// 分页每页条数
        /// </summary>
        public int PageSize
        {
            get { return _PageSize; }
            set { _PageSize = value; }
        }
        private string _UpdateExpression;
        /// <summary>
        /// 更新操作的附加表达式。
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
        /// 指定的查询列
        /// </summary>
        public object[] SelectColumns
        {
            get { return _SelectColumns; }
            set { _SelectColumns = value; }
        }
        private bool _IsSuccess;
        /// <summary>
        /// Begin方法是否调用成功[在End中使用]
        /// </summary>
        public bool IsSuccess
        {
            get { return _IsSuccess; }
            set { _IsSuccess = value; }
        }
        private int _TotalCount;
        /// <summary>
        /// 查询时返回的记录总数（分页总数）
        /// </summary>
        public int TotalCount
        {
            get
            {
                if (_TotalCount == 0 && _Table != null)
                {
                    return _Table.RecordsAffected;
                }
                return _TotalCount;
            }
            set { _TotalCount = value; }
        }

        private int _RowCount;
        /// <summary>
        /// 查询时返回的显示数量
        /// </summary>
        public int RowCount
        {
            get
            {
                if (_RowCount == 0 && _Table != null)
                {
                    return _Table.Rows.Count;
                }
                return _RowCount;
            }
            set { _RowCount = value; }
        }

        private List<AopCustomDbPara> _CustomDbPara;
        /// <summary>
        /// 用户调用SetPara新增加的自定义参数
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
        /// 当前上下文的处理程序。
        /// </summary>
        public MProc MProc
        {
            get { return _MProc; }
            set { _MProc = value; }
        }

        private string _ProcName;
        /// <summary>
        /// 存储过程名或SQL语句
        /// </summary>
        public string ProcName
        {
            get { return _ProcName; }
            set { _ProcName = value; }
        }
        private bool _IsProc;
        /// <summary>
        /// 是否存储过程
        /// </summary>
        public bool IsProc
        {
            get { return _IsProc; }
            set { _IsProc = value; }
        }
        private DbParameterCollection _DbParameters;
        /// <summary>
        /// 命令参数
        /// </summary>
        public DbParameterCollection DBParameters
        {
            get { return _DbParameters; }
            set { _DbParameters = value; }
        }
        private object _ExeResult;
        /// <summary>
        /// 执行后返回的结果
        /// </summary>
        public object ExeResult
        {
            get { return _ExeResult; }
            set { _ExeResult = value; }
        }


    }
    /// <summary>
    /// 设置自定义参数[使用MAction时，对Where条件进行的参数化传参使用]
    /// </summary>
    public class AopCustomDbPara
    {
        private string _ParaName;
        /// <summary>
        /// 参数名称
        /// </summary>
        public string ParaName
        {
            get { return _ParaName; }
            set { _ParaName = value; }
        }
        private object _Value;
        /// <summary>
        /// 参数值
        /// </summary>
        public object Value
        {
            get { return _Value; }
            set { _Value = value; }
        }
        private DbType _DbType;
        /// <summary>
        /// 参数类型
        /// </summary>
        public DbType ParaDbType
        {
            get { return _DbType; }
            set { _DbType = value; }
        }
        //private bool _IsSysPara;
        ///// <summary>
        ///// 是否系统内部产生的参数
        ///// </summary>
        //internal bool IsSysPara
        //{
        //    get { return _IsSysPara; }
        //    set { _IsSysPara = value; }
        //}

    }
}
