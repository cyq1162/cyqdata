using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CYQ.Data.Lock;

namespace DistributedLockTest
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < 10000; i++)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(FileLock), i);
            }
            Console.Read();
        }
        static int ok = 0;
        static int fail = 0;
        static void FileLock(object i)
        {
            string key = "myLock";
            bool isOK = false;
            try
            {
               
                isOK = DistributedLock.File.Lock(key, 2000);
                if (isOK)
                {
                    ok++;
                    Console.WriteLine(ok+"OK");
                    //Console.WriteLine("数字：" + i + " -- 线程ID：" + Thread.CurrentThread.ManagedThreadId + " 获得锁成功。");
                }
                else
                {
                    fail++;
                    Console.WriteLine(fail + "Fail ----------------------------");
                    //Console.WriteLine("数字：" + i + " -- 线程ID：" + Thread.CurrentThread.ManagedThreadId + " 获得锁失败！");
                }
            }
            finally
            {
                if (isOK)
                {
                    DistributedLock.File.UnLock(key);
                }
            }
        }
    }
}
