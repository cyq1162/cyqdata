using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;

using System.Threading;
using CYQ.Data.Tool;
using System.Web;


namespace CYQ.Data
{

    /// <summary>
    /// Write Log to txt or database
    /// <para>日志静态类（可将日志输出到文本中）</para>
    /// </summary>
    public static partial class Log
    {
        #region 公开的方法
        /// <summary>
        /// 如果配置数据库则写数据库，没有配置写文本
        /// </summary>
        public static void Write(Exception err)
        {
            Write(err, null);
        }
        public static void Write(Exception err, string logType)
        {
            if (!string.IsNullOrEmpty(AppConfig.Log.Conn))
            {
                WriteLogToDB(err, logType);
            }
            else
            {
                WriteLogToTxt(err, logType);
            }
        }
        /// <summary>
        /// 如果配置数据库则写数据库，没有配置写文本
        /// </summary>
        public static void Write(string message)
        {
            Write(message, null);
        }
        public static void Write(string message, string logType)
        {
            if (!string.IsNullOrEmpty(AppConfig.Log.Conn))
            {
                WriteLogToDB(message, logType);
            }
            else
            {
                WriteLogToTxt(message, logType);
            }
        }

        #endregion

        #region 写文本、4个方法


        public static void WriteLogToTxt(Exception err)
        {
            WriteLogToTxt(err, null);
        }
        public static void WriteLogToTxt(Exception err, string logType)
        {
            //线程中止异常不记录
            if (err == null || err is ThreadAbortException) { return; }
            string message = GetExceptionMessage(err);
            WriteLogToTxt("[Exception]:" + message + AppConst.NewLine + err.StackTrace, logType);
        }
        /// <summary>
        /// Write log to txt
        ///  <para>指定将日志写到外部txt[web.config中配置路径，配置项为Logpath,默认路径为 "Log/" ]</para>
        /// </summary>
        public static void WriteLogToTxt(string message)
        {
            if (string.IsNullOrEmpty(message)) { return; }
            WriteLogToTxt(message, null);
        }

        public static void WriteLogToTxt(string message, string logType)
        {
            ReadyForWork(message, logType, true);
        }
        #endregion

        #region 写DB、4个方法
        public static void WriteLogToDB(Exception err)
        {
            WriteLogToDB(err, null);
        }
        /// <summary>
        /// write log to database [LogConn must has config value]
        ///  <para>将日志写到数据库中[需要配置LogConn项后方生效 ]</para>
        /// </summary>
        public static void WriteLogToDB(Exception err, string logType)
        {
            //线程中止异常不记录
            if (err == null || err is ThreadAbortException) { return; }
            string message = GetExceptionMessage(err);
            WriteLogToDB("[Exception]:" + message + AppConst.NewLine + err.StackTrace, logType);
        }
        public static void WriteLogToDB(string message)
        {
            WriteLogToDB(message, null);
        }
        /// <summary>
        /// write log to database [LogConn must has config value]
        ///  <para>将日志写到数据库中[需要配置LogConn项后方生效 ]</para>
        /// </summary>
        public static void WriteLogToDB(string message, string logType)
        {
            if (string.IsNullOrEmpty(message)) { return; }
            if (string.IsNullOrEmpty(AppConfig.Log.Conn))
            {
                Error.Throw("you need to add LogConn connectionString on *.config or *.json");
            }
            ReadyForWork(message, logType, false);
        }

        #endregion


        private static void ReadyForWork(string message, string logType, bool isWriteTxt)
        {
            if (!AppConfig.Log.IsEnable)
            {
                Error.Throw("Error : " + logType + " : " + message);
            }
            SysLogs log = new SysLogs();
            if (!isWriteTxt)
            {
                log.IsWriteLogOnError = false;
            }
            log.IsWriteToTxt = isWriteTxt;
            log.Message = message;
            log.LogType = logType;
            log.CreateTime = DateTime.Now;
            try
            {
                if (AppConfig.IsWeb && HttpContext.Current != null && HttpContext.Current.Handler != null)
                {
                    HttpRequest request = HttpContext.Current.Request;
                    log.HttpMethod = request.HttpMethod;
                    log.ClientIP = request.Headers["X-Real-IP"] ?? request.UserHostAddress;
                    Uri uri = request.Url;
                    log.PageUrl = uri.Scheme + "://" + uri.Authority + HttpUtility.UrlDecode(request.RawUrl);
                    if (request.UrlReferrer != null && uri != request.UrlReferrer)
                    {
                        log.RefererUrl = HttpUtility.UrlDecode(request.UrlReferrer.ToString());
                    }
                    
                }
            }
            catch
            {

            }
            LogWorker.Add(log);
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
}
