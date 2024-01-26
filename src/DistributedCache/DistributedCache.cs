
using System.Collections.Generic;
using CYQ.Data.Table;
using CYQ.Data.Tool;
using System;
using CYQ.Data.SQL;
using System.Configuration;
using System.Threading;
using CYQ.Data.Aop;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// 全局缓存类
    /// </summary>
    /// <example><code>
    /// 使用示例：
    /// 实例化： CacheManage cache=CacheManage.Instance;
    /// 添加：   Cache.Set("路过秋天",new MDataTable);
    /// 判断：   if(cache.Contains("路过秋天"))
    ///          {
    /// 获取：       MDataTable table=cache.Get("路过秋天") as MDataTable;
    ///          }
    /// </code></example>
    public abstract partial class DistributedCache
    {
        #region 对外实例
        /// <summary>
        /// 返回唯一实例(根据配置(Redis、MemCache是否配置启用)决定启用顺序：Redis、MemCache、本地缓存）
        /// </summary>
        public static DistributedCache Instance
        {
            get
            {
                if (!string.IsNullOrEmpty(AppConfig.Redis.Servers))
                {
                    return Redis;
                }
                else if (!string.IsNullOrEmpty(AppConfig.MemCache.Servers))
                {
                    return MemCache;
                }
                else
                {
                    return LocalShell.instance;

                }
            }
        }
        /// <summary>
        /// 单机本地缓存
        /// </summary>
        public static DistributedCache Local
        {
            get
            {

                return LocalShell.instance;
            }
        }
        private static DistributedCache _MemCacheInstance;
        private static readonly object lockMemCache = new object();
        /// <summary>
        /// MemCache缓存（需要配置AppConfig.MemCache.Servers）
        /// </summary>
        public static DistributedCache MemCache
        {
            get
            {
                if (_MemCacheInstance == null)
                {
                    lock (lockMemCache)
                    {
                        if (_MemCacheInstance == null)
                        {
                            _MemCacheInstance = new MemCache();
                        }
                    }
                }
                return _MemCacheInstance;
            }
        }
        private static DistributedCache _RedisInstance;
        private static readonly object lockRedisCache = new object();
        /// <summary>
        /// Redis缓存（需要配置AppConfig.Redis.Servers）
        /// </summary>
        public static DistributedCache Redis
        {
            get
            {
                if (_RedisInstance == null)
                {
                    lock (lockRedisCache)
                    {
                        if (_RedisInstance == null)
                        {
                            _RedisInstance = new RedisCache();
                        }
                    }
                }
                return _RedisInstance;

            }
        }
        class LocalShell
        {
            private static readonly object lockLocalCache = new object();
            /// <summary>
            /// 兼容NetCore下迷一般的Bug的写法。
            /// </summary>
            public static LocalCache instance
            {
                get
                {
                    if (_instance == null)
                    {
                        lock (lockLocalCache)
                        {
                            if (_instance == null)
                            {
                                _instance = new LocalCache();
                            }
                        }
                    }
                    return _instance;
                }
            }
            internal static LocalCache _instance;
        }
        //此种方式，会提前处理，导致异常。
        //class MemShell
        //{
        //    internal static readonly MemCache instance = new MemCache();
        //}
        //class RedisShell
        //{
        //    internal static readonly RedisCache instance = new RedisCache();
        //}
        #endregion

        /// <summary>
        /// Redis、MemCache 需要手工刷新配置值时使用。
        /// </summary>
        /// <param name="newConfigValue">新的配置值</param>
        public virtual void RefleshConfig(string newConfigValue)
        {

        }

        /// <summary>
        /// 缓存的实例类型
        /// </summary>
        public abstract CacheType CacheType { get; }
        /// <summary>
        /// 缓存的信息
        /// </summary>
        public abstract MDataTable CacheInfo { get; }

        /// <summary>
        /// 添一个Cache对象(不存在则添加，存在则返回false)
        /// </summary>
        public abstract bool Add(string key, object value);
        public abstract bool Add(string key, object value, double cacheMinutes);
        public abstract bool Add(string key, object value, double cacheMinutes, string fileName);


        /// <summary>
        /// 设置一个Cache对象(存在则更新，不存在则添加)
        /// </summary>
        public abstract bool Set(string key, object value);
        public abstract bool Set(string key, object value, double cacheMinutes);
        public abstract bool Set(string key, object value, double cacheMinutes, string fileName);

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public abstract bool Clear();
        /// <summary>
        /// 是否存在缓存
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract bool Contains(string key);
        /// <summary>
        /// 获和缓存总数
        /// </summary>
        public abstract int Count { get; }
        /// <summary>
        /// 获得一个Cache对象
        /// </summary>
        public abstract object Get(string key);
        /// <summary>
        /// 获得一个Cache对象
        /// </summary>
        public T Get<T>(string key)
        {
            object o = Get(key);
            if (o != null)
            {
                Type t = typeof(T);
                try
                {
                    return (T)ConvertTool.ChangeType(o, t);
                }
                catch (Exception err)
                {
                    Log.Write(err, LogType.Cache);
                    return default(T);
                }
            }
            return default(T);
        }

        /// <summary>
        /// 删除一个Cache对象
        /// </summary>
        public abstract bool Remove(string key);
        /// <summary>
        /// 缓存的工作信息
        /// </summary>
        public abstract string WorkInfo { get; }

    }
    public abstract partial class DistributedCache
    {
        #region 新增三个接口，用于分布式锁

        /// <summary>
        /// 服务器数量
        /// </summary>
        public virtual int ServerCount { get { return 1; } }

        /// <summary>
        /// 往所有节点写入数据【用于分布式锁的超时机制】
        /// </summary>
        public virtual int SetAll(string key, string value, double cacheMinutes) { return 0; }

        /// <summary>
        /// 往所有节点都发起移除数据。
        /// </summary>
        public virtual int RemoveAll(string key) { return 0; }

        /// <summary>
        /// 往所有节点添加数据，不存在则添加成功，存在则添加失败。
        /// </summary>
        public virtual int AddAll(string key, string value, double cacheMinutes) { return 0; }

        /// <summary>
        /// 分布式锁专用，往节点写入数据，超过一半成功，则返回true，否则移除已插入数据。
        /// </summary>
        public virtual bool SetNXAll(string key, string value, double cacheMinutes) { return false; }
        #endregion
    }


    public abstract partial class DistributedCache
    {
        /// <summary>
        /// 获取系统内部缓存Key
        /// </summary>
        public static string GetKey(CacheKeyType ckt, string tableName)
        {
            return GetKey(ckt, tableName, null);
        }
        /// <summary>
        /// 获取系统内部缓存Key
        /// </summary>
        public static string GetKey(CacheKeyType ckt, string tableName, string conn)
        {
            switch (ckt)
            {
                case CacheKeyType.Schema:
                    return TableSchema.GetSchemaKey(tableName, conn);
                case CacheKeyType.AutoCache:
                    return AopCache.GetBaseKey(tableName, conn);
            }
            return string.Empty;
        }
    }
    /// <summary>
    /// Cache的Key类型
    /// </summary>
    public enum CacheKeyType
    {
        /// <summary>
        /// 表架构的Key
        /// </summary>
        Schema,
        /// <summary>
        /// 智能缓存Key
        /// </summary>
        AutoCache
    }
}
