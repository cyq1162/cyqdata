using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CYQ.Data.Table;
using CYQ.Data;

namespace MDataCell_Demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            MDataRow row=new MDataRow();
            row.LoadFrom("{id:1,name:'hello'}");
            MDataCell cell = row[0];
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ColumnName:" + cell.ColumnName);
            sb.AppendLine("Struct.SqlType:" + cell.Struct.SqlType);
            sb.AppendLine("IsNull:" + cell.IsNull);
            sb.AppendLine("IsNullOrEmpty:" + cell.IsNullOrEmpty);
            sb.AppendLine("State:" + cell.State+" (0:不可插入和更新；1：仅可以插入；2：可插入可更新)");
            sb.AppendLine("Value:" + cell.Value);
            sb.AppendLine("------------");

            cell = row[1];
            sb.AppendLine("ColumnName:" + cell.ColumnName);
            sb.AppendLine("Struct.SqlType:" + cell.Struct.SqlType);
            sb.AppendLine("Value:" + cell.Value);
            cell.Struct.SqlType = SqlDbType.Int;//修改数据类型
            sb.AppendLine("修改结构：Struct.SqlType:" + cell.Struct.SqlType);
            sb.Append("FixValue():");
           
          
            try
            { // AppConfig.Log.IsWriteLog = true;
                Exception err;
                if (!cell.FixValue(out err))//修改该值
                {
                    sb.AppendLine(err.Message);
                }
            }
            catch(Exception er)
            {
                sb.AppendLine(er.Message);
            }
            sb.AppendLine("修正后Value:" + cell.Value);
            rtxtText.Text = sb.ToString();
        }
    }
}
