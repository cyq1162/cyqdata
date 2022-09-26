using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Table
{
    /// <summary>
    /// ��������ѡ��
    /// </summary>
    [Flags]
    public enum AcceptOp
    {
        /// <summary>
        /// �������루��ϵͳ����������id��
        /// ��ִ�лῪ������
        /// </summary>
        Insert = 1,
        /// <summary>
        /// �������루���û�ָ��id���룩
        /// ��ִ�лῪ������
        /// </summary>
        InsertWithID = 2,
        /// <summary>
        /// ��������
        /// ��ִ�лῪ������
        /// </summary>
        Update = 4,
        /// <summary>
        /// ����ɾ��
        /// </summary>
        Delete = 8,
        /// <summary>
        /// �����Զ��������£�����������������ڣ�����£������ڣ�����룩
        /// ��ִ�в��Ὺ������
        /// </summary>
        Auto = 16,
        /// <summary>
        /// ��ձ�ֻ�к�Insert��InsertWithID���ʹ��ʱ����Ч��
        /// </summary>
        Truncate = 32
    }
    /// <summary>
    /// MDataTable �� MDataRow SetState �Ĺ���ѡ��
    /// </summary>
    public enum BreakOp
    {
        /// <summary>
        /// δ���ã���������
        /// </summary>
        None = -1,
        /// <summary>
        /// ��������ֵΪNull�ġ�
        /// </summary>
        Null = 0,
        /// <summary>
        /// ��������ֵΪ�յġ�
        /// </summary>
        Empty = 1,
        /// <summary>
        /// ��������ֵΪNull��յġ�
        /// </summary>
        NullOrEmpty = 2
    }

    /// <summary>
    /// MDataRow �� JsonHelper �����ݵĹ���ѡ��
    /// </summary>
    public enum RowOp
    {
        /// <summary>
        /// δ���ã�������У�����Nullֵ����
        /// </summary>
        None = -1,
        /// <summary>
        /// ������У���������Nullֵ����
        /// </summary>
        IgnoreNull = 0,
        /// <summary>
        /// ������в���״̬��ֵ
        /// </summary>
        Insert = 1,
        /// <summary>
        /// ������и���״̬��ֵ
        /// </summary>
        Update = 2
    }


}
