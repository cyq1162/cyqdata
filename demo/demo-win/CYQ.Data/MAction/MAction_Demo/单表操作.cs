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
namespace MAction_Demo
{
    public partial class 单表操作 : Form
    {
        string tableName = "Users";
        MDataTable dt;//和表格同一引用，用于批量操作表格内容。
        public 单表操作()
        {
            AppConfig.DB.EditTimeFields = "EditTime";//该配置的字段，在更新时会自动被更新时间。
            AppConfig.DB.HiddenFields = "IsDeleted";
            InitializeComponent();
            Pager.OnPageChanged += Pager_OnPageChanged;
        }

        void Pager_OnPageChanged(object sender, EventArgs e)
        {
            LoadData();
        }



        private void 单表操作_Load(object sender, EventArgs e)
        {
            try
            {
                LoadData();
            }
            catch (Exception err)
            {

                MessageBox.Show(err.Message);
            }
            

        }
        private void LoadData()
        {
            MDataRow row;
            using (MAction action = new MAction(tableName))
            {
                row = action.Data;
                dt = action.Select(Pager.PageIndex, Pager.PageSize, "order by " + action.Data.PrimaryCell.ColumnName + " desc");
                OutDebugSql(action.DebugInfo, true);
                dt.Columns["UserID"].Description = "用户标识";//增加这一行，是想说明：Winform下通过修改描述可以改变显示的列名
            }
            using (MAction action = new MAction(row))
            {
                
            }
            if (dt != null && dt.Rows.Count > 0)
            {
                if (txtUserID.Text == "")
                {
                    dt.Rows[0].SetToAll(this);
                }
            }
            //   dgView.DataSource = dt.ToDataTable();
            // 
            dt.Bind(dgView);
            Pager.DrawControl(dt.RecordsAffected);
        }
        private void OutDebugSql(string msg)
        {
            OutDebugSql(msg, false);
        }
        private void OutDebugSql(string msg, bool isAdd)
        {
            if (string.IsNullOrEmpty(msg))
            {
                msg = "Auto Cache...";//相关的配置，如：AppConfig.Cache.IsAutoCache = false;
            }
            if (isAdd) { rtxtSql.Text = rtxtSql.Text + "\r\n" + msg; }
            else
            {
                rtxtSql.Text = msg;
            }
        }

        private void btnFill_Click(object sender, EventArgs e)
        {
            using (MAction action = new MAction(tableName))
            {
                if (action.Fill(txtUserID))
                {
                    action.UI.SetToAll(this);
                    OutDebugSql(action.DebugInfo);
                }
            }
        }

        private void btnInsert_Click(object sender, EventArgs e)
        {
            using (MAction action = new MAction(tableName))
            {
                if (!action.Exists(txtName))
                {
                    action.AllowInsertID = chbInsertID.Checked;
                    action.UI.SetAutoParentControl(this);//Web开发的不需要这条
                    if (action.Insert(true, InsertOp.ID))
                    {
                        action.UI.SetToAll(this);
                        LoadData();
                    }
                }
                OutDebugSql(action.DebugInfo);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            using (MAction action = new MAction(tableName))
            {
                action.UI.SetAutoParentControl(this);
                if (action.Update(true))
                {
                    LoadData();
                }
                OutDebugSql(action.DebugInfo);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            using (MAction action = new MAction(tableName))
            {
                if (action.Delete(txtUserID))
                {
                    LoadData();
                }
                OutDebugSql(action.DebugInfo);
            }
        }

        private void btnNoDelete_Click(object sender, EventArgs e)
        {
            AppConfig.DB.DeleteField = "IsDeleted";//演示用代码，一般配置在config对全局起使用。
            btnDelete_Click(sender, e);
            AppConfig.DB.DeleteField = "";
        }



        private void btn_Click(object sender, EventArgs e)
        {
            bool result1, result2;
            using (MAction action = new MAction(tableName))
            {
                result1 = action.Exists(txtUserID);
                result2 = action.Exists(txtName);//自动推导
                OutDebugSql(action.DebugInfo);
            }
            MessageBox.Show(result1 + "," + result2);
        }
        private void btnOpenMutipleTable_Click(object sender, EventArgs e)
        {
            多表查询 m = new 多表查询();
            m.Show();
        }
        private void btnMutipleOperator_Click(object sender, EventArgs e)
        {
            多表操作 m = new 多表操作();
            m.Show();
        }

        private void chbAll_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < dgView.Rows.Count; i++)
            {
                dgView.Rows[i].Cells[0].Value = Convert.ToString(dgView.Rows[i].Cells[0].Value) != "True";
            }
        }

        private void btnBatchDel_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < dgView.Rows.Count; i++)
            {
                if (Convert.ToString(dgView.Rows[i].Cells[0].Value) == "True")
                {
                    sb.Append(dgView.Rows[i].Cells[1].Value + ",");
                }
            }
            string where = sb.ToString().TrimEnd(',');
            bool result = false;
            if (!string.IsNullOrEmpty(where))
            {
                using (MAction action = new MAction(tableName))
                {
                    result = action.Delete(where);
                    OutDebugSql(action.DebugInfo);
                }
            }
            if (result)
            {
                LoadData();
            }
            MessageBox.Show(result.ToString(), "Result");
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            MDataTable delDt = dt.GetSchema(false);
            for (int i = 0; i < dgView.Rows.Count; i++)
            {
                if (Convert.ToString(dgView.Rows[i].Cells[0].Value) == "True")
                {
                    dgView.Rows.RemoveAt(i);
                    delDt.NewRow(true).LoadFrom(dt.Rows[i]);
                }
            }
            dgView.Refresh();
            delDt.AcceptChanges(AcceptOp.Delete);
            delDt.AcceptChanges(AcceptOp.Insert| AcceptOp.Truncate);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            bool result = dt.AcceptChanges(AcceptOp.Auto);
            MessageBox.Show(result.ToString(), "Result");
        }

        private void dgView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > -1 && e.RowIndex < dt.Rows.Count)
            {
                MDataRow row = dt.Rows[e.RowIndex];
                row.SetToAll(this);
            }
        }


    }
}
