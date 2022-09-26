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
          
            DBInfo dbInfo = DBTool.GetDBInfo(AppConfig.DB.DefaultConn);
            string databaseName = dbInfo.DataBaseName;
            if (dbInfo != null)
            {
                OutMsg("数据库名称:" + dbInfo.DataBaseName);
                OutMsg("数据库类型:" + dbInfo.DataBaseType);
                foreach (KeyValuePair<string, TableInfo> item in dbInfo.Tables)//读取所有表
                {
                    OutMsg("表:" + item.Key + " 说明：" + item.Value.Description);
                    MDataColumn mdc = item.Value.Columns;//读取所有列
                    foreach (MCellStruct ms in mdc)
                    {
                        OutMsg("  列:" + ms.ColumnName + " SqlType：" + ms.SqlType);
                    }
                }
            }
            OutMsg("-----------------------------------------");

            string newTableName = "A18";// +DateTime.Now.Second;

            result = DBTool.Exists(newTableName,"U", AppConfig.DB.DefaultConn);//检测表是否存在
            OutMsg("表 " + newTableName + (result ? "存在" : "不存在"));

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
            DataBaseType dalType = DBTool.GetDataBaseType("txt path={0}");
            OutMsg("数据库类型为： " + dalType);
            OutMsg("-----------------------------------------");

            OutMsg(DBTool.Keyword("表关键字", DataBaseType.MsSql));//DBTool.NotKeyword 则取消
            OutMsg(DBTool.Keyword("表关键字", DataBaseType.Oracle));
            OutMsg(DBTool.Keyword("表关键字", DataBaseType.MySql));
            OutMsg(DBTool.Keyword("表关键字", DataBaseType.SQLite));

            string changeDataType = DBTool.GetDataType(newMdc[0], DataBaseType.Access, string.Empty);
            OutMsg("数据类型为： " + changeDataType);
            OutMsg("-----------------------------------------");

            string formatValue = DBTool.FormatDefaultValue(DataBaseType.Access, "[#GETDATE]", 1, System.Data.SqlDbType.DateTime);
            OutMsg("Access的日期数据类型为： " + formatValue);
            OutMsg("-----------------------------------------");

        }
    }
}
