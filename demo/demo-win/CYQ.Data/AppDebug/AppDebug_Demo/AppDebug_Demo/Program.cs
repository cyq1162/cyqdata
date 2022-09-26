using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CYQ.Data;
namespace AppDebug_Demo
{
    class Program
    {
        static void Main(string[] args)
        { 
            AppConfig.Debug.OpenDebugInfo = true;//首先要打开这个

            AppConfig.Debug.InfoFilter = 20;//记录SQL语句执行时间>1毫秒的(这个是在AppDebug开启后的：)
            AppConfig.Debug.SqlFilter = 2;//记录SQL执行语句时间>2毫秒的(这个是所有的SQL语句）
            //注意打开软件文件，执行时间大于2毫秒的将记录在在 SqlFilter_时间.txt 

            AppDebug.Start();//开始记录
            Exe1();
            Exe2();
            Exe3();
            Console.WriteLine(AppDebug.Info);//拿到调试信息
            AppDebug.Stop();//关闭记录
            Console.Read();
        }
        static void Exe1()
        {
             string sql = "select count(*) from users";
             using (MProc proc = new MProc(sql))
             {
                 proc.ExeScalar<string>();
             }

        }
        static void Exe2()
        {
            using (MAction action = new MAction("V_Article"))
            {
                action.Select();
            }
        }
        static void Exe3()
        {
            using (MAction action = new MAction("Users"))
            {
                action.Fill(1);
            }
        }
    }
}
