using CYQ.Data.Cache;
using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;



namespace CYQ.Data.Lock
{
    internal class FileLock : DistributedLock
    {
        private static readonly FileLock _instance = new FileLock();
        string folder = string.Empty;
        private FileLock()
        {
            folder = Path.GetTempPath().TrimEnd('/', '\\') + "/CYQ.Data.FileLock/";
            if (!System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
            }
            else
            {

            }
        }
        public static FileLock Instance
        {
            get
            {
                return _instance;
            }
        }
        public override LockType LockType
        {
            get
            {
                return LockType.File;
            }
        }

        public override bool Lock(string key, int millisecondsTimeout)
        {
            int sleep = 5;
            int count = millisecondsTimeout;
            while (true)
            {
                if (IsLockOK(key))
                {
                    AddToWork(key, null);
                    return true;
                }
                Thread.Sleep(sleep);
                count -= sleep;
                if (sleep < 1000)
                {
                    sleep += 5;
                }
                if (count <= 0)
                {
                    //超时前进行一次锁超时检测
                    if (RemoveTimeoutLock(key)) { return true; }
                    return false;
                }
            }
        }

        public override void UnLock(string key)
        {
            try
            {
                RemoveFromWork(key);
                string path = folder + key + ".lock";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
            catch (System.Exception err)
            {
                Log.Write(err, LogType.Error);

            }
        }
        private static readonly object lockObj = new object();
        private bool IsLockOK(string key)
        {
            string path = folder + key + ".lock";
            if (System.IO.File.Exists(path))
            {
                return false;
            }
            try
            {
                lock (lockObj)
                {
                    if (System.IO.File.Exists(path))
                    {
                        return false;
                    }
                    System.IO.File.Create(path).Close();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        #region 内部定时日志工作

        MDictionary<string, string> keysDic = new MDictionary<string, string>();
        bool threadIsWorking = false;
        private void AddToWork(string key, string value)
        {
            keysDic.Remove(key);
            keysDic.Add(key, value);
            if (!threadIsWorking)
            {
                lock (this)
                {
                    if (!threadIsWorking)
                    {
                        threadIsWorking = true;
                        ThreadBreak.AddGlobalThread(new ParameterizedThreadStart(DoLockWork));
                    }
                }
            }
        }
        private void RemoveFromWork(string key)
        {
            keysDic.Remove(key);
        }
        private void DoLockWork(object p)
        {
            try
            {
                while (true)
                {
                    if (keysDic.Count > 0)
                    {
                        List<string> list = keysDic.GetKeys();
                        foreach (string key in list)
                        {
                            //给 key 设置延时时间
                            if (keysDic.ContainsKey(key))
                            {
                                string path = folder + key + ".lock";
                                System.IO.File.WriteAllText(path, "1"); //延时锁：6秒
                            }
                        }
                        list.Clear();
                        Thread.Sleep(5000);//循环。
                    }
                    else
                    {
                        threadIsWorking = false;
                        break;
                    }
                }
            }
            catch (Exception err)
            {
                Log.Write(err, LogType.Error);
                throw;
            }
        }

        /// <summary>
        /// 清除过期锁
        /// </summary>
        private bool RemoveTimeoutLock(string key)
        {
            if (keysDic.ContainsKey(key)) { return false; }
            var isOK = false;
            try
            {
                isOK = LocalLock.Instance.Lock(key, 1);
                if (isOK)
                {
                    string path = folder + key + ".lock";
                    FileInfo fi = new FileInfo(path);
                    if (fi.Exists && fi.LastWriteTime.AddSeconds(20) < DateTime.Now)
                    {
                        UnLock(key);
                        return true;
                    }
                }
            }
            finally
            {
                if (isOK)
                {
                    LocalLock.Instance.UnLock(key);
                }
            }
            return false;

        }
        #endregion
    }
}
