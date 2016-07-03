

namespace CYQ.Data.Xml
{

    /// <summary>
    /// XHtmlAction 使用时的值替换
    /// </summary>
    public class ValueReplace
    {
        /// <summary>
        /// Set 方法中 原节点的值 ：[#source]
        /// </summary>
        public const string Source = "[#source]";
        /// <summary>
        /// LoadData加载数据后，数据的值 ：[#new]
        /// </summary>
        public const string New = "[#new]";
        /// <summary>
        /// MutilLanguage 类 Get方法取到值后，按[#langsplit]分隔并按当前语言返回分隔符前面或后面值
        /// </summary>
        public const string LangSplit = "[#langsplit]";
    }

}
