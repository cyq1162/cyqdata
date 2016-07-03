using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.UI
{
    /// <summary>
    /// 对于自定义控件，只要继承此接口，即可以使用GetFrom与SetTo方法
    /// </summary>
    public interface IUIValue
    {
        /// <summary>
        /// 控件的值
        /// </summary>
        object MValue { get;set;}
        /// <summary>
        /// 控件的启用状态
        /// </summary>
        bool MEnabled { get;set;}
        /// <summary>
        /// 控件的名称（Name）或ID
        /// </summary>
        string MID { get;}
    }
   
}
