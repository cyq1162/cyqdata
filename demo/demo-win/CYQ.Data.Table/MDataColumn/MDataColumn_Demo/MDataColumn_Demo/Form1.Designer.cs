namespace MDataColumn_Demo
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.dgvData = new System.Windows.Forms.DataGridView();
            this.btnToJson = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.rtxtMsg = new System.Windows.Forms.RichTextBox();
            this.btnSetOrdinal = new System.Windows.Forms.Button();
            this.btnCreate = new System.Windows.Forms.Button();
            this.btnCreateData = new System.Windows.Forms.Button();
            this.btnDefault = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvData)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.groupBox2);
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(734, 195);
            this.panel1.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.dgvData);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 195);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(734, 207);
            this.panel2.TabIndex = 1;
            // 
            // dgvData
            // 
            this.dgvData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvData.Location = new System.Drawing.Point(0, 0);
            this.dgvData.Name = "dgvData";
            this.dgvData.RowTemplate.Height = 23;
            this.dgvData.Size = new System.Drawing.Size(734, 207);
            this.dgvData.TabIndex = 0;
            // 
            // btnToJson
            // 
            this.btnToJson.Location = new System.Drawing.Point(12, 30);
            this.btnToJson.Name = "btnToJson";
            this.btnToJson.Size = new System.Drawing.Size(132, 23);
            this.btnToJson.TabIndex = 0;
            this.btnToJson.Text = "ToJson()";
            this.btnToJson.UseVisualStyleBackColor = true;
            this.btnToJson.Click += new System.EventHandler(this.btnToJson_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.dataGridView1);
            this.groupBox1.Controls.Add(this.btnDefault);
            this.groupBox1.Controls.Add(this.btnCreateData);
            this.groupBox1.Controls.Add(this.btnCreate);
            this.groupBox1.Controls.Add(this.btnSetOrdinal);
            this.groupBox1.Controls.Add(this.btnToJson);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(472, 195);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Function";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.rtxtMsg);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(472, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(262, 195);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "OutMsgs";
            // 
            // rtxtMsg
            // 
            this.rtxtMsg.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtxtMsg.Location = new System.Drawing.Point(3, 17);
            this.rtxtMsg.Name = "rtxtMsg";
            this.rtxtMsg.Size = new System.Drawing.Size(256, 175);
            this.rtxtMsg.TabIndex = 0;
            this.rtxtMsg.Text = "";
            // 
            // btnSetOrdinal
            // 
            this.btnSetOrdinal.Location = new System.Drawing.Point(176, 30);
            this.btnSetOrdinal.Name = "btnSetOrdinal";
            this.btnSetOrdinal.Size = new System.Drawing.Size(87, 23);
            this.btnSetOrdinal.TabIndex = 1;
            this.btnSetOrdinal.Text = "SetOrdinal()";
            this.btnSetOrdinal.UseVisualStyleBackColor = true;
            this.btnSetOrdinal.Click += new System.EventHandler(this.btnSetOrdinal_Click);
            // 
            // btnCreate
            // 
            this.btnCreate.Location = new System.Drawing.Point(12, 106);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(132, 23);
            this.btnCreate.TabIndex = 2;
            this.btnCreate.Text = "CreateMDataColumn";
            this.btnCreate.UseVisualStyleBackColor = true;
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);
            // 
            // btnCreateData
            // 
            this.btnCreateData.Location = new System.Drawing.Point(12, 145);
            this.btnCreateData.Name = "btnCreateData";
            this.btnCreateData.Size = new System.Drawing.Size(132, 23);
            this.btnCreateData.TabIndex = 3;
            this.btnCreateData.Text = "CreateAdnAddData";
            this.btnCreateData.UseVisualStyleBackColor = true;
            this.btnCreateData.Click += new System.EventHandler(this.btnCreateData_Click);
            // 
            // btnDefault
            // 
            this.btnDefault.Location = new System.Drawing.Point(12, 68);
            this.btnDefault.Name = "btnDefault";
            this.btnDefault.Size = new System.Drawing.Size(132, 23);
            this.btnDefault.TabIndex = 4;
            this.btnDefault.Text = "DefaultTable";
            this.btnDefault.UseVisualStyleBackColor = true;
            this.btnDefault.Click += new System.EventHandler(this.btnDefault_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(176, 59);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowTemplate.Height = 23;
            this.dataGridView1.Size = new System.Drawing.Size(240, 109);
            this.dataGridView1.TabIndex = 5;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(734, 402);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "Form1";
            this.Text = "MDataColumn Demo";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvData)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DataGridView dgvData;
        private System.Windows.Forms.Button btnToJson;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RichTextBox rtxtMsg;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnSetOrdinal;
        private System.Windows.Forms.Button btnCreate;
        private System.Windows.Forms.Button btnCreateData;
        private System.Windows.Forms.Button btnDefault;
        private System.Windows.Forms.DataGridView dataGridView1;
    }
}

