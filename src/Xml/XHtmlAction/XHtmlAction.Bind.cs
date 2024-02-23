using System;
using System.Xml;
using CYQ.Data.Table;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using CYQ.Data.Tool;
using System.Text;
using System.Web;
using System.Threading;
using CYQ.Data.Json;

namespace CYQ.Data.Xml
{

    //扩展交互
    public partial class XHtmlAction
    {
        #region 操作数据


        #region 加载表格循环方式

        public delegate string SetForeachEventHandler(string text, MDictionary<string, string> values, int rowIndex);
        /// <summary>
        /// 对于SetForeach函数调用的格式化事件
        /// </summary>
        public event SetForeachEventHandler OnForeach;
        /// <summary>
        /// 绑定数据源并进行循环绑定。
        /// （默认使用“数据源表名+View”或“defaultView”作为节点名，循环内容取值节点：InnerXml）
        /// </summary>
        /// <param name="dataSource"></param>
        public void SetForeach(MDataTable dataSource)
        {
            if (dataSource == null) { return; }
            XmlNode node = Get(dataSource.TableName + "View");
            if (node == null)
            {
                node = Get("defaultView");
            }
            SetForeach(dataSource, node, node.InnerXml, OnForeach);

        }
        /// <summary>
        /// 绑定数据源并进行循环绑定。
        /// </summary>
        /// <param name="dataSource">MDataTable、Json、ListT 等数据源</param>
        /// <param name="idOrName">节点名称，循环内容默认取值节点：InnerXml</param>
        public void SetForeach(object dataSource, string idOrName)
        {
            if (dataSource == null) { return; }
            XmlNode node = Get(idOrName);
            SetForeach(dataSource, node, node.InnerXml, OnForeach);
        }
        /// <summary>
        /// 绑定数据源并进行循环绑定。
        /// </summary>
        /// <param name="dataSource">MDataTable、Json、ListT 等数据源</param>
        /// <param name="idOrName">节点名称，循环内容默认取值节点：InnerXml</param>
        /// <param name="text">自定义要循环的文本内容</param>
        public void SetForeach(object dataSource, string idOrName, string text)
        {
            if (dataSource == null) { return; }
            XmlNode node = Get(idOrName);
            SetForeach(dataSource, node, text, OnForeach);
        }
        /// <summary>
        /// 绑定数据源并进行循环绑定。
        /// </summary>
        /// <param name="dataSource">MDataTable、Json、ListT 等数据源</param>
        /// <param name="node">节点，循环内容默认取值节点：InnerXml</param>
        public void SetForeach(object dataSource, XmlNode node)
        {
            SetForeach(dataSource, node, node.InnerXml, OnForeach);
        }
        /// <summary>
        /// 绑定数据源并进行循环绑定。
        /// </summary>
        /// <param name="dataSource">MDataTable、Json、ListT 等数据源</param>
        /// <param name="node">节点，循环内容默认取值节点：InnerXml</param>
        /// <param name="text">自定义要循环的文本内容</param>
        public void SetForeach(object dataSource, XmlNode node, string text)
        {
            SetForeach(dataSource, node, text, OnForeach);
        }

        /// <summary>
        /// 对列表进行循环绑定处理
        /// </summary>
        /// <param name="dataSource">MDataTable、Json、ListT 等数据源</param>
        /// <param name="node">处理的节点</param>
        /// <param name="text">用于循环的内容【通常传递node.InnerXml】</param>
        /// <param name="eventOnForeach">自定义事件</param>
        public void SetForeach(object dataSource, XmlNode node, string text, SetForeachEventHandler eventOnForeach)
        {
            try
            {
                #region 前置条件处理
                if (dataSource == null || node == null || string.IsNullOrEmpty(text))
                {
                    return;
                }
                MDataTable table;
                if (dataSource is MDataTable)
                {
                    table = dataSource as MDataTable;
                }
                else
                {
                    table = MDataTable.CreateFrom(dataSource);
                }
                if (table == null || table.Rows.Count == 0 || table.Columns.Count == 0)
                {
                    if (node.Attributes["clearflag"] == null)
                    {
                        node.InnerText = string.Empty;
                    }
                    return;
                }
                RemoveAttr(node, "clearflag");
                #endregion

                StringBuilder innerXml = new StringBuilder();

                for (int k = 0; k < table.Rows.Count; k++)
                {
                    #region 循环每一行
                    MDictionary<string, string> values = GetFromRow(table.Rows[k]);
                    string newText = text;
                    if (eventOnForeach != null)
                    {
                        newText = eventOnForeach(text, values, k);//遍历每一行，产生新text。
                    }
                    try
                    {
                        if (newText.IndexOf("${") > -1 || newText.IndexOf("<%#") > -1)
                        {
                            newText = FormatHtml(newText, values);
                        }
                        innerXml.Append(newText);
                    }
                    catch (Exception err)
                    {
                        Log.Write(err, LogType.Error);
                    }
                    finally
                    {
                        values.Clear();
                    }

                    #endregion
                }

                #region 结果赋值

                ////用InnerText性能好，但循环后的内容无法通过节点操作。
                //node.InnerText = innerXml.ToString();
                try
                {

                    node.InnerXml = innerXml.ToString();
                }
                catch
                {
                    try
                    {
                        node.InnerXml = SetCDATA(innerXml.ToString());
                    }
                    catch (Exception err)
                    {
                        Log.Write(err, LogType.Error);
                    }
                }
                #endregion
            }
            finally
            {
                if (eventOnForeach != null)
                {
                    eventOnForeach = null;
                }
            }
        }

        /// <summary>
        /// 能减少反射就减少反射
        /// </summary>
        private MDictionary<string, string> GetFromRow(MDataRow row)
        {
            MDictionary<string, string> keyValuePairs = new MDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in row)
            {
                keyValuePairs.Add(item.ColumnName, item.StringValue);
            }
            return keyValuePairs;
        }
        #endregion

        #region 加载行数据后操作方式

        /// <summary>
        /// 用于替换占位符的数据。
        /// </summary>
        public Dictionary<string, string> KeyValue = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 批量将数据加载到KeyValue 中，以等待输出时进行CMS替换。
        /// </summary>
        /// <param name="anyObj">MDataRow、Entity，Json，KeyValue等对象</param>
        public void LoadData(object anyObj)
        {
            LoadData(anyObj, null);
        }

        /// <summary>
        /// 批量将数据加载到KeyValue 中，以等待输出时进行CMS替换。
        /// </summary>
        /// <param name="pre">为所有字段名指定前缀【当遇相同名称时，可用前缀区分】</param>
        public void LoadData(object anyObj, string pre)
        {
            if (anyObj == null) { return; }
            MDataRow row;
            if (anyObj is MDataRow)
            {
                row = anyObj as MDataRow;
            }
            else
            {
                row = MDataRow.CreateFrom(anyObj);
            }
            if (row == null) { return; }

            foreach (var cell in row)
            {
                string key = pre + cell.ColumnName;
                if (!KeyValue.ContainsKey(key))
                {
                    KeyValue.Add(key, cell.StringValue);
                }
            }
        }

        #endregion

        #endregion


        public override void Dispose()
        {
            KeyValue.Clear();
            base.Dispose();
        }
    }
}
/* 移除已过时方法
         


        //public void SetForeach(MDataTable dataSource, string idOrName, SetType setType)
        //{
        //    string text = string.Empty;
        //    XmlNode node = Get(idOrName);
        //    if (node == null)
        //    {
        //        return;
        //    }
        //    switch (setType)
        //    {
        //        case SetType.InnerText:
        //            text = node.InnerText;
        //            break;
        //        case SetType.InnerXml:
        //            text = node.InnerXml;
        //            break;
        //        case SetType.Value:
        //        case SetType.Href:
        //        case SetType.Src:
        //        case SetType.Class:
        //        case SetType.Disabled:
        //        case SetType.ID:
        //        case SetType.Name:
        //        case SetType.Visible:
        //        case SetType.Title:
        //        case SetType.Style:
        //            string key = setType.ToString().ToLower();
        //            if (node.Attributes[key] != null)
        //            {
        //                text = node.Attributes[key].Value;
        //            }
        //            break;
        //    }
        //    SetForeach(dataSource, node, text, OnForeach);
        //}
 
 
        MDataRow _Row;
        /// <summary>
        /// 装载数据行 （一般后续配合SetFor方法使用或CMS替换）
        /// </summary>
        /// <param name="pre">为所有字段名指定前缀（如："a.",或空前缀：""）</param>
        public void LoadData(MDataRow row, string pre)
        {
            if (pre != null && row != null)
            {
                foreach (MDataCell cell in row)
                {
                    if (cell.IsNullOrEmpty) { continue; }
                    string cName = pre + cell.ColumnName;
                    if (KeyValue.ContainsKey(cName))
                    {
                        KeyValue[cName] = cell.ToString();
                    }
                    else
                    {
                        KeyValue.Add(cName, cell.ToString());
                    }
                }
            }
            _Row = row;
        }
        /// <summary>
        /// 装载行数据 （一般后续配合SetFor方法使用）
        /// </summary>
        /// <param name="row">数据行</param>
        public void LoadData(MDataRow row)
        {
            _Row = row;
        }
        /// <summary>
        /// 为节点设置值，通常在LoadData后使用。
        /// </summary>
        /// <param name="idOrName">节点的id或name</param>
        public void SetFor(string idOrName)
        {
            SetFor(idOrName, SetType.InnerXml);
        }
        /// <summary>
        /// 为节点设置值，通常在LoadData后使用。
        /// </summary>
        /// <param name="setType">节点的类型</param>
        public void SetFor(string idOrName, SetType setType)
        {
            SetFor(idOrName, setType, GetRowValue(idOrName));
        }
        /// <summary>
        /// 为节点设置值，通常在LoadData后使用。
        /// </summary>
        /// <param name="values">setType为Custom时，可自定义值，如“"href","http://www.cyqdata.com","target","_blank"”</param>
        public void SetFor(string idOrName, SetType setType, params string[] values)
        {
            int i = setType == SetType.Custom ? 1 : 0;
            for (; i < values.Length; i++)
            {
                if (values[i].Contains(ValueReplace.New))
                {
                    values[i] = values[i].Replace(ValueReplace.New, GetRowValue(idOrName));
                }
            }
            Set(Get(idOrName), setType, values);
        }
        private string GetRowValue(string idOrName)
        {
            string rowValue = "";
            if (_Row != null)
            {
                MDataCell cell = _Row[idOrName];
                if (cell == null && idOrName.Length > 3)
                {
                    cell = _Row[idOrName.Substring(3)];
                }
                if (cell != null)
                {
                    rowValue = cell.IsNull ? "" : cell.StringValue;
                }
            }
            return rowValue;
        }
 
 
 */