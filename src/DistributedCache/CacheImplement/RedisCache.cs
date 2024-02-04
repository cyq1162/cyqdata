using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Cache;
using CYQ.Data.Table;
using CYQ.Data.Tool;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// Redis分布式缓存类
    /// </summary>
    internal partial class RedisCache : DistributedCache
    {
        private RedisClient client;
        /// <summary>
        /// 读取客户端
        /// </summary>
        public RedisClient Client { get { return client; } }
        internal RedisCache()
        {
            client = RedisClient.Create(AppConfig.Redis.Servers);
            //if (!string.IsNullOrEmpty(AppConfig.Redis.ServersBak))
            //{
            //    RedisClient clientBak = RedisClient.Create(AppConfig.Redis.ServersBak);
            //    client.HostServer.HostServerBak = clientBak.HostServer;
            //}
        }

        public override void RefleshConfig(string newConfigValue)
        {
            client.HostServer.RefleshHostServer(newConfigValue);
        }

        public override bool Set(string key, object value)
        {
            return Set(key, value, 0);
        }

        public override bool Set(string key, object value, double cacheMinutes)
        {
            return Set(key, value, cacheMinutes, null);
        }

        public override bool Set(string key, object value, double cacheMinutes, string fileName)
        {
            if (string.IsNullOrEmpty(key)) { return false; }
            if (value == null) { return Remove(key); }
            if (cacheMinutes == 0) { cacheMinutes = AppConfig.Cache.DefaultMinutes; }
            return client.Set(key, value, Convert.ToInt32(cacheMinutes * 60));
        }
        public override bool Add(string key, object value)
        {
            return Add(key, value, 0, null);
        }
        public override bool Add(string key, object value, double cacheMinutes)
        {
            return Add(key, value, cacheMinutes, null);
        }
        public override bool Add(string key, object value, double cacheMinutes, string fileName)
        {
            if (string.IsNullOrEmpty(key)) { return false; }
            if (value == null) { return Remove(key); }
            if (cacheMinutes == 0) { cacheMinutes = AppConfig.Cache.DefaultMinutes; }
            return client.Add(key, value, Convert.ToInt32(cacheMinutes * 60));
        }


        DateTime allowCacheTableTime = DateTime.Now;
        private MDataTable cacheInfoTable = null;
        public override MDataTable CacheInfo
        {
            get
            {

                if (cacheInfoTable == null || cacheInfoTable.Columns.Count == 0 || DateTime.Now > allowCacheTableTime)
                {

                    MDataTable cacheTable = new MDataTable();

                    #region Create Table
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
                    cacheTable.TableName = "Redis";
                    allowCacheTableTime = DateTime.Now.AddSeconds(1);
                    #endregion

                    cacheInfoTable = cacheTable;

                }
                return cacheInfoTable;
            }
        }

        public override bool Clear()
        {
            return client.FlushAll();
        }

        public override bool Contains(string key)
        {
            if (string.IsNullOrEmpty(key)) { return false; }
            return client.ContainsKey(key);
        }

        //int count = -1;
        //DateTime allowGetCountTime = DateTime.Now;
        private static object o = new object();
        public override int Count
        {
            get
            {

                int count = 0;
                if (CacheInfo != null && CacheInfo.Columns.Count > 0)
                {
                    lock (o)
                    {
                        if (CacheInfo.Columns.Contains("Key"))
                        {
                            IList<MDataRow> rows = CacheInfo.FindAll("Key like 'db%'");
                            if (rows != null && rows.Count > 0)
                            {
                                foreach (MDataRow row in rows)
                                {
                                    for (int i = 1; i < row.Columns.Count; i++)
                                    {
                                        if (row[i].IsNullOrEmpty) { continue; }
                                        count += int.Parse(row[i].StringValue.Split(',')[0].Split('=')[1]);
                                    }
                                }

                            }
                        }
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
            get { return CacheType.Redis; }
        }
    }

    /// <summary>
    /// 分布式锁
    /// </summary>
    internal partial class RedisCache
    {
        public override int ServerCount
        {
            get
            {
                return client.HostServer.HostList.Count;
            }
        }
        public override int SetAll(string key, string value, double cacheMinutes)
        {
            return client.SetAll(key, value, Convert.ToInt32(cacheMinutes * 60));
        }
        public override int RemoveAll(string key)
        {
            return client.DeleteAll(key);
        }
        public override int AddAll(string key, string value, double cacheMinutes)
        {
            return client.AddAll(key, value, Convert.ToInt32(cacheMinutes * 60));
        }
        public override bool SetNXAll(string key, string value, double cacheMinutes)
        {
            return client.SetNXAll(key, value, Convert.ToInt32(cacheMinutes * 60));
        }
    }
}
