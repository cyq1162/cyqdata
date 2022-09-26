using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CYQ.Data;
using CacheDemo.Models;

namespace CacheDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            // 基于CodeFirst自动创建数据库
            DemoDbContext db = new DemoDbContext();
            db.UserInfos.Count();

            // Cache Demo 代码
            CacheDemo demo = new CacheDemo();
            demo.Test();
        }
    }
}
