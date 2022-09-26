using CYQ.Data.Table;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace IUIValue_Demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            MDataRow row = new MDataRow();
            row.LoadFrom("{name:'妹子你在哪？'}");
            row.SetToAll(this);//继承自IUIValue的接口的控件，都可以用自动取值或赋值功能。
        }


    }
}
