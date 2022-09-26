namespace MAction_Demo
{
    partial class 多表查询
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.btnView = new System.Windows.Forms.Button();
            this.btnSql = new System.Windows.Forms.Button();
            this.btnJoin = new System.Windows.Forms.Button();
            this.dgvView = new System.Windows.Forms.DataGridView();
            this.rtxtSql = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.dgvView)).BeginInit();
            this.SuspendLayout();
            // 
            // richTextBox1
            // 
            this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.richTextBox1.Location = new System.Drawing.Point(0, 0);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(695, 78);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = "多表操作的方式：\n1：视图\n2：自定义语句 （不能有Order By）\n3：用MDataTable的Join方法";
            // 
            // btnView
            // 
            this.btnView.Location = new System.Drawing.Point(13, 100);
            this.btnView.Name = "btnView";
            this.btnView.Size = new System.Drawing.Size(94, 23);
            this.btnView.TabIndex = 1;
            this.btnView.Text = "视图读取演示";
            this.btnView.UseVisualStyleBackColor = true;
            this.btnView.Click += new System.EventHandler(this.btnView_Click);
            // 
            // btnSql
            // 
            this.btnSql.Location = new System.Drawing.Point(144, 100);
            this.btnSql.Name = "btnSql";
            this.btnSql.Size = new System.Drawing.Size(104, 23);
            this.btnSql.TabIndex = 1;
            this.btnSql.Text = "SQL语句读取演示";
            this.btnSql.UseVisualStyleBackColor = true;
            this.btnSql.Click += new System.EventHandler(this.btnSql_Click);
            // 
            // btnJoin
            // 
            this.btnJoin.Location = new System.Drawing.Point(273, 100);
            this.btnJoin.Name = "btnJoin";
            this.btnJoin.Size = new System.Drawing.Size(158, 23);
            this.btnJoin.TabIndex = 1;
            this.btnJoin.Text = "MDataTable的Join方法读取演示";
            this.btnJoin.UseVisualStyleBackColor = true;
            this.btnJoin.Click += new System.EventHandler(this.btnJoin_Click);
            // 
            // dgvView
            // 
            this.dgvView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.dgvView.Location = new System.Drawing.Point(0, 307);
            this.dgvView.Name = "dgvView";
            this.dgvView.RowTemplate.Height = 23;
            this.dgvView.Size = new System.Drawing.Size(695, 199);
            this.dgvView.TabIndex = 2;
            // 
            // rtxtSql
            // 
            this.rtxtSql.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.rtxtSql.Location = new System.Drawing.Point(0, 145);
            this.rtxtSql.Name = "rtxtSql";
            this.rtxtSql.Size = new System.Drawing.Size(695, 162);
            this.rtxtSql.TabIndex = 3;
            this.rtxtSql.Text = "";
            // 
            // 多表查询
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(695, 506);
            this.Controls.Add(this.rtxtSql);
            this.Controls.Add(this.dgvView);
            this.Controls.Add(this.btnJoin);
            this.Controls.Add(this.btnSql);
            this.Controls.Add(this.btnView);
            this.Controls.Add(this.richTextBox1);
            this.Name = "多表查询";
            this.Text = "MAction 多表查询操作";
            ((System.ComponentModel.ISupportInitialize)(this.dgvView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button btnView;
        private System.Windows.Forms.Button btnSql;
        private System.Windows.Forms.Button btnJoin;
        private System.Windows.Forms.DataGridView dgvView;
        private System.Windows.Forms.RichTextBox rtxtSql;
    }
}