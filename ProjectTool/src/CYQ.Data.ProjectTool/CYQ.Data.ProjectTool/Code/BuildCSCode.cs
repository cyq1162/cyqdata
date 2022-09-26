using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using CYQ.Data.Table;
using CYQ.Data.SQL;
using CYQ.Data.Tool;

namespace CYQ.Data.ProjectTool
{
    class BuildCSCode
    {
        internal delegate void CreateEndHandle(int count);
        internal static event CreateEndHandle OnCreateEnd;
        private static string FormatKey(string key)
        {
            return key.Replace("-", "_").Replace(" ", "");
        }
        internal static void Create(object nameObj)
        {
            int count = 0;
            try
            {
                string name = Convert.ToString(nameObj);

                using (ProjectConfig config = new ProjectConfig())
                {
                    try
                    {
                        if (config.Fill("Name='" + name + "'"))
                        {
                            DBInfo db = DBTool.GetDBInfo(config.Conn);

                            // Dictionary<string, string> tables = Tool.DBTool.GetTables(config.Conn, out dbName);
                            if (db != null && db.Tables.Count > 0)
                            {
                                string dbName = db.DataBaseName;
                                dbName = dbName[0].ToString().ToUpper() + dbName.Substring(1, dbName.Length - 1);
                                count = db.Tables.Count;
                                if (config.BuildMode.Contains("枚举") || config.BuildMode.Contains("Enum"))//枚举型。
                                {
                                    BuildTableEnumText(db.Tables, config, dbName);

                                }
                                else
                                {
                                    BuildTableEntityText(db.Tables, config, dbName);
                                }
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        Log.WriteLogToTxt(err);
                    }
                }
            }
            finally
            {
                if (OnCreateEnd != null)
                {
                    OnCreateEnd(count);
                }
            }
            //MessageBox.Show(string.Format("执行完成!共计{0}个表", count));
        }

        #region 枚举型的单文件

        static void BuildTableEnumText(Dictionary<string, TableInfo> tables, ProjectConfig config, string dbName)
        {
            try
            {
                StringBuilder tableEnum = new StringBuilder();
                string nameSpace = string.Format(config.NameSpace, dbName).TrimEnd('.');
                tableEnum.AppendFormat("using System;\r\n\r\nnamespace {0}\r\n{{\r\n", nameSpace);
                //tableEnum.AppendFormat("using System;\r\n\r\nnamespace {0}\r\n{{\r\n", ((string.IsNullOrEmpty(dbName) || dbName.Contains(":")) ? config.NameSpace : config.NameSpace + "." + dbName));
                tableEnum.Append(config.MutilDatabase ? string.Format("    public enum {0}Enum {{", dbName) : "    public enum TableNames {");
                foreach (KeyValuePair<string, TableInfo> table in tables)
                {
                    tableEnum.Append(" " + FormatKey(table.Value.Name) + " ,");//处理中括号和空格
                }
                tableEnum[tableEnum.Length - 1] = '}';//最后一个字符变成换大括号。

                tableEnum.Append("\r\n\r\n    #region 枚举 \r\n");

                foreach (KeyValuePair<string, TableInfo> table in tables)
                {
                    tableEnum.Append(GetFiledEnum(table.Value.Name, table.Value.Columns, config));
                }
                tableEnum.Append("    #endregion\r\n}");
                string fileName = "TableNames.cs";
                if (config.MutilDatabase)//多数据库模式。
                {
                    fileName = dbName + "Enum.cs";
                }
                System.IO.File.WriteAllText(config.ProjectPath.TrimEnd('/', '\\') + "\\" + fileName, tableEnum.ToString(), Encoding.Default);
            }
            catch (Exception err)
            {
                Log.WriteLogToTxt(err);
            }
        }
        static string GetFiledEnum(string tableName, MDataColumn column, ProjectConfig config)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("    public enum " + FormatKey(tableName) + " {");
            try
            {
                //MDataColumn column = DBTool.GetColumns(tableName, config.Conn);

                if (column.Count > 0)
                {
                    for (int i = 0; i < column.Count; i++)
                    {
                        string cName = FormatKey(column[i].ColumnName);
                        if (i == 0)
                        {
                            sb.Append(" " + cName);
                        }
                        else
                        {
                            sb.Append(", " + cName);
                        }

                    }
                    sb.Append(" }\r\n");
                }
                else
                {
                    sb.Append("}\r\n");
                    // tableColumnNames = tableColumnNames + "}\r\n";
                }
            }
            catch (Exception err)
            {
                Log.WriteLogToTxt(err);
            }
            return sb.ToString();
        }

        #endregion

        #region 实体型的多文件
        static void BuildTableEntityText(Dictionary<string, TableInfo> tables, ProjectConfig config, string dbName)
        {
            foreach (KeyValuePair<string, TableInfo> table in tables)
            {
                BuildSingTableEntityText(table.Value.Name, table.Value.Description, config, dbName);
            }
        }
        static void BuildSingTableEntityText(string tableName, string description, ProjectConfig config, string dbName)
        {
            string fixTableName = FormatKey(tableName);
            if (config.MapName) { fixTableName = FixName(tableName); }
            //bool onlyEntity = config.BuildMode.StartsWith("纯") || config.BuildMode.Contains("DBFast");//纯实体。
            string baseClassName = string.Empty;
            if (config.BuildMode.Contains("SimpleOrmBase"))
            {
                baseClassName = ": CYQ.Data.Orm.SimpleOrmBase";
            }
            else if (config.BuildMode.Contains("OrmBase"))
            {
                baseClassName = ": CYQ.Data.Orm.OrmBase";
            }
            try
            {
                StringBuilder csText = new StringBuilder();
                string nameSpace = string.Format(config.NameSpace, dbName).TrimEnd('.');

                AppendText(csText, "using System;\r\n");
                AppendText(csText, "namespace {0}", nameSpace);
                AppendText(csText, "{");
                if (!string.IsNullOrEmpty(description))
                {
                    AppendText(csText, "    /// <summary>");
                    AppendText(csText, "    /// {0}", description);
                    AppendText(csText, "    /// </summary>");
                }
                AppendText(csText, "    public partial class {0} {1}", fixTableName + config.EntitySuffix, baseClassName);
                AppendText(csText, "    {");
                if (!string.IsNullOrEmpty(baseClassName))
                {
                    AppendText(csText, "        public {0}()", fixTableName + config.EntitySuffix);
                    AppendText(csText, "        {");
                    AppendText(csText, "            base.SetInit(this, \"{0}\", \"{1}\");", tableName, config.Name);
                    AppendText(csText, "        }");
                }

                #region 循环字段
                MDataColumn column = CYQ.Data.Tool.DBTool.GetColumns(tableName, config.Conn);

                if (column.Count > 0)
                {
                    string columnName = string.Empty;
                    Type t = null;
                    if (config.ForTwoOnly)
                    {
                        foreach (MCellStruct st in column)
                        {
                            columnName = st.ColumnName;
                            if (config.MapName) { columnName = FixName(columnName); }
                            t = DataType.GetType(st.SqlType);
                            AppendText(csText, "        private {0} _{1};", FormatType(t.Name, t.IsValueType, config.ValueTypeNullable), columnName);
                            if (!string.IsNullOrEmpty(st.Description))
                            {
                                AppendText(csText, "        /// <summary>");
                                AppendText(csText, "        /// {0}", st.Description);
                                AppendText(csText, "        /// </summary>");
                            }
                            AppendText(csText, "        public {0} {1}", FormatType(t.Name, t.IsValueType, config.ValueTypeNullable), columnName);
                            AppendText(csText, "        {");
                            AppendText(csText, "            get");
                            AppendText(csText, "            {");
                            AppendText(csText, "                return _{0};", columnName);
                            AppendText(csText, "            }");
                            AppendText(csText, "            set");
                            AppendText(csText, "            {");
                            AppendText(csText, "                _{0} = value;", columnName);
                            AppendText(csText, "            }");
                            AppendText(csText, "        }");
                        }
                    }
                    else
                    {
                        foreach (MCellStruct st in column)
                        {
                            columnName = st.ColumnName;
                            if (config.MapName) { columnName = FixName(columnName); }
                            t = DataType.GetType(st.SqlType);
                            if (!string.IsNullOrEmpty(st.Description))
                            {
                                AppendText(csText, "        /// <summary>");
                                AppendText(csText, "        /// {0}", st.Description);
                                AppendText(csText, "        /// </summary>");
                            }
                            AppendText(csText, "        public {0} {1} {{ get; set; }}", FormatType(t.Name, t.IsValueType, config.ValueTypeNullable), columnName);
                        }
                    }
                }
                #endregion

                AppendText(csText, "    }");
                AppendText(csText, "}");
                string pPath = config.ProjectPath;
                System.IO.File.WriteAllText(pPath.TrimEnd('/', '\\') + "\\" + fixTableName + ".cs", csText.ToString(), Encoding.Default);
            }
            catch (Exception err)
            {
                Log.WriteLogToTxt(err);
            }
        }
        static void AppendText(StringBuilder sb, string text, params string[] format)
        {
            if (text.IndexOf("{0}") > -1)
            {
                sb.AppendFormat(text + "\r\n", format);
            }
            else
            {
                sb.AppendLine(text);
            }
        }
        #endregion
        static string FormatType(string tName, bool isValueType, bool nullable)
        {
            switch (tName)
            {
                case "Int32":
                    tName = "int";
                    break;
                case "String":
                    tName = "string";
                    nullable = false;
                    break;
                case "Boolean":
                    tName = "bool";
                    break;
            }
            if (nullable && isValueType && !tName.EndsWith("[]"))
            {
                tName += "?";
            }
            return tName;
        }
        internal static string FixName(string name)
        {

            if (!string.IsNullOrEmpty(name))
            {
                string lowerName = name.ToLower();
                if (lowerName == "id") { return "ID"; }
                bool isEndWithID = lowerName.EndsWith("id");
                string[] items = name.Split(new char[] { '_', '-', ' ' });
                if (items.Length == 1)
                {
                    name = name[0].ToString().ToUpper() + name.Substring(1, name.Length - 1);
                }
                else
                {
                    name = string.Empty;
                    foreach (string item in items)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            if (item.Length == 1)
                            {
                                name += item.ToUpper();
                            }
                            else
                            {
                                name += item[0].ToString().ToUpper() + item.Substring(1, item.Length - 1);
                            }
                        }
                    }
                }
                if (isEndWithID)
                {
                    name = name.Substring(0, name.Length - 2) + "ID";
                }
            }
            return name;
        }
    }
}
