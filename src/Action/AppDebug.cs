using System;
using CYQ.Data.Cache;

namespace CYQ.Data
{
    /// <summary>
    /// this class can intercept sql
    ///<para> 应用程序调试类,能截到应用程序所有执行的SQL</para>
    /// </summary>
    public static class AppDebug
    {
        private static CacheManage _Cache = CacheManage.LocalInstance;
        /// <summary>
        /// is recoreding sql ?
        /// <para>正在记录中</para>
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
        /// 输出信息时是否包含框架内部Sql（默认否，屏蔽框架内部产生的Sql）。
        /// </summary>
        internal static bool IsContainSysSql = false;
        /// <summary>
        /// get sql detail info
        /// <para>获取调试信息</para>
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
        /// start to record sql
        /// <para>开始记录调试信息</para>
        /// <param name="isContainSysSql">是否包含框架内部Sql（默认否）</param>
        /// </summary>
        public static void Start(bool isContainSysSql)
        {
            _Cache.Set(_Key, string.Empty);
            _Cache.Set(_KeyTime, DateTime.Now);
            IsContainSysSql = isContainSysSql;
        }
        /// <summary>
        /// start to record sql
        /// <para>开始记录调试信息</para>
        /// </summary>
        public static void Start()
        {
            Start(false);
        }
        /// <summary>
        /// stop to record sql
        /// <para>停止并清除记录的调试信息</para>
        /// </summary>
        public static void Stop()
        {
            _Cache.Remove(_Key);
            _Cache.Remove(_KeyTime);
            IsContainSysSql = false;
        }
        internal static void Add(string sql)
        {
            object sqlObj = _Cache.Get(_Key);
            _Cache.Set(_Key, sqlObj + sql);
        }
    }
}
