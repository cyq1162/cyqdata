using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Cache
{
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
}
