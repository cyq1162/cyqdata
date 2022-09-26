using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CYQ.Data;
using CYQ.Data.Table;
using CYQ.Data.Tool;
namespace MProc_Demo
{
    class Program
    {

        static void Main(string[] args)
        {
            //MAction已经演示了配置文件配置链接，这里就用代码了。
            AppConfig.DB.DefaultConn = "Data Source={0}demo.db;failifmissing=false;";
            ExeSql();
            ExeProc();
            Console.Read();
        }
        static void OutMsg(object msg)
        {
            Console.WriteLine(msg.ToString());
        }
        /// <summary>
        /// 执行SQL语句
        /// </summary>
        static void ExeSql()
        {
            AppConfig.DB.DefaultConn = "server=CYQ-PC\\SQL2008;database=Test;uid=sa;pwd=123456";
            string sql = "select * from users";
            using (MProc proc = new MProc(sql))
            {
                proc.BeginTransation();//事务的使用和MAction是一样的

                //MDataTable dt = proc.ExeMDataTable();
                //OutMsg(dt.Rows.Count);

                proc.ResetProc("select count(*) from demo_testa");
                MDataTable dt2 = proc.ExeMDataTable();
                OutMsg(dt2.Rows.Count);

                proc.ResetProc("select name from users where UserID=@UserID");
                proc.Set("UserID", 1);
                string name = proc.ExeScalar<string>();
                OutMsg(name);

                proc.ResetProc("update users set password=123 where name=@name");
                proc.Set("name", name);
                int result = proc.ExeNonQuery();
                OutMsg(result);

                if (result < 1)
                {
                    proc.RollBack();//找不到结果，要回滚事务
                    return;
                }

                proc.ResetProc("select * from users;select * from Article");//多语句执行
                List<MDataTable> dtList = proc.ExeMDataTableList();
                OutMsg(dtList.Count);
                proc.EndTransation();
            }
        }
        /// <summary>
        /// 执行存储过程
        /// </summary>
        static void ExeProc()
        {
            return;
            //SQlite 没有存储过程，只能写示例代码
            using (MProc proc = new MProc("存储过程名"))
            {
                proc.Set("参数1", "值1");
                proc.Set("参数2", "值2");
                proc.SetCustom("ReturnValue", ParaType.ReturnValue);//如果有返回值
                proc.SetCustom("OutPutValue1", ParaType.OutPut);//如果有output值
                proc.SetCustom("OutPutValue2", ParaType.OutPut);//如果有output值多个
                proc.SetCustom("XXX", ParaType.Cursor);//如果是Oracle有游标
                proc.SetCustom("XXX2", ParaType.CLOB);//Oracle的CLOB类型
                proc.SetCustom("XXX3", ParaType.NCLOB);//Oracle的NCLOB类型
                MDataTable dt = proc.ExeMDataTable();//执行语句
                int returnValue = proc.ReturnValue;//拿返回值
                object outPutValue = proc.OutPutValue;//如果只有一个值
                Dictionary<string,string> dic=proc.OutPutValue as Dictionary<string,string>;
                string out1 = dic["OutPutValue1"];
                string out2 = dic["OutPutValue2"];
            }
        }
    }
}
