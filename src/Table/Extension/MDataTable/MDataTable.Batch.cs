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
        /// 批量插入或更新 [提示：操作和当前表名有关，如当前表名不是要提交入库的表名,请给TableName属性重新赋值]
        /// </summary>
        /// <param name="op">操作选项[插入|更新]</param>
        /// <returns>返回false时，若有异常，存在：DynamicData 参数中</returns>
        public bool AcceptChanges(AcceptOp op)
        {
            return AcceptChanges(op, IsolationLevel.Unspecified, string.Empty);
        }
        /// <summary>
        /// 批量插入或更新 [提示：操作和当前表名有关，如当前表名不是要提交入库的表名,请给TableName属性重新赋值]
        /// </summary>
        /// <param name="op">操作选项[插入|更新]</param>
        /// <param name="tranLevel">事务等级【外部没有事务时有效】</param>
        /// <returns>返回false时，若有异常，存在：DynamicData 参数中</returns>
        public bool AcceptChanges(AcceptOp op, IsolationLevel tranLevel)
        {
            return AcceptChanges(op, tranLevel, string.Empty);
        }
        /// <param name="op">操作选项[插入|更新]</param>
        /// <param name="newConn">指定新的数据库链接</param>
        /// <param name="jointPrimaryKeys">AcceptOp为Update或Auto时，若需要设置联合主键为唯一检测或更新条件，则可设置多个字段名</param>
        /// <returns>返回false时，若有异常，存在：DynamicData 参数中</returns>
        public bool AcceptChanges(AcceptOp op, string newConn, params object[] jointPrimaryKeys)
        {
            return AcceptChanges(op, IsolationLevel.Unspecified, newConn, jointPrimaryKeys);
        }
        // <summary>
        /// 批量插入或更新 [提示：操作和当前表名有关，如当前表名不是要提交入库的表名,请给TableName属性重新赋值]
        /// </summary>
        /// <param name="tranLevel">事务等级【外部没有事务时有效】</param>
        /// <returns>返回false时，若有异常，存在：DynamicData 参数中</returns>
        public bool AcceptChanges(AcceptOp op, IsolationLevel tranLevel, string newConn, params object[] jointPrimaryKeys)
        {
            bool result = false;
            if (Columns.Count == 0 || Rows.Count == 0)
            {
                return false;//木有可更新的。
            }
            MDataTableBatchAction action = new MDataTableBatchAction(this, newConn);
            action.TranLevel = tranLevel;
            if ((op & AcceptOp.Truncate) != 0)
            {
                action.IsTruncate = true;
                op = (AcceptOp)(op - AcceptOp.Truncate);
            }
            action.SetJoinPrimaryKeys(jointPrimaryKeys);
            switch (op)
            {
                case AcceptOp.Insert:
                    result = action.Insert(false);
                    break;
                case AcceptOp.InsertWithID:
                    result = action.Insert(true);
                    break;
                case AcceptOp.Update:
                    result = action.Update();
                    break;
                case AcceptOp.Delete:
                    result = action.Delete();
                    break;
                case AcceptOp.Auto:
                    result = action.Auto(false);
                    break;
                case AcceptOp.Auto | AcceptOp.Insert:
                case AcceptOp.Auto | AcceptOp.InsertWithID:
                    result = action.Auto(true);
                    break;
            }
            if (result && AppConfig.AutoCache.IsEnable)
            {
                //取消AOP缓存。
                AopCache.ReadyForRemove(AopCache.GetBaseKey(TableName, newConn));
            }
            return result;
        }
        /// <summary>
        /// 获取修改过的数据
        /// </summary>
        /// <returns></returns>
        public MDataTable GetChanges()
        {
            return GetChanges(RowOp.Update);
        }
        /// <summary>
        /// 获取修改过的数据(若无修改，则返回Null）
        /// </summary>
        /// <param name="rowOp">仅Insert和Update选项可用</param>
        /// <returns></returns>
        public MDataTable GetChanges(RowOp rowOp)
        {
            MDataTable dt = new MDataTable(_TableName);
            dt.Columns = Columns;
            dt.Conn = Conn;
            dt.DynamicData = DynamicData;
            dt.joinOnIndex = joinOnIndex;
            dt.JoinOnName = dt.JoinOnName;
            dt.RecordsAffected = RecordsAffected;
            if (this.Rows.Count > 0)
            {
                if (rowOp == RowOp.Insert || rowOp == RowOp.Update)
                {
                    int stateValue = (int)rowOp;
                    foreach (MDataRow row in Rows)
                    {
                        if (row.GetState() >= stateValue)
                        {
                            dt.Rows.Add(row, false);
                        }
                    }
                }
            }
            return dt;
        }
       
       
        //给MDataTableBatchAction的批量更新使用。
        internal void Load(MDataTable dt, MCellStruct primary)
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                return;
            }
            //if (Rows.Count == dt.Rows.Count)
            //{
            //    for (int i = 0; i < Rows.Count; i++)
            //    {
            //        Rows[i].LoadFrom(dt.Rows[i], RowOp.IgnoreNull, false);
            //    }
            //}
            //else
            //{
            string pkName = primary != null ? primary.ColumnName : Columns.FirstPrimary.ColumnName;
            int i1 = Columns.GetIndex(pkName);
            MDataRow rowA, rowB;

            for (int i = 0; i < Rows.Count; i++)
            {
                rowA = Rows[i];
                rowB = dt.FindRow(pkName + "='" + rowA[i1].StringValue + "'");
                if (rowB != null)
                {
                    rowA.LoadFrom(rowB, RowOp.Update, false);
                    dt.Rows.Remove(rowB);
                }
            }
            // }
        }
    }

}
