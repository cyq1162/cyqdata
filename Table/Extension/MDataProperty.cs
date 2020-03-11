using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Table
{
    /// <summary>
    /// 实现Winform的表格控件绑定功能。
    /// </summary>
    internal partial class MDataProperty : System.ComponentModel.PropertyDescriptor
    {
        private MDataCell cell = null;
        public MDataProperty(MDataCell mdc, Attribute[] attrs)
            : base(mdc.ColumnName, attrs)
        {
            cell = mdc;
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }
        public override Type ComponentType
        {
            get
            {
                return typeof(MDataCell);
            }
        }
        public override object GetValue(object component)
        {
            return ((MDataRow)component)[cell.ColumnName].Value;

        }

        public override bool IsReadOnly
        {
            get
            {
                return false;
            }
        }
        public override void ResetValue(object component)
        {

        }
        //[NonSerialized]
        public override Type PropertyType
        {
            get { return cell.Struct.ValueType; }
        }

        public override void SetValue(object component, object value)
        {
            ((MDataRow)component)[cell.ColumnName].Value = value;
        }

        public override bool ShouldSerializeValue(object component)
        {
            return true;
        }
        public override bool IsBrowsable
        {
            get
            {
                return true;
            }
        }
        public override string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(cell.Struct.Description))
                {
                    return cell.Struct.Description;
                }
                return cell.ColumnName;
            }
        }
        
    }
}
