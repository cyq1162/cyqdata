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
        public void Debug(string message) { Log.WriteLogToTxt(DateTime.Now + loggerName + " - " + message, LogType.Debug); }
        public void Info(string message) { Log.WriteLogToTxt(DateTime.Now + loggerName + " - " + message, LogType.Info); }
        public void Warn(string message) { Log.WriteLogToTxt(DateTime.Now + loggerName + " - " + message, LogType.Warn); }
        public void Error(string message) { Log.WriteLogToTxt(DateTime.Now + loggerName + " - " + message, LogType.Error); }

        public void Debug(string message, Exception e) { Log.WriteLogToTxt(DateTime.Now + loggerName + " - " + message + "\n" + e.Message + "\n" + e.StackTrace, LogType.Debug); }
        public void Info(string message, Exception e) { Log.WriteLogToTxt(DateTime.Now + loggerName + " - " + message + "\n" + e.Message + "\n" + e.StackTrace, LogType.Info); }
        public void Warn(string message, Exception e) { Log.WriteLogToTxt(DateTime.Now + loggerName + " - " + message + "\n" + e.Message + "\n" + e.StackTrace, LogType.Warn); }
        public void Error(string message, Exception e) { Log.WriteLogToTxt(DateTime.Now + loggerName + " - " + message + "\n" + e.Message + "\n" + e.StackTrace, LogType.Error); }



    }
}
