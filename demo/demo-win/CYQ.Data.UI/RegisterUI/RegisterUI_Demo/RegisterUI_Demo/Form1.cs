using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CYQ.Data.UI;
using CYQ.Data.Table;
namespace RegisterUI_Demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            RegisterUI.Add("MyControl", "Text");//控件类名，属性
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            MDataRow row = new MDataRow();
            row.LoadFrom("{name:'现在是夜里快3点了！'}");
            row.SetToAll(this);//第三方控件，注册类名和属性之后，也能用自动赋值了。
        }
    }
}
