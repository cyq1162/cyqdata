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
    /// ����������
    /// Ϊ����.NET Core ȥ��Web.Caching����д
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

        DateTime allowCacheTableTime = DateTime.Now;

        private MDataTable cacheInfoTable;
        /// <summary>
        /// ��ȡ������Ϣ�����б�
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
            Thread thread = new Thread(new ThreadStart(ThreadTask));//�߳����񿪿��������⡾ThreadBreak.AddGlobalThread��NETCore�汾���ֵ��û��桿��ѭ����
            thread.Start();
        }

        void ThreadTask()
        {
            try
            {
                ThreadBreak.AddGlobalThread(new ParameterizedThreadStart(ClearState));
                ThreadBreak.AddGlobalThread(new ParameterizedThreadStart(ConnObject.CheckConnIsOk));//�������ӵļ����ơ�
                //if (AppConfig.Cache.IsAutoCache) // ���Ʊ��ΪAopҲ�ɿ��ƣ����������������������
                //{
                ThreadBreak.AddGlobalThread(new ParameterizedThreadStart(AutoCache.ClearCache));
                //}
            }
            catch (Exception err)
            {
                errorCount++;
                Log.Write(err, LogType.Cache);
            }
        }
        int taskCount = 0, taskInterval = 5;//5������һ�λ��档
        private static DateTime errTime = DateTime.MinValue;
        private void ClearState(object threadID)
        {
            System.Diagnostics.Debug.WriteLine("LocalCache.ClearState on Thread :" + threadID);
            startTime = DateTime.Now;
            while (true)
            {
                try
                {
                    workTime = startTime.AddMinutes((taskCount + 1) * taskInterval);
                    TimeSpan ts = workTime - DateTime.Now;
                    if (ts.TotalSeconds > 0)
                    {
                        Thread.Sleep(ts);//taskInterval * 60 * 1000);//10��������ʱ��
                    }
                    #region �µĻ���
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
                    if (errTime == DateTime.MinValue || errTime.AddMinutes(10) < DateTime.Now) // 10���Ӽ�¼һ��
                    {
                        errTime = DateTime.Now;
                        Log.Write("LocalCache.ClearState : " + Log.GetExceptionMessage(err), LogType.Cache);
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
                    CheckIsSafe();
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
        private int errorCount = 0;//���沶�쳣����
        /// <summary>
        /// �ڴ湤����Ϣ
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
        /// ��ͻ�������
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
        /// ���һ��Cache����
        /// </summary>
        /// <param name="key">��ʶ</param>
        public override object Get(string key)
        {
            if (Contains(key))
            {
                return theCache[key];// && theCache.ContainsKey(key) �ڲ������жϺ�Lock
            }
            return null;
        }
        /// <summary>
        /// �Ƿ���ڻ���
        /// </summary>
        /// <param name="key">��ʶ</param>
        /// <returns></returns>
        public override bool Contains(string key)
        {
            if (string.IsNullOrEmpty(key)) { return false; }
            return theCache.ContainsKey(key) && theKeyTime.ContainsKey(key) && theKeyTime[key] > DateTime.Now;
        }
        /// <summary>
        /// ����һ��Cache����
        /// </summary>
        /// <param name="key">��ʶ</param>
        /// <param name="value">����ֵ</param>
        public override bool Set(string key, object value)
        {
            return Set(key, value, AppConfig.Cache.DefaultCacheTime);
        }
        /// <param name="cacheMinutes">����ʱ��(��λ����)</param>
        public override bool Set(string key, object value, double cacheMinutes)
        {
            return Set(key, value, cacheMinutes, null);
        }
        /// <param name="fileName">�ļ�����·��</param>
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
                        theCache.Add(key, value);//2������value
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
                                theTime[wc].Remove(key); //�Ƴ���ֵ
                            }
                        }
                        theKeyTime[key] = cTime;
                    }
                    else
                    {
                        theKeyTime.Add(key, cTime);
                    }

                    if (theTime.ContainsKey(workCount))//3������time
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

                    if (!string.IsNullOrEmpty(fileName))//3������file
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

        private int GetWorkCount(DateTime cTime)
        {
            TimeSpan ts = cTime - startTime;
            return (int)ts.TotalMinutes / taskInterval;//������뿪ʼ�ж��ٸ����ʱ�䡣
        }

        /// <summary>
        /// ɾ��һ��Cache����
        /// </summary>
        /// <param name="key">��ʶ</param>
        public override bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key)) { return false; }
            DBSchema.Remove(key);
            TableSchema.Remove(key);
            if (key.StartsWith("CCache_"))
            {
                if (!string.IsNullOrEmpty(AppConfig.DB.SchemaMapPath))
                {

                    IOHelper.Delete(AppConfig.RunPath + AppConfig.DB.SchemaMapPath + key + ".ts");
                    //˳���Ƴ����ݿ������ͼ���洢���� �����б����档
                    string[] files = Directory.GetFiles(AppConfig.RunPath + AppConfig.DB.SchemaMapPath, "*.json");
                    if (files != null && files.Length > 0)
                    {
                        foreach (string file in files)
                        {
                            IOHelper.Delete(file);
                        }
                    }

                }
            }
            return theCache.Remove(key);//���Cache�����������ڶ����߳����Ƴ�
        }
        /// <summary>
        /// �Ƴ�Key��Value
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
                                    theFolderWatcher[folder].Changed -= new FileSystemEventHandler(fsy_Changed);//ȡ���¼�
                                    theFolderWatcher.Remove(folder);//�ļ�����û��Ҫ���ӵ��ļ���ȡ���¼��Ͷ���
                                    theFolderKeys.Remove(folder);//�Ƴ�����
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
        /// ������л���
        /// </summary>
        public override void Clear()
        {
            try
            {
                lock (lockObj)
                {
                    DBSchema.Clear();//������ݿ⻺��
                    TableSchema.Clear();//��ձ��ṹ����
                    NoSqlAction.Clear();//����ı����ݿ���ػ���

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
                    if (!string.IsNullOrEmpty(AppConfig.DB.SchemaMapPath))
                    {
                        if (Directory.Exists(AppConfig.RunPath + AppConfig.DB.SchemaMapPath))
                        {
                            Directory.Delete(AppConfig.RunPath + AppConfig.DB.SchemaMapPath, true);
                        }
                    }
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

        #region �����ļ�����
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
                    MList<string> keys = theFolderKeys[folder];//.GetList();
                    int count = keys.Count;
                    //ͳһ·����ʽ
                    fileName = fileName.Replace("/", "\\");
                    for (int i = 0; i < count; i++)
                    {
                        if (i < keys.Count)
                        {
                            string path = theFileName[keys[i]].Replace("/", "\\");
                            if (string.Compare(path, fileName, StringComparison.OrdinalIgnoreCase) == 0)
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