namespace MAction_Demo
{
    partial class 单表操作
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
            this.dgView = new System.Windows.Forms.DataGridView();
            this.chbSelect = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtCreateTime = new System.Windows.Forms.DateTimePicker();
            this.txtEditTime = new System.Windows.Forms.DateTimePicker();
            this.btnSave = new System.Windows.Forms.Button();
            this.chbAll = new System.Windows.Forms.CheckBox();
            this.chbInsertID = new System.Windows.Forms.CheckBox();
            this.btnMutipleOperator = new System.Windows.Forms.Button();
            this.btnOpenMutipleTable = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnBatchDel = new System.Windows.Forms.Button();
            this.btnNoDelete = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnInsert = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtUserID = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btn = new System.Windows.Forms.Button();
            this.btnFill = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.rtxtSql = new System.Windows.Forms.RichTextBox();
            this.Pager = new TActionProject.PagerControl();
            ((System.ComponentModel.ISupportInitialize)(this.dgView)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgView
            // 
            this.dgView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.chbSelect});
            this.dgView.Location = new System.Drawing.Point(0, 274);
            this.dgView.Name = "dgView";
            this.dgView.RowTemplate.Height = 23;
            this.dgView.Size = new System.Drawing.Size(754, 177);
            this.dgView.TabIndex = 0;
            this.dgView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgView_CellClick);
            // 
            // chbSelect
            // 
            this.chbSelect.HeaderText = "";
            this.chbSelect.Name = "chbSelect";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txtCreateTime);
            this.groupBox1.Controls.Add(this.txtEditTime);
            this.groupBox1.Controls.Add(this.btnSave);
            this.groupBox1.Controls.Add(this.chbAll);
            this.groupBox1.Controls.Add(this.chbInsertID);
            this.groupBox1.Controls.Add(this.btnMutipleOperator);
            this.groupBox1.Controls.Add(this.btnOpenMutipleTable);
            this.groupBox1.Controls.Add(this.btnRemove);
            this.groupBox1.Controls.Add(this.btnBatchDel);
            this.groupBox1.Controls.Add(this.btnNoDelete);
            this.groupBox1.Controls.Add(this.btnDelete);
            this.groupBox1.Controls.Add(this.btnUpdate);
            this.groupBox1.Controls.Add(this.btnInsert);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.txtPassword);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.txtName);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtUserID);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.btn);
            this.groupBox1.Controls.Add(this.btnFill);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(389, 268);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Users表";
            // 
            // txtCreateTime
            // 
            this.txtCreateTime.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            this.txtCreateTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.txtCreateTime.Location = new System.Drawing.Point(84, 134);
            this.txtCreateTime.Name = "txtCreateTime";
            this.txtCreateTime.Size = new System.Drawing.Size(152, 21);
            this.txtCreateTime.TabIndex = 8;
            this.txtCreateTime.Value = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            // 
            // txtEditTime
            // 
            this.txtEditTime.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            this.txtEditTime.Enabled = false;
            this.txtEditTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.txtEditTime.Location = new System.Drawing.Point(84, 170);
            this.txtEditTime.Name = "txtEditTime";
            this.txtEditTime.Size = new System.Drawing.Size(152, 21);
            this.txtEditTime.TabIndex = 8;
            this.txtEditTime.Value = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(272, 239);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(98, 23);
            this.btnSave.TabIndex = 7;
            this.btnSave.Text = "批量保存";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // chbAll
            // 
            this.chbAll.AutoSize = true;
            this.chbAll.Location = new System.Drawing.Point(15, 246);
            this.chbAll.Name = "chbAll";
            this.chbAll.Size = new System.Drawing.Size(48, 16);
            this.chbAll.TabIndex = 6;
            this.chbAll.Text = "全选";
            this.chbAll.UseVisualStyleBackColor = true;
            this.chbAll.CheckedChanged += new System.EventHandler(this.chbAll_CheckedChanged);
            // 
            // chbInsertID
            // 
            this.chbInsertID.AutoSize = true;
            this.chbInsertID.Location = new System.Drawing.Point(15, 209);
            this.chbInsertID.Name = "chbInsertID";
            this.chbInsertID.Size = new System.Drawing.Size(84, 16);
            this.chbInsertID.TabIndex = 5;
            this.chbInsertID.Text = "手工插主键";
            this.chbInsertID.UseVisualStyleBackColor = true;
            // 
            // btnMutipleOperator
            // 
            this.btnMutipleOperator.Location = new System.Drawing.Point(308, 171);
            this.btnMutipleOperator.Name = "btnMutipleOperator";
            this.btnMutipleOperator.Size = new System.Drawing.Size(75, 23);
            this.btnMutipleOperator.TabIndex = 4;
            this.btnMutipleOperator.Text = "多表操作";
            this.btnMutipleOperator.UseVisualStyleBackColor = true;
            this.btnMutipleOperator.Click += new System.EventHandler(this.btnMutipleOperator_Click);
            // 
            // btnOpenMutipleTable
            // 
            this.btnOpenMutipleTable.Location = new System.Drawing.Point(308, 140);
            this.btnOpenMutipleTable.Name = "btnOpenMutipleTable";
            this.btnOpenMutipleTable.Size = new System.Drawing.Size(75, 23);
            this.btnOpenMutipleTable.TabIndex = 4;
            this.btnOpenMutipleTable.Text = "多表查询";
            this.btnOpenMutipleTable.UseVisualStyleBackColor = true;
            this.btnOpenMutipleTable.Click += new System.EventHandler(this.btnOpenMutipleTable_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Location = new System.Drawing.Point(161, 239);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(86, 23);
            this.btnRemove.TabIndex = 3;
            this.btnRemove.Text = "移除选中项";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // btnBatchDel
            // 
            this.btnBatchDel.Location = new System.Drawing.Point(69, 239);
            this.btnBatchDel.Name = "btnBatchDel";
            this.btnBatchDel.Size = new System.Drawing.Size(86, 23);
            this.btnBatchDel.TabIndex = 3;
            this.btnBatchDel.Text = "批量Delete";
            this.btnBatchDel.UseVisualStyleBackColor = true;
            this.btnBatchDel.Click += new System.EventHandler(this.btnBatchDel_Click);
            // 
            // btnNoDelete
            // 
            this.btnNoDelete.Location = new System.Drawing.Point(297, 92);
            this.btnNoDelete.Name = "btnNoDelete";
            this.btnNoDelete.Size = new System.Drawing.Size(86, 23);
            this.btnNoDelete.TabIndex = 3;
            this.btnNoDelete.Text = "假Delete";
            this.btnNoDelete.UseVisualStyleBackColor = true;
            this.btnNoDelete.Click += new System.EventHandler(this.btnNoDelete_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(214, 92);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(75, 23);
            this.btnDelete.TabIndex = 3;
            this.btnDelete.Text = "真Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnUpdate
            // 
            this.btnUpdate.Location = new System.Drawing.Point(295, 20);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(75, 23);
            this.btnUpdate.TabIndex = 3;
            this.btnUpdate.Text = "Update";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // btnInsert
            // 
            this.btnInsert.Location = new System.Drawing.Point(109, 202);
            this.btnInsert.Name = "btnInsert";
            this.btnInsert.Size = new System.Drawing.Size(75, 23);
            this.btnInsert.TabIndex = 3;
            this.btnInsert.Text = "Insert";
            this.btnInsert.UseVisualStyleBackColor = true;
            this.btnInsert.Click += new System.EventHandler(this.btnInsert_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(13, 176);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(53, 12);
            this.label5.TabIndex = 0;
            this.label5.Text = "EditTime";
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(84, 94);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(100, 21);
            this.txtPassword.TabIndex = 2;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(13, 140);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(65, 12);
            this.label4.TabIndex = 0;
            this.label4.Text = "CreateTime";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(84, 54);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(100, 21);
            this.txtName.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 97);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 12);
            this.label3.TabIndex = 0;
            this.label3.Text = "Password";
            // 
            // txtUserID
            // 
            this.txtUserID.Location = new System.Drawing.Point(84, 18);
            this.txtUserID.Name = "txtUserID";
            this.txtUserID.Size = new System.Drawing.Size(100, 21);
            this.txtUserID.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 57);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 12);
            this.label2.TabIndex = 0;
            this.label2.Text = "Name";
            // 
            // btn
            // 
            this.btn.Location = new System.Drawing.Point(212, 54);
            this.btn.Name = "btn";
            this.btn.Size = new System.Drawing.Size(75, 23);
            this.btn.TabIndex = 1;
            this.btn.Text = "Exists";
            this.btn.UseVisualStyleBackColor = true;
            this.btn.Click += new System.EventHandler(this.btn_Click);
            // 
            // btnFill
            // 
            this.btnFill.Location = new System.Drawing.Point(212, 18);
            this.btnFill.Name = "btnFill";
            this.btnFill.Size = new System.Drawing.Size(75, 23);
            this.btnFill.TabIndex = 1;
            this.btnFill.Text = "Fill";
            this.btnFill.UseVisualStyleBackColor = true;
            this.btnFill.Click += new System.EventHandler(this.btnFill_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(17, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "PK";
            // 
            // rtxtSql
            // 
            this.rtxtSql.Location = new System.Drawing.Point(389, 0);
            this.rtxtSql.Name = "rtxtSql";
            this.rtxtSql.Size = new System.Drawing.Size(365, 268);
            this.rtxtSql.TabIndex = 2;
            this.rtxtSql.Text = "";
            // 
            // Pager
            // 
            this.Pager.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.Pager.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.Pager.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(78)))), ((int)(((byte)(151)))));
            this.Pager.JumpText = "Go";
            this.Pager.Location = new System.Drawing.Point(0, 448);
            this.Pager.Name = "Pager";
            this.Pager.PageIndex = 1;
            this.Pager.PageSize = 30;
            this.Pager.RecordCount = 0;
            this.Pager.Size = new System.Drawing.Size(754, 29);
            this.Pager.TabIndex = 3;
            // 
            // 单表操作
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(754, 477);
            this.Controls.Add(this.Pager);
            this.Controls.Add(this.rtxtSql);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.dgView);
            this.Name = "单表操作";
            this.Text = "MAction 单表操作演示 - 数据库SQLite";
            this.Load += new System.EventHandler(this.单表操作_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgView)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgView;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox rtxtSql;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnInsert;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtUserID;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnFill;
        private System.Windows.Forms.Button btnNoDelete;
        private System.Windows.Forms.Button btnOpenMutipleTable;
        private System.Windows.Forms.Button btn;
        private System.Windows.Forms.Button btnMutipleOperator;
        private System.Windows.Forms.CheckBox chbInsertID;
        private TActionProject.PagerControl Pager;
        private System.Windows.Forms.CheckBox chbAll;
        private System.Windows.Forms.DataGridViewCheckBoxColumn chbSelect;
        private System.Windows.Forms.Button btnBatchDel;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.DateTimePicker txtEditTime;
        private System.Windows.Forms.DateTimePicker txtCreateTime;

    }
}

