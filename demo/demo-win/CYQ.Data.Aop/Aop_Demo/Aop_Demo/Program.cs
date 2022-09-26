using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CYQ.Data;
namespace Aop_Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateTableAndData();
            //你把下面这行Aop配置开了看看
            AppConfig.Aop = "MyAopForDemo.AopForRecordState,MyAopForDemo";
            Start();
            OperatorLog();
            Console.Read();
        }
        static void OutMsg(string msg)
        {
            Console.WriteLine(msg);
        }
        static void Start()
        {
            int rndNum = DateTime.Now.Second;//随机数
            using (XmlTable xt = new XmlTable())
            {
                if (xt.Fill(rndNum))//查询
                {
                    OutMsg("Fill:" + rndNum);
                    xt.Name = xt.Name + "_N";
                    bool result = xt.Update();
                    OutMsg("Update:" + rndNum + " - " + result);
                }
                rndNum = rndNum + 1;
                if (xt.Exists(rndNum))
                {
                    OutMsg("Exists:" + rndNum);
                    bool result = xt.Delete(rndNum);
                    OutMsg("Delete:" + rndNum + " - " + result);
                }
            }
        }
        //创建并创建50条数据
        static void CreateTableAndData()
        {
            using (XmlTable xt = new XmlTable())//这一行会自动创建表结构
            {
                bool result = xt.Delete("0=0");//清空所有数据。
                OutMsg("Delete 所有: - " + result);
                for (int i = 1; i < 60; i++)
                {
                    xt.Name = "Name" + i.ToString();
                    xt.DateTime = DateTime.Now.AddSeconds(i);
                    xt.Insert();
                }
            }
        }

        static void OperatorLog()
        {
            //这里演示系统日志，其实无它，就是系统日志这个的Aop默认是关掉的，不受Aop影响
            AppConfig.Log.IsWriteLog = true;
            AppConfig.Log.LogConn = "txt path={0}";
            using (SysLogs sl = new SysLogs())
            {
                sl.Message = "我只是消息，但我没错";
                bool result = sl.Insert();
                OutMsg("SysLogs:Insert - " + result);
            }
        }
    }
}
