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

namespace MDataColumn_Demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            AppConfig.Cache.IsAutoCache = false;
        }
        MDataColumn mdc;
        private void Form1_Load(object sender, EventArgs e)
        {
            ViewColumns();
        }
        private void ViewColumns()
        {

            using (MAction action = new MAction("Users"))//实例后就可以拿到表的结构了。
            {
                mdc = action.Data.Columns;
            }

            mdc.ToTable().Bind(dgvData);
        }

        private void btnToJson_Click(object sender, EventArgs e)
        {
            rtxtMsg.Text = mdc.ToJson(true);
        }

        private void btnSetOrdinal_Click(object sender, EventArgs e)
        {
            //把第二位的列移前
            mdc.SetOrdinal(mdc[1].ColumnName, 0);
            if (mdc.Table != null && mdc.Table.Rows.Count > 1)
            {
                new MDataTable().Bind(dgvData);
                MDataTable dt = mdc.Table.Clone();
                dt.Bind(dgvData);
            }
            else
            {
                mdc.ToTable().Bind(dgvData);
            }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            mdc = new MDataColumn();
            mdc.Add("ID", SqlDbType.Int, true, false, 11, true, null);
            mdc.Add("Name");
            mdc.ToTable().Bind(dgvData);
        }

        private void btnCreateData_Click(object sender, EventArgs e)
        {
            MDataTable dt = new MDataTable("MyTable");
            dt.Columns.Add("ID", SqlDbType.Int, true, false, 11, true, null);
            dt.Columns.Add("Name");
            dt.Columns.Add("Password");
            mdc = dt.Columns;
            for (int i = 0; i < 10; i++)
            {
                dt.NewRow(true).Set(0, i + 1).Set(1, "Name" + i).Set(2, i);
            }
            dt.Bind(dgvData);
        }

        private void btnDefault_Click(object sender, EventArgs e)
        {
            ViewColumns();
        }
    }
}
