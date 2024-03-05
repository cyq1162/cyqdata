using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
namespace CYQ.Data.Tool
{
    /// <summary>
    /// ��ASP.NET��ʹ�ö��߳�ʱ��Ϊ�˱ܿ�����dllʱ��������̻߳���Ӱ�죬ʹ�ô������ͨ�������ļ�����������
    /// </summary>
    public class ThreadBreak
    {
        /// <summary>
        /// Ӧ�ó����˳�ʱ���ɵ��ô˷����������˳�ȫ���̡߳�whileѭ������
        /// </summary>
        public static void ClearGlobalThread()
        {
            if (globalThread.Count > 0)
            {
                foreach (Thread thread in globalThread)
                {
                    thread.Abort();
                }
            }
        }
        bool hadThreadBreak = false;
        string threadPath = string.Empty;
        /// <summary>
        /// ��ʾ��AppConfig����Ҫ����ThreadBreakPath���·��
        /// </summary>
        /// <param name="threadName"></param>
        /// <param name="threadID"></param>
        public ThreadBreak(string threadName, object threadID)
        {
            if (ClearThreadBreak(threadName))
            {
                //���������߳�
                threadPath = AppConst.RunPath + AppConfig.Tool.ThreadBreakPath + threadName + "_" + threadID + ".threadbreak";
                try
                {
                    File.Create(threadPath).Close();
                }
                catch (Exception err)
                {
                    Log.Write(err, LogType.Error);
                }
                hadThreadBreak = true;
            }
        }
        /// <summary>
        /// �Ƿ���Ҫ�˳���
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
        /// ����߳�threadbreak�ļ���
        /// </summary>
        /// <param name="threadName">�߳�����</param>
        /// <returns></returns>
        private static bool ClearThreadBreak(string threadName)
        {
            try
            {
                string threadPath = AppConfig.Tool.ThreadBreakPath;
                if (!string.IsNullOrEmpty(threadPath))
                {
                    if (threadPath.IndexOf(":\\") == -1)
                    {
                        threadPath = AppConst.RunPath + threadPath;
                        if (!Directory.Exists(threadPath))
                        {
                            Directory.CreateDirectory(threadPath);
                        }
                    }
                    //��������߳�
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
        /// ������еı�ܹ���
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
                Log.Write(err, LogType.Error);
            }
        }
        */
        public static List<Thread> globalThread = new List<Thread>();
        private static List<string> globalThreadKey = new List<string>();
        private static readonly object lockThreadObj = new object();
        /// <summary>
        /// ���ȫ���߳�[ͨ�����߳��Ǹ���ѭ������ʱ��������]
        /// </summary>
        public static void AddGlobalThread(ParameterizedThreadStart start)
        {
            AddGlobalThread(start, null);
        }
        public static void AddGlobalThread(ParameterizedThreadStart start, object para)
        {
            if (globalThreadKey.Count == 0)//��һ�μ��أ�������п��ܴ��ڵ��߳�Break��
            {
                //ClearSchema();// ��ṹ���ã������һ�μ��ص����⣬������ṹ���������ڴ��У�����˲������~
                ClearThreadBreak(string.Empty);
            }
            string key = Convert.ToString(start.Target) + start.Method.ToString() + Convert.ToString(para);
            if (!globalThreadKey.Contains(key))
            {
                lock (lockThreadObj)
                {
                    try
                    {
                        if (!globalThreadKey.Contains(key))
                        {
                            globalThreadKey.Add(key);
                            Thread thread = new Thread(start);
                            thread.Name = "GlobalThread";
                            thread.IsBackground = true;
                            thread.Start(para ?? thread.ManagedThreadId);
                            globalThread.Add(thread);
                        }
                    }
                    catch (Exception err)
                    {
                        Log.Write(err, LogType.Error);
                    }
                }
            }
        }
    }
}
