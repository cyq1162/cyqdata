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
        #region 隐式转换

        /// <summary>
        /// 从DataReader隐式转换成MDataTable
        /// </summary>
        public static implicit operator MDataTable(DbDataReader sdr)
        {
            MDataTable dt = CreateFrom(sdr);
            if (sdr != null)
            {
                sdr.Close();
                sdr.Dispose();
                sdr = null;
            }
            return dt;
        }

        /// <summary>
        /// 从DataTable隐式转换成MDataTable
        /// </summary>
        public static implicit operator MDataTable(DataTable dt)
        {
            MDataTable mdt = new MDataTable();
            if (dt != null)
            {
                mdt.TableName = dt.TableName;
                if (dt.Columns != null && dt.Columns.Count > 0)
                {
                    foreach (DataColumn item in dt.Columns)
                    {
                        MCellStruct mcs = new MCellStruct(item.ColumnName, DataType.GetSqlType(item.DataType), item.ReadOnly, item.AllowDBNull, item.MaxLength);
                        mcs.valueType = item.DataType;
                        mdt.Columns.Add(mcs);
                    }
                    #region 属性还原
                    string[] cellStates = null;
                    if (dt.ExtendedProperties.ContainsKey("CellState"))// 还原记录总数
                    {
                        int total = 0, rowCount = 0, columnCount = 0;
                        string ra = Convert.ToString(dt.ExtendedProperties["RecordsAffected"]);
                        string rc = Convert.ToString(dt.ExtendedProperties["RowsCount"]);
                        string cc = Convert.ToString(dt.ExtendedProperties["ColumnsCount"]);
                        string cs = Convert.ToString(dt.ExtendedProperties["CellState"]);
                        if (int.TryParse(ra, out total) && int.TryParse(rc, out rowCount) && total >= rowCount)
                        {
                            mdt.RecordsAffected = total;
                        }
                        if (!string.IsNullOrEmpty(cs) && int.TryParse(cc, out columnCount) && columnCount == dt.Columns.Count)
                        {
                            cellStates = cs.Split(';');
                        }
                    }
                    #endregion


                    for (int k = 0; k < dt.Rows.Count; k++)
                    {
                        string[] states = null;
                        if (cellStates != null && cellStates.Length > k)
                        {
                            states = cellStates[k].Split(',');
                        }
                        DataRow row = dt.Rows[k];
                        MDataRow mdr = mdt.NewRow();
                        for (int i = 0; i < dt.Columns.Count; i++)
                        {
                            mdr[i].Value = row[i];
                            if (states != null && states.Length > i)
                            {
                                mdr[i].State = int.Parse(states[i]);
                            }
                        }
                        mdt.Rows.Add(mdr, states == null && row.RowState != DataRowState.Modified);
                    }
                }
            }
            return mdt;
        }
        /// <summary>
        /// 从行集合隐式转换成MDataTable
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static implicit operator MDataTable(List<MDataRow> rows)
        {
            return (MDataRowCollection)rows;
        }
        /// <summary>
        /// 将一行数据装载成一个表。
        /// </summary>
        /// <returns></returns>
        public static implicit operator MDataTable(MDataRow row)
        {
            MDataTable mTable = new MDataTable(row.TableName);
            mTable.Conn = row.Conn;
            mTable.LoadRow(row);
            return mTable;
        }
        /// <summary>
        /// 将一行数据装载成一个表。
        /// </summary>
        /// <returns></returns>
        public static implicit operator MDataTable(MDataRowCollection rows)
        {
            MDataTable mdt = new MDataTable();
            if (rows != null && rows.Count > 0)
            {
                mdt.TableName = rows[0].TableName;
                mdt.Conn = rows[0].Conn;
                mdt.Columns = rows[0].Columns;
                mdt.Rows.AddRange(rows);
            }
            return mdt;

        }
        #endregion

    }
}
