using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.UI
{
    /// <summary>
    /// �����Զ���ؼ���ֻҪ�̳д˽ӿڣ�������ʹ��GetFrom��SetTo����
    /// </summary>
    public interface IUIValue
    {
        /// <summary>
        /// �ؼ���ֵ
        /// </summary>
        object MValue { get;set;}
        /// <summary>
        /// �ؼ�������״̬
        /// </summary>
        bool MEnabled { get;set;}
        /// <summary>
        /// �ؼ������ƣ�Name����id
        /// </summary>
        string MID { get;}
    }
   
}
