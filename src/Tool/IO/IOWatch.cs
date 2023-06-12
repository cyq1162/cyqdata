using System.IO;
namespace CYQ.Data.Tool
{
    internal class IOWatch
    {
        /// <summary>
        /// 监控中的列表。
        /// </summary>
        private static MList<string> watchPathList = new MList<string>();
        private IOWatch()
        {

        }
        public static void On(string fileName, WatchDelegate watch)
        {
            if (!watchPathList.Contains(fileName))
            {
                watchPathList.Add(fileName);
                IOWatch fileWatch = new IOWatch();
                fileWatch.WatchOn(fileName, watch);
            }
        }
        public delegate void WatchDelegate(FileSystemEventArgs e);
        private WatchDelegate watch;
        public void WatchOn(string fileName, WatchDelegate watch)
        {
            this.watch = watch;
            FileSystemWatcher fsy = new FileSystemWatcher(Path.GetDirectoryName(fileName), Path.GetFileName(fileName));
            fsy.EnableRaisingEvents = true;
            fsy.IncludeSubdirectories = false;
            fsy.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
            fsy.Changed += new FileSystemEventHandler(fsy_Changed);
        }

        private void fsy_Changed(object sender, FileSystemEventArgs e)
        {
            lock (e.FullPath)
            {
                if (watch != null)
                {
                    watch(e);
                }
            }
        }
    }
}
