using CYQ.Data;
using CYQ.Data.Table;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MAction_Demo
{
    public partial class 多表操作 : Form
    {
        public 多表操作()
        {
            InitializeComponent();
        }
        private void OutSql(string msg)
        {
            rtxtSql.Text = msg;
        }
        private void LoadData(string where)
        {
            MDataTable dt;
            using (MAction action = new MAction("V_Article"))
            {
                action.SetSelectColumns("UserID", "Name", "Title", "Content", "PubTime");//设置要显示的列
                dt = action.Select(1, 100, where);
            }
            dt.Bind(dgvView);
        }
        private void btnTransation_Click(object sender, EventArgs e)
        {
            //for (int i = 0; i < 100; i++)
            //{


                MDataTable dt = null;
                string guid = Guid.NewGuid().ToString();
                using (MAction action = new MAction("Users"))
                {
                    bool result = false;
                    action.SetTransLevel(IsolationLevel.ReadCommitted);//可设置的事务级别，一般可以不用设置
                    action.BeginTransation();//设置开启事务标识
                    action.Set("Name", guid.Substring(1, 5));
                    action.Set("Password", "123456");
                    int id = 0;
                    if (action.Insert())//第一个执行时，事务才被加载
                    {
                        id = action.Get<int>(0);
                        action.ResetTable("Article");
                        action.Set("UserID", id);
                        action.Set("Title", guid.Substring(3, 5));
                        action.Set("Content", guid.Substring(5, 5));
                        action.Set("PubTime", DateTime.Now);
                        result = action.Insert(InsertOp.None);
                    }
                    else
                    {
                        action.RollBack();//手工回滚
                    }
                    action.EndTransation();//提交事务
                    if (result)
                    {
                        LoadData("UserID=" + id);
                    }
                    OutSql(action.DebugInfo);
                }
                if (dt != null)
                {
                    dt.Bind(dgvView);
                }
            //}
        }

        private void 多表操作_Load(object sender, EventArgs e)
        {
            LoadData(null);
        }

        private void btnShowInfo_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            MDataTable dt = null;
            using (MAction action = new MAction("Article"))
            {
                sb.Append("AllowInsertID:");
                sb.AppendLine(action.AllowInsertID.ToString());

                sb.Append("ConnectionString:");
                sb.AppendLine(action.ConnString);

                sb.Append("DalType:");
                sb.AppendLine(action.DataBaseType.ToString());

                sb.Append("DalVersion:");
                sb.AppendLine(action.DataBaseVersion);

                sb.Append("DebugInfo:");
                sb.AppendLine(action.DebugInfo);

                sb.Append("RecordsAffected:(通常在执行一个命令后，返回受影响的行数)");
                sb.AppendLine(action.RecordsAffected.ToString());

                sb.Append("TableName:");
                sb.AppendLine(action.TableName);

                sb.Append("TimeOut:");
                sb.AppendLine(action.TimeOut.ToString());

                sb.Append("UI对象:");
                sb.AppendLine(action.UI.ToString());

                dt = action.Data.Columns.ToTable();
            }
            dt.Bind(dgvView);
            rtxtSql.Text = sb.ToString();
        }

        private void btnPara_Click(object sender, EventArgs e)
        {
            MDataTable dt;
            using (MAction action = new MAction("Users"))
            {
                action.SetPara("Name", "0%", DbType.String);
                dt = action.Select("Name like @Name");
            }
            dt.Bind(dgvView);
        }
    }
}
