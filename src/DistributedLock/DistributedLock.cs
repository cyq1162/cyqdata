using CYQ.Data.Cache;
using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Lock
{

    /// <summary>
    /// 分布式锁
    /// </summary>
    public abstract class DistributedLock
    {
        #region 对外实例
        /// <summary>
        /// 分布式锁实例【根据配置顺序取值：Redis=》MemCache=》Local】
        /// </summary>
        public static DistributedLock Instance
        {
            get
            {
                switch (DistributedCache.Instance.CacheType)
                {
                    case CacheType.Redis:
                        return RedisLock.Instance;
                    case CacheType.MemCache:
                        return MemCacheLock.Instance;
                    default:
                        return RedisLock.Local;
                }
            }
        }

        /// <summary>
        /// Redis 分布式锁实例
        /// </summary>
        public static DistributedLock Redis
        {
            get
            {
                return RedisLock.Instance;
            }
        }
        /// <summary>
        /// MemCach 分布式锁实例
        /// </summary>
        public static DistributedLock MemCache
        {
            get
            {
                return MemCacheLock.Instance;
            }
        }


        /// <summary>
        /// Local 单机锁 基于 Mutex 锁实例
        /// </summary>
        public static DistributedLock Local
        {
            get
            {
                return LocalLock.Instance;

            }
        }

        /// <summary>
        /// Local 单机内 文件锁【可跨进程或线程释放】
        /// </summary>
        public static DistributedLock File
        {
            get
            {
                return FileLock.Instance;
            }
        }

        ///// <summary>
        ///// 数据库 分布式锁实例
        ///// </summary>
        //public static DistributedLock DataBase
        //{
        //    get
        //    {

        //        return DataBaseLock.Instance;

        //    }
        //}
        #endregion
        /// <summary>
        /// 锁类型
        /// </summary>
        public abstract LockType LockType { get; }

        /// <summary>
        /// 对指定key进行分布式锁
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="millisecondsTimeout">尝试获取锁的最大等待时间（ms毫秒），超过这个值，则认为获取锁失败</param>
        /// <returns></returns>
        public abstract bool Lock(string key, int millisecondsTimeout);

        /// <summary>
        /// 释放（分布式锁）
        /// </summary>
        /// <param name="key">key</param>
        public abstract void UnLock(string key);

        /// <summary>
        /// 幂等性
        /// </summary>
        /// <param name="key">key</param>
        /// <returns></returns>
        public abstract bool Idempotent(string key);
        /// <summary>
        /// 幂等性
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="keepMinutes">数据保留时间，单位分钟，0 则永久。</param>
        /// <returns></returns>
        public abstract bool Idempotent(string key,double keepMinutes);
    }





}
