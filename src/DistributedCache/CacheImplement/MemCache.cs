using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Cache;
using CYQ.Data.Table;
using CYQ.Data.Tool;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// MemCache�ֲ�ʽ������
    /// </summary>
    internal partial class MemCache : DistributedCache
    {
        private MemcachedClient client;
        /// <summary>
        /// ���ؿͻ���
        /// </summary>
        public MemcachedClient Client { get { return client; } }
        internal MemCache()
        {
            client = MemcachedClient.Create(AppConfig.MemCache.Servers);
            if (!string.IsNullOrEmpty(AppConfig.MemCache.ServersBak))
            {
                MemcachedClient clientBak = MemcachedClient.Create(AppConfig.MemCache.ServersBak);
                client.HostServer.HostServerBak = clientBak.HostServer;
            }
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
            return client.Set(key, value, DateTime.Now.AddMinutes(cacheMinutes));
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
                                if (cacheTable.Columns.Count == 0)//��һ��
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

        public override bool Clear()
        {
           return client.FlushAll();
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

    /// <summary>
    /// �ֲ�ʽ��
    /// </summary>
    internal partial class MemCache
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
            return client.SetAll(key, value, DateTime.Now.AddMinutes(cacheMinutes));
        }
        public override int RemoveAll(string key)
        {
            return client.DeleteAll(key);
        }
        public override int AddAll(string key, string value, double cacheMinutes)
        {
            return client.AddAll(key, value, DateTime.Now.AddMinutes(cacheMinutes)); 
        }
        public override bool SetNXAll(string key, string value, double cacheMinutes)
        {
            return client.SetNXAll(key, value, DateTime.Now.AddMinutes(cacheMinutes));
        }
    }
}
