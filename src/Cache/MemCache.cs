using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Cache;
using CYQ.Data.Table;
using CYQ.Data.Tool;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// MemCache分布式缓存类
    /// </summary>
    internal class MemCache : CacheManage
    {
        MemcachedClient client;
        internal MemCache()
        {
            if (string.IsNullOrEmpty(AppConfig.MemCache.Servers))
            {
                string err = "AppConfig.MemCache.Servers cant' be Empty!";
                Log.WriteLogToTxt(err, LogType.Cache);
                Error.Throw(err);
            }
            client = MemcachedClient.Create(AppConfig.MemCache.Servers);
            if (client.hostServer.HostList.Count == 0)
            {
                string err = "AppConfig.MemCache.Servers can't find the host for service : " + AppConfig.MemCache.Servers;
                Log.WriteLogToTxt(err, LogType.Cache);
                Error.Throw(err);
            }
            if (!string.IsNullOrEmpty(AppConfig.MemCache.ServersBak))
            {
                MemcachedClient clientBak = MemcachedClient.Create(AppConfig.MemCache.ServersBak);
                client.hostServer.hostServerBak = clientBak.hostServer;
            }


        }
        public override bool Set(string key, object value)
        {
            return Set(key, value, 60 * 24 * 30);
        }
        public override bool Set(string key, object value, double cacheMinutes)
        {
            return Set(key, value, cacheMinutes, null);
        }
        public override bool Set(string key, object value, double cacheMinutes, string fileName)
        {
            if (string.IsNullOrEmpty(key)) { return false; }
            if (value == null) { return Remove(key); }
            return client.Set(key, value, DateTime.Now.AddMinutes(cacheMinutes));
        }

        public override bool Add(string key, object value)
        {
            return Add(key, value, AppConfig.DefaultCacheTime, null);
        }
        public override bool Add(string key, object value, double cacheMinutes)
        {
            return Add(key, value, cacheMinutes, null);
        }
        public override bool Add(string key, object value, double cacheMinutes, string fileName)
        {
            if (string.IsNullOrEmpty(key)) { return false; }
            if (value == null) { return Remove(key); }
            return client.Add(key, value, DateTime.Now.AddMinutes(cacheMinutes));
        }

        DateTime allowCacheTableTime = DateTime.Now;
        private MDataTable cacheInfoTable = null;
        public override MDataTable CacheInfo
        {
            get
            {
                if (cacheInfoTable == null || DateTime.Now > allowCacheTableTime)
                {
                    #region Create Table
                    MDataTable cacheTable = new MDataTable();
                    Dictionary<string, Dictionary<string, string>> status = client.Stats();
                    if (status != null)
                    {

                        foreach (KeyValuePair<string, Dictionary<string, string>> item in status)
                        {
                            if (item.Value.Count > 0)
                            {
                                MDataTable dt = MDataTable.CreateFrom(item.Value);
                                if (cacheTable.Columns.Count == 0)//第一次
                                {
                                    cacheTable = dt;
                                }
                                else
                                {
                                    cacheTable.JoinOnName = "Key";
                                    cacheTable = cacheTable.Join(dt, "Value");
                                }
                                cacheTable.Columns["Value"].ColumnName = item.Key;
                            }
                        }
                    }
                    cacheTable.TableName = "MemCache";
                    allowCacheTableTime = DateTime.Now.AddSeconds(1); 
                    #endregion

                    cacheInfoTable = cacheTable;
                }
                return cacheInfoTable;
            }
        }

        public override void Clear()
        {
            client.FlushAll();
        }

        public override bool Contains(string key)
        {
            if (string.IsNullOrEmpty(key)) { return false; }
            return Get(key) != null;
        }

        //int count = -1;
        //DateTime allowGetCountTime = DateTime.Now;
        public override int Count
        {
            get
            {
                int count = 0;
                MDataRow row = CacheInfo.FindRow("Key='curr_items'");
                if (row != null)
                {
                    for (int i = 1; i < row.Columns.Count; i++)
                    {
                        count += int.Parse(row[i].StringValue);
                    }
                }
                return count;
            }
        }

        public override object Get(string key)
        {
            if (string.IsNullOrEmpty(key)) { return null; }
            return client.Get(key);
        }


        public override bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key)) { return false; }
            return client.Delete(key);
        }

        public override string WorkInfo
        {
            get
            {
                return client.WorkInfo;

            }
        }

        public override CacheType CacheType
        {
            get { return CacheType.MemCache; }
        }
    }
}
