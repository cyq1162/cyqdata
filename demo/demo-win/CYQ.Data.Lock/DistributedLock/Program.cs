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
            RedisIdempotent.Start();
            //FileLockDemo.Start();
            Console.Read();
        }
    }
}
