using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
namespace CYQ.Data.Tool
{
    /// <summary>
    /// 在ASP.NET中使用多线程时，为了避开升级dll时产生多个线程互相影响，使用此类可以通过外置文件进行跳出。
    /// </summary>
    public class ThreadBreak
    {
        bool hadThreadBreak = false;
        string threadPath = string.Empty;
        /// <summary>
        /// 提示：AppConfig中需要配置ThreadBreakPath项的路径
        /// </summary>
        /// <param name="threadName"></param>
        /// <param name="threadID"></param>
        public ThreadBreak(string threadName, object threadID)
        {
            if (ClearThreadBreak(threadName))
            {
                //创建自身线程
                threadPath = AppConfig.WebRootPath + AppConfig.ThreadBreakPath + threadName + "_" + threadID + ".threadbreak";
                try
                {
                    File.Create(threadPath).Close();
                }
                catch (Exception err)
                {
                    Log.WriteLogToTxt(err);
                }
                hadThreadBreak = true;
            }
        }
        /// <summary>
        /// 是否需要退出。
        /// </summary>
        /// <returns></returns>
        public bool NeedToBreak()
        {
            if (hadThreadBreak)
            {
                return !File.Exists(threadPath);
            }
            return false;

        }
        /// <summary>
        /// 清除线程threadbreak文件。
        /// </summary>
        /// <param name="threadName">线程名称</param>
        /// <returns></returns>
        private static bool ClearThreadBreak(string threadName)
        {
            try
            {
                string threadPath = AppConfig.ThreadBreakPath;
                if (!string.IsNullOrEmpty(threadPath))
                {
                    if (threadPath.IndexOf(":\\") == -1)
                    {
                        threadPath = AppConfig.WebRootPath + threadPath;
                        if (!Directory.Exists(threadPath))
                        {
                            Directory.CreateDirectory(threadPath);
                        }
                    }
                    //清除其它线程
                    DirectoryInfo info = new DirectoryInfo(threadPath);
                    if (info.Exists)
                    {
                        FileInfo[] delInfo = info.GetFiles(threadName + "*.threadbreak");
                        if (delInfo != null && delInfo.Length > 0)
                        {
                            foreach (FileInfo del in delInfo)
                            {
                                try
                                {
                                    if (del.Exists)
                                    {
                                        del.Delete();
                                    }
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                            delInfo = null;
                        }
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        /*
        /// <summary>
        /// 清除所有的表架构。
        /// </summary>
        private static void ClearSchema()
        {
            try
            {
                string path = AppConfig.DB.SchemaMapPath;
                if (!string.IsNullOrEmpty(path))
                {
                    path = AppConfig.WebRootPath + path;
                    if (Directory.Exists(path))
                    {
                        string[] files = Directory.GetFiles(path, "*.ts");
                        if (files != null && files.Length > 0)
                        {
                            foreach (string file in files)
                            {
                                IOHelper.Delete(file);
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Log.WriteLogToTxt(err);
            }
        }
        */
        private static List<ParameterizedThreadStart> globalThread = new List<ParameterizedThreadStart>();
        private static readonly object lockThreadObj = new object();
        /// <summary>
        /// 添加全局线程[通常该线程是个死循环，定时处理事情]
        /// </summary>
        public static void AddGlobalThread(ParameterizedThreadStart start)
        {
            AddGlobalThread(start, null);
        }
        public static void AddGlobalThread(ParameterizedThreadStart start, object para)
        {
            if (globalThread.Count == 0)//第一次加载，清除所有可能存在的线程Break。
            {
                //ClearSchema();// 表结构外置（解决第一次加载的问题，后续表结构都缓存在内存中）！因此不能清空~
                ClearThreadBreak(string.Empty);
            }
            if (!globalThread.Contains(start))
            {
                lock (lockThreadObj)
                {
                    try
                    {
                        if (!globalThread.Contains(start))
                        {
                            globalThread.Add(start);
                            Thread thread = new Thread(start);
                            thread.IsBackground = true;
                            thread.Start(para ?? thread.ManagedThreadId);
                        }
                    }
                    catch (Exception err)
                    {
                        Log.WriteLogToTxt(err);
                    }
                }
            }
        }
    }
}
