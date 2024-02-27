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
    public partial class MDataRow
    {
        #region 提供给Emit调用的方法
        /*
         * 方法为啥用public 而不用 private
         .net 中只能为 public,用 private为因为安全选项因素而限制访问。
         .net core 允许private 调用。
         */
        /// <summary>
        /// 此方法被内部 Emit 所调用：MDataRowToEntity、MDataRowSetToEntity
        /// </summary>
        public object GetItemValue(string index)
        {
            MDataCell cell = this[index];
            if (cell == null)
            {
                return null;
            }
            if (cell.Value == null)
            {
                //有可能是 DBNull.Value
                return cell.CellValue.SourceValue;
            }
            return cell.Value;
        }

        /// <summary>
        /// 此方法被内部 Emit 所调用：MDataRowToEntity 、MDataRowSetToEntity
        /// </summary>
        public object GetItemValue(int index)
        {
            MDataCell cell = this[index];
            if (cell == null)
            {
                return null;
            }
            if (cell.Value == null)
            {
                //有可能是 DBNull.Value
                return cell.CellValue.SourceValue;
            }
            return cell.Value;
        }
        /// <summary>
        /// 此方法被内部 Emit 所调用：MDataRowLoadEntity 
        /// </summary>
        public void SetItemValue(string key, object value, int state)
        {
            MDataCell cell = this[key];
            if (cell != null)
            {
                cell.Value = value;
                cell.State = state;
                ////外部调用已进行类型转换，直接赋原始值，避免取值时重复进行类型转换。
                //cell.CellValue.SourceValue = value;
                //cell.CellValue.StringValue = Convert.ToString(value);
                //cell.CellValue.State = state;
                //cell.CellValue.IsNull = value == null || value == DBNull.Value;
            }
        }
        /// <summary>
        /// 此方法被内部 Emit 所调用：MDataRowLoadEntity
        /// </summary>
        public void SetItemValue(int index, object value, int state)
        {
            MDataCell cell = this[index];
            if (cell != null)
            {
                cell.Value = value;
                cell.State = state;
            }
        }
        #endregion
    }

}
