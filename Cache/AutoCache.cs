using CYQ.Data.Aop;
using CYQ.Data.SQL;
using CYQ.Data.Table;
using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// 内部智能缓存
    /// </summary>
    internal class AutoCache
    {
        private static CacheManage _MemCache = CacheManage.Instance;//有可能使用MemCache操作

        internal bool GetCache(AopEnum action, AopInfo aopInfo)//Begin
        {
            switch (action)
            {
                case AopEnum.ExeNonQuery:
                case AopEnum.Insert:
                case AopEnum.Update:
                case AopEnum.Delete:
                    return false;
            }
            string baseKey = GetBaseKey(aopInfo);
            if (!IsCanOperateCache(baseKey))
            {
                return false;
            }
            //查看是否通知我移除
            string key = GetKey(action, aopInfo, baseKey);
            object obj = _MemCache.Get(key);
            switch (action)
            {
                case AopEnum.ExeMDataTableList:
                    if (obj != null)
                    {
                        List<MDataTable> list = new List<MDataTable>();
                        Dictionary<string, string> jd = JsonHelper.Split(obj.ToString());
                        if (jd != null && jd.Count > 0)
                        {
                            foreach (KeyValuePair<string, string> item in jd)
                            {
                                list.Add(MDataTable.CreateFrom(item.Value));
                            }
                        }
                        aopInfo.TableList = list;
                    }
                    break;
                case AopEnum.Select:
                case AopEnum.ExeMDataTable:
                    if (obj != null)
                    {
                        aopInfo.Table = MDataTable.CreateFrom(obj.ToString());
                    }
                    break;
                case AopEnum.ExeScalar:
                    if (obj != null)
                    {
                        aopInfo.ExeResult = obj;
                    }
                    break;
                case AopEnum.Fill:
                    if (obj != null)
                    {
                        aopInfo.Row = ((MDataRow)obj).Clone();
                        aopInfo.IsSuccess = true;
                    }
                    break;
                case AopEnum.GetCount:
                    if (obj != null)
                    {
                        aopInfo.RowCount = int.Parse(obj.ToString());
                    }
                    break;
            }
            return obj != null;
        }

        internal void SetCache(AopEnum action, AopInfo aopInfo)//End
        {
            string baseKey = GetBaseKey(aopInfo);
            switch (action)
            {
                case AopEnum.ExeNonQuery:
                case AopEnum.Insert:
                case AopEnum.Update:
                case AopEnum.Delete:
                    if (aopInfo.IsSuccess || aopInfo.RowCount > 0)
                    {
                        ReadyForRemove(baseKey);
                    }
                    return;
            }

            if (!IsCanOperateCache(baseKey))
            {
                return;
            }
            string key = GetKey(action, aopInfo, baseKey);
            bool isKnownTable;
            SetBaseKeys(aopInfo, key, out isKnownTable);//存档Key，后续缓存失效 批量删除
            double cacheTime = (24 - DateTime.Now.Hour) * 60 + DateTime.Now.Second;//缓存到夜里1点
            if (!isKnownTable || aopInfo.PageIndex > 3) // 后面的页数，缓存时间可以短一些
            {
                cacheTime = 3;//未知道操作何表时，只缓存3分钟（比如存储过程等语句）
            }
            switch (action)
            {
                case AopEnum.ExeMDataTableList:
                    if (IsCanCache(aopInfo.TableList))
                    {
                        JsonHelper js = new JsonHelper(false, false);
                        foreach (MDataTable table in aopInfo.TableList)
                        {
                            js.Add(table.TableName, table.ToJson(true, true, RowOp.IgnoreNull));
                        }
                        js.AddBr();
                        _MemCache.Set(key, js.ToString(), cacheTime);
                    }
                    break;
                case AopEnum.Select:
                case AopEnum.ExeMDataTable:
                    if (IsCanCache(aopInfo.Table))
                    {
                        _MemCache.Set(key, aopInfo.Table.ToJson(true, true, RowOp.IgnoreNull), cacheTime);
                    }
                    break;
                case AopEnum.ExeScalar:
                    _MemCache.Set(key, aopInfo.ExeResult, cacheTime);
                    break;
                case AopEnum.Fill:
                    _MemCache.Set(key, aopInfo.Row, cacheTime);
                    break;
                case AopEnum.GetCount:
                    _MemCache.Set(key, aopInfo.RowCount, cacheTime);
                    break;
            }

        }

        #region 检测过滤
        bool IsCanCache(List<MDataTable> dtList)
        {
            foreach (MDataTable item in dtList)
            {
                if (!IsCanCache(item))
                {
                    return false;
                }
            }
            return true;
        }
        bool IsCanCache(MDataTable dt)
        {
            foreach (MCellStruct item in dt.Columns)
            {
                if (DataType.GetGroup(item.SqlType) == 999)//只存档基础类型
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region 缓存Key的管理
        /// <summary>
        /// 单机存档： 表的baseKey:key1,key2,key3...
        /// </summary>
        private static MDictionary<string, StringBuilder> cacheKeys = new MDictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);
        private void SetBaseKeys(AopInfo para, string key, out bool isKnownTable)
        {
            List<string> items = GetBaseKeys(para, out isKnownTable);
            if (items != null && items.Count > 0)
            {
                foreach (string item in items)
                {
                    SetBaseKey(item, key);
                }
                items = null;
            }
        }
        private void SetBaseKey(string baseKey, string key)
        {
            //baseKey是表的，不包括视图和自定义语句
            if (_MemCache.CacheType == CacheType.LocalCache)
            {
                if (cacheKeys.ContainsKey(baseKey))
                {
                    cacheKeys[baseKey] = cacheKeys[baseKey].Append("," + key);
                }
                else
                {
                    cacheKeys.Add(baseKey, new StringBuilder(key));
                }
            }
            else
            {
                StringBuilder sb = _MemCache.Get<StringBuilder>(baseKey);
                if (sb == null)
                {
                    _MemCache.Set(baseKey, new StringBuilder(key));
                }
                else
                {
                    sb.Append("," + key);
                    _MemCache.Set(baseKey, sb);
                }
            }

        }
        private bool IsCanOperateCache(string baseKey)
        {
            if (baseKey.Contains(".ActionV") || baseKey.Contains(".ProcS"))
            {

                return true;
            }
            TimeSpan ts = DateTime.Now - _MemCache.Get<DateTime>("Del:" + baseKey);
            return ts.TotalSeconds > 6;//5秒内无缓存。
        }

        private string GetBaseKey(AopInfo para)
        {
            return GetBaseKey(para, null);
        }
        internal static string GetBaseKey(DalType dalType, string database, string tableName)
        {
            return "AutoCache:" + dalType + "." + database + "." + tableName;
        }
        private string GetBaseKey(AopInfo para, string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                if (para.MAction != null)
                {
                    if (para.MAction.Data.Columns.isViewOwner)
                    {
                        tableName = "ActionV" + para.TableName.GetHashCode();
                    }
                    else
                    {
                        tableName = para.TableName;
                    }
                }
                else
                {
                    tableName = "ProcS" + para.ProcName.GetHashCode();
                }
            }
            return GetBaseKey(para.DalType, para.DataBase, tableName);
        }
        private List<string> GetBaseKeys(AopInfo para, out bool isKnownTable)
        {
            isKnownTable = true;
            List<string> baseKeys = new List<string>();
            List<string> tables = null;
            if (para.MAction != null)
            {
                tables = para.MAction.Data.Columns.relationTables;
            }
            else if (para.MProc != null && !para.IsProc)
            {
                if (para.Table != null)
                {
                    tables = para.Table.Columns.relationTables;
                }
                else
                {
                    tables = SqlFormat.GetTableNamesFromSql(para.ProcName);
                }
            }
            if (tables != null && tables.Count > 0)
            {
                foreach (string tableName in tables)
                {
                    baseKeys.Add(GetBaseKey(para, tableName));
                }
            }
            if (baseKeys.Count == 0)
            {
                isKnownTable = false;
                //baseKeys.Add(GetBaseKey(para, null));
            }
            return baseKeys;
        }
        private string GetKey(AopEnum action, AopInfo aopInfo)
        {
            return GetKey(action, aopInfo, GetBaseKey(aopInfo));
        }
        private string GetKey(AopEnum action, AopInfo aopInfo, string baseKey)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(baseKey);
            switch (action)
            {
                case AopEnum.ExeNonQuery:
                case AopEnum.Insert:
                case AopEnum.Update:
                case AopEnum.Delete:
                    return sb.ToString();
            }

            #region Key1：DBType
            sb.Append(".");
            sb.Append(action);
            if (aopInfo.DBParameters != null && aopInfo.DBParameters.Count > 0)
            {
                foreach (DbParameter item in aopInfo.DBParameters)
                {
                    sb.Append(item.ParameterName);
                    sb.Append(item.Value);
                }
            }
            if (aopInfo.CustomDbPara != null)
            {
                foreach (AopCustomDbPara item in aopInfo.CustomDbPara)
                {
                    sb.Append(item.ParaName);
                    sb.Append(item.Value);
                }
            }
            if (aopInfo.SelectColumns != null)
            {
                foreach (object item in aopInfo.SelectColumns)
                {
                    sb.Append(item);
                    sb.Append(item);
                }
            }
            #endregion

            switch (action)
            {
                case AopEnum.ExeMDataTableList:
                case AopEnum.ExeMDataTable:
                case AopEnum.ExeScalar:
                    sb.Append(aopInfo.IsProc);
                    sb.Append(aopInfo.ProcName);
                    break;
                case AopEnum.Fill:
                case AopEnum.GetCount:
                case AopEnum.Select:
                    sb.Append(aopInfo.PageIndex);
                    sb.Append(aopInfo.PageSize);
                    sb.Append(aopInfo.Where);
                    break;
            }
            return MD5.Get(sb.ToString());
        }
        #endregion

        #region 定时清理Cache机制
        //根据移除的频率，控制该项缓存的存在。
        //此缓存算法，后续增加
        private static Queue<string> removeList = new Queue<string>();
        public static void ReadyForRemove(string baseKey)
        {
            _MemCache.Set("Del:" + baseKey, DateTime.Now);//设置好要更新的时间
            if (!removeList.Contains(baseKey))
            {
                try
                {
                    removeList.Enqueue(baseKey);
                }
                catch
                {

                }

            }
        }

        public void ClearCache(object threadID)
        {
            while (true)
            {
                Thread.Sleep(100);
                if (removeList.Count > 0)
                {
                    string baseKey = removeList.Dequeue();
                    if (!string.IsNullOrEmpty(baseKey))
                    {
                        RemoveCache(baseKey);
                    }
                }
            }
        }
        private static readonly object lockObj = new object();
        private static DateTime errTime = DateTime.MinValue;
        internal void RemoveCache(string baseKey)
        {
            try
            {
                lock (lockObj)
                {
                    string keys = string.Empty;
                    if (_MemCache.CacheType == CacheType.LocalCache)
                    {
                        if (cacheKeys.ContainsKey(baseKey))
                        {
                            keys = cacheKeys[baseKey].ToString();
                            cacheKeys.Remove(baseKey);
                        }
                    }
                    else
                    {
                        keys = _MemCache.Get<string>(baseKey);
                    }
                    if (!string.IsNullOrEmpty(keys))
                    {
                        foreach (string item in keys.Split(','))
                        {
                            _MemCache.Remove(item);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                if (errTime == DateTime.MinValue || errTime.AddMinutes(10) < DateTime.Now) // 10分钟记录一次
                {
                    errTime = DateTime.Now;
                    if (!(err is OutOfMemoryException))
                    {
                        Log.WriteLogToTxt(err);
                    }
                }
            }
        }
        #endregion

        #region 实例对象
        /// <summary>
        /// 单例
        /// </summary>
        public static AutoCache Instance
        {
            get
            {

                return Shell.instance;
            }
        }
        class Shell
        {
            internal static readonly AutoCache instance = new AutoCache();
        }
        internal AutoCache()
        {
            ThreadBreak.AddGlobalThread(new System.Threading.ParameterizedThreadStart(ClearCache));
        }

        #endregion
    }
}
