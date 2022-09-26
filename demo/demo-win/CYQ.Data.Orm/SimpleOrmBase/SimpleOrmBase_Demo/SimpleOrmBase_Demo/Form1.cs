using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CYQ.Data.Orm;
using Web.Entity;
using CYQ.Data.Table;
using CYQ.Data;
namespace DBFast_Demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            AppDebug.Start();
        }
        private void OutMsg()
        {
            rtxtSql.Text = AppDebug.Info;
            AppDebug.Stop();
            AppDebug.Start();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            LoadData();
            OutMsg();
        }
        private void LoadData()
        {
            using (UsersBean ub = new UsersBean())
            {
                dgvUsers.DataSource = ub.Select<UsersBean>();
            }
        }

        private void btnFill_Click(object sender, EventArgs e)
        {
            using (UsersBean ub = new UsersBean())
            {
                if (ub.Fill(txtUserID))
                {
                    MDataRow.CreateFrom(ub).SetToAll(this);
                }
            }
            OutMsg();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            using (UsersBean ub = new UsersBean())
            {
                MDataRow row = MDataRow.CreateFrom(ub);
                row.LoadFrom(false, this);
                row.SetToEntity(ub);
                ub.Update();
            }
            LoadData();
            OutMsg();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            using (UsersBean ub = new UsersBean())
            {
                ub.Delete(txtUserID);
            }
            LoadData();
            OutMsg();
        }

        private void btn_Click(object sender, EventArgs e)
        {
            using (UsersBean ub = new UsersBean())
            {
                ub.Exists(txtName);
            }
            OutMsg();
        }

        private void btnInsert_Click(object sender, EventArgs e)
        {
            using (UsersBean ub = new UsersBean())
            {
                MDataRow row = MDataRow.CreateFrom(ub);
                row.LoadFrom(false, this);
                row.SetToEntity(ub);
                if (ub.Insert(InsertOp.ID, chbInsertID.Checked))
                {
                    row.SetToAll(this);
                }
            }
            LoadData();
            OutMsg();
        }
    }
}
