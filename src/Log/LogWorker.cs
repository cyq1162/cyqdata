using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CYQ.Data
{
    /// <summary>
    /// 内部定时日志工作
    /// </summary>
    internal class LogWorker
    {
        /// <summary>
        /// 待处理的工作队列
        /// </summary>
        static Queue<SysLogs> _LogQueue = new Queue<SysLogs>(1024);
        /// <summary>
        /// 存档Hash，5分钟内存在相同的错误，则直接忽略
        /// </summary>
        static MDictionary<long, DateTime> hashObj = new MDictionary<long, DateTime>();
        static bool threadIsWorking = false;
        public static void Add(SysLogs log)
        {
            long hash = log.Message.GetHashCode() + log.LogType.GetHashCode();
            if (hashObj.ContainsKey(hash))
            {
                if (hashObj[hash].AddMinutes(3) < DateTime.Now)
                {
                    hashObj.Remove(hash);//超过1分钟的，不再存档。
                }
                log = null;//直接丢掉。
                return;
            }
            hashObj.Add(hash, DateTime.Now);
            _LogQueue.Enqueue(log);
            if (!threadIsWorking)
            {
                threadIsWorking = true;
                ThreadPool.QueueUserWorkItem(new WaitCallback(DoWork), null);
            }

        }
        private static void DoWork(object p)
        {
            //检测文件夹
            string folder = AppConfig.WebRootPath;
            string logPath = AppConfig.Log.LogPath;
            if (logPath.StartsWith("~/"))
            {
                logPath = logPath.Substring(2);
            }
            if (!AppConfig.IsWeb && logPath.Contains(":"))//winform 自定义绝对路径
            {
                string c = logPath.Contains("\\") ? "\\" : "/";
                if (!logPath.EndsWith(c))
                {
                    logPath = logPath + c;
                }
                folder = logPath;
            }
            else
            {
                folder = folder + logPath;
            }
            if (!System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
            }
            int empty = 0;
            while (true)
            {
                empty++;
                bool dbErr = false;
                while (_LogQueue.Count > 0)
                {
                    empty = 0;

                    SysLogs sys = _LogQueue.Dequeue();
                    if (sys == null)
                    {
                        continue;
                    }
                    string todayKey = DateTime.Today.ToString("yyyyMMdd") + ".txt";
                    if (!string.IsNullOrEmpty(sys.LogType))
                    {
                        todayKey = sys.LogType.TrimEnd('_') + '_' + todayKey;
                    }
                    string filePath = folder + todayKey;
                    //检测数据库
                    //检测文件路径：
                    string body = sys.GetFormatterText();
                    IOHelper.Save(filePath, body, true, false);
                    try
                    {
                        if (!dbErr)
                        {
                            if (!sys.IsWriteToTxt)
                            {
                                sys.Insert(InsertOp.None);//直接写数据库，日志文件依旧要写。
                            }
                            sys.Dispose();
                        }
                    }
                    catch
                    {
                        //数据库异常，不处理。
                        dbErr = true;
                    }
                }
                Thread.Sleep(1000);
                if (empty > 100)
                {
                    //超过10分钟没日志产生
                    hashObj.Clear();
                    threadIsWorking = false;
                    break;//结束线程。
                }
            }
        }
    }
}
