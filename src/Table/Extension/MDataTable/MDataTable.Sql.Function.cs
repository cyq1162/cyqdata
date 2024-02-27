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

        #region 列的取值：Min、Max、Sum、Avg、Distinct（过滤重复行）、Pivot（行列转换）
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
        /// 获取列的最小值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="columnName">列名</param>
        /// <returns></returns>
        public T Min<T>(string columnName)
        {
            return GetMinMaxValue<T>(Columns.GetIndex(columnName), true);
        }
        /// <summary>
        /// 获取列的最小值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="index">列索引</param>
        /// <returns></returns>
        public T Min<T>(int index)
        {
            return GetMinMaxValue<T>(index, true);
        }

        /// <summary>
        /// 获取列的最大值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="columnName">列名</param>
        /// <returns></returns>
        public T Max<T>(string columnName)
        {
            return GetMinMaxValue<T>(Columns.GetIndex(columnName), false);
        }
        /// <summary>
        /// 获取列的最大值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="index">列索引</param>
        /// <returns></returns>
        public T Max<T>(int index)
        {
            return GetMinMaxValue<T>(index, false);
        }
        /// <summary>
        /// 汇总某列的值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="columnName">列名</param>
        /// <returns></returns>
        public T Sum<T>(string columnName)
        {
            return Sum<T>(Columns.GetIndex(columnName));
        }

        /// <summary>
        /// 汇总某列的值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="index">列索引</param>
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
        /// 记算某列的平均值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="columnName">列名</param>
        /// <returns></returns>
        public T Avg<T>(string columnName)
        {
            return Avg<T>(Columns.GetIndex(columnName));
        }

        /// <summary>
        ///记算某列的平均值
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="index">列索引</param>
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
        /// 行转换列（不指定时，默认取最后三列处理）
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
        /// 行转换列
        /// </summary>
        /// <param name="rowName">用于指定行的列名</param>
        /// <param name="colName">用于分拆成列的列名</param>
        /// <param name="valueName">用于显示值的列名</param>
        /// <returns></returns>
        public MDataTable Pivot(string rowName, string colName, string valueName)
        {
            MDataTable dt = new MDataTable(TableName);

            #region 处理列头
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

            #region 处理数据
            List<string> rowNameItems = GetColumnItems<string>(rowName, BreakOp.None, true);
            MDataTable splitTable = this;
            for (int i = 0; i < rowNameItems.Count; i++)
            {
                MDataRow nameRow = dt.NewRow(true).Set(0, rowNameItems[i]);//新表的一行
                MDataTable[] dt2 = splitTable.Split(rowName + "='" + rowNameItems[i] + "'");//筛选分隔
                splitTable = dt2[1];//剩下的作为下次分隔

                foreach (MDataRow row in dt2[0].Rows)//填写数据
                {
                    if (!row[colName].IsNullOrEmpty)//列名不能为空或Null
                    {
                        nameRow.Set(row[colName].Value, row[valueName].Value);
                    }
                }
            }
            #endregion
            return dt;
        }
        

        /// <summary>
        /// 过滤掉重复的数据行（仅比较基础类型、复杂数据类型仅比较类型名称）。
        /// <param name="filterRows">被过滤的数据集表</param>
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
                    for (int j = Rows.Count - 1; j >= 0 && j != i; j--)//反序检测。
                    {
                        int eqCount = 0;
                        for (int k = 0; k < cCount; k++)//比较列
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
                    filterRows.Columns = filterRows.Columns.Clone();//重置头部引用
                    Columns._Table = this;
                }
            }
        }
        /// <summary>
        /// 过滤掉重复的数据行（仅比较基础类型、复杂数据类型仅比较类型名称）。
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
