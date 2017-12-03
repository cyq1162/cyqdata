
namespace System.Windows.Forms
{
    // 摘要:
    //     为 System.Windows.Forms.ListBox 类和 System.Windows.Forms.ComboBox 类提供一个共同的成员实现方法。
   
    internal abstract class ListControl : Control
    {
        public object DataSource { get; set; }
        public string ValueMember { get; internal set; }
        public string DisplayMember { get; internal set; }
    }
}