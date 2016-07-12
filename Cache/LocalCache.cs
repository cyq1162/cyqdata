using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Threading;

using CYQ.Data.Tool;
using CYQ.Data.Table;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// 单机缓存类
    /// </summary>
    internal class LocalCache : CacheManage
    {
        private readonly HttpContext H = new HttpContext(new HttpRequest("Null.File", "http://www.cyqdata.com", String.Empty), new HttpResponse(null));
        private System.Web.Caching.Cache theCache = null;
        private SortedDictionary<int, StringBuilder> keyTime = new SortedDictionary<int, StringBuilder>();
        /// <summary>
        /// 非线程安全。
        /// </summary>
        private MDictionary<string, CacheDependencyInfo> theState = new MDictionary<string, CacheDependencyInfo>(500, StringComparer.OrdinalIgnoreCase);
        private static object lockObj = new object();
        private DateTime workTime, startTime;
        /// <summary>
        /// 获取当前缓存信息对象列表
        /// </summary>
        public override Dictionary<string, CacheDependencyInfo> CacheInfo
        {
            get
            {
                Dictionary<string, CacheDependencyInfo> info = null;
                lock (lockObj)
                {
                    try
                    {
                        info = new Dictionary<string, CacheDependencyInfo>(theState);
                    }
                    catch
                    {

                    }
                }
                return info;
            }
        }
        private DateTime getCacheTableTime = DateTime.Now;//获取缓存表的数据的时间。
        private MDataTable _CacheSchemaTable;
        private MDataTable CacheSchemaTable
        {
            get
            {
                if (_CacheSchemaTable == null || _CacheSchemaTable.Columns.Count == 0)
                {
                    _CacheSchemaTable = new MDataTable("SysDefaultCacheSchemaTable");
                    _CacheSchemaTable.Columns.Add("ID", System.Data.SqlDbType.Int, false);
                    _CacheSchemaTable.Columns.Add("CacheKey", System.Data.SqlDbType.NVarChar);
                    _CacheSchemaTable.Columns.Add("CacheMinutes", System.Data.SqlDbType.Float);
                    _CacheSchemaTable.Columns.Add("CallCount", System.Data.SqlDbType.Int);
                    _CacheSchemaTable.Columns.Add("IsChanged", System.Data.SqlDbType.Bit);
                    _CacheSchemaTable.Columns.Add("CreateTime", System.Data.SqlDbType.DateTime);
                }
                return _CacheSchemaTable;
            }
        }

        /// <summary>
        /// 获取缓存信息对象列表（以表信息返回，默认5分钟更新一次，特殊情况会快速更新（如：Key被添加移除时））
        /// </summary>
        public override MDataTable CacheTable
        {
            get
            {
                if (CacheSchemaTable.Rows.Count == 0 || _CacheSchemaTable.Rows.Count != theState.Count || getCacheTableTime.AddMinutes(5) < DateTime.Now)
                {
                    getCacheTableTime = DateTime.Now;
                    CacheSchemaTable.Rows.Clear();
                    MDataRow row = null;
                    int id = 1;
                    foreach (KeyValuePair<string, CacheDependencyInfo> item in CacheInfo)
                    {
                        row = _CacheSchemaTable.NewRow();
                        row[0].Value = id;
                        row[1].Value = item.Key;
                        row[2].Value = item.Value.cacheMinutes;
                        row[3].Value = item.Value.callCount;
                        row[4].Value = item.Value.IsChanged;
                        row[5].Value = item.Value.createTime;
                        _CacheSchemaTable.Rows.Add(row);
                        id++;
                    }
                }
                return _CacheSchemaTable;
            }
        }
        internal LocalCache()
        {
            try
            {
                theCache = H.Cache;//如果配置文件错误会引发此异常。
                ThreadBreak.AddGlobalThread(new ParameterizedThreadStart(ClearState));
                if (AppConfig.Cache.IsAutoCache)
                {
                    ThreadBreak.AddGlobalThread(new ParameterizedThreadStart(AutoCache.ClearCache));
                }
            }
            catch (Exception err)
            {
                Log.WriteLogToTxt(err);
                // throw;
            }
        }
        int taskCount = 0, taskInterval = 10;//10分钟清一次缓存。
        private static DateTime errTime = DateTime.MinValue;
        private void ClearState(object threadID)
        {
            startTime = DateTime.Now;
            while (true)
            {
                try
                {
                    workTime = startTime.AddMinutes((taskCount + 1) * taskInterval);
                    TimeSpan ts = workTime - DateTime.Now;
                    if (ts.TotalSeconds > 0)
                    {
                        Thread.Sleep(ts);//taskInterval * 60 * 1000);//10分钟休眠时间
                    }
                    #region 新的机制
                    if (keyTime.ContainsKey(taskCount))
                    {
                        RemoveList(keyTime[taskCount].ToString().Split(','));
                        keyTime.Remove(taskCount);
                    }
                    #endregion

                    #region 旧方式、注释掉
                    /*
                        if (theCache.Count != theState.Count)//准备同步
                        {
                            workTime = DateTime.Now.AddMinutes(cacheClearWorkTime);

                            foreach (KeyValuePair<string, CacheDependencyInfo> item in CacheInfo)
                            {
                                if (item.Value == null)
                                {
                                    continue;
                                }
                                if (item.Value.createTime.AddMinutes(item.Value.cacheMinutes) < DateTime.Now)//清过期cacheState进行同步
                                {
                                    if (!removeKeys.Contains(item.Key))
                                    {
                                        removeKeys.Add(item.Key);
                                    }
                                }
                            }

                            RemoveList(removeKeys);
                            clearCount = removeKeys.Count;
                            removeKeys.Clear();
                        } */
                    #endregion

                }
                catch (OutOfMemoryException)
                { }
                catch (Exception err)
                {
                    if (errTime == DateTime.MinValue || errTime.AddMinutes(10) < DateTime.Now) // 10分钟记录一次
                    {
                        errTime = DateTime.Now;
                        Log.WriteLogToTxt("LocalCache.ClearState:" + Log.GetExceptionMessage(err));
                    }
                }
                finally
                {
                    taskCount++;
                    if (taskCount % 10 == 9)
                    {
                        try
                        {
                            if (RemainMemoryPercentage < 25)
                            {
                                GC.Collect();
                            }
                        }
                        catch
                        {

                        }
                    }
                }

            }
        }
        private int errorCount = 0;//缓存捕异常次数
        /// <summary>
        /// 内存工作信息
        /// </summary>
        public override string WorkInfo
        {
            get
            {
                JsonHelper js = new JsonHelper(false, false);
                js.Add("TaskCount", taskCount.ToString(), true);
                js.Add("ErrorCount", errorCount.ToString(), true);
                js.Add("NextTaskTime", workTime.ToString());
                js.AddBr();
                return js.ToString();
                // return string.Format("try catch error count:{0}--clear count:{1}--next clear work time at:{2}", errorCount, taskCount, workTime);
            }
        }
        /// <summary>
        /// 获和缓存总数
        /// </summary>
        public override int Count
        {
            get
            {
                return theCache == null ? 0 : theCache.Count;
            }
        }

        /// <summary>
        /// 还可用的缓存百分比
        /// </summary>
        public override long RemainMemoryPercentage
        {
            get
            {
                return theCache.EffectivePercentagePhysicalMemoryLimit;
            }
        }
        /// <summary>
        /// 还可用的缓存字节数
        /// </summary>
        public override long RemainMemoryBytes
        {
            get
            {
                return theCache.EffectivePrivateBytesLimit;
            }
        }

        /// <summary>
        /// 获得一个Cache对象
        /// </summary>
        /// <param name="key">标识</param>
        public override object Get(string key)
        {
            if (theState.ContainsKey(key))
            {
                lock (lockObj)
                {
                    try
                    {
                        theState[key].callCount++;
                    }
                    catch
                    {
                        //errorCount++;
                    }
                }
                return theCache[key];
            }
            return null;
        }
        /// <summary>
        /// 是否存在缓存
        /// </summary>
        /// <param name="key">标识</param>
        /// <returns></returns>
        public override bool Contains(string key)
        {
            if (theState.ContainsKey(key))
            {
                CacheDependencyInfo info = theState[key];
                bool isOutTime = info.createTime.AddMinutes(info.cacheMinutes) < DateTime.Now;
                if (!isOutTime && theCache[key] != null)
                {
                    return true;
                }
                else
                {
                    lock (lockObj)
                    {
                        try
                        {
                            theState.Remove(key);
                            if (isOutTime)
                            {
                                theCache.Remove(key);
                            }
                        }
                        catch
                        {
                            //errorCount++;
                        }
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 添加一个Cache对象
        /// </summary>
        /// <param name="key">标识</param>
        /// <param name="value">对象值</param>
        public override void Add(string key, object value)
        {
            Add(key, value, null, AppConfig.Cache.DefaultCacheTime);
        }
        /// <param name="fileName">关联的文件</param>
        public override void Add(string key, object value, string fileName)
        {
            Add(key, value, fileName, AppConfig.Cache.DefaultCacheTime);//再插入Cache
        }
        /// <param name="cacheMinutes">缓存时间(单位分钟)</param>
        public override void Add(string key, object value, double cacheMinutes)
        {
            Add(key, value, null, cacheMinutes, CacheItemPriority.Default);
        }
        /// <param name="cacheMinutes">缓存时间(单位分钟)</param>
        public override void Add(string key, object value, string fileName, double cacheMinutes)
        {
            Add(key, value, fileName, cacheMinutes, CacheItemPriority.Default);
        }
        /// <param name="level">缓存级别[高于Normal的将不受定时缓存清理的限制]</param>
        public override void Add(string key, object value, string fileName, double cacheMinutes, CacheItemPriority level)
        {
            if (Contains(key))
            {
                Remove(key);
            }
            Insert(key, value, fileName, cacheMinutes, level);//再插入Cache

        }
        /// <summary>
        /// 缓存设置：有则更新，无则添加
        /// </summary>
        public override void Set(string key, object value)
        {
            Set(key, value, AppConfig.Cache.DefaultCacheTime);
        }
        public override void Set(string key, object value, double cacheMinutes)
        {
            if (Contains(key))
            {
                theCache[key] = value;
                theState[key].cacheMinutes = cacheMinutes;
            }
            else
            {
                Insert(key, value, null, cacheMinutes, CacheItemPriority.Default);//再插入Cache
            }
        }
        /// <summary>
        /// 更新缓存，缓存存在则更更新，不存在则跳过
        /// </summary>
        public override void Update(string key, object value)
        {
            if (Contains(key))
            {
                theCache[key] = value;
            }
        }
        /// <summary>
        /// 相对底层Cache添加方法,添加一个Cache请用Add方法
        /// </summary>
        /// <param name="key">标识</param>
        /// <param name="value">对象值</param>
        /// <param name="fileName">依赖文件</param>
        /// <param name="cacheMinutes">缓存分钟数</param>
        /// <param name="level" >缓存级别</param>
        private void Insert(string key, object value, string fileName, double cacheMinutes, CacheItemPriority level)
        {
            CacheDependency theCacheDependency = null;
            if (!string.IsNullOrEmpty(fileName))
            {
                theCacheDependency = new CacheDependency(fileName);
            }
            double cacheTime = cacheMinutes;
            if (cacheMinutes <= 0)
            {
                cacheTime = AppConfig.Cache.DefaultCacheTime;
            }
            DateTime cTime = DateTime.Now.AddMinutes(cacheTime);
            theCache.Insert(key, value == null ? string.Empty : value, theCacheDependency, cTime, TimeSpan.Zero, level, null);
            CacheDependencyInfo info = new CacheDependencyInfo(theCacheDependency, cacheTime);
            lock (lockObj)
            {
                try
                {
                    if (theState.ContainsKey(key))
                    {
                        theState[key] = info;
                    }
                    else
                    {
                        theState.Add(key, info);
                    }
                    TimeSpan ts = cTime - startTime;
                    int workCount = (int)ts.TotalMinutes / taskInterval;//计算出离开始有多少个间隔时间。
                    if (keyTime.ContainsKey(workCount))
                    {
                        keyTime[workCount].Append("," + key);
                    }
                    else
                    {
                        keyTime.Add(workCount, new StringBuilder(key));
                    }
                }
                catch
                {

                }
            }
            getCacheTableTime = DateTime.Now.AddMinutes(-5);//设置缓存表过期。
        }

        /// <summary>
        /// 删除一个Cache对象
        /// </summary>
        /// <param name="key">标识</param>
        public override void Remove(string key)
        {
            if (Contains(key))//检测存在时中会自动清除cacheState
            {
                try
                {
                    theCache.Remove(key);
                    lock (lockObj)
                    {
                        try
                        {
                            theState.Remove(key);
                        }
                        catch
                        {

                        }
                    }
                }
                catch
                {
                    errorCount++;
                }
            }
        }
        private void RemoveList(string[] keys)
        {
            if (keys != null && keys.Length > 0)
            {
                lock (lockObj)
                {
                    foreach (string key in keys)
                    {
                        try
                        {
                            if (theCache[key] != null)
                            {
                                theCache.Remove(key);
                            }

                            if (theState.ContainsKey(key))
                            {
                                theState.Remove(key);
                            }
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public override void Clear()
        {
            try
            {
                if (theCache.Count > 0)
                {
                    System.Collections.IDictionaryEnumerator e = theCache.GetEnumerator();
                    while (e.MoveNext())
                    {
                        theCache.Remove(Convert.ToString(e.Key));
                    }
                }
                lock (lockObj)
                {
                    try
                    {
                        theState.Clear();
                        theState = null;
                        theState = new MDictionary<string, CacheDependencyInfo>(500, StringComparer.OrdinalIgnoreCase);
                    }
                    catch
                    {

                    }
                }
            }
            catch
            {
                errorCount++;
            }
        }
        /// <summary>
        /// 获取目标的文件依赖是否发生更改
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override bool GetFileDependencyHasChanged(string key)
        {
            if (Contains(key))
            {
                CacheDependencyInfo info = theState[key];
                if (info != null)
                {
                    return info.IsChanged;
                }
            }
            return true;
        }
        /// <summary>
        /// 手动对缓存象标识为已更改
        /// </summary>
        public override void SetChange(string key, bool isChange)
        {
            if (Contains(key))
            {
                CacheDependencyInfo info = theState[key];
                if (info != null)
                {
                    info.SetState(isChange);
                }
            }
            else
            {
                Add("Cache:Temp_" + key, isChange, null, 0.1);//缓存失效时，产生6秒的key缓冲
            }
        }
        /// <summary>
        /// 获取缓存对象是否被手工标识为已更改
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override bool GetHasChanged(string key)
        {
            if (Contains(key))
            {
                CacheDependencyInfo info = theState[key];
                if (info != null)
                {
                    return info.IsChanged ? false : info.UserChange;
                }
            }
            else if (Contains("Cache:Temp_" + key))
            {
                return (bool)theCache.Get("Cache:Temp_" + key);
            }
            return false;
        }

        public override CacheType CacheType
        {
            get { return CacheType.LocalCache; }
        }
    }
}
