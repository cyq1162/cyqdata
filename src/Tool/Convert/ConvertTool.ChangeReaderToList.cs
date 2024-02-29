using CYQ.Data.Emit;
using CYQ.Data.Json;
using CYQ.Data.SQL;
using CYQ.Data.Table;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;

namespace CYQ.Data.Tool
{
    /// <summary>
    /// 类型转换（支持json转实体）
    /// </summary>
    public static partial class ConvertTool
    {

        /// <summary>
        /// DbDataReader To List
        /// </summary>
        internal static List<T> ChangeReaderToList<T>(DbDataReader reader)
        {
            if (reader == null) { return null; }

            if (!reader.HasRows || reader.FieldCount == 0) { return new List<T>(); }
            try
            {
                Type t = typeof(T);
                if (t.IsValueType || t.Name == "String")
                {
                    return ReaderToListValueType<T>(reader);
                }
                if (t.Name == "MDataRow")
                {
                    return ReaderToListMDataRow<T>(reader);
                }
                return ReaderToListEntity<T>(reader);
                //List<T> list = new List<T>();
                //SetEntityTypeFromReader<T>(reader, list, t);
                //return list;
            }
            finally
            {
                reader.Close();
                reader.Dispose();
                reader = null;
            }
        }
        private static List<T> ReaderToListValueType<T>(DbDataReader reader)
        {
            List<T> list = new List<T>();
            Type t = typeof(T);
            bool isError = false;
            bool isT = reader.GetFieldType(0) == t;
            while (reader.Read())
            {
                object value;
                try
                {
                    if (isError)
                    {
                        //兼容Sqlite的底版本异常。
                        value = reader.GetString(0);
                    }
                    else
                    {
                        value = reader[0];
                    }
                }
                catch
                {
                    isError = true;
                    value = reader.GetString(0);
                }
                if (!isT)
                {
                    value = ConvertTool.ChangeType(value, t);
                }
                list.Add((T)value);
            }
            return list;
        }
        private static List<T> ReaderToListMDataRow<T>(DbDataReader reader)
        {
            List<T> list = new List<T>();
            MDataTable dt = reader;
            foreach (object row in dt.Rows)
            {
                list.Add((T)row);
            }
            return list;
        }

        private static List<T> ReaderToListEntity<T>(DbDataReader reader)
        {
            List<T> list = new List<T>();
            var func = DbDataReaderToEntity.Delegate(typeof(T));
            while (reader.Read())
            {
                object obj = func(reader);
                if (obj != null)
                {
                    list.Add((T)obj);
                }
            }
            return list;
        }

        /*
        private static void SetEntityTypeFromReader<T>(DbDataReader reader, List<T> list, Type t)
        {
            Dictionary<string, Type> kv = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string key = reader.GetName(i);
                if (!kv.ContainsKey(key))//ado.net 底层bug，特殊情况会读出多余重复字段结构，见：https://q.cnblogs.com/q/143699/
                {
                    kv.Add(key, reader.GetFieldType(i));
                }
            }


            List<PropertyInfo> pInfoList = ReflectTool.GetPropertyList(t);
            List<FieldInfo> fInfoList = ReflectTool.GetFieldList(t);

            //SQLite 报错：该字符串未被识别为有效的 DateTime，不能用sr[类型读]，要用sdr.GetString读
            // List<string> errIndex = new List<string>();//SQLite提供的dll不靠谱，sdr[x]类型转不过时，会直接抛异常
            Dictionary<string, int> errIndex = new Dictionary<string, int>();
            while (reader.Read())
            {
                T obj = Activator.CreateInstance<T>();

                if (pInfoList.Count > 0)
                {
                    foreach (PropertyInfo p in pInfoList)//遍历实体
                    {
                        if (p.CanWrite && kv.ContainsKey(p.Name))
                        {
                            object objValue = null;
                            try
                            {
                                if (errIndex.ContainsKey(p.Name))
                                {
                                    int index = errIndex[p.Name];
                                    if (!reader.IsDBNull(index))
                                    {
                                        objValue = reader.GetString(index);
                                    }
                                }
                                else
                                {
                                    objValue = reader[p.Name];
                                }
                            }
                            catch
                            {
                                int index = reader.GetOrdinal(p.Name);
                                if (!errIndex.ContainsKey(p.Name))
                                {
                                    errIndex.Add(p.Name, index);
                                }
                                objValue = reader.GetString(index);
                            }

                            if (objValue != null && objValue != DBNull.Value)
                            {
                                if (p.PropertyType != kv[p.Name] || errIndex.ContainsKey(p.Name))
                                {
                                    objValue = ChangeType(objValue, p.PropertyType);//尽量避免类型转换
                                }
                                p.SetValue(obj, objValue, null);
                            }
                        }
                    }
                }
                if (fInfoList.Count > 0)
                {
                    foreach (FieldInfo f in fInfoList)//遍历实体
                    {
                        if (kv.ContainsKey(f.Name))
                        {
                            object objValue = null;
                            try
                            {
                                if (errIndex.ContainsKey(f.Name))
                                {
                                    objValue = reader.GetString(errIndex[f.Name]);
                                }
                                else
                                {
                                    objValue = reader[f.Name];
                                }
                            }
                            catch
                            {
                                int index = reader.GetOrdinal(f.Name);
                                errIndex.Add(f.Name, index);
                                objValue = reader.GetString(index);
                            }
                            if (objValue != null && objValue != DBNull.Value)
                            {
                                if (f.FieldType != kv[f.Name] || errIndex.ContainsKey(f.Name))
                                {
                                    objValue = ChangeType(objValue, f.FieldType);//尽量避免类型转换
                                }
                                f.SetValue(obj, objValue);
                            }
                        }
                    }
                }
                list.Add(obj);
            }
        }
        */
        /// <summary>
        /// DbDataReader To Json
        /// </summary>
        internal static string ChangeReaderToJson(DbDataReader reader, JsonHelper js, bool needCheckHiddenField)
        {
            if (js == null)
            {
                js = new JsonHelper(false, false);
            }
            if (reader != null)
            {
                bool[] isBool = new bool[reader.FieldCount];
                bool[] isDateTime = new bool[reader.FieldCount];
                bool[] isNoQuotes = new bool[reader.FieldCount];
                bool[] isHiddenField = new bool[reader.FieldCount];
                string hiddenFields = "," + AppConfig.DB.HiddenFields.ToLower() + ",";
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (needCheckHiddenField)
                    {
                        isHiddenField[i] = hiddenFields.IndexOf("," + reader.GetName(i) + ",", StringComparison.OrdinalIgnoreCase) > -1;
                    }
                    switch (DataType.GetGroup(DataType.GetSqlType(reader.GetFieldType(i))))
                    {
                        case DataGroupType.Bool:
                            isBool[i] = true;
                            isNoQuotes[i] = true;
                            break;
                        case DataGroupType.Number:
                            isNoQuotes[i] = true;
                            break;
                        case DataGroupType.Date:
                            isDateTime[i] = true;
                            break;
                    }
                }
                #region Reader
                //SQLite 报错：该字符串未被识别为有效的 DateTime，不能用sr[类型读]，要用sdr.GetString读
                // List<string> errIndex = new List<string>();//SQLite提供的dll不靠谱，sdr[x]类型转不过时，会直接抛异常
                bool[] errIndex = new bool[reader.FieldCount];
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        js.RowCount++;
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (isHiddenField[i]) { continue; }
                            object objValue = null;
                            try
                            {
                                if (errIndex[i])
                                {
                                    objValue = reader.GetString(i);
                                }
                                else
                                {
                                    objValue = reader.GetValue(i);
                                }
                            }
                            catch
                            {
                                errIndex[i] = true;
                                objValue = reader.GetString(i);
                            }


                            if (isDateTime[i])
                            {
                                DateTime dt;
                                if (DateTime.TryParse(Convert.ToString(objValue), out dt))
                                {
                                    objValue = dt.ToString(js.JsonOp.DateTimeFormatter);
                                }
                            }
                            else if (isBool[i])
                            {
                                objValue = objValue.ToString().ToLower();
                            }

                            js.Add(reader.GetName(i), Convert.ToString(objValue), isNoQuotes[i]);
                        }
                        js.AddBr();
                    }
                }
                reader.Close();
                reader.Dispose();
                reader = null;
                #endregion
            }
            return js.ToString();
        }
    }
}
