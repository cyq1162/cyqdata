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
        internal delegate void OnConfigChangedDelegate(string configValue);
        public event OnConfigChangedDelegate OnConfigChangedEvent;

        public HostConfigWatch(string configValue)
        {
            StartWatch(configValue);
            CompareHostListByConfig(configValue);
        }

        private void StartWatch(string configValue)
        {
            if (string.IsNullOrEmpty(configValue)) { return; }
            if (configValue.EndsWith(".txt") || configValue.EndsWith(".ini"))
            {
                string path = AppConst.RunPath + configValue;
                if (File.Exists(path))
                {
                    IOWatch.On(path, delegate (FileSystemEventArgs e)
                    {
                        if (OnConfigChangedEvent != null)
                        {
                            OnConfigChangedEvent(IOHelper.ReadAllText(path));
                        }
                    });
                }
            }
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

        /// <summary>
        /// �;�������Ƚϣ��ҳ��仯���������Ϣ
        /// </summary>
        /// <param name="configValue">�µ�����ֵ</param>
        public void CompareHostListByConfig(string configValue)
        {
            lock (this)
            {
                if (string.IsNullOrEmpty(configValue)) { return; }
                string[] hostItems = configValue.Trim().Split(',');
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
