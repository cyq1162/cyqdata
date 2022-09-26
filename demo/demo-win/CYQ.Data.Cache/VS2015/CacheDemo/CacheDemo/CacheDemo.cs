using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CYQ.Data;
using CacheDemo.Models;
using CYQ.Data.Table;

namespace CacheDemo
{
    /// <summary>
    /// CYQ.Data.Cache 使用示例
    /// </summary>
    class CacheDemo
    {
        ////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Type: 变量定义
        //
        ////////////////////////////////////////////////////////////////////////////////////////////

        public CYQ.Data.Cache.CacheManage cache = CYQ.Data.Cache.CacheManage.Instance;

        ////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Type: 函数
        //
        ////////////////////////////////////////////////////////////////////////////////////////////

        public void Test()
        {
            using (MAction action = new MAction("UserInfo"))
            {
                action.Fill("Account='admin'");
                UserInfo adminUser = action.Data.ToEntity<UserInfo>();
                MDataTable tableUser = action.Select();

                Console.WriteLine("Cache Count:{0}", cache.Count);

                // 缓存用户信息，5分钟后丢弃
                cache.Add("User_Admin", adminUser, null, 5);
                Console.WriteLine("Cache Count:{0}", cache.Count);

                // 缓存Table
                cache.Add("User_Table", tableUser);
                Console.WriteLine("Cache Count:{0}", cache.Count);

                if (cache.Contains("User_Admin"))
                {
                    UserInfo user = cache.Get("User_Admin") as UserInfo;
                    Console.WriteLine("Cache Hit User_Admin， Account={0}， Name={1}", 
                        user.Account, user.Name);
                }

                if (cache.Contains("User_Table"))
                {
                    MDataTable table = cache.Get("User_Table") as MDataTable;
                    Console.WriteLine("Cache Hit User_Table， User Count={0}",
                        table.Rows.Count);
                }

                // 显示缓存信息
                Console.WriteLine("Cache Count:{0}", cache.Count);
                Console.WriteLine("Cache Free Memory:{0}bytes, {1}%", 
                    cache.RemainMemoryBytes, cache.RemainMemoryPercentage);
                //cache.CacheInfo
            }
        }
    }
}
