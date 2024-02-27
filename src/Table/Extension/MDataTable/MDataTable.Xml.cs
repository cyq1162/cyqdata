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
        /// 输出Xml文档
        /// </summary>
        public string ToXml()
        {
            return ToXml(false);
        }
        public string ToXml(bool isConvertNameToLower)
        {
            return ToXml(isConvertNameToLower, true, true);
        }
        /// <summary>
        /// 输出Xml文档
        /// </summary>
        /// <param name="isConvertNameToLower">名称转小写</param>
        /// <returns></returns>
        public string ToXml(bool isConvertNameToLower, bool needHeader, bool needRootNode)
        {
            StringBuilder xml = new StringBuilder();
            if (Columns.Count > 0)
            {
                string tableName = string.IsNullOrEmpty(_TableName) ? "Root" : _TableName;
                string rowName = string.IsNullOrEmpty(_TableName) ? "Row" : _TableName;
                if (isConvertNameToLower)
                {
                    tableName = tableName.ToLower();
                    rowName = rowName.ToLower();
                }
                if (needHeader)
                {
                    xml.Append("<?xml version=\"1.0\" standalone=\"yes\"?>");
                }
                if (needRootNode)
                {
                    xml.AppendFormat("\r\n<{0}>", tableName);
                }
                foreach (MDataRow row in Rows)
                {
                    xml.AppendFormat("\r\n  <{0}>", rowName);
                    foreach (MDataCell cell in row)
                    {
                        xml.Append(cell.ToXml(isConvertNameToLower));
                    }
                    xml.AppendFormat("\r\n  </{0}>", rowName);
                }
                if (needRootNode)
                {
                    xml.AppendFormat("\r\n</{0}>", tableName);
                }
            }
            return xml.ToString();
        }
        public bool WriteXml(string fileName)
        {
            return WriteXml(fileName, false);
        }
        /// <summary>
        /// 保存Xml
        /// </summary>
        public bool WriteXml(string fileName, bool isConvertNameToLower)
        {
            return IOHelper.Write(fileName, ToXml(isConvertNameToLower), Encoding.UTF8);
        }

    }
}
