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
            if (string.IsNullOrEmpty(AppConfig.Cache.MemCacheServers))
            {
                Error.Throw("AppConfig.Cache.MemCacheServers cant' be Empty!");
            }
            client = MemcachedClient.Setup("MemCache", AppConfig.Cache.MemCacheServers.Split(','));
            if (!string.IsNullOrEmpty(AppConfig.Cache.MemCacheServersBak))
            {
                MemcachedClient clientBak = MemcachedClient.Setup("MemCacheBak", AppConfig.Cache.MemCacheServersBak.Split(','));
                client.serverPool.serverPoolBak = clientBak.serverPool;
            }


        }
        public override void Set(string key, object value)
        {
            Set(key, value, 0);
        }
        public override void Set(string key, object value, double cacheMinutes)
        {
            Set(key, value, cacheMinutes, null);
        }
        public override void Set(string key, object value, double cacheMinutes, string fileName)
        {
            client.Add(key, value, DateTime.Now.AddMinutes(cacheMinutes));
        }

        DateTime allowCacheTableTime = DateTime.Now;
        private MDataTable cacheTable = null;
        public override MDataTable CacheInfo
        {
            get
            {
                if (cacheTable == null || DateTime.Now > allowCacheTableTime)
                {
                    cacheTable = null;
                    cacheTable = new MDataTable();
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
                    allowCacheTableTime = DateTime.Now.AddMinutes(1);
                }
                return cacheTable;
            }
        }

        public override void Clear()
        {
            client.FlushAll();
        }

        public override bool Contains(string key)
        {
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
            return client.Get(key);
        }


        public override void Remove(string key)
        {
            client.Delete(key);
        }


        DateTime allowGetWorkInfoTime = DateTime.Now;
        string workInfo = string.Empty;
        public override string WorkInfo
        {
            get
            {
                if (workInfo == string.Empty || DateTime.Now > allowGetWorkInfoTime)
                {
                    workInfo = null;
                    Dictionary<string, Dictionary<string, string>> status = client.Status();
                    if (status != null)
                    {
                        JsonHelper js = new JsonHelper(false, false);
                        js.Add("OKServerCount", client.okServer.ToString());
                        js.Add("DeadServerCount", client.errorServer.ToString());
                        foreach (KeyValuePair<string, Dictionary<string, string>> item in status)
                        {
                            js.Add(item.Key, JsonHelper.ToJson(item.Value));
                        }
                        js.AddBr();
                        workInfo = js.ToString();
                    }
                    allowGetWorkInfoTime = DateTime.Now.AddMinutes(5);
                }
                return workInfo;
            }
        }

        public override CacheType CacheType
        {
            get { return CacheType.MemCache; }
        }
    }
}
