using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using CYQ.Data.Tool;

namespace CYQ.Data
{
    /// <summary>
    /// 链接配置管理
    /// </summary>
    internal class ConnConfigWatch
    {
        /// <summary>
        /// 监控列表。
        /// </summary>
        private static MDictionary<string, string> watchList = new MDictionary<string, string>();
        private static readonly object o = new object();
        /// <summary>
        /// 开启加载一个配置。
        /// </summary>
        /// <param name="connName">Conn的名称</param>
        /// <param name="connPath">Conn指向的路径</param>
        /// <returns></returns>
        public static string Start(string connName, string connPath)
        {
            lock (o)
            {
                string path = AppConfig.RunPath + connPath;
                string json = JsonHelper.ReadJson(path);
                if (string.IsNullOrEmpty(json))
                {
                    return connName;
                }
                WatchConfig config = JsonHelper.ToEntity<WatchConfig>(JsonHelper.GetValue(json, connName));
                if (config != null && !string.IsNullOrEmpty(config.Master))
                {
                    AppConfig.SetConn(connName, config.Master);
                    if (!string.IsNullOrEmpty(config.Backup))
                    {
                        AppConfig.SetConn(connName + "_Bak", config.Backup);
                    }
                    if (config.Slave != null && config.Slave.Length > 0)
                    {
                        for (int i = 0; i < config.Slave.Length; i++)
                        {
                            AppConfig.SetConn(connName + "_Slave" + (i + 1), config.Slave[i]);
                        }
                    }
                    if (!watchList.ContainsValue(connPath))
                    {
                        IOWatch.On(path, delegate (FileSystemEventArgs e)
                        {
                            fsy_Changed(e);
                        });
                    }
                    if (!watchList.ContainsKey(connName))
                    {
                        watchList.Add(connName, connPath);
                    }

                    return config.Master;
                }
            }
            return connName;
        }
        private static void fsy_Changed(FileSystemEventArgs e)
        {
            string json = JsonHelper.ReadJson(e.FullPath);
            Dictionary<string, string> dic = JsonHelper.Split(json);
            if (dic != null && dic.Count > 0)
            {
                foreach (KeyValuePair<string, string> item in dic)
                {
                    //移除所有缓存的Key
                    AppConfig.SetConn(item.Key, null);
                    AppConfig.SetConn(item.Key + "_Bak", null);
                    for (int i = 1; i < 10000; i++)
                    {
                        if (!AppConfig.SetConn(item.Key + "_Slave" + i, null))
                        {
                            break;
                        }
                    }
                    ConnObject.ClearCache(item.Key);
                    ConnBean.ClearCache(item.Key);
                }
            }
        }
    }

    internal class WatchConfig
    {
        public string Master;
        public string Backup;
        public string[] Slave;
    }
}
