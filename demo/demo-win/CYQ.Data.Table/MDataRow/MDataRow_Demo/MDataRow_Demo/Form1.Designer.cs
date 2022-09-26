namespace MDataRow_Demo
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
            this.btnToJson = new System.Windows.Forms.Button();
            this.btnGetFromAll = new System.Windows.Forms.Button();
            this.BtnSetToAll = new System.Windows.Forms.Button();
            this.rtxtText = new System.Windows.Forms.RichTextBox();
            this.txtCreateTime = new System.Windows.Forms.TextBox();
            this.txtName = new System.Windows.Forms.TextBox();
            this.txtID = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.dgvRow = new System.Windows.Forms.DataGridView();
            this.BtnToEntity = new System.Windows.Forms.Button();
            this.btnSetToEntity = new System.Windows.Forms.Button();
            this.btnLoadFrom = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRow)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnLoadFrom);
            this.panel1.Controls.Add(this.btnSetToEntity);
            this.panel1.Controls.Add(this.BtnToEntity);
            this.panel1.Controls.Add(this.btnToJson);
            this.panel1.Controls.Add(this.btnGetFromAll);
            this.panel1.Controls.Add(this.BtnSetToAll);
            this.panel1.Controls.Add(this.rtxtText);
            this.panel1.Controls.Add(this.txtCreateTime);
            this.panel1.Controls.Add(this.txtName);
            this.panel1.Controls.Add(this.txtID);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(302, 377);
            this.panel1.TabIndex = 0;
            // 
            // btnToJson
            // 
            this.btnToJson.Location = new System.Drawing.Point(209, 132);
            this.btnToJson.Name = "btnToJson";
            this.btnToJson.Size = new System.Drawing.Size(75, 23);
            this.btnToJson.TabIndex = 4;
            this.btnToJson.Text = "ToJson";
            this.btnToJson.UseVisualStyleBackColor = true;
            this.btnToJson.Click += new System.EventHandler(this.btnToJson_Click);
            // 
            // btnGetFromAll
            // 
            this.btnGetFromAll.Location = new System.Drawing.Point(108, 132);
            this.btnGetFromAll.Name = "btnGetFromAll";
            this.btnGetFromAll.Size = new System.Drawing.Size(84, 23);
            this.btnGetFromAll.TabIndex = 3;
            this.btnGetFromAll.Text = "GetFromAll";
            this.btnGetFromAll.UseVisualStyleBackColor = true;
            this.btnGetFromAll.Click += new System.EventHandler(this.btnGetFromAll_Click);
            // 
            // BtnSetToAll
            // 
            this.BtnSetToAll.Location = new System.Drawing.Point(12, 132);
            this.BtnSetToAll.Name = "BtnSetToAll";
            this.BtnSetToAll.Size = new System.Drawing.Size(75, 23);
            this.BtnSetToAll.TabIndex = 3;
            this.BtnSetToAll.Text = "SetToAll";
            this.BtnSetToAll.UseVisualStyleBackColor = true;
            this.BtnSetToAll.Click += new System.EventHandler(this.BtnSetToAll_Click);
            // 
            // rtxtText
            // 
            this.rtxtText.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.rtxtText.Location = new System.Drawing.Point(0, 250);
            this.rtxtText.Name = "rtxtText";
            this.rtxtText.Size = new System.Drawing.Size(302, 127);
            this.rtxtText.TabIndex = 2;
            this.rtxtText.Text = "";
            // 
            // txtCreateTime
            // 
            this.txtCreateTime.Location = new System.Drawing.Point(108, 93);
            this.txtCreateTime.Name = "txtCreateTime";
            this.txtCreateTime.Size = new System.Drawing.Size(121, 21);
            this.txtCreateTime.TabIndex = 1;
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(108, 56);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(121, 21);
            this.txtName.TabIndex = 1;
            // 
            // txtID
            // 
            this.txtID.Location = new System.Drawing.Point(108, 13);
            this.txtID.Name = "txtID";
            this.txtID.Size = new System.Drawing.Size(121, 21);
            this.txtID.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(25, 96);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(77, 12);
            this.label3.TabIndex = 0;
            this.label3.Text = "CreateTime：";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(25, 59);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 12);
            this.label2.TabIndex = 0;
            this.label2.Text = "Name：";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(25, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "ID：";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.dgvRow);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(302, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(283, 377);
            this.panel2.TabIndex = 1;
            // 
            // dgvRow
            // 
            this.dgvRow.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvRow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvRow.Location = new System.Drawing.Point(0, 0);
            this.dgvRow.Name = "dgvRow";
            this.dgvRow.RowTemplate.Height = 23;
            this.dgvRow.Size = new System.Drawing.Size(283, 377);
            this.dgvRow.TabIndex = 0;
            // 
            // BtnToEntity
            // 
            this.BtnToEntity.Location = new System.Drawing.Point(13, 162);
            this.BtnToEntity.Name = "BtnToEntity";
            this.BtnToEntity.Size = new System.Drawing.Size(75, 23);
            this.BtnToEntity.TabIndex = 5;
            this.BtnToEntity.Text = "ToEntity";
            this.BtnToEntity.UseVisualStyleBackColor = true;
            this.BtnToEntity.Click += new System.EventHandler(this.BtnToEntity_Click);
            // 
            // btnSetToEntity
            // 
            this.btnSetToEntity.Location = new System.Drawing.Point(108, 161);
            this.btnSetToEntity.Name = "btnSetToEntity";
            this.btnSetToEntity.Size = new System.Drawing.Size(85, 23);
            this.btnSetToEntity.TabIndex = 6;
            this.btnSetToEntity.Text = "SetToEntity";
            this.btnSetToEntity.UseVisualStyleBackColor = true;
            this.btnSetToEntity.Click += new System.EventHandler(this.btnSetToEntity_Click);
            // 
            // btnLoadFrom
            // 
            this.btnLoadFrom.Location = new System.Drawing.Point(209, 161);
            this.btnLoadFrom.Name = "btnLoadFrom";
            this.btnLoadFrom.Size = new System.Drawing.Size(75, 23);
            this.btnLoadFrom.TabIndex = 7;
            this.btnLoadFrom.Text = "LoadFrom";
            this.btnLoadFrom.UseVisualStyleBackColor = true;
            this.btnLoadFrom.Click += new System.EventHandler(this.btnLoadFrom_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(585, 377);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "Form1";
            this.Text = "MDataRow Demo";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvRow)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.DataGridView dgvRow;
        private System.Windows.Forms.RichTextBox rtxtText;
        private System.Windows.Forms.TextBox txtCreateTime;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.TextBox txtID;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnToJson;
        private System.Windows.Forms.Button btnGetFromAll;
        private System.Windows.Forms.Button BtnSetToAll;
        private System.Windows.Forms.Button btnSetToEntity;
        private System.Windows.Forms.Button BtnToEntity;
        private System.Windows.Forms.Button btnLoadFrom;
    }
}

