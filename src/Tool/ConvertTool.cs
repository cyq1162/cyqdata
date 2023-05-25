using CYQ.Data.SQL;
using CYQ.Data.Table;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Web;

namespace CYQ.Data.Tool
{
    /// <summary>
    /// 类型转换（支持json转实体）
    /// </summary>
    public static class ConvertTool
    {
        /// <summary>
        /// 类型转换(精准强大)
        /// </summary>
        /// <param name="value">值处理</param>
        /// <param name="t">类型</param>
        /// <returns></returns>
        public static object ChangeType(object value, Type t)
        {
            if (t == null) { return null; }
            string strValue = Convert.ToString(value);
            if (t.IsEnum)
            {

                if (strValue != "")
                {

                    if (Enum.IsDefined(t, strValue))
                    {
                        return Enum.Parse(t, strValue);
                    }
                    int v = 0;
                    if (int.TryParse(strValue, out v))
                    {
                        object v1 = Enum.Parse(t, strValue);
                        if (v1.ToString() != strValue)
                        {
                            return v1;
                        }
                    }
                    string[] names = Enum.GetNames(t);
                    string lower = strValue.ToLower();
                    foreach (string name in names)
                    {
                        if (name.ToLower() == lower)
                        {
                            return Enum.Parse(t, name);
                        }
                    }

                }

                //取第一个值。
                string firstKey = Enum.GetName(t, -1);
                if (!string.IsNullOrEmpty(firstKey))
                {
                    return Enum.Parse(t, firstKey);
                }
                return Enum.Parse(t, Enum.GetNames(t)[0]);

            }
            if (value == null)
            {
                return t.IsValueType ? Activator.CreateInstance(t) : null;
            }
            if (t.FullName == "System.Object")
            {
                return value;
            }
            if (t.FullName == "System.Type")
            {
                return (Type)value;
            }
            if (t.FullName == "System.IO.Stream" && value is HttpPostedFile)
            {
                return ((HttpPostedFile)value).InputStream;
            }

            if (t.IsGenericType && t.Name.StartsWith("Nullable"))
            {
                t = Nullable.GetUnderlyingType(t);
                if (strValue == "")
                {
                    return null;
                }
            }
            if (t.Name == "String")
            {
                if (value is byte[])
                {
                    return Convert.ToBase64String((byte[])value);
                }
                return strValue;
            }
            if (t.FullName == "System.Text.StringBuilder")
            {
                return value as StringBuilder;
            }
            if (t.FullName == "System.Text.Encoding")
            {
                return value as Encoding;
            }
            if (strValue.Trim() == "")
            {
                if (t.Name.EndsWith("[]")) { return null; }
                return Activator.CreateInstance(t);
            }
            else if (t.IsValueType)
            {
                strValue = strValue.Trim('\r', '\n', '\t', ' ');
                if (t.Name == "DateTime")
                {
                    switch (strValue.ToLower().TrimEnd(')', '('))
                    {
                        case "now":
                        case "getdate":
                        case "current_timestamp":
                            return DateTime.Now;
                    }
                    if (DateTime.Parse(strValue) == DateTime.MinValue)
                    {
                        return (DateTime)SqlDateTime.MinValue;
                    }
                    return Convert.ChangeType(value, t);//这里用value，避免丢失毫秒
                }
                else if (t.Name == "Guid")
                {
                    if (strValue == SqlValue.Guid || strValue.StartsWith("newid"))
                    {
                        return Guid.NewGuid();
                    }
                    else if (strValue.ToLower() == "null")
                    {
                        return Guid.Empty;
                    }
                    return new Guid(strValue);
                }
                else
                {
                    switch (strValue.ToLower())
                    {
                        case "yes":
                        case "true":
                        case "success":
                        case "1":
                        case "on":
                        case "ok":
                        case "是":
                        case "√":
                            if (t.Name == "Boolean")
                                return true;
                            else strValue = "1";
                            break;
                        case "no":
                        case "false":
                        case "fail":
                        case "0":
                        case "":
                        case "否":
                        case "null":
                            if (t.Name == "Boolean")
                                return false;
                            else strValue = "0";
                            break;
                        case "infinity":
                        case "正无穷大":
                            if (t.Name == "Double" || t.Name == "Single")
                                return double.PositiveInfinity;
                            break;
                        case "-infinity":
                        case "负无穷大":
                            if (t.Name == "Double" || t.Name == "Single")
                                return double.NegativeInfinity;
                            break;
                        default:
                            if (t.Name == "Boolean")
                                return false;
                            break;
                    }

                    if (t.Name.StartsWith("Int") || t.Name == "Byte")
                    {
                        if (strValue.IndexOf('.') > -1)//11.22
                        {
                            strValue = strValue.Split('.')[0];
                        }
                        else if (value.GetType().IsEnum)
                        {
                            return (int)value;
                        }
                    }
                }
                return Convert.ChangeType(strValue, t);
            }
            else
            {
                Type valueType = value.GetType();
                //if(valueType.IsEnum && t.is)

                if (valueType.FullName != t.FullName)
                {
                    if ((strValue.StartsWith("{") || strValue.StartsWith("[")) && (strValue.EndsWith("}") || strValue.EndsWith("]")))
                    {
                        return JsonHelper.ToEntity(t, strValue, EscapeOp.Default);
                    }
                    switch (ReflectTool.GetSystemType(ref t))
                    {
                        case SysType.Custom:
                            return MDataRow.CreateFrom(value).ToEntity(t);
                        case SysType.Collection:
                            return MDataTable.CreateFrom(value).ToList(t);
                        case SysType.Generic:
                            if (t.Name.StartsWith("List") || t.Name.StartsWith("IList") || t.Name.StartsWith("MList"))
                            {
                                return MDataTable.CreateFrom(value).ToList(t);
                            }
                            return MDataRow.CreateFrom(value).ToEntity(t);
                        case SysType.Array:
                            if (t.Name == "Byte[]")
                            {
                                if (valueType.Name == "String")
                                {
                                    return Convert.FromBase64String(strValue);
                                }
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    new BinaryFormatter().Serialize(ms, value);
                                    return ms.ToArray();
                                }
                            }
                            break;
                    }
                }
                return Convert.ChangeType(value, t);
            }
        }

        /// <summary>
        /// 类型转换(精准强大)
        /// </summary>
        public static T ChangeType<T>(object value)
        {
            return (T)ChangeType(value, typeof(T));
        }
        /// <summary>
        /// DbDataReader To List
        /// </summary>
        internal static List<T> ChangeReaderToList<T>(DbDataReader reader)
        {
            Type t = typeof(T);
            List<T> list = new List<T>();
            if (t.Name == "MDataRow")
            {
                MDataTable dt = reader;
                foreach (object row in dt.Rows)
                {
                    list.Add((T)row);
                }
            }
            else if (reader != null)
            {
                #region Reader


                if (reader.HasRows)
                {
                    Dictionary<string, Type> kv = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        kv.Add(reader.GetName(i), reader.GetFieldType(i));
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
                                            objValue = reader.GetString(errIndex[p.Name]);
                                        }
                                        else
                                        {
                                            objValue = reader[p.Name];
                                        }
                                    }
                                    catch
                                    {
                                        int index = reader.GetOrdinal(p.Name);
                                        errIndex.Add(p.Name, index);
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
                    kv.Clear();
                    kv = null;
                    errIndex.Clear();
                    errIndex = null;
                }
                reader.Close();
                reader.Dispose();
                reader = null;
                #endregion
            }
            return list;
            //return List<default(T)>;
        }

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
                                    objValue = dt.ToString(js.DateTimeFormatter);
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
