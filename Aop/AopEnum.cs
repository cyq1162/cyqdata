using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Aop
{
    /// <summary>
    /// ����ڲ����ݿ��������ö��
    /// </summary>
    public enum AopEnum
    {
        Select,
        /// <summary>
        /// Orm (DBFast��SimpleOrmBase�Ȳ�ѯ����ʵ���б�
        /// </summary>
        SelectList,
        Insert,
        Update,
        Delete,
        Fill,
        GetCount,
        Exists,
        ExeMDataTableList,
        ExeMDataTable,
        ExeNonQuery,
        ExeScalar,
        /// <summary>
        /// δ����ѡ�
        /// </summary>
        ExeList
    }
    /// <summary>
    /// Aop�����Ĵ�����
    /// </summary>
    public enum AopResult
    {
        /// <summary>
        /// �������ִ��ԭ���¼���������Aop.End�¼�
        /// </summary>
        Default,
        /// <summary>
        /// �����������ִ��ԭ���¼���Aop.End�¼�
        /// </summary>
        Continue,
        /// <summary>
        /// �����������ԭ���¼�,����ִ��Aop End�¼�
        /// </summary>
        Break,
        /// <summary>
        /// �������ֱ������ԭ�к�����ִ��
        /// </summary>
        Return,
    }

    /// <summary>
    /// Aop����ѡ��
    /// </summary>
    public enum AopOp
    {
        /// <summary>
        /// ������
        /// </summary>
        OpenAll,
        /// <summary>
        /// �����ڲ�Aop�����Զ����棬�ر��ⲿAop��
        /// </summary>
        OnlyInner,
        /// <summary>
        /// �����ⲿAop���ر��Զ����棩
        /// </summary>
        OnlyOuter,
        /// <summary>
        /// ���ⶼ�أ��Զ�������ⲿAop��
        /// </summary>
        CloseAll
    }
}
