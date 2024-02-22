using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Collections;
using System.ComponentModel;
using CYQ.Data.SQL;
using CYQ.Data.Tool;
using System.Reflection;
using System.Collections.Specialized;
using CYQ.Data.UI;
using CYQ.Data.Json;
using CYQ.Data.Emit;

namespace CYQ.Data.Table
{
    /// <summary>
    /// һ�м�¼
    /// </summary>
    public partial class MDataRow
    {
        /// <summary>
        /// ����ö��������
        /// </summary>
        public MDataCell this[object field]
        {
            get
            {
                if (field == null) { return null; }
                if (field is int || (field is Enum && AppConfig.DB.IsEnumToInt))
                {
                    int index = (int)field;
                    if (Count > index)
                    {
                        return this[index];
                    }
                }
                else if (field is string)
                {
                    return this[field as string];
                }
                //else if (field is IField)
                //{
                //    IField iFiled = field as IField;
                //    if (iFiled.ColID > -1)
                //    {
                //        return this[iFiled.ColID];
                //    }
                //    return this[iFiled.Name];
                //}
                return this[field.ToString()];
            }
        }
        public MDataCell this[string key]
        {
            get
            {
                int index = Columns.GetIndex(key);//���¼�����Ƿ�һ�¡�;
                if (index == -1)
                {
                    if (key.Length < 3) //2<=20
                    {
                        //�ж��Ƿ�Ϊ���֡�
                        if (!int.TryParse(key, out index))
                        {
                            index = -1;
                        }
                    }
                }
                if (index > -1)
                {
                    return this[index];
                }
                return null;
            }
        }
        public MDataCell this[int index]
        {
            get
            {
                if (index > -1 && index < Count)
                {
                    return CellList[index];
                }
                return null;
            }
            set
            {
                Error.Throw(AppConst.Global_NotImplemented);
            }
        }

    }

}
