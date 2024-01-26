using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Aop
{
    /// <summary>
    /// Aop�ӿڣ���Ҫʵ��ʱ�̳�
    /// </summary>
    public interface IAop
    {
        /// <summary>
        /// ��������֮ǰ������
        /// </summary>
        /// <param name="action">��������</param>
        /// <param name="aopInfo">������֧����</param>
        AopResult Begin(AopEnum action, AopInfo aopInfo);
        /// <summary>
        /// ��������֮�󱻵���
        /// </summary>
        /// <param name="action">��������</param>
        /// <param name="aopInfo">������֧����</param>
        void End(AopEnum action, AopInfo aopInfo);
        /// <summary>
        /// ���ݿ���������쳣ʱ,�����˷���
        /// </summary>
        /// <param name="msg"></param>
        void OnError(string msg);

        /// <summary>
        /// �ڲ���ȡ����Aop���ⲿʹ�÷���null���ɡ�
        /// </summary>
        /// <returns></returns>
        //IAop GetFromConfig();
        /// <summary>
        /// ��¡����һ���µĶ���
        /// </summary>
        /// <returns></returns>
        IAop Clone();
        /// <summary>
        /// Aop �״μ���ʱ���������¼�
        /// </summary>
        void OnLoad();
    }
}
