using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Xml;
using System.IO;
using System.Data.Common;
using System.ComponentModel;
using CYQ.Data.UI;
using CYQ.Data.Cache;
using CYQ.Data.SQL;
using CYQ.Data.Tool;
using System.Collections.Specialized;
using System.Web;
using CYQ.Data.Json;
using CYQ.Data.Orm;
using CYQ.Data.Aop;

namespace CYQ.Data.Table
{

    public partial class MDataTable
    {

        #region �е�ȡֵ��Min��Max��Sum��Avg��Distinct�������ظ��У���Pivot������ת����
        private T GetMinMaxValue<T>(int index, bool isMin)
        {
            if (Columns != null && index < Columns.Count && Rows != null && Rows.Count > 0)
            {
                List<T> itemList = GetColumnItems<T>(index, BreakOp.NullOrEmpty);
                if (itemList.Count > 0)
                {
                    itemList.Sort();
                    return isMin ? itemList[0] : itemList[itemList.Count - 1];
                }
            }

            return default(T);
        }
        /// <summary>
        /// ��ȡ�е���Сֵ
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <param name="columnName">����</param>
        /// <returns></returns>
        public T Min<T>(string columnName)
        {
            return GetMinMaxValue<T>(Columns.GetIndex(columnName), true);
        }
        /// <summary>
        /// ��ȡ�е���Сֵ
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <param name="index">������</param>
        /// <returns></returns>
        public T Min<T>(int index)
        {
            return GetMinMaxValue<T>(index, true);
        }

        /// <summary>
        /// ��ȡ�е����ֵ
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <param name="columnName">����</param>
        /// <returns></returns>
        public T Max<T>(string columnName)
        {
            return GetMinMaxValue<T>(Columns.GetIndex(columnName), false);
        }
        /// <summary>
        /// ��ȡ�е����ֵ
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <param name="index">������</param>
        /// <returns></returns>
        public T Max<T>(int index)
        {
            return GetMinMaxValue<T>(index, false);
        }
        /// <summary>
        /// ����ĳ�е�ֵ
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <param name="columnName">����</param>
        /// <returns></returns>
        public T Sum<T>(string columnName)
        {
            return Sum<T>(Columns.GetIndex(columnName));
        }

        /// <summary>
        /// ����ĳ�е�ֵ
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <param name="index">������</param>
        /// <returns></returns>
        public T Sum<T>(int index)
        {
            if (Columns != null && index < Columns.Count && Rows != null && Rows.Count > 0)
            {
                List<Decimal> itemList = GetColumnItems<Decimal>(index, BreakOp.NullOrEmpty);
                if (itemList.Count > 0)
                {
                    Decimal sum = 0;
                    for (int i = 0; i < itemList.Count; i++)
                    {
                        sum += itemList[i];
                    }
                    return (T)ConvertTool.ChangeType(sum, typeof(T));
                }
            }
            return default(T);
        }
        /// <summary>
        /// ����ĳ�е�ƽ��ֵ
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <param name="columnName">����</param>
        /// <returns></returns>
        public T Avg<T>(string columnName)
        {
            return Avg<T>(Columns.GetIndex(columnName));
        }

        /// <summary>
        ///����ĳ�е�ƽ��ֵ
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <param name="index">������</param>
        /// <returns></returns>
        public T Avg<T>(int index)
        {
            Decimal sum = Sum<Decimal>(index);
            if (sum > 0)
            {
                return (T)ConvertTool.ChangeType(sum / Rows.Count, typeof(T));
            }
            return default(T);
        }

        /// <summary>
        /// ��ת���У���ָ��ʱ��Ĭ��ȡ������д���
        /// </summary>
        public MDataTable Pivot()
        {
            if (Columns.Count < 3)
            {
                Error.Throw("At least three columns when call Pivot()");
            }
            int count = Columns.Count;
            return Pivot(Columns[count - 3].ColumnName, Columns[count - 2].ColumnName, Columns[count - 1].ColumnName);
        }
        /// <summary>
        /// ��ת����
        /// </summary>
        /// <param name="rowName">����ָ���е�����</param>
        /// <param name="colName">���ڷֲ���е�����</param>
        /// <param name="valueName">������ʾֵ������</param>
        /// <returns></returns>
        public MDataTable Pivot(string rowName, string colName, string valueName)
        {
            MDataTable dt = new MDataTable(TableName);

            #region ������ͷ
            List<string> colNameItems = GetColumnItems<string>(colName, BreakOp.NullOrEmpty, true);
            if (colNameItems == null || colNameItems.Count == 0 || colNameItems.Count > 255)
            {
                return dt;
            }
            dt.Columns.Add(rowName);
            for (int i = 0; i < colNameItems.Count; i++)
            {
                dt.Columns.Add(colNameItems[i]);
            }

            #endregion

            #region ��������
            List<string> rowNameItems = GetColumnItems<string>(rowName, BreakOp.None, true);
            MDataTable splitTable = this;
            for (int i = 0; i < rowNameItems.Count; i++)
            {
                MDataRow nameRow = dt.NewRow(true).Set(0, rowNameItems[i]);//�±��һ��
                MDataTable[] dt2 = splitTable.Split(rowName + "='" + rowNameItems[i] + "'");//ɸѡ�ָ�
                splitTable = dt2[1];//ʣ�µ���Ϊ�´ηָ�

                foreach (MDataRow row in dt2[0].Rows)//��д����
                {
                    if (!row[colName].IsNullOrEmpty)//��������Ϊ�ջ�Null
                    {
                        nameRow.Set(row[colName].Value, row[valueName].Value);
                    }
                }
            }
            #endregion
            return dt;
        }
        

        /// <summary>
        /// ���˵��ظ��������У����Ƚϻ������͡������������ͽ��Ƚ��������ƣ���
        /// <param name="filterRows">�����˵����ݼ���</param>
        /// </summary>
        public void Distinct(out MDataTable filterRows)
        {
            Distinct(out filterRows, true);
        }
        private void Distinct(out MDataTable filterRows, bool isNeedOut)
        {
            filterRows = null;
            if (Rows.Count > 0)
            {
                List<MDataRow> rowList = new List<MDataRow>();
                int cCount = Columns.Count;
                for (int i = 0; i < Rows.Count; i++)
                {
                    for (int j = Rows.Count - 1; j >= 0 && j != i; j--)//�����⡣
                    {
                        int eqCount = 0;
                        for (int k = 0; k < cCount; k++)//�Ƚ���
                        {
                            if (Rows[i][k].StringValue == Rows[j][k].StringValue)
                            {
                                eqCount++;
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (eqCount == cCount)
                        {
                            if (isNeedOut)
                            {
                                rowList.Add(Rows[j]);
                            }
                            Rows.RemoveAt(j);
                        }
                    }
                }
                if (rowList.Count > 0)
                {
                    filterRows = rowList;
                    filterRows.Columns = filterRows.Columns.Clone();//����ͷ������
                    Columns._Table = this;
                }
            }
        }
        /// <summary>
        /// ���˵��ظ��������У����Ƚϻ������͡������������ͽ��Ƚ��������ƣ���
        /// </summary>
        public void Distinct()
        {
            MDataTable filterRows;
            Distinct(out filterRows, false);
            filterRows = null;
        }

        #endregion


    }
}
