
using System.Collections.Generic;
using CYQ.Data.Table;
using System.Web.Caching;
using CYQ.Data.Tool;
using System;
using CYQ.Data.SQL;
namespace CYQ.Data.Cache
{
    /// <summary>
    /// 全局缓存类
    /// </summary>
    /// <example><code>
    /// 使用示例：
    /// 实例化： CacheManage cache=CacheManage.Instance;
    /// 添加：   cache.Add("路过秋天",new MDataTable);
    /// 判断：   if(cache.Contains("路过秋天"))
    ///          {
    /// 获取：       MDataTable table=cache.Get("路过秋天") as MDataTable;
    ///          }
    /// </code></example>
    public abstract partial class CacheManage
    {


        #region 对外实例
        /// <summary>
        /// 返回唯一实例(根据是否配置AppConfig.Cache.MemCacheServers的服务器决定启用本地缓存或分布式缓存）
        /// </summary>
        public static CacheManage Instance
        {
            get
            {
                if (string.IsNullOrEmpty(AppConfig.Cache.MemCacheServers))
                {
                    return LocalShell.instance;
                }
                else
                {
                    return MemShell.instance;
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

        class LocalShell
        {
            internal static readonly LocalCache instance = new LocalCache();
        }
        class MemShell
        {
            internal static readonly MemCache instance = new MemCache();
        }
        #endregion
        public abstract CacheType CacheType { get; }
        /// <summary>
        /// 添加一个Cache对象
        /// </summary>
        public abstract void Add(string key, object value);
        public abstract void Add(string key, object value, double cacheMinutes);
        public abstract void Add(string key, object value, string fileName);
        public abstract void Add(string key, object value, string fileName, double cacheMinutes);
        public abstract void Add(string key, object value, string fileName, double cacheMinutes, CacheItemPriority level);
        public abstract Dictionary<string, CacheDependencyInfo> CacheInfo { get; }
        public abstract MDataTable CacheTable { get; }
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
                    return (T)StaticTool.ChangeType(o, t);
                }
                catch (Exception err)
                {
                    Log.WriteLogToTxt(err);
                    return default(T);
                }
            }
            return default(T);
        }
        /// <summary>
        /// 获取目标的文件依赖是否发生更改
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract bool GetFileDependencyHasChanged(string key);
        /// <summary>
        /// 获取缓存对象是否被手工标识为已更改
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract bool GetHasChanged(string key);
        /// <summary>
        /// 还可用的缓存字节数
        /// </summary>
        public abstract long RemainMemoryBytes { get; }
        /// <summary>
        /// 还可用的缓存百分比
        /// </summary>
        public abstract long RemainMemoryPercentage { get; }
        /// <summary>
        /// 删除一个Cache对象
        /// </summary>
        public abstract void Remove(string key);
        /// <summary>
        /// 缓存设置：有则更新，无则添加
        /// </summary>
        public abstract void Set(string key, object value);
        public abstract void Set(string key, object value, double cacheMinutes);
        /// <summary>
        /// 手动对缓存象标识为已更改
        /// </summary>
        public abstract void SetChange(string key, bool isChange);
        /// <summary>
        /// 更新缓存，缓存存在则更更新，不存在则跳过
        /// </summary>
        public abstract void Update(string key, object value);
        public abstract string WorkInfo { get; }

    }
    public abstract partial class CacheManage
    {
        /// <summary>
        /// 获取系统内部缓存Key
        /// </summary>
        public static string GetKey(CacheKeyType ckt, string tableName)
        {
            return GetKey(ckt, tableName, AppConfig.DB.DefaultDataBase, AppConfig.DB.DefaultDalType);
        }
        /// <summary>
        /// 获取系统内部缓存Key
        /// </summary>
        public static string GetKey(CacheKeyType ckt, string tableName, string dbName, DalType dalType)
        {
            switch (ckt)
            {
                case CacheKeyType.Schema:
                    return TableSchema.GetSchemaKey(tableName, dbName, dalType);
                case CacheKeyType.AutoCache:
                    return AutoCache.GetBaseKey(dalType, dbName, tableName);
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
        /// 分布式MemCached缓存
        /// </summary>
        MemCache
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
