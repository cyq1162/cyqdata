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
namespace MDataRow_Demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        MDataRow row;
        private void Form1_Load(object sender, EventArgs e)
        {
            CreateRow();
            row.ToTable().Bind(dgvRow);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Row：");
            sb.AppendLine("TableName：" + row.TableName);
            sb.AppendLine("Columns.Count：" + row.Columns.Count);
            sb.AppendLine("Conn：" + row.Conn);
            sb.AppendLine("PrimaryCell.ColumnName：" + row.PrimaryCell.ColumnName);
            sb.AppendLine("JointPrimaryCell.Count：" + row.JointPrimaryCell.Count);
            sb.AppendLine("RowError：" + row.RowError);
            rtxtText.Text = sb.ToString();
        }
        void CreateRow()
        {
            MDataColumn mdc = new MDataColumn();
            mdc.Add("ID", SqlDbType.Int, true);
            mdc.Add("Name");
            mdc.Add("CreateTime", SqlDbType.DateTime);
            row = new MDataRow(mdc);
            row.Set(0, 1).Set(1, "hello").Set(2, DateTime.Now);
            row.SetToAll(this);

        }

        private void BtnSetToAll_Click(object sender, EventArgs e)
        {
            row.SetToAll(this);
        }

        private void btnGetFromAll_Click(object sender, EventArgs e)
        {
            row.SetState(0);
            row.LoadFrom(false, this);//该方法可以从各式各样的场景中批量获得值，如Json,entity,xml,dictionary等
            row.ToTable().Bind(dgvRow);
        }

        private void btnToJson_Click(object sender, EventArgs e)
        {
            rtxtText.Text = row.ToJson();
        }

        private void BtnToEntity_Click(object sender, EventArgs e)
        {
            Entity et = row.ToEntity<Entity>();
            rtxtText.Text = "ToEntity:" + GetText(et); ;
        }
        private string GetText(Entity e)
        {
            string s = e.GetType().Name;
            s += "\r\nID：" + e.ID;
            s += "\r\nName：" + e.Name;
            s += "\r\nCreateTime：" + e.CreateTime;
            return s;
        }

        private void btnSetToEntity_Click(object sender, EventArgs e)
        {
            Entity et = new Entity();
            row.SetToEntity(et);
            rtxtText.Text = "SetoToEntity:" + GetText(et);
        }

        private void btnLoadFrom_Click(object sender, EventArgs e)
        {
            row.SetState(0);//对于状态为2的Cell值，是会忽略自动取值的，只能用Set方法赋值。

            Dictionary<string, string> kv = new Dictionary<string, string>();
            kv.Add("name", Guid.NewGuid().ToString());
            row.LoadFrom(kv);
            row.SetToAll(this);

            Entity et = new Entity();
            et.ID = DateTime.Now.Second;
            et.CreateTime = DateTime.Now.AddYears(DateTime.Now.Second);
            row.LoadFrom(et, BreakOp.NullOrEmpty);//忽略掉空值或Null值
            row.ToTable().Bind(dgvRow);
        }
    }

    public class Entity
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
