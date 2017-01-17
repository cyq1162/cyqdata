
using System.Collections.Generic;
using CYQ.Data.Table;
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
            internal static readonly LocalCache instance = new LocalCache();
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
        public abstract void Set(string key, object value);
        public abstract void Set(string key, object value, double cacheMinutes);
        public abstract void Set(string key, object value, double cacheMinutes, string fileName);
        
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
        /// 删除一个Cache对象
        /// </summary>
        public abstract void Remove(string key);
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
    public abstract partial class CacheManage
    {
        /// <summary>
        /// 通过该方法可以预先加载整个数据库的表结构缓存
        /// </summary>
        public static void PreLoadDBSchemaToCache()
        {
            PreLoadDBSchemaToCache(AppConfig.DB.DefaultConn, true);
        }
        private static readonly object obj = new object();
        /// <summary>
        /// 通过该方法可以预先加载整个数据库的表结构缓存(异常会抛，外层Try Catch)
        /// </summary>
        /// <param name="conn">指定数据链接</param>
        /// <param name="isUseThread">是否开启线程</param>
        public static void PreLoadDBSchemaToCache(string conn, bool isUseThread)
        {
            if (TableSchema.tableCache == null || TableSchema.tableCache.Count == 0)
            {
                lock (obj)
                {
                    if (TableSchema.tableCache == null || TableSchema.tableCache.Count == 0)
                    {
                        if (isUseThread)
                        {
                            ThreadBreak.AddGlobalThread(new System.Threading.ParameterizedThreadStart(LoadDBSchemaCache), conn);
                        }
                        else
                        {
                            LoadDBSchemaCache(conn);
                        }
                    }
                }
            }
        }
        private static void LoadDBSchemaCache(object connObj)
        {
          
                string conn = Convert.ToString(connObj);
                Dictionary<string, string> dic = DBTool.GetTables(Convert.ToString(conn));
                if (dic != null && dic.Count > 0)
                {
                    DbBase helper = DalCreate.CreateDal(conn);
                    if (helper.dalType != DalType.Txt && helper.dalType != DalType.Xml)
                    {
                        foreach (string key in dic.Keys)
                        {
                            TableSchema.GetColumns(key, ref helper);
                        }
                    }
                    helper.Dispose();
                }
            
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
