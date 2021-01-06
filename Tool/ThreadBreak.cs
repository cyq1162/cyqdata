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
                threadPath = AppConfig.RunPath + AppConfig.ThreadBreakPath + threadName + "_" + threadID + ".threadbreak";
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
                string threadPath = AppConfig.ThreadBreakPath;
                if (!string.IsNullOrEmpty(threadPath))
                {
                    if (threadPath.IndexOf(":\\") == -1)
                    {
                        threadPath = AppConfig.RunPath + threadPath;
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
        private static List<string> globalThread = new List<string>();
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
            if (globalThread.Count == 0)//��һ�μ��أ�������п��ܴ��ڵ��߳�Break��
            {
                //ClearSchema();// ��ṹ���ã������һ�μ��ص����⣬������ṹ���������ڴ��У�����˲������~
                ClearThreadBreak(string.Empty);
            }
            string key = Convert.ToString(start.Target) + start.Method.ToString();
            if (!globalThread.Contains(key))
            {
                lock (lockThreadObj)
                {
                    try
                    {
                        if (!globalThread.Contains(key))
                        {
                            globalThread.Add(key);
                            Thread thread = new Thread(start);
                            thread.Name = "GlobalThread";
                            thread.IsBackground = true;
                            thread.Start(para ?? thread.ManagedThreadId);
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
