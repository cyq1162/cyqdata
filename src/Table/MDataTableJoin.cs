using CYQ.Data.SQL;
using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Table
{
    public partial class MDataTable
    {
        internal int joinOnIndex = -1;
        /// <summary>
        /// 用于关联表的列名，不设置时默认取表主键值
        /// </summary>
        public string JoinOnName
        {
            get
            {
                if (joinOnIndex > -1)
                {
                    return Columns[joinOnIndex].ColumnName;
                }
                return string.Empty;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    joinOnIndex = Columns.GetIndex(value);
                    if (joinOnIndex == -1)
                    {
                        Error.Throw("not exist the column name : " + value);
                    }
                }
            }
        }
        /// <summary>
        /// 两表LeftJoin关联
        /// </summary>
        /// <param name="dt">关联表</param>
        /// <param name="appendColumns">追加显示的列，没有指定则追加关联表的所有列</param>
        /// <returns></returns>
        public MDataTable Join(MDataTable dt, params string[] appendColumns)
        {
            return MDataTableJoin.Join(this, dt, appendColumns);
        }

        /// <summary>
        /// 两表LeftJoin关联
        /// </summary>
        /// <param name="tableName">关联表名</param>
        /// <param name="joinOnName">关联的字段名，设置Null则自动取表主键为关联名</param>
        /// <param name="appendColumns">追加显示的列，没有指定则追加关联表的所有列</param>
        /// <returns></returns>
        public MDataTable Join(object tableName, string joinOnName, params string[] appendColumns)
        {
            return MDataTableJoin.Join(this, Convert.ToString(tableName), joinOnName, appendColumns);
        }
    }
    /// <summary>
    /// 表间的关联关系
    /// </summary>
    class MDataTableJoin
    {
        internal static MDataTable Join(MDataTable dtA, string tableName, string joinOnName, params string[] appendColumns)
        {
            MDataTable dtB = null;

            using (MAction action = new MAction(tableName, dtA.Conn))
            {
                if (!action.Data.Columns.Contains(joinOnName))
                {
                    joinOnName = action.Data.Columns.FirstPrimary.ColumnName;
                }
                //action.SetAopState(CYQ.Data.Aop.AopOp.CloseAll);
                action.dalHelper.IsRecordDebugInfo = false || AppDebug.IsContainSysSql;//屏蔽SQL日志记录 2000数据库大量的In条件会超时。
                if (appendColumns.Length > 0)
                {
                    if (appendColumns.Length == 1)
                    {
                        appendColumns = appendColumns[0].Split(',');
                    }
                    List<string> items = new List<string>(appendColumns.Length + 1);
                    items.AddRange(appendColumns);
                    if (!items.Contains(joinOnName))
                    {
                        items.Add(joinOnName);
                    }
                    action.SetSelectColumns(items.ToArray());
                }
                string whereIn = SqlCreate.GetWhereIn(action.Data[joinOnName].Struct, dtA.GetColumnItems<string>(dtA.joinOnIndex, BreakOp.NullOrEmpty, true), action.DataBaseType);
                dtB = action.Select(whereIn);
                dtB.JoinOnName = joinOnName;

            }
            return Join(dtA, dtB, appendColumns);
        }
        internal static MDataTable Join(MDataTable dtA, MDataTable dtB, params string[] columns)
        {
            //记录 id as Pid 映射的列名，中间记录，修改dtB的列名，后面还原
            Dictionary<string, string> mapName = new Dictionary<string, string>();
            #region 判断条件
            int aIndex = dtA.joinOnIndex;
            if (aIndex == -1 && dtA.Columns.FirstPrimary != null)
            {
                aIndex = dtA.Columns.GetIndex(dtA.Columns.FirstPrimary.ColumnName);
            }
            int bIndex = dtB.joinOnIndex;
            if (bIndex == -1 && dtB.Columns.FirstPrimary != null)
            {
                bIndex = dtB.Columns.GetIndex(dtB.Columns.FirstPrimary.ColumnName);
            }
            if (aIndex == -1 || bIndex == -1)
            {
                Error.Throw("set MDataTable's JoinOnName first");
            }
            #endregion

            #region 构建新表及表结构
            MDataTable joinTable = new MDataTable("V_" + dtA.TableName);
            joinTable.Conn = dtA.Conn;
            joinTable.Columns.AddRange(dtA.Columns.Clone());
            if (columns.Length == 0)
            {
                joinTable.Columns.AddRange(dtB.Columns.Clone());
            }
            else
            {
                foreach (string column in columns)
                {
                    string[] items = column.Split(' ');
                    string name = items[0];
                    MCellStruct ms = null;
                    if (dtB.Columns.Contains(name))
                    {
                        ms = dtB.Columns[name].Clone();
                    }
                    if (items.Length > 1)
                    {
                        name = items[items.Length - 1];
                        if (ms == null && dtB.Columns.Contains(name))
                        {
                            ms = dtB.Columns[name].Clone();
                        }
                    }

                    if (ms != null)
                    {
                        if (ms.ColumnName != name)
                        {
                            dtB.Columns[ms.ColumnName].ColumnName = name;//修改DtB的列名，结尾再还原。
                            mapName.Add(name, ms.ColumnName);
                            ms.ColumnName = name;
                        }
                        joinTable.Columns.Add(ms);
                    }
                }
            }
            #endregion

            List<string> noFind = new List<string>();
            Dictionary<string, string> yesFind = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string v1 = string.Empty;
            MDataRow row, joinRow;
            int count = dtB.Rows.Count;
            for (int i = 0; i < dtA.Rows.Count; i++)
            {
                row = dtA.Rows[i];
                if (count == 0 || row[aIndex].IsNullOrEmpty || noFind.Contains(row[aIndex].StringValue))
                {
                    joinRow = joinTable.NewRow(true);
                    joinRow.LoadFrom(row);//后载加A表（同名则复盖）
                }
                else
                {
                    v1 = row[aIndex].StringValue;
                    if (yesFind.ContainsKey(v1)) // 找到已匹配的数据
                    {
                        string[] items = yesFind[v1].Split(',');
                        foreach (string item in items)
                        {
                            joinRow = joinTable.NewRow(true);
                            joinRow.LoadFrom(dtB.Rows[int.Parse(item)]);//先加载B表
                            joinRow.LoadFrom(row);//后载加A表（同名则复盖）
                        }
                    }
                    else
                    {
                        bool isFind = false;
                        for (int j = 0; j < dtB.Rows.Count; j++)
                        {
                            if (v1 == dtB.Rows[j][bIndex].StringValue)//找到
                            {
                                joinRow = joinTable.NewRow(true);
                                joinRow.LoadFrom(dtB.Rows[j]);//先加载B表
                                joinRow.LoadFrom(row);//后载加A表（同名则复盖）
                                isFind = true;
                                if (yesFind.ContainsKey(v1))
                                {
                                    yesFind[v1] = yesFind[v1] + "," + j;
                                }
                                else
                                {
                                    yesFind.Add(v1, j.ToString());
                                }
                            }
                        }
                        if (!isFind)
                        {
                            noFind.Add(v1);
                            joinRow = joinTable.NewRow(true);//找不到时，只加载A表。
                            joinRow.LoadFrom(row);//后载加A表（同名则复盖）
                        }
                    }
                }



            }
            //还原DtB的列
            if (mapName.Count > 0)
            {
                foreach (KeyValuePair<string,string> item in mapName)
                {
                    dtB.Columns[item.Key].ColumnName = item.Value;
                }
            }
            #region 注销临时变量
            noFind.Clear();
            noFind = null;
            yesFind.Clear();
            yesFind = null;
            mapName = null;
            #endregion
          
            return joinTable;
        }
        #region 匹配单条数据 已注释
        /*
          internal static MDataTable Join(MDataTable dtA, MDataTable dtB, params string[] columns)
        {
            #region 判断条件
            int aIndex = dtA.joinOnIndex;
            if (aIndex == -1 && dtA.Columns.FirstPrimary != null)
            {
                aIndex = dtA.Columns.GetIndex(dtA.Columns.FirstPrimary.ColumnName);
            }
            int bIndex = dtB.joinOnIndex;
            if (bIndex == -1 && dtB.Columns.FirstPrimary != null)
            {
                bIndex = dtB.Columns.GetIndex(dtB.Columns.FirstPrimary.ColumnName);
            }
            if (aIndex == -1 || bIndex == -1)
            {
                Error.Throw("set MDataTable's JoinOnName first");
            }
            #endregion

            #region 构建新表及表结构
            MDataTable joinTable = new MDataTable("V_" + dtA.TableName);
            joinTable.Columns.AddRange(dtA.Columns.Clone());

            Dictionary<string, int> keyValue = new Dictionary<string, int>();
            if (columns.Length == 0)
            {
                joinTable.Columns.AddRange(dtB.Columns.Clone());
            }
            else
            {
                foreach (string column in columns)
                {
                    if (dtB.Columns.Contains(column))
                    {
                        joinTable.Columns.Add(dtB.Columns[column].Clone());
                    }
                }
            }
            #endregion

            string v1 = string.Empty;
            MDataRow row, joinRow;
            MDataCell cell;
            int count = dtB.Rows.Count;
            for (int i = 0; i < dtA.Rows.Count; i++)
            {
                joinRow = joinTable.NewRow(true);
                row = dtA.Rows[i];
                if (count > 0)
                {
                    cell = dtA.Rows[i][aIndex];
                    if (!cell.IsNullOrEmpty)
                    {
                        v1 = cell.strValue;
                        if (keyValue.ContainsKey(v1)) // 找到已匹配的数据
                        {
                            if (keyValue[v1] > -1)
                            {
                                joinRow.LoadFrom(dtB.Rows[keyValue[v1]]);//先加载B表
                            }
                        }
                        else
                        {
                            bool isFind = false;
                            for (int j = 0; j < dtB.Rows.Count; j++)
                            {
                                if (v1 == dtB.Rows[j][bIndex].strValue)
                                {
                                    joinRow.LoadFrom(dtB.Rows[j]);
                                    keyValue.Add(v1, j);
                                    isFind = true;
                                    break;
                                }
                            }
                            if (!isFind)
                            {
                                keyValue.Add(v1, -1);
                            }
                        }
                    }
                }
                joinRow.LoadFrom(row);//后载加A表（同名则复盖）
            }

            keyValue.Clear();
            keyValue = null;
            return joinTable;
        }
         */
        #endregion

    }
}
