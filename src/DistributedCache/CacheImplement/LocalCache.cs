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
    /// ����������
    /// Ϊ����.NET Core ȥ��Web.Caching����д
    /// </summary>
    internal partial class LocalCache : DistributedCache
    {
        /// <summary>
        /// ���� key=���������
        /// </summary>
        private MDictionary<string, object> theCache = new MDictionary<string, object>(2048, StringComparer.OrdinalIgnoreCase);//key,cache
        /// <summary>
        /// ���� key=> ��ʱʱ��
        /// </summary>
        private MDictionary<string, DateTime> theKeyTime = new MDictionary<string, DateTime>(2048, StringComparer.OrdinalIgnoreCase);//key,time
        /// <summary>
        /// ���� key=> �ļ�������·����
        /// </summary>
        private MDictionary<string, string> theFileName = new MDictionary<string, string>();//key,filename

        /// <summary>
        /// ���� ʱ��� => keys , ���������ʱ��ֲ�%������
        /// </summary>
        private SortedDictionary<int, MList<string>> theTime = new SortedDictionary<int, MList<string>>();//worktime,keylist
        /// <summary>
        /// ���� folderPath =���ļ��м��
        /// </summary>
        private MDictionary<string, FileSystemWatcher> theFolderWatcher = new MDictionary<string, FileSystemWatcher>();//folderPath,watch
        /// <summary>
        /// ���� folderPath =�� keys
        /// </summary>
        private MDictionary<string, MList<string>> theFolderKeys = new MDictionary<string, MList<string>>();//folderPath,keylist

        private static object lockAdd = new object();
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

        /// <summary>
        /// ���������Թ��캯����ʹ�á�
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
        int taskCount = 0, taskInterval = 5;//5������һ�λ��档
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
                    isThreadAbort = true;
                }
                catch (OutOfMemoryException)
                {
                    errorCount++;
                }
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
        /// ���һ��Cache����
        /// </summary>
        /// <param name="key">��ʶ</param>
        /// <param name="value">����ֵ</param>
        public override bool Set(string key, object value)
        {
            return Set(key, value, 0);
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
            return (int)ts.TotalMinutes / taskInterval;//������뿪ʼ�ж��ٸ����ʱ�䡣
        }

        /// <summary>
        /// ɾ��һ��Cache����
        /// </summary>
        /// <param name="key">��ʶ</param>
        public override bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key)) { return false; }
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
        /// ����ⲿ��������
        /// </summary>
        private void ClearDB()
        {
            DBSchema.Clear();//������ݿ⻺��
            TableSchema.Clear();//��ձ�ṹ����
            NoSqlAction.Clear();//����ı����ݿ���ػ���
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
                    bool isVSBug = fileName.EndsWith("~");//VS Debug ģʽ�£�VS��Bug��ֻ�ܰ������ļ�����ջ��档

                    //������ͳһ·����ʽ�����ļ���ջ���
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
