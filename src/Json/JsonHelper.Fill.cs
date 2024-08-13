using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using CYQ.Data.Table;
using System.Data;
using System.Text.RegularExpressions;
using CYQ.Data.SQL;
using System.IO;
using System.Reflection;
using CYQ.Data.Xml;
using System.ComponentModel;
using CYQ.Data.Tool;
using CYQ.Data.Emit;

namespace CYQ.Data.Json
{

    // 扩展交互部分
    public partial class JsonHelper
    {

        private bool CheckIsLoop(object value)
        {
            if (value is ValueType) { return false; }
            int hash = value.GetHashCode();
            //检测是否循环引用
            if (JsonOp.LoopCheckList.ContainsKey(hash))
            {
                //continue;
                int level = JsonOp.LoopCheckList[hash];
                if (level < JsonOp.Level)
                {
                    return true;
                }
                //else
                //{
                //    JsonOp.LoopCheckList[hash] = JsonOp.Level;//更新级别
                //}
            }
            else
            {
                JsonOp.LoopCheckList.Add(hash, JsonOp.Level);
            }
            return false;
        }

        /// <summary>
        /// Fill obj and get json from  ToString() method
        /// <para>从数据表中取数据填充,最终可输出json字符串</para>
        /// </summary>
        public void Fill(MDataTable table)
        {
            if (table == null)
            {
                ErrorMsg = "MDataTable object is null";
                return;
            }
            if (_AddSchema)
            {
                Fill(table.Columns, false);
            }
            //RowCount = table.Rows.Count;
            Total = table.RecordsAffected;

            if (table.Rows.Count > 0)
            {
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    Fill(table.Rows[i]);
                }
            }
        }
        /// <summary>
        /// Fill obj and get json from  ToString() method
        /// <para>从数据行中取数据填充,最终可输出json字符串</para>
        /// </summary>
        public void Fill(MDataRow row)
        {
            if (row == null)
            {
                ErrorMsg = "MDataRow object is null";
                return;
            }
            var rowOp = this.JsonOp.RowOp;
            for (int i = 0; i < row.Count; i++)
            {
                MDataCell cell = row[i];
                if (cell.IsJsonIgnore)
                {
                    continue;
                }
                if (rowOp == RowOp.None || (!cell.IsNull && (cell.Struct.IsPrimaryKey || cell.State >= (int)rowOp)))
                {
                    #region MyRegion
                    string name = cell.ColumnName;
                    switch (this.JsonOp.NameCaseOp)
                    {
                        case NameCaseOp.ToUpper:
                            name = name.ToUpper(); break;
                        case NameCaseOp.ToLower:
                            name = name.ToLower(); break;
                    }
                    Add(name, cell.Value);
                    //string value = cell.ToString();
                    //DataGroupType group = DataType.GetGroup(cell.Struct.SqlType);
                    //bool noQuot = group == DataGroupType.Number || group == DataGroupType.Bool;
                    //if (cell.IsNull)
                    //{
                    //    value = "null";
                    //    noQuot = true;
                    //}
                    //else
                    //{

                    //    if (group == DataGroupType.Bool || (cell.Struct.MaxSize == 1 && group == DataGroupType.Number)) // oracle 下的number 1会处理成bool类型
                    //    {
                    //        value = value.ToLower();
                    //    }
                    //    else if (group == DataGroupType.Date)
                    //    {
                    //        DateTime dt;
                    //        if (DateTime.TryParse(value, out dt))
                    //        {
                    //            value = dt.ToString(this.JsonOp.DateTimeFormatter);
                    //        }
                    //    }
                    //    else if (group == DataGroupType.Object)
                    //    {
                    //        if (CheckIsLoop(cell.Value)) { continue; }
                    //        Type t = cell.Struct.ValueType;
                    //        if (t.FullName == "System.Object")
                    //        {
                    //            t = cell.Value.GetType();
                    //        }
                    //        if (t.Name == "Byte[]")
                    //        {
                    //            value = Convert.ToBase64String(cell.Value as byte[]);
                    //        }
                    //        else if (t.Name == "String")
                    //        {
                    //            value = cell.StringValue;
                    //        }
                    //        else
                    //        {
                    //            if (cell.Value is IEnumerable)
                    //            {
                    //                int len = ReflectTool.GetArgumentLength(ref t);
                    //                if (len <= 1)//List<T>
                    //                {
                    //                    JsonHelper js = new JsonHelper(false, false, this.JsonOp.Clone());
                    //                    if (cell.Value is MDataRowCollection)
                    //                    {
                    //                        MDataTable dtx = (MDataRowCollection)cell.Value;
                    //                        js.Fill(dtx);
                    //                    }
                    //                    else
                    //                    {
                    //                        js.Fill(cell.Value);
                    //                    }
                    //                    value = js.ToString(true);
                    //                    noQuot = true;
                    //                }
                    //                else if (len == 2)//Dictionary<T,K>
                    //                {
                    //                    MDataRow dicRow = MDataRow.CreateFrom(cell.Value);
                    //                    value = dicRow.ToJson(this.JsonOp.Clone());
                    //                    noQuot = true;
                    //                }
                    //            }
                    //            else
                    //            {
                    //                if (!t.FullName.StartsWith("System."))//普通对象。
                    //                {
                    //                    MDataRow oRow = new MDataRow(TableSchema.GetColumnByType(t));
                    //                    oRow.LoadFrom(cell.Value);
                    //                    value = oRow.ToJson(this.JsonOp.Clone());
                    //                    noQuot = true;
                    //                }
                    //                else if (t.FullName == "System.Data.DataTable")
                    //                {
                    //                    MDataTable dt = cell.Value as DataTable;
                    //                    value = dt.ToJson(false, false, this.JsonOp.Clone());
                    //                    noQuot = true;
                    //                }
                    //            }

                    //        }

                    //    }
                    //}
                    //Add(name, value, noQuot);

                    #endregion
                }

            }
            AddBr();
        }
        /// <summary>
        /// 从数据结构填充，最终可输出json字符串。
        /// </summary>
        /// <param name="column">数据结构</param>
        /// <param name="isFullSchema">false：输出单行的[列名：数据类型]；true：输出多行的完整的数据结构</param>
        public void Fill(MDataColumn column, bool isFullSchema)
        {
            if (column == null)
            {
                ErrorMsg = "MDataColumn object is null";
                return;
            }

            if (isFullSchema)
            {
                if (!string.IsNullOrEmpty(column.TableName))
                {
                    _AddHead = true;
                    headText.Append("{");
                    headText.Append("\"TableName\":\"" + column.TableName + "\",");
                    headText.Append("\"Description\":\"" + column.Description + "\",");
                    headText.Append("\"RelationTables\":\"" + string.Join(",", column.RelationTables.ToArray()) + "\",");
                    headText.Append("\"Columns\":");
                }
                foreach (MCellStruct item in column)
                {
                    Add("ColumnName", item.ColumnName);
                    Add("SqlType", item.ValueType.FullName);
                    Add("SqlTypeName", item.SqlTypeName);
                    Add("IsAutoIncrement", item.IsAutoIncrement.ToString().ToLower(), true);
                    Add("IsCanNull", item.IsCanNull.ToString().ToLower(), true);
                    Add("MaxSize", item.MaxSize.ToString(), true);
                    Add("Scale", item.Scale.ToString().ToLower(), true);
                    Add("IsPrimaryKey", item.IsPrimaryKey.ToString().ToLower(), true);
                    Add("DefaultValue", Convert.ToString(item.DefaultValue));
                    Add("Description", item.Description);
                    //新增属性
                    Add("TableName", item.TableName);
                    Add("IsUniqueKey", item.IsUniqueKey.ToString().ToLower(), true);
                    Add("IsForeignKey", item.IsForeignKey.ToString().ToLower(), true);
                    Add("FKTableName", item.FKTableName);

                    AddBr();
                }
            }
            else
            {
                for (int i = 0; i < column.Count; i++)
                {
                    Add(column[i].ColumnName, column[i].ValueType.FullName);
                }
                AddBr();
            }
            rowCount = 0;//重置为0
        }
        /// <summary>
        ///  Fill obj and get json from  ToString() method
        /// <para>可从类(对象,泛型List、泛型Dictionary）中填充，最终可输出json字符串。</para>
        /// </summary>
        /// <param name="obj">实体类对象</param>
        public void Fill(object obj)
        {
            FillObject(obj, null);
        }

        private void FillObject(object obj, Type entityType)
        {
            if (obj != null)
            {
                if (CheckIsLoop(obj)) { return; }
                if (FillAs(obj)) { return; }
                else if (obj is IEnumerable)
                {
                    #region IEnumerable
                    Type t = obj.GetType();
                    Type[] argTypes;
                    int len = ReflectTool.GetArgumentLength(ref t, out argTypes);
                    if (len == 1)
                    {
                        var objIEnumerable = obj as IEnumerable;
                        Type objType = argTypes[0];
                        foreach (object o in objIEnumerable)
                        {
                            if (objType.Name == "Object") { objType = o.GetType(); }
                            FillObject(o, objType);
                        }
                    }
                    else if (len == 2)
                    {
                        if (t.Name.StartsWith("MDictionary") && argTypes[0].Name == "String")
                        {
                            List<string> items = t.GetMethod("GetKeys").Invoke(obj, new object[0]) as List<string>;
                            IEnumerable values = t.GetMethod("GetValues").Invoke(obj, new object[0]) as IEnumerable;
                            int i = 0;
                            foreach (object value in values)
                            {
                                Add(items[i], value);
                                i++;
                            }
                        }
                        else if (t.Name.StartsWith("Dictionary") && argTypes[0].Name == "String")
                        {
                            IEnumerable keys = t.GetProperty("Keys").GetValue(obj, null) as IEnumerable;
                            IEnumerable values = t.GetProperty("Values").GetValue(obj, null) as IEnumerable;

                            List<string> items = new List<string>();
                            foreach (string key in keys)
                            {
                                items.Add(key);
                            }
                            int i = 0;
                            foreach (object value in values)
                            {
                                Add(items[i], value);
                                i++;
                            }
                        }
                        else
                        {
                            Fill(MDataRow.CreateFrom(obj, t));
                        }
                    }
                    #endregion
                }
                else
                {
                    FillEntity(obj, entityType);
                }
            }
        }

        public void Fill(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Trim();
                char start = text[0];
                char end = text[text.Length - 1];
                if ((start == '{' && end == '}') || (start == '[' && end == ']'))
                {
                    FillJson(text);
                }
                else if (text.Contains("="))
                {
                    FillQuery(text);
                }
                else
                {
                    FillItem(text);
                }
            }
        }

        #region Fill String
        //填充完整 Json 项
        private void FillJson(string json)
        {
            json = json.TrimStart('{', '[').TrimEnd('}', ']');
            bodyItems.Add(json);
            AddBr();
        }
        // 可能是数组的值,字符串 或 值类型
        private void FillItem(string item)
        {
            bodyItems.Add("\"" + item + "\"");
            //bodyItems.Add(item);
        }
        //填充查询字符串
        private void FillQuery(string query)
        {
            query = query.Trim('?');
            string[] items = query.Split('&');
            foreach (string item in items)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    int index = item.IndexOf('=');
                    if (index > -1)
                    {
                        Add(item.Substring(0, index), item.Substring(index + 1, item.Length - index - 1));
                    }
                    else
                    {
                        Add(item, "");
                    }
                }
            }
        }
        #endregion

        public void Fill(Dictionary<string, string> dic)
        {
            foreach (var item in dic)
            {
                Add(item.Key, item.Value);
            }
        }

        public void Fill(MDictionary<string, string> dic)
        {
            List<string> items = dic.GetKeys();
            foreach (var item in items)
            {
                Add(item, dic[item]);
            }
        }
        public void Fill(Dictionary<string, object> dic)
        {
            foreach (var item in dic)
            {
                Add(item.Key, item.Value);
            }
        }

        public void Fill(MDictionary<string, object> dic)
        {
            List<string> items = dic.GetKeys();
            foreach (var item in items)
            {
                Add(item, dic[item]);
            }
        }

        public void Fill(ValueType value)
        {
            if (value is DateTime)
            {
                DateTime dt = (DateTime)value;
                bodyItems.Add("\"" + dt.ToString(this.JsonOp.DateTimeFormatter) + "\"");
            }
            else if (value is Enum || value is Guid)
            {
                string str = Convert.ToString(value);
                bodyItems.Add("\"" + str + "\"");
            }
            else
            {
                string str = Convert.ToString(value);
                bodyItems.Add(str);
            }
        }


        /// <summary>
        /// 加载实体
        /// </summary>
        private void FillEntity(object entity, Type entityType)
        {
            if (entityType == null)
            {
                entityType = entity.GetType();
            }

            var func = JsonHelperFillEntity.Delegate(entityType);
            func(this, entity);


            //var funcToDic = EntityToDictionary.Delegate(entityType);

            //var dic = funcToDic(entity);
            //Fill(dic);


            //    List<PropertyInfo> pList = ReflectTool.GetPropertyList(entityType);
            //    if (pList.Count > 0)
            //    {
            //        foreach (PropertyInfo item in pList)
            //        {
            //            SetJson(entity, item, null);
            //        }
            //    }
            //    List<FieldInfo> fList = ReflectTool.GetFieldList(entityType);
            //    if (fList.Count > 0)
            //    {
            //        foreach (FieldInfo item in fList)
            //        {
            //            SetJson(entity, null, item);
            //        }
            //    }

            AddBr();
        }
        /*
        private void SetJson(object entity, PropertyInfo pi, FieldInfo fi)
        {
            if (ReflectTool.ExistsAttr(AppConst.JsonIgnoreType, pi, fi))//获取Json忽略标识
            {
                return;//被Json忽略的列，不在返回列结构中。
            }
            string name = pi != null ? pi.Name : fi.Name;
            object objValue = null;
            var getterFunc = EntityGetter.GetterFunc(pi, fi);
            if (getterFunc != null)
            {
                objValue = getterFunc(entity);
            }
            else
            {
                objValue = pi != null ? pi.GetValue(entity, null) : fi.GetValue(entity);
            }
            Type type = pi != null ? pi.PropertyType : fi.FieldType;
            string dateFormat = this.JsonOp.DateTimeFormatter;
            if (type.IsEnum)
            {
                if (ReflectTool.ExistsAttr(AppConst.JsonEnumToStringType, pi, fi))
                {
                    objValue = objValue.ToString();
                    type = typeof(String);
                }
                else if (ReflectTool.ExistsAttr(AppConst.JsonEnumToDescriptionType, pi, fi))
                {
                    FieldInfo field = type.GetField(objValue.ToString());
                    if (field != null)
                    {
                        DescriptionAttribute da = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                        if (da != null)
                        {
                            objValue = da.Description;
                            type = typeof(String);
                        }
                    }
                }
                else
                {
                    objValue = (int)objValue;
                }
            }
            else if (type.FullName.Contains("System.DateTime") && ReflectTool.ExistsAttr(AppConst.JsonFormatType, pi, fi))
            {
                JsonFormatAttribute jf = ReflectTool.GetAttr<JsonFormatAttribute>(pi, fi);
                if (jf != null)
                {
                    dateFormat = jf.DatetimeFormat;
                }
            }

            SetNameValue(name, objValue, type, dateFormat);
        }
       
        private void SetNameValue(string name, object objValue, Type valueType, string dateFormat)
        {
            switch (this.JsonOp.NameCaseOp)
            {
                case NameCaseOp.ToUpper:
                    name = name.ToUpper(); break;
                case NameCaseOp.ToLower:
                    name = name.ToLower(); break;
            }
            string value = null;
            bool noQuot = false;
            if (objValue == null || objValue == DBNull.Value)
            {
                if (this.JsonOp.RowOp == Table.RowOp.IgnoreNull)
                {
                    return;
                }
                value = "null";
                noQuot = true;
            }
            else
            {
                if (valueType.FullName == "System.Object")
                {
                    valueType = objValue.GetType();//定位到指定类型。
                }

                value = Convert.ToString(objValue);
                DataGroupType group = DataType.GetGroup(DataType.GetSqlType(valueType));
                noQuot = group == DataGroupType.Number || group == DataGroupType.Bool;
                #region 处理非Null的情况

                if (group == DataGroupType.Bool) // oracle 下的number 1会处理成bool类型
                {
                    value = value.ToLower();
                }
                else if (group == DataGroupType.Date)
                {
                    DateTime dt;
                    if (DateTime.TryParse(value, out dt))
                    {
                        value = dt.ToString(dateFormat);
                    }
                }
                else if (group == DataGroupType.Object)
                {
                    #region 处理对象及循环引用。

                    int hash = objValue.GetHashCode();
                    //检测是否循环引用
                    if (LoopCheckList.ContainsKey(hash))
                    {
                        //continue;
                        int level = LoopCheckList[hash];
                        if (level < Level)
                        {
                            return;
                        }
                        else
                        {
                            LoopCheckList[hash] = Level;//更新级别
                        }
                    }
                    else
                    {
                        LoopCheckList.Add(hash, Level);
                    }
                    if (valueType.Name == "Byte[]")
                    {
                        value = Convert.ToBase64String(objValue as byte[]);
                    }
                    else
                    {
                        JsonHelper js = new JsonHelper(false, false, this.JsonOp);
                        js.Level = Level + 1;
                        js.LoopCheckList = LoopCheckList;
                        js.Fill(objValue);
                        value = js.ToString(objValue is IList || objValue is MDataTable || objValue is DataTable);
                        noQuot = true;


                        //if (objValue is IEnumerable)
                        //{
                        //    int len = ReflectTool.GetArgumentLength(ref valueType);
                        //    if (len <= 1)//List<T>
                        //    {
                        //        JsonHelper js = new JsonHelper(false, false);
                        //        js.Level = Level + 1;
                        //        js.LoopCheckList = LoopCheckList;
                        //        js.Escape = Escape;
                        //        js._RowOp = _RowOp;
                        //        js.DateTimeFormatter = dateFormat;
                        //        js.IsConvertNameToLower = IsConvertNameToLower;
                        //        if (objValue is MDataRowCollection)
                        //        {
                        //            MDataTable dtx = (MDataRowCollection)objValue;
                        //            js.Fill(dtx);
                        //        }
                        //        else
                        //        {
                        //            js.Fill(objValue);
                        //        }
                        //        value = js.ToString(true);
                        //        noQuot = true;
                        //    }
                        //    else if (len == 2)//Dictionary<T,K>
                        //    {
                        //        MDataRow dicRow = MDataRow.CreateFrom(objValue);
                        //        dicRow.DynamicData = LoopCheckList;
                        //        value = dicRow.ToJson(RowOp, IsConvertNameToLower, Escape);
                        //        noQuot = true;
                        //    }
                        //}
                        //else
                        //{
                        //    if (!valueType.FullName.StartsWith("System."))//普通对象。
                        //    {
                        //        MDataRow oRow = new MDataRow(TableSchema.GetColumnByType(valueType));
                        //        oRow.DynamicData = LoopCheckList;
                        //        oRow.LoadFrom(objValue);
                        //        value = oRow.ToJson(RowOp, IsConvertNameToLower, Escape);
                        //        noQuot = true;
                        //    }
                        //    else if (valueType.FullName == "System.Data.DataTable")
                        //    {
                        //        MDataTable dt = objValue as DataTable;
                        //        dt.DynamicData = LoopCheckList;
                        //        value = dt.ToJson(false, false, RowOp, IsConvertNameToLower, Escape);
                        //        noQuot = true;
                        //    }
                        //}

                    }
                    #endregion
                }
                #endregion
            }
            Add(name, value, noQuot);


        }
         */
        private bool FillAs(object obj)
        {
            if (obj is ValueType)
            {
                Fill(obj as ValueType);
            }
            else if (obj is String || obj is Type || obj is Type[])
            {
                Fill(Convert.ToString(obj));
            }
            else if (obj is DataTable)
            {
                MDataTable dt = obj as DataTable;
                Fill(dt);
            }
            else if (obj is DataRow)
            {
                MDataRow row = obj as DataRow;
                Fill(obj as DataRow);
            }
            else if (obj is MDataTable)
            {
                Fill(obj as MDataTable);
            }
            else if (obj is MDataRow)
            {
                Fill(obj as MDataRow);
            }
            else if (obj is DataRowCollection)
            {
                MDataTable dt = (MDataRowCollection)obj;
                Fill(dt);
            }
            else if (obj is MDataRowCollection)
            {
                MDataTable dt = (MDataRowCollection)obj;
                Fill(dt);
            }
            else if (obj is DataColumnCollection)
            {
                MDataColumn mdc = obj as DataColumnCollection;
                Fill(mdc, true);
            }
            else if (obj is Dictionary<string, string>)
            {
                Fill(obj as Dictionary<string, string>);//避开转Row，提升性能
            }
            else if (obj is MDictionary<string, string>)
            {
                Fill(obj as MDictionary<string, string>);//避开转Row，提升性能
            }
            else if (obj is Dictionary<string, object>)
            {
                Fill(obj as Dictionary<string, object>);//避开转Row，提升性能
            }
            else if (obj is MDictionary<string, object>)
            {
                Fill(obj as MDictionary<string, object>);//避开转Row，提升性能
            }
            else
            {
                return false;
            }
            return true;
        }
    }
}
