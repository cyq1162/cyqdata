using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data
{
    internal static class Error
    {
        /// <summary>
        /// �׳��쳣
        /// </summary>
        /// <param name="msg"></param>
        internal static object Throw(string msg)
        {
#if DEBUG
           // return msg;
            //#else
#endif
            throw new Exception("V" + AppConfig.Version + " " + msg);

        }
    }
    /// <summary>
    /// LogType
    /// <para>��־����</para>
    /// </summary>
    public static class LogType
    {
        /// <summary>
        /// ����
        /// </summary>
        public const string Debug = "Debug";
        /// <summary>
        /// ��Ϣ
        /// </summary>
        public const string Info = "Info";
        /// <summary>
        /// ����
        /// </summary>
        public const string Warn = "Warn";
        /// <summary>
        /// ����
        /// </summary>
        public const string Error = "Error";
        /// <summary>
        /// DataBase
        /// </summary>
        public const string DataBase = "DataBase";
        /// <summary>
        /// Cache
        /// </summary>
        public const string Cache = "Cache";
        /// <summary>
        /// Aries Dev Framework
        /// </summary>
        public const string Aries = "Aries";
        /// <summary>
        /// Taurus Dev Framework
        /// </summary>
        public const string Taurus = "Taurus";
        /// <summary>
        /// Gemini Workflow Dev Framework
        /// </summary>
        public const string Gemini = "Gemini";
        /// <summary>
        /// Taurus.MicroService
        /// </summary>
        public const string MicroService = "MicroService";
    }
}
