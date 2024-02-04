using CYQ.Data;
using CYQ.Data.Cache;
using CYQ.Data.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace CacheManage_Demo
{
    /// <summary>
    /// 说明：V5.9.X.Y 版本后 CacheManage 更名为：DistributedCache
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            
            //说明：如果你确定缓存一定是在本机，使用：CacheManage cache= CacheManage.LocalInstance
            //如果只是缓存一般数据，将来有可能启用分布式时，用：CacheManage cache = CacheManage.Instance;

            //比如框架对一些表架构的元数据的缓存，用的是本机（速度快）：CacheManage.LocalInstance
            //而框架对于自动缓存（表的数据），用的是：CacheManage.Instance （将来随便分布式启用分散到各缓存服务器）
            
        }
        /// <summary>
        /// 本地缓存示例：（基本使用）
        /// </summary>
        static void LocalCache()
        {
            DistributedCache cache = DistributedCache.Instance;//自动获取缓存类型（默认本地缓存）。
            //CacheManage cache = CacheManage.LocalInstance;//指定本地缓存
            if (!cache.Contains("a1"))
            {
                cache.Set("a1", "a1", 0.1);
            }
            cache.Set("a2", "a2", 0.5);//存在则更新，不存在则添加。
            cache.Set("a3", "a3", 2.2);
            cache.Set("a0", "a0");
            cache.Set("table", cache.CacheInfo);

            Console.WriteLine(cache.Get<string>("a0"));
            Console.WriteLine(cache.Get<string>("a1"));
            Console.WriteLine(cache.Get<string>("a2"));
            Console.WriteLine(cache.Get<string>("a3"));
            MDataTable table = cache.Get<MDataTable>("table");
            if (table != null)
            {
                Console.WriteLine(table.Rows.Count);
            }

            if (cache.CacheType == CacheType.LocalCache)//只能拿到本机的信息
            {
                Console.WriteLine("缓存数：" + table.Rows.Count);
                Console.WriteLine("总内存(M)：" + GC.GetTotalMemory(false) / 1024); // 感觉拿到的值不太靠谱。
            }
            cache.Remove("a0");//单个移除
            cache.Clear();//清除所有缓存
            Console.Read();
        }

        /// <summary>
        /// 分布式缓存：memcache 示例：
        /// </summary>
        static void MemCache()
        {
            //使用
            AppConfig.MemCache.Servers = "127.0.0.1:11211";//配置启用MemCache,127.0.0.1:11212
            LocalCache();
        }

        /// <summary>
        /// 分布式缓存：Redis 示例：
        /// </summary>
        static void RedisCache()
        {
            //使用分布式缓存：Redis
            AppConfig.Redis.Servers = "127.0.0.1:6379";//配置启用Redis,127.0.0.1:6379
           // AppConfig.Cache.RedisServers = "127.0.0.1:6379 - 123456";//配置启用Redis,127.0.0.1:6379，带密码：123456
            LocalCache();
        }

    }
}
