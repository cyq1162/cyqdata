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
        /// ���Json
        /// </summary>
        public string ToJson()
        {
            return ToJson(true);
        }
        public string ToJson(bool addHead)
        {
            return ToJson(addHead, false);
        }
        /// <param name="addHead">���ͷ����Ϣ[��count��Success��ErrorMsg](Ĭ��true)</param>
        /// <param name="addSchema">���������ܹ���Ϣ,������ʱ�ɻ�ԭ�ܹ�(Ĭ��false)</param>
        public string ToJson(bool addHead, bool addSchema)
        {
            var jsonOp = new JsonOp();
            jsonOp.RowOp = RowOp.None;
            return ToJson(addHead, addSchema, jsonOp);
        }

        /// <param name="op">����ת��ѡ��</param>
        public string ToJson(bool addHead, bool addSchema, JsonOp jsonOp)
        {
            JsonHelper helper = new JsonHelper(addHead, addSchema, jsonOp);
            helper.Fill(this);
            bool checkArrayEnd = !addHead && !addSchema;
            return helper.ToString(checkArrayEnd);
        }
        /// <summary>
        /// ���Json[��ָ������·��]
        /// </summary>
        public bool WriteJson(bool addHead, bool addSchema, string fileName)
        {
            return IOHelper.Write(fileName, ToJson(addHead, addSchema));
        }

    }
}
