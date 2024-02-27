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

    public partial class MDataTable
    {
        #region ��̬���� CreateFrom

        /// <summary>
        /// ���ر�Sdr����Ϊ�ⲿMProc.ExeMDataTableList����Ҫʹ�ã�
        /// </summary>
        /// <param name="sdr"></param>
        /// <returns></returns>
        internal static MDataTable CreateFrom(DbDataReader sdr)
        {
            if (sdr != null && sdr is NoSqlDataReader)
            {
                return ((NoSqlDataReader)sdr).table;
            }
            MDataTable mTable = new MDataTable("SysDefault");
            if (sdr != null && sdr.FieldCount > 0)
            {

                DataTable dt = null;
                bool noSchema = OracleDal.clientType == 0;
                if (!noSchema)
                {
                    try
                    {
                        dt = sdr.GetSchemaTable();
                    }
                    catch { noSchema = true; }
                }
                #region ����ṹ
                if (noSchema) //OracleClient ��֧���Ӳ�ѯ��GetSchemaTable����ODP.NET��֧�ֵġ�
                {
                    //��DataReader��ȡ��ṹ��������û�����ݡ�
                    string hiddenFields = "," + AppConfig.DB.HiddenFields.ToLower() + ",";
                    MCellStruct mStruct;
                    for (int i = 0; i < sdr.FieldCount; i++)
                    {
                        string name = sdr.GetName(i);
                        if (string.IsNullOrEmpty(name))
                        {
                            name = "Empty_" + i;
                        }
                        bool isHiddenField = hiddenFields.IndexOf("," + name + ",", StringComparison.OrdinalIgnoreCase) > -1;
                        if (!isHiddenField)
                        {
                            mStruct = new MCellStruct(name, DataType.GetSqlType(sdr.GetFieldType(i)));
                            mStruct.ReaderIndex = i;
                            mTable.Columns.Add(mStruct);
                        }
                    }
                }
                else if (dt != null && dt.Rows.Count > 0)
                {
                    mTable.Columns = TableSchema.GetColumnByTable(dt, sdr, false);
                    mTable.Columns.DataBaseType = DalCreate.GetDalTypeByReaderName(sdr.GetType().Name);
                }
                #endregion
                if (sdr.HasRows)
                {
                    MDataRow mRecord = null;
                    //SQLite �������ַ���δ��ʶ��Ϊ��Ч�� DateTime��������sr[���Ͷ�]��Ҫ��sdr.GetString��
                    List<int> errIndex = new List<int>();//SQLite�ṩ��dll�����ף�sdr[x]����ת����ʱ����ֱ�����쳣
                    while (sdr.Read())
                    {
                        if (mTable.Rows.Count == 0)
                        {
                            object[] obj = new object[3];
                            sdr.GetValues(obj);
                        }
                        #region ��������
                        mRecord = mTable.NewRow(true);

                        for (int i = 0; i < mTable.Columns.Count; i++)
                        {
                            MCellStruct ms = mTable.Columns[i];
                            object value = null;
                            try
                            {
                                if (errIndex.Contains(i))
                                {
                                    value = sdr.GetString(ms.ReaderIndex);
                                }
                                else
                                {
                                    value = sdr[ms.ReaderIndex];
                                }
                            }
                            catch
                            {
                                if (!errIndex.Contains(i))
                                {
                                    errIndex.Add(i);
                                }
                                value = sdr.GetString(ms.ReaderIndex);
                            }

                            if (value == null || value == DBNull.Value)
                            {
                                mRecord[i].Value = DBNull.Value;
                                mRecord[i].State = 0;
                            }
                            else if (Convert.ToString(value) == string.Empty)
                            {
                                mRecord[i].Value = string.Empty;
                                mRecord[i].State = 1;
                            }
                            else
                            {
                                mRecord[i].Value = value; //sdr.GetValue(i); �ô˸�ֵ���ڲ����������ת����
                                mRecord[i].State = 1;//��ʼʼ״̬Ϊ1
                            }

                        }
                        #endregion
                    }
                }
            }
            return mTable;
        }
        /// <summary>
        /// ��List�б�����س�MDataTable
        /// </summary>
        /// <param name="entityList">ʵ���б����</param>
        /// <returns></returns>
        public static MDataTable CreateFrom(object entityList, BreakOp op)
        {
            if (entityList is MDataTable) { return entityList as MDataTable; }
            if (entityList is MDataView)//Ϊ�˴���UI�󶨵ı��˫���ٵ����
            {
                return ((MDataView)entityList).Table;
            }
            MDataTable dt = new MDataTable();
            if (entityList != null)
            {
                try
                {
                    bool isObj = true;
                    Type t = entityList.GetType();
                    dt.TableName = t.Name;
                    if (t.IsGenericType)
                    {
                        #region ������ͷ
                        Type[] types;
                        int len = ReflectTool.GetArgumentLength(ref t, out types);
                        if (len == 2)//�ֵ�
                        {
                            if (t.Name == "KeyCollection")
                            {
                                Type objType = types[0];
                                dt.TableName = objType.Name;
                                dt.Columns = TableSchema.GetColumnByType(objType);
                            }
                            else if (t.Name == "ValueCollection")
                            {
                                Type objType = types[1];
                                dt.TableName = objType.Name;
                                dt.Columns = TableSchema.GetColumnByType(objType);
                            }
                            else
                            {
                                dt.Columns.Add("Key", DataType.GetSqlType(types[0]));
                                dt.Columns.Add("Value", DataType.GetSqlType(types[1]));
                            }
                        }
                        else
                        {
                            Type objType = types[0];
                            if ((objType.FullName.StartsWith("System.") && objType.FullName.Split('.').Length == 2) || objType.IsEnum)//ϵͳ���͡�
                            {
                                isObj = false;
                                string name = objType.Name.Split('`')[0];
                                if (name.StartsWith("Nullable"))
                                {
                                    name = Nullable.GetUnderlyingType(objType).Name;
                                }
                                dt.Columns.Add(name, DataType.GetSqlType(objType), false);
                            }
                            else
                            {
                                dt.TableName = objType.Name;
                                dt.Columns = TableSchema.GetColumnByType(objType);
                            }
                        }
                        #endregion
                    }
                    else if (entityList is Hashtable)
                    {
                        dt.Columns.Add("Key");
                        dt.Columns.Add("Value");
                    }
                    else if (entityList is DbParameterCollection)
                    {
                        dt.Columns = TableSchema.GetColumnByType(typeof(DbParameter));
                    }
                    else
                    {
                        if (entityList is IEnumerable)
                        {
                            isObj = false;
                            dt.Columns.Add(t.Name.Replace("[]", ""), SqlDbType.Variant, false);
                        }
                        else
                        {
                            MDataRow row = MDataRow.CreateFrom(entityList);
                            if (row != null)
                            {
                                return row.Table;
                            }
                        }
                    }
                    foreach (object o in entityList as IEnumerable)
                    {
                        MDataRow row = dt.NewRow();
                        if (isObj)
                        {
                            row.LoadFrom(o, op, 1);//��ʼֵ״̬Ϊ1
                        }
                        else
                        {
                            row.Set(0, o, 1);
                        }
                        dt.Rows.Add(row, false);
                    }
                }
                catch (Exception err)
                {
                    Log.Write(err, LogType.Error);
                }
            }
            return dt;
        }
        public static MDataTable CreateFrom(object entityList)
        {
            return CreateFrom(entityList, BreakOp.None);
        }
        public static MDataTable CreateFrom(NameObjectCollectionBase noc)
        {
            MDataTable dt = new MDataTable();
            if (noc != null)
            {
                if (noc is NameValueCollection)
                {
                    dt.TableName = "NameValueCollection";
                    dt.Columns.Add("Key,Value");
                    NameValueCollection nv = noc as NameValueCollection;
                    foreach (string key in nv)
                    {
                        dt.NewRow(true).Sets(0, key, nv[key]);
                    }
                }
                else if (noc is HttpCookieCollection)
                {
                    dt.TableName = "HttpCookieCollection";
                    HttpCookieCollection nv = noc as HttpCookieCollection;
                    dt.Columns.Add("Name,Value,HttpOnly,Domain,Expires,Path");
                    for (int i = 0; i < nv.Count; i++)
                    {
                        HttpCookie cookie = nv[i];
                        dt.NewRow(true).Sets(0, cookie.Name, cookie.Value, cookie.HttpOnly, cookie.Domain, cookie.Expires, cookie.Path);
                    }
                }
                else
                {
                    dt = CreateFrom(noc as IEnumerable);
                }
            }
            return dt;
        }

        /// <summary>
        /// ��Json��Xml�ַ��������س�MDataTable
        /// </summary>
        public static MDataTable CreateFrom(string jsonOrXml)
        {
            return CreateFrom(jsonOrXml, null);
        }
        public static MDataTable CreateFrom(string jsonOrXml, MDataColumn mdc)
        {
            return CreateFrom(jsonOrXml, mdc, JsonHelper.DefaultEscape);
        }
        /// <summary>
        /// ��Json��Xml�ַ��������س�MDataTable
        /// </summary>
        public static MDataTable CreateFrom(string jsonOrXml, MDataColumn mdc, EscapeOp op)
        {
            if (!string.IsNullOrEmpty(jsonOrXml))
            {
                if (jsonOrXml[0] == '<' || jsonOrXml.EndsWith(".xml"))
                {
                    return CreateFromXml(jsonOrXml, mdc);
                }
                else
                {
                    return JsonHelper.ToMDataTable(jsonOrXml, mdc, op);
                }
            }
            return new MDataTable();
        }
        internal static MDataTable CreateFromXml(string xmlOrFileName, MDataColumn mdc)
        {
            MDataTable dt = new MDataTable();
            if (mdc != null)
            {
                dt.Columns = mdc;
            }
            if (string.IsNullOrEmpty(xmlOrFileName))
            {
                return dt;
            }
            xmlOrFileName = xmlOrFileName.Trim();
            XmlDocument doc = new XmlDocument();
            bool loadOk = false;
            if (!xmlOrFileName.StartsWith("<"))//�������ļ�·��
            {
                dt.TableName = Path.GetFileNameWithoutExtension(xmlOrFileName);
                dt.Columns = MDataColumn.CreateFrom(xmlOrFileName, false);
                if (File.Exists(xmlOrFileName))
                {
                    try
                    {
                        doc.Load(xmlOrFileName);
                        loadOk = true;
                    }
                    catch (Exception err)
                    {
                        Log.Write(err, LogType.Error);
                    }
                }
            }
            else  // xml �ַ���
            {
                try
                {
                    doc.LoadXml(xmlOrFileName);
                    loadOk = true;
                }
                catch (Exception err)
                {
                    Log.Write(err, LogType.Error);
                }
            }
            if (loadOk)
            {
                if (doc.DocumentElement.ChildNodes.Count > 0)
                {
                    dt.TableName = doc.DocumentElement.Name;
                    if (dt.Columns.Count == 0)
                    {
                        //���绯��ܹ�
                        bool useChildToGetSchema = doc.DocumentElement.ChildNodes[0].ChildNodes.Count > 0;
                        foreach (XmlNode item in doc.DocumentElement.ChildNodes)
                        {
                            if (useChildToGetSchema)
                            {
                                if (item.ChildNodes.Count > 0)//���ӽڵ�,���ӽڵ�����Ƶ��ֶ�
                                {
                                    foreach (XmlNode child in item.ChildNodes)
                                    {
                                        if (!dt.Columns.Contains(child.Name))
                                        {
                                            dt.Columns.Add(child.Name);
                                        }
                                    }
                                }
                            }
                            else//�����ӽڵ㣬�õ�ǰ�ڵ�����Ե��ֶ�
                            {
                                if (item.Attributes != null && item.Attributes.Count > 0)//���ӽڵ�,���ӽڵ�����Ƶ��ֶ�
                                {
                                    foreach (XmlAttribute attr in item.Attributes)
                                    {
                                        if (!dt.Columns.Contains(attr.Name))
                                        {
                                            dt.Columns.Add(attr.Name);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                MDataRow dr = null;
                foreach (XmlNode row in doc.DocumentElement.ChildNodes)
                {
                    dr = dt.NewRow();
                    if (row.ChildNodes.Count > 0)//���ӽڵ㴦��
                    {
                        foreach (XmlNode cell in row.ChildNodes)
                        {
                            if (!cell.InnerXml.StartsWith("<![CDATA["))
                            {
                                dr.Set(cell.Name, cell.InnerXml.Trim(), 1);
                            }
                            else
                            {
                                dr.Set(cell.Name, cell.InnerText.Trim(), 1);
                            }
                        }
                        dt.Rows.Add(dr);
                    }
                    else if (row.Attributes != null && row.Attributes.Count > 0) //�����Դ���
                    {
                        foreach (XmlAttribute cell in row.Attributes)
                        {
                            dr.Set(cell.Name, cell.Value.Trim(), 1);
                        }
                        dt.Rows.Add(dr);
                    }

                }
            }
            return dt;
        }
        #endregion

    }
}
