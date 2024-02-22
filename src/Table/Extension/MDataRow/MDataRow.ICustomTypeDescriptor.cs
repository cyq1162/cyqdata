using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Collections;
using System.ComponentModel;
using CYQ.Data.SQL;
using CYQ.Data.Tool;
using System.Reflection;
using System.Collections.Specialized;
using CYQ.Data.UI;


namespace CYQ.Data.Table
{
    public partial class MDataRow : System.ComponentModel.ICustomTypeDescriptor
    {
        #region ICustomTypeDescriptor ≥…‘±

        System.ComponentModel.AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return null;
        }

        string ICustomTypeDescriptor.GetClassName()
        {
            return "MDataRow";
        }

        string ICustomTypeDescriptor.GetComponentName()
        {
            return string.Empty;
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return null;
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return null;
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return null;
        }

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return null;
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return null;
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return null;
        }
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            return Create();

        }
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return Create();
        }

        object ICustomTypeDescriptor.GetPropertyOwner(System.ComponentModel.PropertyDescriptor pd)
        {
            return this;
        }
        [NonSerialized]
        PropertyDescriptorCollection properties;
        PropertyDescriptorCollection Create()
        {
            if (properties != null)
            {
                return properties;
            }
            properties = new PropertyDescriptorCollection(null);

            foreach (MDataCell mdc in this)
            {
                properties.Add(new MDataProperty(mdc, null));
            }
            return properties;
        }
        #endregion
    }

}
