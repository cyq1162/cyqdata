namespace MAction_Demo
{
    partial class 多表操作
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.dgvView = new System.Windows.Forms.DataGridView();
            this.rtxtSql = new System.Windows.Forms.RichTextBox();
            this.btnTransation = new System.Windows.Forms.Button();
            this.btnShowInfo = new System.Windows.Forms.Button();
            this.btnPara = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvView)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnPara);
            this.groupBox1.Controls.Add(this.btnShowInfo);
            this.groupBox1.Controls.Add(this.btnTransation);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(723, 100);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "功能区";
            // 
            // dgvView
            // 
            this.dgvView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.dgvView.Location = new System.Drawing.Point(0, 300);
            this.dgvView.Name = "dgvView";
            this.dgvView.RowTemplate.Height = 23;
            this.dgvView.Size = new System.Drawing.Size(723, 150);
            this.dgvView.TabIndex = 1;
            // 
            // rtxtSql
            // 
            this.rtxtSql.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtxtSql.Location = new System.Drawing.Point(0, 100);
            this.rtxtSql.Name = "rtxtSql";
            this.rtxtSql.Size = new System.Drawing.Size(723, 200);
            this.rtxtSql.TabIndex = 2;
            this.rtxtSql.Text = "";
            // 
            // btnTransation
            // 
            this.btnTransation.Location = new System.Drawing.Point(13, 21);
            this.btnTransation.Name = "btnTransation";
            this.btnTransation.Size = new System.Drawing.Size(123, 23);
            this.btnTransation.TabIndex = 0;
            this.btnTransation.Text = "多表切换及事务";
            this.btnTransation.UseVisualStyleBackColor = true;
            this.btnTransation.Click += new System.EventHandler(this.btnTransation_Click);
            // 
            // btnShowInfo
            // 
            this.btnShowInfo.Location = new System.Drawing.Point(161, 21);
            this.btnShowInfo.Name = "btnShowInfo";
            this.btnShowInfo.Size = new System.Drawing.Size(131, 23);
            this.btnShowInfo.TabIndex = 1;
            this.btnShowInfo.Text = "显示一些重要的属性";
            this.btnShowInfo.UseVisualStyleBackColor = true;
            this.btnShowInfo.Click += new System.EventHandler(this.btnShowInfo_Click);
            // 
            // btnPara
            // 
            this.btnPara.Location = new System.Drawing.Point(328, 21);
            this.btnPara.Name = "btnPara";
            this.btnPara.Size = new System.Drawing.Size(100, 23);
            this.btnPara.TabIndex = 2;
            this.btnPara.Text = "参数化执行";
            this.btnPara.UseVisualStyleBackColor = true;
            this.btnPara.Click += new System.EventHandler(this.btnPara_Click);
            // 
            // 多表操作
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(723, 450);
            this.Controls.Add(this.rtxtSql);
            this.Controls.Add(this.dgvView);
            this.Controls.Add(this.groupBox1);
            this.Name = "多表操作";
            this.Text = "MAction 多表操作(事务-批量）";
            this.Load += new System.EventHandler(this.多表操作_Load);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.DataGridView dgvView;
        private System.Windows.Forms.RichTextBox rtxtSql;
        private System.Windows.Forms.Button btnTransation;
        private System.Windows.Forms.Button btnShowInfo;
        private System.Windows.Forms.Button btnPara;
    }
}