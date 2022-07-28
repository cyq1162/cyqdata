using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Orm;

namespace CYQ.Data
{
    /// <summary>
    /// A class for you to Write log to database
    /// <para>��־��¼�����ݿ⣨��Ҫ����LogConn���Ӻ���Ч��</para>
    /// </summary>
    public partial class SysLogs : SimpleOrmBase
    {

        public SysLogs()
        {
            if (!string.IsNullOrEmpty(AppConfig.Log.LogConn))
            {
                base.SetInit(this, AppConfig.Log.LogTableName, AppConfig.Log.LogConn);
            }
            else
            {
                IsWriteToTxt = true;
            }
        }

        private int _ID;
        /// <summary>
        /// ��ʶ����
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
        /// ��־����
        /// </summary>
        [Length(50)]
        public string LogType
        {
            get { return _LogType ?? ""; }
            set { _LogType = value; }
        }
        private string _HostName;
        /// <summary>
        /// �������������(ϵͳĬ�϶�ȡ������)
        /// </summary>
        [Length(100)]
        public string HostName
        {
            get
            {
                if (string.IsNullOrEmpty(_HostName))
                {
                    _HostName = LocalEnvironment.HostName;
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
        /// ���������IP(ϵͳĬ�϶�ȡ��������IP�������򷵻�������)
        /// </summary>
        [Length(100)]
        public string Host
        {
            get
            {
                if (string.IsNullOrEmpty(_Host))
                {
                    _Host = LocalEnvironment.HostIP;
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
        /// ���󷽷�
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
        /// �ͻ�������IP
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
        private string _PageUrl;
        /// <summary>
        /// ����ĵ�ַ
        /// </summary>
        [Length(500)]
        public string PageUrl
        {
            get
            {
                return _PageUrl;
            }
            set
            {
                _PageUrl = value;
            }
        }

        private string _RefererUrl;
        /// <summary>
        /// ����ĵ�ַ
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
        /// ��־����
        /// </summary>
        [Length(2000)]
        public string Message
        {
            get
            {
                return _Message;
            }
            set
            {
                _Message = value;
            }
        }

        private DateTime _CreateTime;
        /// <summary>
        /// ����ʱ��
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
        /// ����־д�뵽�����д��߳�ִ�С�
        /// </summary>
        public void Write()
        {
            if (!AppConfig.Log.IsWriteLog)
            {
                Error.Throw("Error : " + LogType + " : " + Message);
            }
            LogWorker.Add(this);
        }
        /// <summary>
        /// �Ƿ�д���ı���
        /// </summary>
        internal bool IsWriteToTxt = false;
        /// <summary>
        /// ��ȡ�ı���ʽ����־����
        /// </summary>
        /// <returns></returns>
        internal string GetFormatterText()
        {
            string title = string.Format("V{0} Record On : {1} : {2} {3}",
                AppConfig.Version, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), HttpMethod, PageUrl ?? "");// + Log.Url;
            if (!string.IsNullOrEmpty(ClientIP))
            {
                title += " - " + ClientIP;
            }
            if (!string.IsNullOrEmpty(RefererUrl))
            {
                title += AppConst.NewLine + AppConst.NewLine + "Referer : " + RefererUrl;
            }
            string body = title + AppConst.NewLine + AppConst.NewLine + Message.Replace("<br />", AppConst.NewLine) + AppConst.NewLine + AppConst.NewLine;
            body += "---------------------------------------" + AppConst.NewLine + AppConst.NewLine;
            return body;
        }
    }
}
