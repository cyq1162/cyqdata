using System;
using CYQ.Data.Cache;

namespace CYQ.Data
{
    /// <summary>
    /// 应用程序调试类,能截到应用程序所有执行的SQL
    /// </summary>
    public static class AppDebug
    {
        private static CacheManage _Cache = CacheManage.LocalInstance;
        /// <summary>
        /// 正在记录中
        /// </summary>
        public static bool IsRecording
        {
            get
            {
                return _Cache.Contains(_Key);
            }
        }
        private const string _Key = "AppDebug_RecordSQL";// "CYQ.Data.AppDebug_RecordSQL";
        private const string _KeyTime = "AppDebug_RecordTime";// "CYQ.Data.AppDebug_RecordTime";
        /// <summary>
        /// 获取调试信息
        /// </summary>
        public static string Info
        {
            get
            {
                string info = string.Empty;
                if (AppConfig.Debug.OpenDebugInfo)
                {
                    info = Convert.ToString(_Cache.Get(_Key));
                    object time = _Cache.Get(_KeyTime);
                    if (time != null && time is DateTime)
                    {
                        DateTime start = (DateTime)time;
                        TimeSpan ts = DateTime.Now - start;
                        info += AppConst.HR + "all execute time is: " + ts.TotalMilliseconds + " (ms)";
                    }
                }
                return info;

            }
        }
        /// <summary>
        /// 开始记录调试信息
        /// </summary>
        public static void Start()
        {
            _Cache.Set(_Key, string.Empty);
            _Cache.Set(_KeyTime, DateTime.Now);
        }
        /// <summary>
        /// 停止并清除记录的调试信息
        /// </summary>
        public static void Stop()
        {
            _Cache.Remove(_Key);
            _Cache.Remove(_KeyTime);
        }
        internal static void Add(string sql)
        {
            object sqlObj = _Cache.Get(_Key);
            _Cache.Set(_Key, sqlObj + sql);
        }
    }
}
