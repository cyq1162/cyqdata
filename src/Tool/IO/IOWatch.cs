using System;
using System.IO;
using System.Threading;

namespace CYQ.Data.Tool
{
    internal class IOWatch
    {
        /// <summary>
        /// ����е��б�
        /// </summary>
        //private static MList<string> watchPathList = new MList<string>();
        private static MDictionary<string, IOWatch> watchs = new MDictionary<string, IOWatch>();
        private IOWatch()
        {

        }
        public static void On(string fileName, WatchDelegate watch)
        {
            if (!watchs.ContainsKey(fileName))
            {
                IOWatch fileWatch = new IOWatch();
                watchs.Add(fileName, fileWatch);
                fileWatch.WatchOn(fileName, watch);
            }
        }

        public delegate void WatchDelegate(FileSystemEventArgs e);
        private WatchDelegate watch;
        private FileSystemWatcher fsy;
        public void WatchOn(string fileName, WatchDelegate watch)
        {
            this.watch = watch;
            this.fsy = new FileSystemWatcher(Path.GetDirectoryName(fileName), Path.GetFileName(fileName));
            fsy.EnableRaisingEvents = true;
            fsy.IncludeSubdirectories = false;
            fsy.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
            fsy.Changed += new FileSystemEventHandler(fsy_Changed);
        }


        private DateTime lastTime = DateTime.MinValue;
        private void fsy_Changed(object sender, FileSystemEventArgs e)
        {
            if (lastTime.AddSeconds(3) > DateTime.Now)
            {
                //�������ظ��¼�
                return;
            }
            //Log.WriteLogToTxt("IOWatch.On Change :" + e.FullPath, LogType.Debug);
            lastTime = DateTime.Now;
            lock (e.FullPath)
            {
                if (watch != null)
                {
                    Thread.Sleep(10);//��ʱ���ȴ��ļ������꣬�����п��ܶ������ļ���
                    watch(e);
                }
            }
        }
    }
}
