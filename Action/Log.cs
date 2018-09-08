using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;

using System.Threading;
using CYQ.Data.Tool;


namespace CYQ.Data
{
    internal static class Error
    {
        /// <summary>
        /// 抛出异常
        /// </summary>
        /// <param name="msg"></param>
        internal static object Throw(string msg)
        {
            //#if DEBUG
            //            return "";
            //#else
            throw new Exception("V" + AppConfig.Version + " " + msg);
            //#endif
        }
    }
    /// <summary>
    /// LogType
    /// <para>日志类型</para>
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// 调试
        /// </summary>
        Debug,
        /// <summary>
        /// 信息
        /// </summary>
        Info,
        /// <summary>
        /// 警告
        /// </summary>
        Warn,
        /// <summary>
        /// 错误
        /// </summary>
        Error,
        /// <summary>
        /// 断言
        /// </summary>
        Assert
    }
    /// <summary>
    /// Write Log to txt or database
    /// <para>日志静态类（可将日志输出到文本中）</para>
    /// </summary>
    public static class Log
    {

        /// <summary>
        /// 内部写日志
        /// </summary>
        /// <param name="message"></param>
        internal static void WriteLog(string message)
        {
            WriteLog(false, message);
        }
        internal static void WriteLog(bool isWriteLog, string message)
        {
            if (isWriteLog || AppConfig.Log.IsWriteLog)
            {
                if (message.Contains(":OpenCon()"))//数据库链接异常不再写数据库（因为很多情况都指向同一个库）
                {
                    WriteLogToTxt(message, LogType.Error);
                }
                else
                {
                    WriteLogToDB(message, LogType.Error);
                }
            }
            else
            {
                Error.Throw("Error : " + message);
            }
        }
        /// <summary>
        /// write log to database [LogConn must has config value]
        ///  <para>将日志写到数据库中[需要配置LogConn项后方生效 ]</para>
        /// </summary>
        public static void WriteLogToDB(Exception err)
        {
            WriteLogToDB(GetExceptionMessage(err));
        }
        /// <summary>
        /// write log to database [LogConn must has config value]
        ///  <para>将日志写到数据库中[需要配置LogConn项后方生效 ]</para>
        /// </summary>
        public static void WriteLogToDB(string message)
        {
            WriteLogToDB(message, LogType.Error);
        }

        public static void WriteLogToDB(string message, LogType logType)
        {
            WriteLogToDB(message, logType, "System");
        }

        public static void WriteLogToDB(string message, LogType logType, string userName)
        {
            WriteLogToDB(message, logType.ToString(), userName);
        }


        public static void WriteLogToDB(string message, string logType, string userName)
        {
            string conn = AppConfig.Log.LogConn;
            if (string.IsNullOrEmpty(conn))
            {
                WriteLogToTxt("[LogConnIsEmpty]:" + message);
                return;
            }
            try
            {
                string pageUrl = string.Empty;
                if (AppConfig.IsWeb)
                {
                    System.Web.HttpRequest request = System.Web.HttpContext.Current.Request;
                    pageUrl = request.Url.Scheme + "://" + request.Url.Authority + request.RawUrl;
                }
                else
                {
                    pageUrl = AppConst.RunFolderPath;
                }

                using (SysLogs el = new SysLogs())
                {
                    el.AllowWriteLog = false;
                    el.LogType = logType.ToString();
                    el.PageUrl = pageUrl;
                    el.Message = message;
                    el.UserName = userName;
                    el.CreateTime = DateTime.Now;
                    el.Insert(InsertOp.None);
                }
            }
            catch
            {
                WriteLogToTxt("[WriteDbLogError]:" + message);
            }
        }

        #region 日志写到文件


        /// <summary>
        /// Write log to txt
        ///  <para>将日志写到外部txt[web.config中配置路径，配置项为Logpath,默认路径为 "Log/" ]</para>
        /// </summary>
        public static void WriteLogToTxt(string message)
        {
            WriteLogToTxt(message, null);
        }


        public static void WriteLogToTxt(string message, LogType logType)
        {
            WriteLogToTxt(message, logType.ToString());
        }

        public static void WriteLogToTxt(string message, string logType)
        {
            try
            {
                string folder = AppConfig.RunPath;
                string logPath = AppConfig.Log.LogPath;
                if (logPath.StartsWith("~/"))
                {
                    logPath = logPath.Substring(2);
                }
                folder = folder + logPath;
                if (!System.IO.Directory.Exists(folder))
                {
                    System.IO.Directory.CreateDirectory(folder);
                }
                string todayKey = DateTime.Today.ToString("yyyyMMdd") + ".txt";
                if (!string.IsNullOrEmpty(logType))
                {
                    if (logType.EndsWith(".txt"))
                    {
                        todayKey = logType;
                    }
                    else
                    {
                        todayKey = logType.TrimEnd('_') + '_' + todayKey;
                    }
                }
                string filePath = folder + todayKey;
                string title = "V" + AppConfig.Version + " Record On : " + DateTime.Now.ToString() + " : " + Log.Url;
                string body = title + AppConst.NewLine + AppConst.NewLine + message + AppConst.NewLine;
                body += "---------------------------------------" + AppConst.NewLine + AppConst.NewLine;
                IOHelper.Save(filePath, body, true, false);

            }
            catch //(Exception err)
            {
                //Error.Throw("Log.WriteLogToTxt() : " + err.Message);
            }
        }

        public static void WriteLogToTxt(Exception err)
        {
            if (err is ThreadAbortException)//线程中止异常不记录
            {
                return;
            }
            string message = GetExceptionMessage(err);
            WriteLogToTxt("[Exception]:" + message + AppConst.NewLine + err.StackTrace);
        }
        #endregion

        internal static string Url
        {
            get
            {
                string pageUrl = string.Empty;
                if (AppConfig.IsWeb)
                {
                    System.Web.HttpRequest request = System.Web.HttpContext.Current.Request;
                    pageUrl = request.Url.Scheme + "://" + request.Url.Authority + request.RawUrl;
                    if (request.UrlReferrer != null && request.Url != request.UrlReferrer)
                    {
                        pageUrl += AppConst.NewLine + AppConst.NewLine + "Referer:" + request.UrlReferrer.ToString();
                    }
                }
                return pageUrl;
            }
        }

        /// <summary>
        /// Convert Exception to string
        /// <para>获取异常的内部信息</para>
        /// </summary>
        public static string GetExceptionMessage(Exception err)
        {
            string message = err.Message;
            if (err.InnerException != null)
            {
                message += ":" + err.InnerException.Message + AppConst.NewLine + err.InnerException.StackTrace;
            }
            else
            {
                message += ":" + AppConst.NewLine + err.StackTrace;
            }
            return message;
        }
    }
    /// <summary>
    /// A class for you to Write log to database
    /// <para>日志记录到数据库（需要配置LogConn链接后方有效）</para>
    /// </summary>
    public class SysLogs : Orm.SimpleOrmBase
    {
        public SysLogs()
        {
            base.SetInit(this, AppConfig.Log.LogTableName, AppConfig.Log.LogConn);
        }
        private int _ID;
        /// <summary>
        /// 标识主键
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
        private string _LogType;
        /// <summary>
        /// 日志类型
        /// </summary>
        public string LogType
        {
            get { return _LogType; }
            set { _LogType = value; }
        }

        private string _PageUrl;
        /// <summary>
        /// 请求的地址
        /// </summary>
        public string PageUrl
        {
            get
            {
                if (string.IsNullOrEmpty(_PageUrl))
                {
                    _PageUrl = Log.Url;
                }
                return _PageUrl;
            }
            set
            {
                _PageUrl = value;
            }
        }
        private string _Message;
        /// <summary>
        /// 日志内容
        /// </summary>
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
        private string _UserName;
        /// <summary>
        /// 记录者用户名
        /// </summary>
        public string UserName
        {
            get { return _UserName; }
            set { _UserName = value; }
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
}
