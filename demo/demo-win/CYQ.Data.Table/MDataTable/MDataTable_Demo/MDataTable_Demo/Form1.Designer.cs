namespace MDataTable_Demo
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
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.dgvTable = new System.Windows.Forms.DataGridView();
            this.rtxtText = new System.Windows.Forms.RichTextBox();
            this.btnTo = new System.Windows.Forms.Button();
            this.btnShow = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnGetUpdate = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTable)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.panel4);
            this.panel1.Controls.Add(this.panel3);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(736, 254);
            this.panel1.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.dgvTable);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 254);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(736, 233);
            this.panel2.TabIndex = 1;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.rtxtText);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(736, 254);
            this.panel3.TabIndex = 0;
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.richTextBox1);
            this.panel4.Controls.Add(this.button1);
            this.panel4.Controls.Add(this.btnGetUpdate);
            this.panel4.Controls.Add(this.btnUpdate);
            this.panel4.Controls.Add(this.btnShow);
            this.panel4.Controls.Add(this.btnTo);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel4.Location = new System.Drawing.Point(0, 0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(360, 254);
            this.panel4.TabIndex = 1;
            // 
            // dgvTable
            // 
            this.dgvTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvTable.Location = new System.Drawing.Point(0, 0);
            this.dgvTable.Name = "dgvTable";
            this.dgvTable.RowTemplate.Height = 23;
            this.dgvTable.Size = new System.Drawing.Size(736, 233);
            this.dgvTable.TabIndex = 0;
            // 
            // rtxtText
            // 
            this.rtxtText.Location = new System.Drawing.Point(366, 0);
            this.rtxtText.Name = "rtxtText";
            this.rtxtText.Size = new System.Drawing.Size(367, 248);
            this.rtxtText.TabIndex = 0;
            this.rtxtText.Text = "123";
            // 
            // btnTo
            // 
            this.btnTo.Location = new System.Drawing.Point(13, 13);
            this.btnTo.Name = "btnTo";
            this.btnTo.Size = new System.Drawing.Size(201, 23);
            this.btnTo.TabIndex = 0;
            this.btnTo.Text = "Table=>A=>Table（各种互转演示）";
            this.btnTo.UseVisualStyleBackColor = true;
            this.btnTo.Click += new System.EventHandler(this.btnTo_Click);
            // 
            // btnShow
            // 
            this.btnShow.Location = new System.Drawing.Point(13, 53);
            this.btnShow.Name = "btnShow";
            this.btnShow.Size = new System.Drawing.Size(335, 23);
            this.btnShow.TabIndex = 1;
            this.btnShow.Text = "行列转换、（不同）多表关联、（相同）多表数据合并等）";
            this.btnShow.UseVisualStyleBackColor = true;
            this.btnShow.Click += new System.EventHandler(this.btnShow_Click);
            // 
            // btnUpdate
            // 
            this.btnUpdate.Location = new System.Drawing.Point(139, 206);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(75, 23);
            this.btnUpdate.TabIndex = 2;
            this.btnUpdate.Text = "批量更新";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // btnGetUpdate
            // 
            this.btnGetUpdate.Location = new System.Drawing.Point(13, 206);
            this.btnGetUpdate.Name = "btnGetUpdate";
            this.btnGetUpdate.Size = new System.Drawing.Size(120, 23);
            this.btnGetUpdate.TabIndex = 3;
            this.btnGetUpdate.Text = "获取修改过的数据";
            this.btnGetUpdate.UseVisualStyleBackColor = true;
            this.btnGetUpdate.Click += new System.EventHandler(this.btnGetUpdate_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(261, 13);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(87, 23);
            this.button1.TabIndex = 4;
            this.button1.Text = "读数据库所有";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(13, 82);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(335, 118);
            this.richTextBox1.TabIndex = 5;
            this.richTextBox1.Text = "常用方法：\nMDataTable.CreateFrom方法可以批量接收数据。\nAcceptChanges方法可以批量提交到数据库。\n每个单元格都有State状态（" +
    "是否插入或更新由状态决定）\n这在有些情况，需要：SetState来变更值的状态，才能继续后续的操作。";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(736, 487);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "Form1";
            this.Text = "MDataTable Demo";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTable)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DataGridView dgvTable;
        private System.Windows.Forms.RichTextBox rtxtText;
        private System.Windows.Forms.Button btnTo;
        private System.Windows.Forms.Button btnShow;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnGetUpdate;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.RichTextBox richTextBox1;
    }
}

