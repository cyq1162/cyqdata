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
    public partial class 多表查询 : Form
    {
        public 多表查询()
        {
            AppDebug.Start();
            InitializeComponent();
        }
        private void OutSql()
        {
            rtxtSql.Text = AppDebug.Info;
            AppDebug.Stop();
            AppDebug.Start();
        }
        private void btnView_Click(object sender, EventArgs e)
        {
            MDataTable dt;
            using (MAction action = new MAction("V_Article"))
            {
                dt = action.Select();
                OutSql();
            }
            dt.Bind(dgvView);
        }

        private void btnSql_Click(object sender, EventArgs e)
        {
            string sql = "select a.*,u.Name from article a left join users u on a.UserID=u.UserID";
            MDataTable dt;
            using (MAction action = new MAction(sql))
            {
                dt = action.Select("order by userid desc");
                OutSql();
            }
            dt.Bind(dgvView);
        }

        private void btnJoin_Click(object sender, EventArgs e)
        {
            MDataTable dt;
            using (MAction action = new MAction("Article"))
            {
                dt = action.Select();

            }
            dt.JoinOnName = "UserID";
            dt = dt.Join("Users", "UserID", "Name");
            OutSql();
            dt.Bind(dgvView);

        }
    }
}
