using CYQ.Data.Orm;
using CYQ.Data.SQL;
using CYQ.Data.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Tool
{

    /// <summary>
    /// （表、视图、存储过程）相关信息
    /// </summary>
    public class TableInfo
    {
        internal TableInfo()
        {

        }
        public TableInfo(string name, string type, string description, DBInfo dbInfo)
        {
            _Name = name;
            _Type = type;
            _Description = description;
            _DBInfo = dbInfo;
        }
        private string _Name;
        /// <summary>
        /// （表、视图、存储过程）名称（只读）
        /// </summary>
        public string Name
        {
            get { return _Name; }
        }
        private string _Type;
        /// <summary>
        /// 类型（只读）
        /// </summary>
        public string Type { get { return _Type; } }
        private string _Description;
        /// <summary>
        /// 描述（可写）
        /// </summary>
        public string Description { get { return _Description; } set { _Description = value; } }
        private DBInfo _DBInfo;
        /// <summary>
        /// 数据库信息（只读）
        /// </summary>
        [JsonIgnore]
        public DBInfo DBInfo { get { return _DBInfo; } }
        /// <summary>
        /// 获取指定（表、视图、存储过程）名称的Hash值
        /// </summary>
        /// <returns></returns>
        public static string GetHashKey(string name)
        {
            return StaticTool.GetHashKey(name.Replace("-", "").Replace("_", "").Replace(" ", "").ToLower());
        }
        /// <summary>
        /// 获取相关的列架构（对表和视图有效）
        /// </summary>
        public MDataColumn Columns
        {
            get
            {
                if (Type == "U" || Type == "V")
                {
                    return TableSchema.GetColumns(Name, DBInfo.ConnString);
                }
                return null;
            }
        }
    }
}
