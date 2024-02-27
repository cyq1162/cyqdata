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
using CYQ.Data.Emit;

namespace CYQ.Data.Table
{

    /// <summary>
    /// 表格
    /// </summary>
    public partial class MDataTable
    {
        /// <summary>
        /// 转实体列表
        /// </summary>
        public List<T> ToList<T>() where T : class
        {
            List<T> list = new List<T>();
            if (Rows != null && Rows.Count > 0)
            {
                foreach (MDataRow row in Rows)
                {
                    list.Add(row.ToEntity<T>());
                }
            }
            return list;
        }
        internal object ToList(Type t)
        {
            if (t.Name == "MDataTable")
            {
                return this;
            }
            if (Rows == null || Rows.Count == 0) { return null; }
            Type[] paraTypeList = null;
            if (ReflectTool.GetArgumentLength(ref t, out paraTypeList) != 1)
            {
                return null;
            }
            var func = MDataTableToList.Delegate(t, paraTypeList[0]);
            return func(this);

            //if (t.Name.EndsWith("[]"))
            //{

            //        //object listObj = Activator.CreateInstance(t, Rows.Count);//创建实例
            //        //MethodInfo method = t.GetMethod("Set");
            //        //for (int i = 0; i < Rows.Count; i++)
            //        //{
            //        //    method.Invoke(listObj, new object[] { i, Rows[i].ToEntity(paraTypeList[0]) });
            //        //}
            //        //return listObj;

            //}
            //else
            //{
            //    object listObj = Activator.CreateInstance(t);//创建实例


            //        MethodInfo method = t.GetMethod("Add");
            //        foreach (MDataRow row in Rows)
            //        {
            //            method.Invoke(listObj, new object[] { row.ToEntity(paraTypeList[0]) });
            //        }


            //    return listObj;
            //}
            //return null;
        }
    }
}
