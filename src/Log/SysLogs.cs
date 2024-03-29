using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Orm;

namespace CYQ.Data
{
    /// <summary>
    /// A class for you to Write log to database
    /// <para>日志记录到数据库（需要配置LogConn链接后方有效）</para>
    /// </summary>
    public partial class SysLogs : SimpleOrmBase
    {

        public SysLogs()
        {
            if (!string.IsNullOrEmpty(AppConfig.Log.Conn))
            {
                base.SetInit(this, AppConfig.Log.TableName, AppConfig.Log.Conn);
            }
            else
            {
                IsWriteToTxt = true;
            }
        }

        private int _ID;
        /// <summary>
        /// 标识主键
        /// </summary>
        [Key(true, true, false)]
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

        private string _LogType;
        /// <summary>
        /// 日志类型
        /// </summary>
        [Length(50)]
        public string LogType
        {
            get { return _LogType ?? ""; }
            set { _LogType = value; }
        }

        private string _TraceID;
        /// <summary>
        /// 分布式追踪ID
        /// </summary>
        [Length(50)]
        public string TraceID
        {
            get
            {
                return _TraceID ?? "";
            }
            set
            {
                _TraceID = value;
            }
        }

        private string _HostName;
        /// <summary>
        /// 请求的主机名称(系统默认读取主机名)
        /// </summary>
        [Length(100)]
        public string HostName
        {
            get
            {
                if (string.IsNullOrEmpty(_HostName))
                {
                    _HostName = Environment.MachineName;
                }
                return _HostName;
            }
            set
            {
                _HostName = value;
            }
        }
        private string _Host;
        /// <summary>
        /// 请求的主机IP(系统默认读取主机内网IP，若无则返回主机名)
        /// </summary>
        [Length(100)]
        public string Host
        {
            get
            {
                if (string.IsNullOrEmpty(_Host))
                {
                    _Host = AppConst.HostIP;
                }
                return _Host;
            }
            set
            {
                _Host = value;
            }
        }

        private string _HttpMethod;
        /// <summary>
        /// 请求方法
        /// </summary>
        [Length(10)]
        public string HttpMethod
        {
            get
            {
                return _HttpMethod;
            }
            set
            {
                _HttpMethod = value;
            }
        }
        private string _ClientIP;
        /// <summary>
        /// 客户端请求IP
        /// </summary>
        [Length(50)]
        public string ClientIP
        {
            get
            {
                return _ClientIP;
            }
            set
            {

                _ClientIP = value == "::1" ? "127.0.0.1" : value;
            }
        }
        private string _RequestUrl;
        /// <summary>
        /// 请求的地址
        /// </summary>
        [Length(500)]
        public string RequestUrl
        {
            get
            {
                return _RequestUrl;
            }
            set
            {
                _RequestUrl = value;
            }
        }

        private string _RefererUrl;
        /// <summary>
        /// 请求的地址
        /// </summary>
        [Length(500)]
        public string RefererUrl
        {
            get
            {
                return _RefererUrl;
            }
            set
            {
                _RefererUrl = value;
            }
        }


        private string _Message;
        /// <summary>
        /// 日志内容
        /// </summary>
        [Length(2000)]
        public string Message
        {
            get
            {
                return _Message ?? "";
            }
            set
            {
                _Message = value;
            }
        }

        private DateTime _CreateTime;
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime
        {
            get
            {
                if (_CreateTime == DateTime.MinValue)
                {
                    _CreateTime = DateTime.Now;
                }
                return _CreateTime;
            }
            set
            {
                _CreateTime = value;
            }
        }
    }

    public partial class SysLogs
    {
        /// <summary>
        /// 将日志写入到队列中待线程执行。
        /// </summary>
        public void Write()
        {
            if (!AppConfig.Log.IsEnable)
            {
                Error.Throw("Error : " + LogType + " : " + Message);
            }
            LogWorker.Add(this);
        }
        /// <summary>
        /// 是否写到文本中
        /// </summary>
        internal bool IsWriteToTxt = false;
        /// <summary>
        /// 获取文本格式的日志内容
        /// </summary>
        /// <returns></returns>
        internal string GetFormatterText()
        {
            string title = string.Format("V{0} Record On : {1} : {2} {3}",
                AppConst.Version, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), HttpMethod, RequestUrl ?? "");// + Log.Url;
            if (!string.IsNullOrEmpty(ClientIP))
            {
                title += " - " + ClientIP;
            }
            if (!string.IsNullOrEmpty(TraceID))
            {
                title += AppConst.NewLine + AppConst.NewLine + "TraceID : " + TraceID;
            }
            if (!string.IsNullOrEmpty(RefererUrl))
            {
                if (string.IsNullOrEmpty(TraceID))
                {
                    title += AppConst.NewLine;
                }
                title += AppConst.NewLine + "Referer : " + RefererUrl;
            }
            string body = title + AppConst.NewLine + AppConst.NewLine + Message.Replace("<br />", AppConst.NewLine) + AppConst.NewLine + AppConst.NewLine;
            body += "---------------------------------------" + AppConst.NewLine + AppConst.NewLine;
            return body;
        }
    }
}
