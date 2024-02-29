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
using CYQ.Data.Json;
using CYQ.Data.Emit;

namespace CYQ.Data.Table
{
    //��չ��������
    public partial class MDataRow
    {

        #region ToJson��ToXml��ToTable��ToEntity

        /// <summary>
        /// ����е�����Json
        /// </summary>
        public string ToJson()
        {
            return JsonHelper.ToJson(this);
        }

        /// <summary>
        /// ���Json
        /// </summary>
        /// <param name="jsonOp">Json ѡ��</param>
        /// <returns></returns>
        public string ToJson(JsonOp jsonOp)
        {
            return JsonHelper.ToJson(this, jsonOp);
        }

        internal string ToXml(bool isConvertNameToLower)
        {
            string xml = string.Empty;
            foreach (MDataCell cell in this)
            {
                xml += cell.ToXml(isConvertNameToLower);
            }
            return xml;
        }
        /// <summary>
        /// ���е�����ת�����У�ColumnName��Value���ı�
        /// </summary>
        public MDataTable ToTable()
        {
            MDataTable dt = this.Columns.ToTable();
            MCellStruct msValue = new MCellStruct("Value", SqlDbType.Variant);
            MCellStruct msState = new MCellStruct("State", SqlDbType.Int);
            dt.Columns.Insert(1, msValue);
            dt.Columns.Insert(2, msState);
            for (int i = 0; i < Count; i++)
            {
                dt.Rows[i][1].Value = this[i].Value;
                dt.Rows[i][2].Value = this[i].State;
            }
            return dt;
        }
        /// <summary>
        /// ���е�����ת�����У�ColumnName��Value��State���ı�
        /// </summary>
        /// <param name="onlyData">�����ݣ�������ͷ�ṹ��</param>
        /// <returns></returns>
        public MDataTable ToTable(bool onlyData)
        {
            if (onlyData)
            {
                MDataTable dt = new MDataTable(this.TableName);
                dt.Columns.Add("ColumnName", SqlDbType.NVarChar);
                dt.Columns.Add("Value", SqlDbType.Variant);
                dt.Columns.Add("State", SqlDbType.Int);
                for (int i = 0; i < Count; i++)
                {
                    MDataCell cell = this[i];
                    dt.NewRow(true)
                        .Set(0, cell.ColumnName)
                        .Set(1, cell.Value)
                        .Set(2, cell.State);
                }
                return dt;
            }
            return ToTable();
        }

        /// <summary>
        /// תʵ����
        /// </summary>
        /// <typeparam name="T">����</typeparam>
        /// <returns></returns>
        public T ToEntity<T>()
        {
            Type t = typeof(T);
            return (T)ToEntity(t);
        }

        internal object ToEntity(Type t)
        {
            if (t.Name == "MDataRow")
            {
                return this;
            }
            if (t.IsValueType || t.Name == "String")
            {
                return ConvertTool.ChangeType(this[0].Value, t);
            }
            var sysType = ReflectTool.GetSystemType(ref t);

            if (sysType == SysType.Custom)
            {
                var func = MDataRowToEntity.Delegate(t);
                return func(this);
            }
            else
            {
                int len = ReflectTool.GetArgumentLength(ref t);
                if (len == 1)
                {
                    return this.Table.ToList(t);
                }
                if (len == 2)
                {
                    var func = MDataRowToKeyValue.Delegate(t);
                    var obj = func(this);
                    return obj;
                }
                return null;
            }
        }

        #endregion

        #region SetToEntity

        /// <summary>
        /// �����е�����ֵ����ʵ�����
        /// </summary>
        /// <param name="obj">ʵ�����</param>
        public void SetToEntity(object obj)
        {
            if (obj == null || this.Count == 0)
            {
                return;
            }
            Type objType = obj.GetType();
            var setToEntity = MDataRowSetToEntity.Delegate(objType);
            setToEntity(this, obj);

            //string objName = objType.FullName, cellName = "";
            //try
            //{
            //    #region �������
            //    List<PropertyInfo> pis = ReflectTool.GetPropertyList(objType);
            //    if (pis.Count > 0)
            //    {
            //        foreach (PropertyInfo p in pis)//����ʵ��
            //        {
            //            if (p.CanWrite)
            //            {
            //                SetValueToPropertyOrField(ref obj, row, p, null, op, out cellName);
            //            }
            //        }
            //    }
            //    else
            //    {
            //        List<FieldInfo> fis = ReflectTool.GetFieldList(objType);
            //        if (fis.Count > 0)
            //        {
            //            foreach (FieldInfo f in fis)//����ʵ��
            //            {
            //                SetValueToPropertyOrField(ref obj, row, null, f, op, out cellName);
            //            }
            //        }
            //    }
            //    #endregion
            //}
            //catch (Exception err)
            //{
            //    string msg = "[AttachInfo]:" + string.Format("ObjName:{0} PropertyName:{1}", objName, cellName) + "\r\n";
            //    msg += Log.GetExceptionMessage(err);
            //    Log.WriteLogToTxt(msg, LogType.Error);
            //}
        }

        //private void SetValueToPropertyOrField(ref object obj, MDataRow row, PropertyInfo p, FieldInfo f, RowOp op, out string cellName)
        //{
        //    cellName = p != null ? p.Name : f.Name;
        //    MDataCell cell = row[cellName];
        //    if (cell == null)
        //    {
        //        return;
        //    }
        //    if (op == RowOp.IgnoreNull && cell.IsNull)
        //    {
        //        return;
        //    }
        //    else if (op == RowOp.Insert && cell.State == 0)
        //    {
        //        return;
        //    }
        //    else if (op == RowOp.Update && cell.State != 2)
        //    {
        //        return;
        //    }
        //    Type propType = p != null ? p.PropertyType : f.FieldType;
        //    object objValue = GetObj(propType, cell.Value);

        //    if (p != null)
        //    {
        //        p.SetValue(obj, objValue, null);
        //    }
        //    else
        //    {
        //        f.SetValue(obj, objValue);
        //    }
        //}
        #endregion

    }

}
