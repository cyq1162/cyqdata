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
        #region �ṩ��Emit���õķ���
        /*
         * ����Ϊɶ��public ������ private
         .net ��ֻ��Ϊ public,�� privateΪ��Ϊ��ȫѡ�����ض����Ʒ��ʡ�
         .net core ����private ���á�
         */
        /// <summary>
        /// �˷������ڲ� Emit �����ã�MDataRowToEntity��MDataRowSetToEntity
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
                //�п����� DBNull.Value
                return cell.CellValue.SourceValue;
            }
            return cell.Value;
        }

        /// <summary>
        /// �˷������ڲ� Emit �����ã�MDataRowToEntity ��MDataRowSetToEntity
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
                //�п����� DBNull.Value
                return cell.CellValue.SourceValue;
            }
            return cell.Value;
        }
        /// <summary>
        /// �˷������ڲ� Emit �����ã�MDataRowLoadEntity 
        /// </summary>
        public void SetItemValue(string key, object value, int state)
        {
            MDataCell cell = this[key];
            if (cell != null)
            {
                cell.Value = value;
                cell.State = state;
                ////�ⲿ�����ѽ�������ת����ֱ�Ӹ�ԭʼֵ������ȡֵʱ�ظ���������ת����
                //cell.CellValue.SourceValue = value;
                //cell.CellValue.StringValue = Convert.ToString(value);
                //cell.CellValue.State = state;
                //cell.CellValue.IsNull = value == null || value == DBNull.Value;
            }
        }
        /// <summary>
        /// �˷������ڲ� Emit �����ã�MDataRowLoadEntity
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
