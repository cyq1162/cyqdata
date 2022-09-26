using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using CYQ.Data.Xml;
using CYQ.Data;
using CYQ.Data.Table;
using CYQ.Data.Tool;
using System.Data;
namespace XHtmlAction_Demo
{
    public partial class Default : System.Web.UI.Page
    {
        string tableName = "demo";
        protected void Page_Load(object sender, EventArgs e)
        {
            CreateTable();
            using (XHtmlAction xml = new XHtmlAction(true, false))
            {
                xml.Load(Server.MapPath("/demo.html"));//加载html模板。
                using (MAction action = new MAction(tableName)) //数据库操作。
                {
                    if (action.Fill("1=1"))//查询id=1的数据
                    {
                        action.Set(1, "hello...");
                        action.Set(2, DateTime.Now);
                        action.Update();
                        xml.LoadData(action.Data, "txt");
                    }
                    MDataTable dt = action.Select();
                    Response.Write("记录总数：" + dt.Rows.Count);
                    xml.LoadData(dt);
                    xml.SetForeach("divFor", SetType.InnerXml);
                }
                Response.Write(xml.OutXml);//输出模板
            }
        }
        //创建文件数据库，并添加50条数据。
        void CreateTable()
        {
            Response.Write("文章见：http://www.cnblogs.com/cyq1162/p/3443244.html <hr />");
            if (DBTool.ExistsTable(tableName))
            {
                using (MAction action = new MAction(tableName))
                {
                    if (action.Fill("order by id desc"))
                    {
                        action.Delete("id<=" + action.Get<int>(0));
                    }
                }
                //DBTool.DropTable(tableName);
            }
            else
            {
                MDataColumn mdc = new MDataColumn();
                mdc.Add("ID", SqlDbType.Int, true);
                mdc.Add("Name");
                mdc.Add("CreateTime", SqlDbType.DateTime);
                DBTool.CreateTable(tableName, mdc);
            }
            MDataTable dt = new MDataTable(tableName);
            dt.Columns = DBTool.GetColumns(tableName);
            for (int i = 0; i < 60; i++)
            {
                dt.NewRow(true).Set(1, "Name_" + i).Set(2, DateTime.Now.AddSeconds(i));
            }
            dt.AcceptChanges(AcceptOp.Insert);

        }
    }

}