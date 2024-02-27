using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Xml;
using System.IO;
using System.Data.Common;
using System.ComponentModel;
using CYQ.Data.UI;
using CYQ.Data.Cache;
using CYQ.Data.SQL;
using CYQ.Data.Tool;
using System.Collections.Specialized;
using System.Web;
using CYQ.Data.Json;
using CYQ.Data.Orm;
using CYQ.Data.Aop;

namespace CYQ.Data.Table
{

    /// <summary>
    /// ���
    /// </summary>
    public partial class MDataTable
    {
        #region Select��FindRow��FindAll��GetIndex��GetCount��Split
        /// <summary>
        /// ʹ�ñ���ѯ���õ���¡�������
        /// </summary>
        public MDataTable Select(object where)
        {
            return Select(0, 0, where);
        }
        /// <summary>
        /// ʹ�ñ���ѯ���õ���¡�������
        /// </summary>
        public MDataTable Select(int topN, object where)
        {
            return Select(1, topN, where);
        }
        /// <summary>
        /// ʹ�ñ���ѯ���õ���¡�������
        /// </summary>
        public MDataTable Select(int pageIndex, int pageSize, object where, params object[] selectColumns)
        {
            return MDataTableFilter.Select(this, pageIndex, pageSize, where, selectColumns);
        }
        /// <summary>
        /// ʹ�ñ���ѯ���õ�ԭ���ݵ����á�
        /// </summary>
        public MDataRow FindRow(object where)
        {
            return MDataTableFilter.FindRow(this, where);
        }
        /// <summary>
        /// ʹ�ñ���ѯ���õ�ԭ���ݵ����á�
        /// </summary>
        public MDataRowCollection FindAll(object where)
        {
            return MDataTableFilter.FindAll(this, where);
        }
        /// <summary>
        /// ͳ�����������������ڵ�����
        /// </summary>
        public int GetIndex(object where)
        {
            return MDataTableFilter.GetIndex(this, where);
        }
        /// <summary>
        /// ͳ����������������
        /// </summary>
        public int GetCount(object where)
        {
            return MDataTableFilter.GetCount(this, where);
        }
        /// <summary>
        /// ���������ֲ�������������������ͷ����������ġ����ֳ����������к�ԭʼ������ͬһ������
        /// </summary>
        public MDataTable[] Split(object where)
        {
            return MDataTableFilter.Split(this, where);
        }
        #endregion
    }
}
