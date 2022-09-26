
using System.Collections.Generic;
using CYQ.Data.Table;
using CYQ.Data.Tool;
using System;
using CYQ.Data.SQL;
using System.Configuration;
using System.Threading;
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
    public abstract partial class CacheManage
    {


        #region 对外实例
        /// <summary>
        /// 返回唯一实例(根据配置(AppConfig.Cache.XXXCacheServers)决定启用顺序：Redis、MemCache、本地缓存）
        /// </summary>
        public static CacheManage Instance
        {
            get
            {
                if (!string.IsNullOrEmpty(AppConfig.Cache.RedisServers))
                {
                    return RedisInstance;
                }
                else if (!string.IsNullOrEmpty(AppConfig.Cache.MemCacheServers))
                {
                    return MemCacheInstance;
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
        public static CacheManage LocalInstance
        {
            get
            {

                return LocalShell.instance;
            }
        }
        private static CacheManage _MemCacheInstance;
        private static readonly object lockMemCache = new object();
        /// <summary>
        /// MemCache缓存（需要配置AppConfig.Cache.MemCacheServers）
        /// </summary>
        public static CacheManage MemCacheInstance
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
        private static CacheManage _RedisInstance;
        private static readonly object lockRedisCache = new object();
        /// <summary>
        /// Redis缓存（需要配置AppConfig.Cache.RedisServers）
        /// </summary>
        public static CacheManage RedisInstance
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
        /// 缓存的实例类型
        /// </summary>
        public abstract CacheType CacheType { get; }
        /// <summary>
        /// 缓存的信息
        /// </summary>
        public abstract MDataTable CacheInfo { get; }
        /// <summary>
        /// 设置一个Cache对象
        /// </summary>
        public abstract bool Set(string key, object value);
        public abstract bool Set(string key, object value, double cacheMinutes);
        public abstract bool Set(string key, object value, double cacheMinutes, string fileName);

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public abstract void Clear();
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
    public abstract partial class CacheManage
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
                    return AutoCache.GetBaseKey(tableName, conn);
            }
            return string.Empty;
        }
    }
    /// <summary>
    /// 支持的Cache类型
    /// </summary>
    public enum CacheType
    {
        /// <summary>
        /// 本地缓存
        /// </summary>
        LocalCache,
        /// <summary>
        /// MemCached分布式缓存
        /// </summary>
        MemCache,
        /// <summary>
        /// Redis分布式缓存
        /// </summary>
        Redis
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
