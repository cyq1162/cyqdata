using System;
using System.Threading;
using CYQ.Data.Cache;

namespace CYQ.Data
{
    /// <summary>
    /// this class can intercept sql
    ///<para> 应用程序调试类,能截到应用程序执行的SQL</para>
    /// </summary>
    public static class AppDebug
    {
        private static DistributedCache _Cache = DistributedCache.Local;
        /// <summary>
        /// is recoreding sql ?
        /// <para>正在记录中</para>
        /// </summary>
        public static bool IsRecording
        {
            get
            {
                return _Cache.Contains(Key);
            }
        }
        private static string Key
        {
            get
            {
                return "AppDebug_Key" + Thread.CurrentThread.ManagedThreadId;// "CYQ.Data.AppDebug_RecordSQL";
            }
        }

        private static string KeyTime
        {
            get
            {
                return "AppDebug_KeyTime" + Thread.CurrentThread.ManagedThreadId;// "CYQ.Data.AppDebug_RecordSQL";
            }
        }
        private static string KeySys
        {
            get
            {
                return "AppDebug_KeySys" + Thread.CurrentThread.ManagedThreadId;// "CYQ.Data.AppDebug_RecordSQL";
            }
        }
        /// <summary>
        /// 输出信息时是否包含框架内部Sql（默认否，屏蔽框架内部产生的Sql）。
        /// </summary>
        internal static bool IsContainSysSql
        {
            get
            {
                return _Cache.Get<bool>(KeySys);
            }
            set
            {
                _Cache.Set(KeySys, value);
            }
        }

        /// <summary>
        /// get sql detail info
        /// <para>获取调试信息</para>
        /// </summary>
        public static string Info
        {
            get
            {
                string info = _Cache.Get<string>(Key);
                if (!string.IsNullOrEmpty(info))
                {
                    DateTime time = _Cache.Get<DateTime>(KeyTime);
                    if (time != DateTime.MinValue)
                    {
                        DateTime start = (DateTime)time;
                        TimeSpan ts = DateTime.Now - start;
                        info += AppConst.HR + "all execute time is: " + ts.TotalMilliseconds + " (ms)";
                    }
                }
                return info;

            }
        }
        private static int _InfoFilter = 0;
        /// <summary>
        /// 设置Info信息进行过滤（毫秒ms)SQL语句。
        /// </summary>
        public static int InfoFilter
        {
            get
            {
                return _InfoFilter;
            }
            set
            {
                _InfoFilter = value;
            }
        }
        /// <summary>
        /// start to record sql
        /// <para>开始记录调试信息</para>
        /// <param name="isContainSysSql">是否包含框架内部Sql（默认否）</param>
        /// </summary>
        public static void Start(bool isContainSysSql)
        {
            _Cache.Set(Key, string.Empty);
            _Cache.Set(KeyTime, DateTime.Now);
            _Cache.Set(KeySys, isContainSysSql);
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
            _Cache.Remove(Key);
            _Cache.Remove(KeyTime);
            _Cache.Remove(KeySys);
        }
        internal static void Add(string sql)
        {
            string sqlObj = _Cache.Get<string>(Key);
            _Cache.Set(Key, sqlObj + sql);
        }
    }
}
