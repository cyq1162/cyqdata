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
        /// 输出Json
        /// </summary>
        public string ToJson()
        {
            return ToJson(true);
        }
        public string ToJson(bool addHead)
        {
            return ToJson(addHead, false);
        }
        /// <param name="addHead">输出头部信息[带count、Success、ErrorMsg](默认true)</param>
        /// <param name="addSchema">首行输出表架构信息,反接收时可还原架构(默认false)</param>
        public string ToJson(bool addHead, bool addSchema)
        {
            var jsonOp = new JsonOp();
            jsonOp.RowOp = RowOp.None;
            return ToJson(addHead, addSchema, jsonOp);
        }

        /// <param name="op">符号转义选项</param>
        public string ToJson(bool addHead, bool addSchema, JsonOp jsonOp)
        {
            JsonHelper helper = new JsonHelper(addHead, addSchema, jsonOp);
            helper.Fill(this);
            bool checkArrayEnd = !addHead && !addSchema;
            return helper.ToString(checkArrayEnd);
        }
        /// <summary>
        /// 输出Json[可指定保存路径]
        /// </summary>
        public bool WriteJson(bool addHead, bool addSchema, string fileName)
        {
            return IOHelper.Write(fileName, ToJson(addHead, addSchema));
        }

    }
}
