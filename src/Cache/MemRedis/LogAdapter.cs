using System;

namespace CYQ.Data.Cache
{
    internal class LogAdapter
    {
        public static LogAdapter GetLogger(Type type)
        {
            return new LogAdapter(type);
        }
        private string loggerName;
        private LogAdapter(string name) { loggerName = name; }
        private LogAdapter(Type type) { loggerName = type.FullName; }
        public void Error(string message) { Log.Write(loggerName + " - " + message, LogType.Cache); }
        public void Error(string message, Exception e) { Log.Write(loggerName + " - " + message + "\r\n" + e.Message + "\r\n" + e.StackTrace, LogType.Cache); }



    }
}
