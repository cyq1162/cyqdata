using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using CYQ.Data.Tool;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// 用于监控分布式缓存的配置列表，实际高可用，动态加载，而不需要关停运行的服务。
    /// </summary>
    internal class HostConfigWatch
    {
        //用于验证权限的委托事件。
        internal delegate void OnConfigChangedDelegate();
        public event OnConfigChangedDelegate OnConfigChangedEvent;

        CacheType cacheType;
        string configValue;
        public HostConfigWatch(CacheType cacheType, string configValue)
        {
            this.cacheType = cacheType;
            this.configValue = configValue;
            ResetHostListByConfig(true);
        }
        /// <summary>
        /// 存档配置文件对应的原始主机列表。
        /// </summary>
        public MList<string> HostList = new MList<string>();
        /// <summary>
        /// 变化后要追加的主机。
        /// </summary>
        public MList<string> HostAddList = new MList<string>();
        /// <summary>
        /// 变化后要移除的主机。
        /// </summary>
        public MList<string> HostRemoveList = new MList<string>();


        //private void StartFileSystemWatcher(string fileName)
        //{
        //    FileSystemWatcher fsy = new FileSystemWatcher(Path.GetDirectoryName(fileName), Path.GetFileName(fileName));
        //    fsy.EnableRaisingEvents = true;
        //    fsy.IncludeSubdirectories = false;
        //    fsy.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
        //    fsy.Changed += new FileSystemEventHandler(fsy_Changed);
        //}
        //
        //private void fsy_Changed(object sender, FileSystemEventArgs e)
        //{


        //}
        private static readonly object obj2 = new object();
        private void ResetHostListByConfig(bool isNeedStartWatch)
        {
            lock (obj2)
            {
                if (string.IsNullOrEmpty(configValue)) { return; }
                string[] hostItems = null;
                if (configValue.EndsWith(".txt") || configValue.EndsWith(".ini"))
                {
                    string path = AppConfig.RunPath + configValue;
                    if (File.Exists(path))
                    {
                        if (isNeedStartWatch)
                        {
                            IOWatch.On(path, delegate (FileSystemEventArgs e) {
                                ResetHostListByConfig(false);
                                if (OnConfigChangedEvent != null)
                                {
                                    OnConfigChangedEvent();
                                }
                            });
                        }

                        hostItems = IOHelper.ReadAllLines(path);
                    }
                }
                else
                {
                    hostItems = configValue.Trim().Split(',');
                }
                if (hostItems == null || hostItems.Length == 0) { return; }


                HostAddList.Clear();
                HostRemoveList.Clear();
                //开始比较主机配置版本差异
                if (HostList.Count == 0)
                {
                    foreach (string host in hostItems)
                    {
                        string item = host.Trim();
                        if (string.IsNullOrEmpty(item) || item.StartsWith("#"))
                        {
                            continue;
                        }
                        if (!HostAddList.Contains(item))
                        {
                            HostAddList.Add(item);
                            HostList.Add(item);
                        }
                    }
                }
                else
                {
                    foreach (string host in hostItems)
                    {
                        string item = host.Trim();
                        if (string.IsNullOrEmpty(item) || item.StartsWith("#"))
                        {
                            continue;
                        }
                        if (HostList.Contains(item))
                        {
                            HostList.Remove(item);
                        }
                        else if (!HostAddList.Contains(item))
                        {
                            HostAddList.Add(item);
                        }
                    }
                    //剩下的就是要移除的。
                    foreach (string host in HostList.List)
                    {
                        HostRemoveList.Add(host);
                    }
                    HostList.Clear();
                    //把项添加到HostList项。
                    foreach (string host in hostItems)
                    {
                        string item = host.Trim();
                        if (string.IsNullOrEmpty(item) || item.StartsWith("#"))
                        {
                            continue;
                        }
                        if (!HostList.Contains(item))
                        {
                            HostList.Add(item);
                        }
                    }
                }
            }
        }
    }
}
