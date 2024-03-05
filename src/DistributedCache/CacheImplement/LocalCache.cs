using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CYQ.Data.Tool;
using CYQ.Data.Table;
using CYQ.Data.SQL;
using System.IO;
using CYQ.Data.Json;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// 单机缓存类
    /// 为兼容.NET Core 去掉Web.Caching，重写
    /// </summary>
    internal partial class LocalCache : DistributedCache
    {
        /// <summary>
        /// 保存 key=》缓存对象
        /// </summary>
        private MDictionary<string, object> theCache = new MDictionary<string, object>(2048, StringComparer.OrdinalIgnoreCase);//key,cache
        /// <summary>
        /// 保存 key=> 超时时间
        /// </summary>
        private MDictionary<string, DateTime> theKeyTime = new MDictionary<string, DateTime>(2048, StringComparer.OrdinalIgnoreCase);//key,time
        /// <summary>
        /// 保存 key=> 文件依赖【路径】
        /// </summary>
        private MDictionary<string, string> theFileName = new MDictionary<string, string>();//key,filename

        /// <summary>
        /// 保存 时间点 => keys , 将任务过期时间分布%任务间隔
        /// </summary>
        private SortedDictionary<int, MList<string>> theTime = new SortedDictionary<int, MList<string>>();//worktime,keylist
        /// <summary>
        /// 保存 folderPath =》文件夹监控
        /// </summary>
        private MDictionary<string, FileSystemWatcher> theFolderWatcher = new MDictionary<string, FileSystemWatcher>();//folderPath,watch
        /// <summary>
        /// 保存 folderPath =》 keys
        /// </summary>
        private MDictionary<string, MList<string>> theFolderKeys = new MDictionary<string, MList<string>>();//folderPath,keylist

        private static object lockAdd = new object();
        private static object lockObj = new object();
        private DateTime workTime, startTime;

        DateTime allowCacheTableTime = DateTime.Now;

        private MDataTable cacheInfoTable;
        /// <summary>
        /// 获取缓存信息对象列表
        /// </summary>
        public override MDataTable CacheInfo
        {
            get
            {
                if (cacheInfoTable == null || cacheInfoTable.Columns.Count == 0 || DateTime.Now > allowCacheTableTime)
                {
                    #region Create Table
                    MDataTable cacheTable = new MDataTable("LocalCache");
                    cacheTable.Columns.Add("Key,Value");
                    cacheTable.NewRow(true).Set(0, "CacheCount").Set(1, theCache.Count);
                    cacheTable.NewRow(true).Set(0, "TimeCount").Set(1, theTime.Count);
                    cacheTable.NewRow(true).Set(0, "FileCount").Set(1, theFileName.Count);
                    cacheTable.NewRow(true).Set(0, "KeyTimeCount").Set(1, theKeyTime.Count);
                    cacheTable.NewRow(true).Set(0, "FolderWatcherCount").Set(1, theFolderWatcher.Count);
                    cacheTable.NewRow(true).Set(0, "TaskStartTime").Set(1, startTime);
                    cacheTable.NewRow(true).Set(0, "TaskWorkCount").Set(1, taskCount);
                    cacheTable.NewRow(true).Set(0, "ErrorCount").Set(1, errorCount);

                    #endregion

                    allowCacheTableTime = DateTime.Now.AddSeconds(5);
                    cacheInfoTable = cacheTable;
                }
                return cacheInfoTable;
            }
        }
        internal LocalCache()
        {
            Thread thread = new Thread(new ThreadStart(ThreadTask));//线程任务开开启，避免【ThreadBreak.AddGlobalThread里NETCore版本下又调用缓存】死循环。
            thread.Start();
        }

        /// <summary>
        /// 单例，可以构造函数里使用。
        /// </summary>
        void ThreadTask()
        {
            try
            {
                ThreadBreak.AddGlobalThread(new ParameterizedThreadStart(ClearState));
            }
            catch (Exception err)
            {
                errorCount++;
                Log.Write(err, LogType.Cache);
            }
        }
        int taskCount = 0, taskInterval = 5;//5分钟清一次缓存。
        private static DateTime errTime = DateTime.MinValue;
        private void ClearState(object threadID)
        {
            System.Diagnostics.Debug.WriteLine("LocalCache.ClearState on Thread :" + threadID);
            startTime = DateTime.Now;
            bool isThreadAbort = false;
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
                    isThreadAbort = true;
                }
                catch (OutOfMemoryException)
                {
                    errorCount++;
                }
                catch (Exception err)
                {
                    errorCount++;
                    if (errTime == DateTime.MinValue || errTime.AddMinutes(10) < DateTime.Now) // 10分钟记录一次
                    {
                        errTime = DateTime.Now;
                        Log.Write("LocalCache.ClearState : " + Log.GetExceptionMessage(err), LogType.Cache);
                    }
                }
                finally
                {
                    if (!isThreadAbort)
                    {
                        taskCount++;
                        try
                        {
                            if (taskCount % 10 == 9)
                            {
                                if (theCache.Count > 100000)// theKey.Count > 100000)
                                {
                                    NoSqlAction.ResetStaticVar();
                                    GC.Collect();
                                }
                            }
                            CheckIsSafe();
                        }
                        catch
                        {
                            errorCount++;
                        }
                    }
                }

            }
        }
        private bool isFirstCheck = false;
        private void CheckIsSafe()
        {
            if (!isFirstCheck)
            {
                isFirstCheck = true;
                if (!EncryptHelper.HashKeyIsValid())
                {
                    AppConfig.SetApp("Conn" + AppConst.Result, "false");
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
            if (string.IsNullOrEmpty(key)) { return false; }
            return theCache.ContainsKey(key) && theKeyTime.ContainsKey(key) && theKeyTime[key] > DateTime.Now;
        }
        /// <summary>
        /// 添加一个Cache对象
        /// </summary>
        /// <param name="key">标识</param>
        /// <param name="value">对象值</param>
        public override bool Set(string key, object value)
        {
            return Set(key, value, 0);
        }
        /// <param name="cacheMinutes">缓存时间(单位分钟)</param>
        public override bool Set(string key, object value, double cacheMinutes)
        {
            return Set(key, value, cacheMinutes, null);
        }
        /// <param name="fileName">文件依赖路径</param>
        public override bool Set(string key, object value, double cacheMinutes, string fileName)
        {

            try
            {
                if (string.IsNullOrEmpty(key)) { return false; }
                if (value == null) { return Remove(key); }

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
                        cacheTime = AppConfig.Cache.DefaultMinutes;
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
                return true;
            }
            catch (Exception err)
            {
                Log.Write(err, LogType.Cache);
                errorCount++;
                return false;
            }
        }

        public override bool Add(string key, object value)
        {
            return Add(key, value, 0, null);
        }
        public override bool Add(string key, object value, double cacheMinutes)
        {
            return Add(key, value, cacheMinutes, null);
        }
        public override bool Add(string key, object value, double cacheMinutes, string fileName)
        {
            if (Contains(key))
            {
                return false;
            }
            lock (lockAdd)
            {
                if (Contains(key))
                {
                    return false;
                }
                return Set(key, value, cacheMinutes, fileName);
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
        public override bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key)) { return false; }
            return theCache.Remove(key);//清除Cache，其它数据在定义线程中移除
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
        public override bool Clear()
        {
            try
            {
                lock (lockObj)
                {
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
                ClearDB();
                return true;
            }
            catch
            {
                errorCount++;
                return false;
            }
        }

        /// <summary>
        /// 清空外部缓存数据
        /// </summary>
        private void ClearDB()
        {
            DBSchema.Clear();//清空数据库缓存
            TableSchema.Clear();//清空表结构缓存
            NoSqlAction.Clear();//清空文本数据库相关缓存
            if (!string.IsNullOrEmpty(AppConfig.DB.SchemaMapPath))
            {
                string path = AppConst.WebRootPath + AppConfig.DB.SchemaMapPath.Trim('/', '\\');
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
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

        void fsy_Changed(object sender, FileSystemEventArgs e)
        {
            lock (e.FullPath)
            {
                string fileName = e.FullPath;
                string folder = Path.GetDirectoryName(fileName);
                if (theFolderKeys.ContainsKey(folder))
                {
                    MList<string> keys = theFolderKeys[folder];
                    int count = keys.Count;
                    bool isVSBug = fileName.EndsWith("~");//VS Debug 模式下，VS的Bug，只能按整个文件夹清空缓存。

                    //正常，统一路径方式，按文件清空缓存
                    fileName = fileName.Replace("/", "\\").Replace("\\\\", "\\");
                    for (int i = 0; i < count; i++)
                    {
                        if (i < keys.Count)
                        {
                            string path = theFileName[keys[i]].Replace("/", "\\").Replace("\\\\", "\\");
                            if (isVSBug || string.Compare(path, fileName, StringComparison.OrdinalIgnoreCase) == 0)
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
