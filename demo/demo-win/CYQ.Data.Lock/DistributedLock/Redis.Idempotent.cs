using CYQ.Data.Lock;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CYQ.Data;
namespace DistributedLockTest
{
    /// <summary>
    /// Redis 幂等性
    /// </summary>
    class RedisIdempotent
    {
        public static void Start()
        {
            AppConfig.Redis.Servers = "127.0.0.1:6379";
            for (int i = 0; i < 1000000; i++)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(Idempotent), i);
            }
            Console.Read();
        }
        static int ok = 0;
        static int fail = 0;
        static void Idempotent(object i)
        {
            string key = "myLock";
            bool isOK = false;
            try
            {

                isOK = DistributedLock.Redis.Idempotent(key, 2.0/60);
                if (isOK)
                {
                    ok++;
                    Console.WriteLine(ok + " - OK");
                    //Console.WriteLine("数字：" + i + " -- 线程ID：" + Thread.CurrentThread.ManagedThreadId + " 获得锁成功。");
                }
                else
                {
                   // fail++;
                   // Console.WriteLine(fail + "Fail ----------------------------");
                    //Console.WriteLine("数字：" + i + " -- 线程ID：" + Thread.CurrentThread.ManagedThreadId + " 获得锁失败！");
                }
            }
            finally
            {
                
            }
        }
    }
}
