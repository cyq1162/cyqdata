using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RegisterUI_Demo
{
    public partial class MyControl : UserControl
    {
        public MyControl()
        {
            InitializeComponent();
        }
        public string Text 
        {
            get
            {
                return txtValue.Text;
            }
            set
            {
                txtValue.Text = value;
            }
        }
    }
}
