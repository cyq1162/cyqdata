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
    /// ���
    /// </summary>
    public partial class MDataTable
    {

        /// <summary>
        /// ������������ [��ʾ�������͵�ǰ�����йأ��統ǰ��������Ҫ�ύ���ı���,���TableName�������¸�ֵ]
        /// </summary>
        /// <param name="op">����ѡ��[����|����]</param>
        /// <returns>����falseʱ�������쳣�����ڣ�DynamicData ������</returns>
        public bool AcceptChanges(AcceptOp op)
        {
            return AcceptChanges(op, IsolationLevel.Unspecified, string.Empty);
        }
        /// <summary>
        /// ������������ [��ʾ�������͵�ǰ�����йأ��統ǰ��������Ҫ�ύ���ı���,���TableName�������¸�ֵ]
        /// </summary>
        /// <param name="op">����ѡ��[����|����]</param>
        /// <param name="tranLevel">����ȼ����ⲿû������ʱ��Ч��</param>
        /// <returns>����falseʱ�������쳣�����ڣ�DynamicData ������</returns>
        public bool AcceptChanges(AcceptOp op, IsolationLevel tranLevel)
        {
            return AcceptChanges(op, tranLevel, string.Empty);
        }
        /// <param name="op">����ѡ��[����|����]</param>
        /// <param name="newConn">ָ���µ����ݿ�����</param>
        /// <param name="jointPrimaryKeys">AcceptOpΪUpdate��Autoʱ������Ҫ������������ΪΨһ�������������������ö���ֶ���</param>
        /// <returns>����falseʱ�������쳣�����ڣ�DynamicData ������</returns>
        public bool AcceptChanges(AcceptOp op, string newConn, params object[] jointPrimaryKeys)
        {
            return AcceptChanges(op, IsolationLevel.Unspecified, newConn, jointPrimaryKeys);
        }
        // <summary>
        /// ������������ [��ʾ�������͵�ǰ�����йأ��統ǰ��������Ҫ�ύ���ı���,���TableName�������¸�ֵ]
        /// </summary>
        /// <param name="tranLevel">����ȼ����ⲿû������ʱ��Ч��</param>
        /// <returns>����falseʱ�������쳣�����ڣ�DynamicData ������</returns>
        public bool AcceptChanges(AcceptOp op, IsolationLevel tranLevel, string newConn, params object[] jointPrimaryKeys)
        {
            bool result = false;
            if (Columns.Count == 0 || Rows.Count == 0)
            {
                return false;//ľ�пɸ��µġ�
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
                //ȡ��AOP���档
                AopCache.ReadyForRemove(AopCache.GetBaseKey(TableName, newConn));
            }
            return result;
        }
        /// <summary>
        /// ��ȡ�޸Ĺ�������
        /// </summary>
        /// <returns></returns>
        public MDataTable GetChanges()
        {
            return GetChanges(RowOp.Update);
        }
        /// <summary>
        /// ��ȡ�޸Ĺ�������(�����޸ģ��򷵻�Null��
        /// </summary>
        /// <param name="rowOp">��Insert��Updateѡ�����</param>
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
       
       
        //��MDataTableBatchAction����������ʹ�á�
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
