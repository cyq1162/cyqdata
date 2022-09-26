using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows.Forms;
using CYQ.Data;
using CYQ.Data.Table;
using System.Threading;
namespace CYQ.Data.ProjectTool
{
    public partial class OpForm : Form
    {

        public OpForm()
        {
            InitializeComponent();
            Form.CheckForIllegalCrossThreadCalls = false;
            DealWithEnglish();
            BuildCSCode.OnCreateEnd += new BuildCSCode.CreateEndHandle(BuildCSCode_OnCreateEnd);
        }
        bool isIniting = false;
        private void OpForm_Load(object sender, EventArgs e)
        {
            isIniting = true;
            ddlDBType.SelectedIndex = 0;
            ddlBuildMode.SelectedIndex = 0;
            InitConfig();
            if (string.IsNullOrEmpty(txtProjectPath.Text))
            {
                txtProjectPath.Text = Program.path;
            }
            isIniting = false;
        }
        void InitConfig()
        {
            using (ProjectConfig config = new ProjectConfig())
            {
                MDataTable dt = config.Select();
                if (dt.Rows.Count > 0)
                {
                    foreach (MDataRow row in dt.Rows)
                    {
                        ddlName.Items.Add(row.Get<string>("Name"));
                        if (row.Get<bool>("IsMain"))
                        {
                            ddlName.Text = row.Get<string>("Name");
                        }
                    }
                }
                else
                {
                    ddlName.Text = "DefaultConn";
                }
            }
        }
        void ResetMainState()
        {
            MDataTable table = null;
            using (ProjectConfig config = new ProjectConfig())
            {
                table = config.Select();//更新其它的状态。
            }
            if (table.Rows.Count > 0)
            {
                foreach (MDataRow row in table.Rows)
                {
                    row.Set("IsMain", false);
                }
                table.AcceptChanges(AcceptOp.Update);
            }
        }
        string SaveConfig()
        {
            string name = ddlName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                name = "DefaultConn";
            }
            ResetMainState();
            bool result = false;
            using (ProjectConfig config = new ProjectConfig())
            {
                config.UI.SetAutoParentControl(gbConn, gbBuild);

                if (config.Fill("Name='" + name + "'"))
                {
                    config.IsMain = true;

                    result = config.Update(null, true);
                    bool ccc = config.MutilDatabase;
                    bool bb = ccc;
                }
                else
                {
                    config.IsMain = true;
                    if (config.Insert(true))
                    {
                        ddlName.Items.Add(name);
                        result = true;
                    }
                }
            }
            if (!result)
            {
                MessageBox.Show("Save fail", "Tip");
            }
            return name;
        }

        void LoadConfig(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                using (ProjectConfig config = new ProjectConfig())
                {
                    if (config.Fill("Name='" + name + "'"))
                    {
                        config.UI.SetToAll(this);
                    }
                }
            }
        }
        private void btnTestConn_Click(object sender, EventArgs e)
        {
            btnTestConn.Enabled = false;
            Thread thread = new Thread(new ThreadStart(TestConn));
            thread.IsBackground = true;
            thread.Start();
        }

        private void TestConn()
        {
            try
            {
                string conn = txtConn.Text.Trim();
                string errMsg = string.Empty;
                if (Tool.DBTool.TestConn(conn, out errMsg))
                {
                    SaveConfig();
                    MessageBox.Show("OK!", "Tip");
                }
                else
                {
                    MessageBox.Show("Fail：" + errMsg, "Tip");
                }
            }
            catch
            { }
            finally
            {
                btnTestConn.Enabled = true;
            }
        }

        private void ddlProvider_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isIniting)
            {
                return;
            }
            int index = ddlDBType.SelectedIndex;
            switch (index)
            {
                case 0:
                    txtConn.Text = "server=.;database=demo;uid=sa;pwd=123456";
                    txtTip.Text = "";
                    break;
                case 1:
                    txtConn.Text = "Provider=MSDAORA;Data Source=ip/dbname;User ID=sa;Password=123456";
                    txtTip.Text = "";
                    break;
                case 2:
                    txtConn.Text = "host=127.0.0.1;Port=3306;Database=demo;uid=sa;pwd=123456";
                    txtTip.Text = "该功能引用：MySql.Data.dll";
                    break;
                case 3:
                    txtConn.Text = "Data Source={0}xxx.db;failifmissing=false";
                    txtTip.Text = "该功能引用：System.Data.SQLite.dll（注意x86或x64的区别）";
                    break;
                case 4:
                    txtConn.Text = "Data Source=127.0.0.1;Port=5000;UID=sa;PWD='123456';Database='Demo'";
                    txtTip.Text = "该功能引用：Sybase.AdoNet2.AseClient.dll（Sybase软件安装目录下有）";
                    break;
                case 5:
                    txtConn.Text = "Provider=Microsoft.Jet.OLEDB.4.0; Data Source={0}";
                    txtTip.Text = "";
                    break;
                case 6:
                    txtConn.Text = "Provider=Microsoft.ACE.OLEDB.12.0; Data Source={0}";
                    txtTip.Text = "";
                    break;
                case 7:
                    txtConn.Text = "Txt Path={0}App_Data";
                    txtTip.Text = "";
                    break;
                case 8:
                    txtConn.Text = "Xml Path={0}App_Data";
                    txtTip.Text = "";
                    break;
                case 9:
                    txtConn.Text = "Provider=VFPOLEDB.1;Data Source={0}xxx.dbf";
                    txtTip.Text = "";
                    break;
                case 10:
                    txtConn.Text = "server=.;port=5432;database=xx;uid=xx;pwd=xx";
                    txtTip.Text = "该功能引用：Npgsql.dll";
                    break;
                case 11:
                    txtConn.Text = "Provider=IBMDADB2.IBMDBCL1;Data Source=dbname;Persist Security Info=True;User ID=username;pwd=123456;Location=ip";
                    txtTip.Text = "该功能引用：IBM.Data.DB2.dll（DB2软件安装目录下有）";
                    break;

            }
            //}
        }

        private void ddlName_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadConfig(ddlName.Text.Trim());

        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            string sourcePath = txtProjectPath.Text.Trim();
            if (!string.IsNullOrEmpty(sourcePath) && System.IO.Directory.Exists(sourcePath))
            {
                dialog.SelectedPath = sourcePath;
            }
            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtProjectPath.Text = dialog.SelectedPath;
            }
        }

        private void btnBuild_Click(object sender, EventArgs e)
        {
            string conn = txtConn.Text.Trim();
            if (!string.IsNullOrEmpty(conn) && Tool.DBTool.TestConn(conn))
            {
                if (MessageBox.Show("To continue？", "Tip", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    string path = txtProjectPath.Text.Trim();
                    if (!System.IO.Directory.Exists(path))
                    {
                        try
                        {
                            System.IO.Directory.CreateDirectory(path);
                        }
                        catch(Exception err)
                        {
                            MessageBox.Show(err.Message, "Tip");
                            return;
                        }
                    }
                    
                    string name = SaveConfig();
                    btnBuild.Enabled = false;
                    Thread thread = new Thread(new ParameterizedThreadStart(BuildCSCode.Create));
                    thread.IsBackground = true;
                    thread.Start(name);
                }
            }
            else
            {
                MessageBox.Show("Fail！", "Tip");
            }
        }
        void BuildCSCode_OnCreateEnd(int count)
        {
            btnBuild.Enabled = true;
            MessageBox.Show("OK，Total : " + count + " tables", "Tip");
        }

        private void ddlBuildMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = ddlBuildMode.SelectedIndex;
            chbMutilDatabase.Visible = chbMapName.Visible = chbMutilDatabase.Enabled = index == 0;
            txtEntitySuffix.Visible = txtEntitySuffix.Enabled = chbForTwoOnly.Visible = chbMapName.Visible = chbForTwoOnly.Enabled = chbValueTypeNullable.Visible = chbValueTypeNullable.Enabled = index > 0;
            if (index == 0)
            {
                txtNameSpace.Text = txtNameSpace.Text.Replace("Entity", "Enum");
            }
            else
            {
                txtNameSpace.Text = txtNameSpace.Text.Replace("Enum", "Entity");
            }
        }

        private void lnkGotoUrl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            StartHttp("https://github.com/cyq1162/cyqdata");
        }

        #region Open By WebBrowser
        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        static extern IntPtr ShellExecute(
            IntPtr hwnd,
            string lpOperation,
            string lpFile,
            string lpParameters,
            string lpDirectory,
            int nShowCmd);
        /// <summary>
        /// 用默认浏览器打开网址
        /// </summary>
        protected static void StartHttp(string url)
        {
            try
            {
                ShellExecute(IntPtr.Zero, "open", url, "", "", 4);
            }
            catch
            {
                System.Diagnostics.Process.Start("IEXPLORE.EXE", url);
            }
        }
        #endregion

        private void lnkOpenFolder_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(AppDomain.CurrentDomain.BaseDirectory);
        }

        private void btnOpenProjectFolder_Click(object sender, EventArgs e)
        {
            string path=txtProjectPath.Text.Trim();
            if (!string.IsNullOrEmpty(path))
            {
                if (!System.IO.Directory.Exists(path))
                {
                    MessageBox.Show("Directory not Exists :" + path,"Tip");
                    return;
                }
                System.Diagnostics.Process.Start(path);
            }
        }

        private void lnkCopyPath_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Clipboard.SetText(Application.ExecutablePath);
            MessageBox.Show("Copy Path OK!", "Tip");
        }

        #region Deal with English
        private void DealWithEnglish()
        {
            if (Program.IsEnglish)
            {
                this.Text = Text.Replace("配置工具", "Config Tool");
                lbCodeMode.Text = "CodeMode";
                lbConn.Text = "Connection";
                lbDalType.Text = "DB Type";
                lbDefaultNameSpace.Text = "NameSpace";
                lbEntityBean.Text = "Entity SubFix";
                lbName.Text = "Name";
                lbForDbName.Text = "{0} - For DataBaseName";

                lbSavePath.Text = "Save Path";
                btnTestConn.Text = "Test Connect";
                btnBuild.Text = "Build Code";

                chbMutilDatabase.Text = "Mutil DataBase";
                chbForTwoOnly.Text = "For vs2005";
                chbMapName.Text = "Map Name";
                chbValueTypeNullable.Text = "Nullable";

                lnkCopyPath.Text = "CopyPath";
                lnkGotoUrl.Text = "Source Url";
                lnkOpenFolder.Text = "OpenFolder";

                ddlBuildMode.Items.Clear();
                ddlBuildMode.Items.Add("Enum for (MAction/MProc)");
                ddlBuildMode.Items.Add("Entity for OrmBase");
                ddlBuildMode.Items.Add("Entity for SimpleOrmBase");
                ddlBuildMode.Items.Add("Entity for DBFast");
            }
        }
        #endregion
    }

}