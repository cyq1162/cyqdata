using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Caching;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// 缓存依赖信息
    /// </summary>
    public class CacheDependencyInfo
    {
        /// <summary>
        /// 被调用的次数。
        /// </summary>
        public int callCount = 0;
        /// <summary>
        /// 缓存产生的时间
        /// </summary>
        public DateTime createTime;
        /// <summary>
        /// 缓存多少分钟
        /// </summary>
        public Double cacheMinutes = 0;
        private DateTime cacheChangeTime = DateTime.MinValue;
        private CacheDependency fileDependency = null;
        public CacheDependencyInfo(CacheDependency dependency, double cacheMinutes)
        {
            if (dependency != null)
            {
                fileDependency = dependency;
                cacheChangeTime = fileDependency.UtcLastModified;
            }
            createTime = DateTime.Now;
            this.cacheMinutes = cacheMinutes;
        }
        /// <summary>
        /// 系统文件依赖是否发生改变
        /// </summary>
        public bool IsChanged
        {
            get
            {
                if (fileDependency != null && (fileDependency.HasChanged || cacheChangeTime != fileDependency.UtcLastModified))
                {
                    cacheChangeTime = fileDependency.UtcLastModified;
                    return true;
                }
                return false;
            }
        }

        internal bool UserChange = false;
        /// <summary>
        /// 标识缓存对象的更改状态
        /// </summary>
        /// <param name="isChange"></param>
        internal void SetState(bool isChange)
        {
            UserChange = IsChanged ? false : isChange;
        }
    }
}
