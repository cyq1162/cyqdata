using CYQ.Data.SQL;
using CYQ.Data.Table;

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
            name = SqlFormat.NotKeyword(name);
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
                    return TableSchema.GetColumns(Name, DBInfo.ConnString);//里面有缓存，所以无需要存档。
                }
                return null;
            }
        }
        /// <summary>
        /// 刷新表列结构缓存
        /// </summary>
        public void Reflesh()
        {
            if (Type == "U" || Type == "V")
            {
                TableSchema.GetColumns(Name, DBInfo.ConnString, true);
            }
        }
        /// <summary>
        /// 清空缓存【内存和硬盘】
        /// </summary>
        internal void RemoveCache()
        {
            TableSchema.Remove(Name, DBInfo.ConnString);
        }
    }
}
