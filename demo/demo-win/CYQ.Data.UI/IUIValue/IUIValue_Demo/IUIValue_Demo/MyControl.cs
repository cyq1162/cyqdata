using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace IUIValue_Demo
{
    public partial class MyControl : UserControl, CYQ.Data.UI.IUIValue
    {
        public MyControl()
        {
            InitializeComponent();
        }

        public bool MEnabled
        {
            get
            {
                return txtValue.Enabled;
            }
            set
            {
                txtValue.Enabled = value;
            }
        }

        public string MID
        {
            get
            {
                return this.Name;
            }
        }

        public object MValue
        {
            get
            {
                return txtValue.Text;
            }
            set
            {
                txtValue.Text = Convert.ToString(value);
            }
        }
    }
}
