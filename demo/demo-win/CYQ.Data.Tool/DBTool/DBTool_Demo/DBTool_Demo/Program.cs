using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CYQ.Data;
using CYQ.Data.Tool;
using CYQ.Data.Table;
namespace DBTool_Demo
{
    class Program
    {
        static void OutMsg(string msg)
        {
            Console.WriteLine(msg);
        }
        static void Main(string[] args)
        {
            Start();
            Console.Read();


        }
        static void Start()
        {
            bool result = DBTool.TestConn(AppConfig.DB.DefaultConn);//检测数据库链接是否正常
            OutMsg("数据库链接：" + result);
            OutMsg("-----------------------------------------");
            string databaseName;
            Dictionary<string, string> tables = DBTool.GetTables(AppConfig.DB.DefaultConn, out databaseName);//读取所有表
            if (tables != null)
            {
                OutMsg("数据库:" + databaseName);
                foreach (KeyValuePair<string, string> item in tables)
                {
                    OutMsg("表:" + item.Key + " 说明：" + item.Value);
                    MDataColumn mdc = DBTool.GetColumns(item.Key);//读取所有列
                    foreach (MCellStruct ms in mdc)
                    {
                        OutMsg("  列:" + ms.ColumnName + " SqlType：" + ms.SqlType);
                    }
                }
            }
            OutMsg("-----------------------------------------");

            string newTableName = "A18";// +DateTime.Now.Second;

            DalType dalType;
            result = DBTool.ExistsTable(newTableName, AppConfig.DB.DefaultConn, out dalType);//检测表是否存在
            OutMsg("表 " + newTableName + (result ? "存在" : "不存在") + " 数据库类型：" + dalType);

            OutMsg("-----------------------------------------");
            if (result)
            {
                result = DBTool.DropTable(newTableName);
                OutMsg("表 " + newTableName + " 删除?" + result);
                OutMsg("-----------------------------------------");
            }

            MDataColumn newMdc = new MDataColumn();
            newMdc.Add("ID", System.Data.SqlDbType.Int);
            newMdc.Add("Name", System.Data.SqlDbType.NVarChar);

            result = DBTool.CreateTable(newTableName, newMdc);
            OutMsg("表 " + newTableName + " 创建?" + result);
            OutMsg("-----------------------------------------");

            newMdc[1].ColumnName = "UserName";
            newMdc[1].AlterOp = AlterOp.Rename;//将新创建的表name => username
            newMdc.Add("Password");
            newMdc[2].AlterOp = AlterOp.AddOrModify;// 新增列 Password

            result = DBTool.AlterTable(newTableName, newMdc);
            OutMsg("表 " + newTableName + " 修改结构?" + result);
            OutMsg("-----------------------------------------");

            OutMsg("------------------其它操作-------------------");
            dalType = DBTool.GetDalType("txt path={0}");
            OutMsg("数据库类型为： " + dalType);
            OutMsg("-----------------------------------------");
           
            OutMsg(DBTool.Keyword("表关键字", DalType.MsSql));//DBTool.NotKeyword 则取消
            OutMsg(DBTool.Keyword("表关键字", DalType.Oracle));
            OutMsg(DBTool.Keyword("表关键字", DalType.MySql));
            OutMsg(DBTool.Keyword("表关键字", DalType.SQLite));

            string changeDataType = DBTool.GetDataType(newMdc[0], DalType.Access, string.Empty);
            OutMsg("数据类型为： " + changeDataType);
            OutMsg("-----------------------------------------");

            string formatValue = DBTool.FormatDefaultValue(DalType.Access,"[#GETDATE]",1, System.Data.SqlDbType.DateTime);
            OutMsg("Access的日期数据类型为： " + formatValue);
            OutMsg("-----------------------------------------");

        }
    }
}
