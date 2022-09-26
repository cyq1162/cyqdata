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
            dgvUsers.DataSource = DBFast.Select<UsersBean>();
        }

        private void btnFill_Click(object sender, EventArgs e)
        {
            UsersBean ub = DBFast.Find<UsersBean>(txtUserID);
            MDataRow.CreateFrom(ub).SetToAll(this);
            OutMsg();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            MDataRow row = MDataRow.CreateFrom(new UsersBean());
            row.LoadFrom(false, this);

            DBFast.Update<UsersBean>(row.ToEntity<UsersBean>());
            LoadData();
            OutMsg();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            DBFast.Delete<UsersBean>(txtUserID);
            LoadData();
            OutMsg();
        }

        private void btn_Click(object sender, EventArgs e)
        {
            DBFast.Exists<UsersBean>(txtName);
            OutMsg();
        }

        private void btnInsert_Click(object sender, EventArgs e)
        {
            MDataRow row = MDataRow.CreateFrom(new UsersBean());
            row.LoadFrom(false, this);
            DBFast.Insert<UsersBean>(row.ToEntity<UsersBean>(), InsertOp.ID, chbInsertID.Checked);
            LoadData();
            OutMsg();
        }
    }
}
