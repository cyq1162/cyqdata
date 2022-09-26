using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data
{
    /// <summary>
    /// MAction Insert Options
    /// <para>MAction��Insert�����ķ���ֵѡ��</para>
    /// </summary>
    public enum InsertOp
    {
        /// <summary>
        /// only insert,no return autoIncrement id
        /// <para>ʹ�ô���������ݺ�[MSSQL�᷵��id,�������ݿ��򲻻᷵��id]</para>
        /// </summary>
        None,
        /// <summary>
        /// insert and return autoincrement id (default option)
        /// <para>ʹ�ô���������ݺ�᷵��id[Ĭ��ѡ��]��</para>
        /// </summary>
        ID,
        /// <summary>
        /// insert and select top 1 data to fill row
        /// <para>ʹ�ô���������ݺ�,����ݷ���id���в�ѯ����������С�</para>
        /// </summary>
        Fill,
    }

}

