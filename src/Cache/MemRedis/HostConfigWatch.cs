using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using CYQ.Data.Tool;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// ���ڼ�طֲ�ʽ����������б�ʵ�ʸ߿��ã���̬���أ�������Ҫ��ͣ���еķ���
    /// </summary>
    internal class HostConfigWatch
    {
        //������֤Ȩ�޵�ί���¼���
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
        /// �浵�����ļ���Ӧ��ԭʼ�����б�
        /// </summary>
        public MList<string> HostList = new MList<string>();
        /// <summary>
        /// �仯��Ҫ׷�ӵ�������
        /// </summary>
        public MList<string> HostAddList = new MList<string>();
        /// <summary>
        /// �仯��Ҫ�Ƴ���������
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
                //��ʼ�Ƚ��������ð汾����
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
                    //ʣ�µľ���Ҫ�Ƴ��ġ�
                    foreach (string host in HostList.List)
                    {
                        HostRemoveList.Add(host);
                    }
                    HostList.Clear();
                    //������ӵ�HostList�
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
