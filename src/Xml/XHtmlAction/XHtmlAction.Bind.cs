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

    //��չ����
    public partial class XHtmlAction
    {
        #region ��������


        #region ���ر��ѭ����ʽ

        public delegate string SetForeachEventHandler(string text, MDictionary<string, string> values, int rowIndex);
        /// <summary>
        /// ����SetForeach�������õĸ�ʽ���¼�
        /// </summary>
        public event SetForeachEventHandler OnForeach;
        /// <summary>
        /// ������Դ������ѭ���󶨡�
        /// ��Ĭ��ʹ�á�����Դ����+View����defaultView����Ϊ�ڵ�����ѭ������ȡֵ�ڵ㣺InnerXml��
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
        /// ������Դ������ѭ���󶨡�
        /// </summary>
        /// <param name="dataSource">MDataTable��Json��ListT ������Դ</param>
        /// <param name="idOrName">�ڵ����ƣ�ѭ������Ĭ��ȡֵ�ڵ㣺InnerXml</param>
        public void SetForeach(object dataSource, string idOrName)
        {
            if (dataSource == null) { return; }
            XmlNode node = Get(idOrName);
            SetForeach(dataSource, node, node.InnerXml, OnForeach);
        }
        /// <summary>
        /// ������Դ������ѭ���󶨡�
        /// </summary>
        /// <param name="dataSource">MDataTable��Json��ListT ������Դ</param>
        /// <param name="idOrName">�ڵ����ƣ�ѭ������Ĭ��ȡֵ�ڵ㣺InnerXml</param>
        /// <param name="text">�Զ���Ҫѭ�����ı�����</param>
        public void SetForeach(object dataSource, string idOrName, string text)
        {
            if (dataSource == null) { return; }
            XmlNode node = Get(idOrName);
            SetForeach(dataSource, node, text, OnForeach);
        }
        /// <summary>
        /// ������Դ������ѭ���󶨡�
        /// </summary>
        /// <param name="dataSource">MDataTable��Json��ListT ������Դ</param>
        /// <param name="node">�ڵ㣬ѭ������Ĭ��ȡֵ�ڵ㣺InnerXml</param>
        public void SetForeach(object dataSource, XmlNode node)
        {
            SetForeach(dataSource, node, node.InnerXml, OnForeach);
        }
        /// <summary>
        /// ������Դ������ѭ���󶨡�
        /// </summary>
        /// <param name="dataSource">MDataTable��Json��ListT ������Դ</param>
        /// <param name="node">�ڵ㣬ѭ������Ĭ��ȡֵ�ڵ㣺InnerXml</param>
        /// <param name="text">�Զ���Ҫѭ�����ı�����</param>
        public void SetForeach(object dataSource, XmlNode node, string text)
        {
            SetForeach(dataSource, node, text, OnForeach);
        }

        /// <summary>
        /// ���б����ѭ���󶨴���
        /// </summary>
        /// <param name="dataSource">MDataTable��Json��ListT ������Դ</param>
        /// <param name="node">����Ľڵ�</param>
        /// <param name="text">����ѭ�������ݡ�ͨ������node.InnerXml��</param>
        /// <param name="eventOnForeach">�Զ����¼�</param>
        public void SetForeach(object dataSource, XmlNode node, string text, SetForeachEventHandler eventOnForeach)
        {
            try
            {
                #region ǰ����������
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
                    #region ѭ��ÿһ��
                    MDictionary<string, string> values = GetFromRow(table.Rows[k]);
                    string newText = text;
                    if (eventOnForeach != null)
                    {
                        newText = eventOnForeach(text, values, k);//����ÿһ�У�������text��
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

                #region �����ֵ

                ////��InnerText���ܺã���ѭ����������޷�ͨ���ڵ������
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
        /// �ܼ��ٷ���ͼ��ٷ���
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

        #region ���������ݺ������ʽ

        /// <summary>
        /// �����滻ռλ�������ݡ�
        /// </summary>
        public Dictionary<string, string> KeyValue = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// ���������ݼ��ص�KeyValue �У��Եȴ����ʱ����CMS�滻��
        /// </summary>
        /// <param name="anyObj">MDataRow��Entity��Json��KeyValue�ȶ���</param>
        public void LoadData(object anyObj)
        {
            LoadData(anyObj, null);
        }

        /// <summary>
        /// ���������ݼ��ص�KeyValue �У��Եȴ����ʱ����CMS�滻��
        /// </summary>
        /// <param name="pre">Ϊ�����ֶ���ָ��ǰ׺��������ͬ����ʱ������ǰ׺���֡�</param>
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
/* �Ƴ��ѹ�ʱ����
         


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
        /// װ�������� ��һ��������SetFor����ʹ�û�CMS�滻��
        /// </summary>
        /// <param name="pre">Ϊ�����ֶ���ָ��ǰ׺���磺"a.",���ǰ׺��""��</param>
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
        /// װ�������� ��һ��������SetFor����ʹ�ã�
        /// </summary>
        /// <param name="row">������</param>
        public void LoadData(MDataRow row)
        {
            _Row = row;
        }
        /// <summary>
        /// Ϊ�ڵ�����ֵ��ͨ����LoadData��ʹ�á�
        /// </summary>
        /// <param name="idOrName">�ڵ��id��name</param>
        public void SetFor(string idOrName)
        {
            SetFor(idOrName, SetType.InnerXml);
        }
        /// <summary>
        /// Ϊ�ڵ�����ֵ��ͨ����LoadData��ʹ�á�
        /// </summary>
        /// <param name="setType">�ڵ������</param>
        public void SetFor(string idOrName, SetType setType)
        {
            SetFor(idOrName, setType, GetRowValue(idOrName));
        }
        /// <summary>
        /// Ϊ�ڵ�����ֵ��ͨ����LoadData��ʹ�á�
        /// </summary>
        /// <param name="values">setTypeΪCustomʱ�����Զ���ֵ���硰"href","http://www.cyqdata.com","target","_blank"��</param>
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