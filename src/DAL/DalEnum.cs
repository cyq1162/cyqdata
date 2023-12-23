using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data
{
    /// <summary>
    /// ���������ݿ�����
    /// </summary>
    public enum DataBaseType
    {
        None,
        /// <summary>
        /// MSSQL[2000/2005/2008/2012/...]
        /// </summary>
        MsSql,
        FoxPro,
        Excel,
        Access,
        Oracle,
        MySql,
        SQLite,
        FireBird,
        /// <summary>
        /// PostgreSQL 
        /// </summary>
        PostgreSQL,
        /// <summary>
        /// Txt DataBase
        /// </summary>
        Txt,
        /// <summary>
        /// Xml DataBase
        /// </summary>
        Xml,
        Sybase,
        DB2,
        /// <summary>
        /// �����������ݿ�
        /// </summary>
        DaMeng
    }
    /// <summary>
    /// �����������[MProc SetCustom������ʹ�õĲ���]
    /// </summary>
    public enum ParaType
    {
        /// <summary>
        /// Oracle �α�����
        /// </summary>
        Cursor,
        /// <summary>
        /// �������
        /// </summary>
        OutPut,
        /// <summary>
        /// �����������
        /// </summary>
        InputOutput,
        /// <summary>
        /// ����ֵ����
        /// </summary>
        ReturnValue,
        /// <summary>
        /// Oracle CLOB����
        /// </summary>
        CLOB,
        /// <summary>
        ///  Oracle NCLOB����
        /// </summary>
        NCLOB,
        /// <summary>
        ///  MSSQL �û����������
        /// </summary>
        Structured
    }

    /// <summary>
    /// �������ͷ���
    /// ��ĸ�ͷ���0�������ͷ���1�������ͷ���2��bool����3��guid����4����������999
    /// </summary>
    public enum DataGroupType
    {
        /// <summary>
        /// δ����
        /// </summary>
        None=-1,
        /// <summary>
        /// �ı�����0
        /// </summary>
        Text = 0,
        /// <summary>
        /// �����ͷ���1
        /// </summary>
        Number = 1,
        /// <summary>
        /// �����ͷ���2
        /// </summary>
        Date = 2,
        /// <summary>
        /// bool����3
        /// </summary>
        Bool = 3,
        /// <summary>
        /// guid����4
        /// </summary>
        Guid = 4,
        /// <summary>
        /// ��������999
        /// </summary>
        Object = 999

    }
    /// <summary>
    /// �������ݿ�Ľ��
    /// </summary>
    internal enum DBResetResult
    {
        /// <summary>
        ///  �ɹ��л� ���ݿ�����
        /// </summary>
        Yes,
        /// <summary>
        /// δ�л� - ��ͬ���ݿ�����
        /// </summary>
        No_SaveDbName,
        /// <summary>
        /// δ�л� - �����С�
        /// </summary>
        No_Transationing,
        /// <summary>
        /// δ�л� - �����ݿ��������ڡ�
        /// </summary>
        No_DBNoExists,
    }
    /// <summary>
    /// �������ӵļ���
    /// </summary>
    internal enum AllowConnLevel
    {
        Master = 1,
        MasterBackup = 2,
        MaterBackupSlave = 3
    }
}
