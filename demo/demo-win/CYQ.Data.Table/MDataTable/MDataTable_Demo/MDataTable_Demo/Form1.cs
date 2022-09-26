using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CYQ.Data;
using CYQ.Data.Table;
namespace MDataTable_Demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        MDataTable dt = null;
        private void Form1_Load(object sender, EventArgs e)
        {
            LoadData();
        }
        void LoadData()
        {
            CYQ.Data.AppConfig.Cache.IsAutoCache = false;
            using (MAction action = new MAction("Users"))
            {
                dt = action.Select();
                dt.Conn = "Data Source={0}demo2.db;failifmissing=false;";
                bool resut = dt.AcceptChanges(AcceptOp.Auto);
                
                int i = 2;
            }
            // dt.Columns[0].Description = "用户ID";
            // dt.Columns[1].Description = "用户名";
            dt.Bind(dgvTable);
            OutProperty();
        }
        void OutProperty()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("TableName:" + dt.TableName);
            sb.AppendLine("Conn:" + dt.Conn);
            sb.AppendLine("Columns.Count:" + dt.Columns.Count);
            sb.AppendLine("Rows.Count:" + dt.Rows.Count);
            sb.AppendLine("RecordsAffected:" + dt.RecordsAffected);
            sb.AppendLine("JoinOnName:" + dt.JoinOnName);
            sb.AppendLine("DynamicData:" + dt.DynamicData);
            rtxtText.Text = sb.ToString();
        }

        private void btnTo_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            string json = dt.ToJson();
            sb.AppendLine("ToJson：" + json);
            MDataTable t = MDataTable.CreateFrom(json);
            sb.AppendLine("从Josn还原了------------------");
            string xml = t.ToXml();
            sb.AppendLine("ToXml：" + xml);
            t = MDataTable.CreateFrom(xml);
            sb.AppendLine("从Xml还原了--------------------");

            sb.AppendLine("还可以和List<T>,Dictionary,ArrayList,HasTable等几乎所有的常用类或数组交互");

            rtxtText.Text = sb.ToString();
        }

        private void btnShow_Click(object sender, EventArgs e)
        {
            rtxtText.Text = "Url：http://www.cnblogs.com/cyq1162/p/5618048.html";
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            bool result = dt.AcceptChanges(AcceptOp.Update);
            rtxtText.Text = "AcceptChanges(Update):" + result;
        }

        private void btnGetUpdate_Click(object sender, EventArgs e)
        {
            MDataTable dt2 = dt.GetChanges();
            if (dt2 != null && dt2.Rows.Count > 0)
            {
                dt2.Bind(dgvTable);
            }
            else
            {
                rtxtText.Text = "没有修改的数据！";
            }


        }

        private void button1_Click(object sender, EventArgs e)
        {
            LoadData();
        }
    }
}
