using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using CYQ.Data.Tool;
using CYQ.Data.Table;
using CYQ.Data.SQL;
using System.IO;


namespace CYQ.Data.Cache
{
    /// <summary>
    /// 单机缓存类
    /// 为兼容.NET Core 去掉Web.Caching，重写
    /// </summary>
    internal class LocalCache : CacheManage
    {
        private MDictionary<string, object> theCache = new MDictionary<string, object>(2048, StringComparer.OrdinalIgnoreCase);//key,cache
        private MDictionary<string, DateTime> theKeyTime = new MDictionary<string, DateTime>(2048, StringComparer.OrdinalIgnoreCase);//key,time
        private MDictionary<string, string> theFileName = new MDictionary<string, string>();//key,filename

        private SortedDictionary<int, MList<string>> theTime = new SortedDictionary<int, MList<string>>();//worktime,keylist
        private MDictionary<string, FileSystemWatcher> theFolderWatcher = new MDictionary<string, FileSystemWatcher>();//folderPath,watch
        private MDictionary<string, MList<string>> theFolderKeys = new MDictionary<string, MList<string>>();//folderPath,keylist

        private static object lockObj = new object();
        private DateTime workTime, startTime;
        private MDataTable _CacheSchemaTable;
        private MDataTable CacheSchemaTable
        {
            get
            {
                if (_CacheSchemaTable == null || _CacheSchemaTable.Columns.Count == 0)
                {
                    _CacheSchemaTable = new MDataTable(CacheType.ToString());
                    _CacheSchemaTable.Columns.Add("Key", System.Data.SqlDbType.NVarChar);
                    _CacheSchemaTable.Columns.Add("Value", System.Data.SqlDbType.NVarChar);
                }
                return _CacheSchemaTable;
            }
        }

        private DateTime getCacheTableTime = DateTime.Now;//获取缓存表的数据的时间。
        /// <summary>
        /// 获取缓存信息对象列表
        /// </summary>
        public override MDataTable CacheInfo
        {
            get
            {
                if (CacheSchemaTable.Rows.Count == 0 || getCacheTableTime.AddSeconds(20) < DateTime.Now)
                {
                    getCacheTableTime = DateTime.Now;
                    CacheSchemaTable.Rows.Clear();
                    CacheSchemaTable.NewRow(true).Set(0, "CacheCount").Set(1, theCache.Count);
                    CacheSchemaTable.NewRow(true).Set(0, "TimeCount").Set(1, theTime.Count);
                    CacheSchemaTable.NewRow(true).Set(0, "FileCount").Set(1, theFileName.Count);
                    CacheSchemaTable.NewRow(true).Set(0, "KeyTimeCount").Set(1, theKeyTime.Count);
                    CacheSchemaTable.NewRow(true).Set(0, "FolderWatcherCount").Set(1, theFolderWatcher.Count);
                    CacheSchemaTable.NewRow(true).Set(0, "TaskStartTime").Set(1, startTime);
                    CacheSchemaTable.NewRow(true).Set(0, "TaskWorkCount").Set(1, taskCount);
                    CacheSchemaTable.NewRow(true).Set(0, "ErrorCount").Set(1, errorCount);

                }
                return _CacheSchemaTable;
            }
        }
        internal LocalCache()
        {
            try
            {
                ThreadBreak.AddGlobalThread(new ParameterizedThreadStart(ClearState));
                if (AppConfig.Cache.IsAutoCache)
                {
                    ThreadBreak.AddGlobalThread(new ParameterizedThreadStart(AutoCache.ClearCache));
                }
            }
            catch (Exception err)
            {
                errorCount++;
                Log.WriteLogToTxt(err);
            }
        }
        int taskCount = 0, taskInterval = 5;//5分钟清一次缓存。
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
                    if (theTime.ContainsKey(taskCount))
                    {
                        RemoveList(theTime[taskCount].GetList());
                        theTime.Remove(taskCount);
                    }
                    #endregion


                }
                catch (ThreadAbortException e)
                {
                }
                catch (OutOfMemoryException)
                { errorCount++; }
                catch (Exception err)
                {
                    errorCount++;
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
                            if (theCache.Count > 100000)// theKey.Count > 100000)
                            {
                                NoSqlAction.ResetStaticVar();
                                GC.Collect();
                            }
                        }
                        catch
                        {
                            errorCount++;
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
                return theCache.Count;
                // return theKey.Count;
            }
        }

        /// <summary>
        /// 获得一个Cache对象
        /// </summary>
        /// <param name="key">标识</param>
        public override object Get(string key)
        {
            if (Contains(key))
            {
                return theCache[key];// && theCache.ContainsKey(key) 内部已有判断和Lock
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
            return theCache.ContainsKey(key) && theKeyTime.ContainsKey(key) && theKeyTime[key] > DateTime.Now;
        }
        /// <summary>
        /// 添加一个Cache对象
        /// </summary>
        /// <param name="key">标识</param>
        /// <param name="value">对象值</param>
        public override void Set(string key, object value)
        {
            Set(key, value, AppConfig.Cache.DefaultCacheTime);
        }
        /// <param name="cacheMinutes">缓存时间(单位分钟)</param>
        public override void Set(string key, object value, double cacheMinutes)
        {
            Set(key, value, cacheMinutes, null);
        }
        /// <param name="fileName">文件依赖路径</param>
        public override void Set(string key, object value, double cacheMinutes, string fileName)
        {
            try
            {
                lock (lockObj)
                {
                    if (theCache.ContainsKey(key))
                    {
                        theCache[key] = value;
                    }
                    else
                    {
                        theCache.Add(key, value);//2：设置value
                    }
                    double cacheTime = cacheMinutes;
                    if (cacheMinutes <= 0)
                    {
                        cacheTime = AppConfig.Cache.DefaultCacheTime;
                    }
                    DateTime cTime = DateTime.Now.AddMinutes(cacheTime);
                    int workCount = GetWorkCount(cTime);
                    if (theKeyTime.ContainsKey(key))
                    {
                        int wc = GetWorkCount(theKeyTime[key]);
                        if (wc != workCount && theTime.ContainsKey(wc))
                        {
                            if (theTime[wc].Contains(key))
                            {
                                theTime[wc].Remove(key); //移除旧值
                            }
                        }
                        theKeyTime[key] = cTime;
                    }
                    else
                    {
                        theKeyTime.Add(key, cTime);
                    }

                    if (theTime.ContainsKey(workCount))//3：设置time
                    {
                        if (!theTime[workCount].Contains(key))
                        {
                            theTime[workCount].Add(key);
                        }
                    }
                    else
                    {
                        MList<string> list = new MList<string>();
                        list.Add(key);
                        theTime.Add(workCount, list);
                    }

                    if (!string.IsNullOrEmpty(fileName))//3：设置file
                    {
                        if (fileName.IndexOf("\\\\") > -1)
                        {
                            fileName = fileName.Replace("\\\\", "\\");
                        }
                        if (!theFileName.ContainsKey(key))
                        {
                            theFileName.Add(key, fileName);
                        }
                        string folder = Path.GetDirectoryName(fileName);
                        if (!theFolderWatcher.ContainsKey(folder) && Directory.Exists(folder))
                        {
                            theFolderWatcher.Add(folder, CreateFileSystemWatcher(folder));
                        }

                        if (theFolderKeys.ContainsKey(folder))
                        {
                            if (!theFolderKeys[folder].Contains(key))
                            {
                                theFolderKeys[folder].Add(key);
                            }
                        }
                        else
                        {
                            MList<string> list = new MList<string>();
                            list.Add(key);
                            theFolderKeys.Add(folder, list);
                        }
                    }

                }
            }
            catch
            {
                errorCount++;
            }
        }

        private int GetWorkCount(DateTime cTime)
        {
            TimeSpan ts = cTime - startTime;
            return (int)ts.TotalMinutes / taskInterval;//计算出离开始有多少个间隔时间。
        }

        /// <summary>
        /// 删除一个Cache对象
        /// </summary>
        /// <param name="key">标识</param>
        public override void Remove(string key)
        {
            theCache.Remove(key);//清除Cache，其它数据在定义线程中移除
        }
        /// <summary>
        /// 移除Key和Value
        /// </summary>
        /// <param name="removeKeys"></param>
        private void RemoveList(List<string> removeKeys)
        {
            if (removeKeys != null && removeKeys.Count > 0)
            {
                lock (lockObj)
                {
                    foreach (string key in removeKeys)
                    {
                        try
                        {
                            if (theCache.ContainsKey(key))
                            {
                                theCache.Remove(key);
                            }
                            if (theKeyTime.ContainsKey(key))
                            {
                                theKeyTime.Remove(key);
                            }
                            if (theFileName.ContainsKey(key))
                            {
                                string folder = Path.GetDirectoryName(theFileName[key]);
                                MList<string> keys = theFolderKeys[folder];
                                keys.Remove(key);
                                if (keys.Count == 0)
                                {
                                    theFolderWatcher[folder].Changed -= new FileSystemEventHandler(fsy_Changed);//取消事件
                                    theFolderWatcher.Remove(folder);//文件夹下没有要监视的文件，取消事件和对象。
                                    theFolderKeys.Remove(folder);//移除对象。
                                }

                                theFileName.Remove(key);//file
                            }
                            //file
                        }
                        catch
                        {
                            errorCount++;
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
                lock (lockObj)
                {
                    TableSchema.tableCache.Clear();
                    TableSchema.columnCache.Clear();

                    theCache.Clear();
                    theTime.Clear();
                    theFileName.Clear();
                    theKeyTime.Clear();
                    for (int i = 0; i < theFolderWatcher.Count; i++)
                    {
                        theFolderWatcher[i].Changed -= new FileSystemEventHandler(fsy_Changed);
                        theFolderWatcher[i] = null;
                    }
                    theFolderWatcher.Clear();
                    theFolderKeys.Clear();
                }
            }
            catch
            {
                errorCount++;
            }
        }


        public override CacheType CacheType
        {
            get { return CacheType.LocalCache; }
        }

        #region 处理文件依赖
        private FileSystemWatcher CreateFileSystemWatcher(string folderName)
        {
            FileSystemWatcher fsy = new FileSystemWatcher(folderName, "*.*");
            fsy.EnableRaisingEvents = true;
            fsy.IncludeSubdirectories = false;
            fsy.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            fsy.Changed += new FileSystemEventHandler(fsy_Changed);
            return fsy;
        }
        private static readonly object obj2 = new object();
        void fsy_Changed(object sender, FileSystemEventArgs e)
        {
            lock (obj2)
            {
                string fileName = e.FullPath;
                string folder = Path.GetDirectoryName(fileName);
                if (theFolderKeys.ContainsKey(folder))
                {
                    List<string> keys = theFolderKeys[folder].GetList();
                    int count = keys.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (i < keys.Count)
                        {
                            if (theFileName[keys[i]] == fileName)
                            {
                                Remove(keys[i]);
                                keys.Remove(keys[i]);
                                i--;
                            }
                        }
                    }

                }
            }
        }
        #endregion
    }
}
