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

    /// <summary>
    /// 表格
    /// </summary>
    public partial class MDataTable
    {
        /// <summary>
        /// 转换成DataTable
        /// </summary>
        public DataTable ToDataTable()
        {
            return ToDataTable(false);
        }
        /// <summary>
        /// 转换成DataTable
        /// </summary>
        /// <param name="isAddExtend">是否追加【值状态、记录总数】到扩展属性</param>
        /// <returns></returns>
        public DataTable ToDataTable(bool isAddExtend)
        {
            DataTable dt = new DataTable(_TableName);

            if (Columns != null && Columns.Count > 0)
            {
                int count = Columns.Count;
                for (int j = 0; j < count; j++)
                {
                    MCellStruct item = Columns[j];
                    string columnName = item.ColumnName;
                    if (string.IsNullOrEmpty(columnName))
                    {
                        item.ColumnName = "Empty_" + j;
                    }
                    if (dt.Columns.Contains(columnName))//去重。
                    {
                        columnName = columnName + "_" + j;
                    }
                    dt.Columns.Add(item.ColumnName, item.ValueType);
                }
                StringBuilder stateSB = new StringBuilder();
                for (int k = 0; k < Rows.Count; k++)
                {
                    if (isAddExtend)
                    {
                        if (k > 0)
                        {
                            stateSB.Append(";");
                        }
                    }
                    MDataRow row = Rows[k];
                    int[] states = isAddExtend ? new int[count] : null;
                    DataRow dr = dt.NewRow();
                    for (int i = 0; i < count; i++)
                    {
                        if (row[i].IsNull)
                        {
                            dr[i] = DBNull.Value;
                        }
                        else
                        {
                            dr[i] = row[i].Value;
                        }
                        if (isAddExtend)
                        {
                            if (i > 0)
                            {
                                stateSB.Append(",");
                            }
                            stateSB.Append(row[i].State);
                        }
                    }
                    dt.Rows.Add(dr);
                }
                if (isAddExtend)
                {
                    dt.ExtendedProperties.Add("RowsCount", this.Rows.Count);
                    dt.ExtendedProperties.Add("ColumnsCount", this.Columns.Count);
                    dt.ExtendedProperties.Add("RecordsAffected", this.RecordsAffected);
                    dt.ExtendedProperties.Add("CellState", stateSB.ToString());
                }
            }
            dt.AcceptChanges();
            return dt;
        }
    }
}
