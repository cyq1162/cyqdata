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
    internal static class AutoCache
    {
        private static CacheManage _MemCache = CacheManage.Instance;//有可能使用MemCache操作

        internal static bool GetCache(AopEnum action, AopInfo aopInfo)//Begin
        {
            switch (action)
            {
                case AopEnum.ExeNonQuery:
                case AopEnum.Insert:
                case AopEnum.Update:
                case AopEnum.Delete:
                    return false;
            }
            if (!IsCanOperateCache(aopInfo))
            {
                return false;
            }
            string baseKey = GetBaseKey(aopInfo);
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
                        MDataRow row = obj as MDataRow;
                        if (_MemCache.CacheType == CacheType.LocalCache)
                        {
                            row = row.Clone();
                        }
                        aopInfo.Row = row;
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
            baseKey = key = null;
            return obj != null;
        }

        internal static void SetCache(AopEnum action, AopInfo aopInfo)//End
        {
            if (!IsCanOperateCache(aopInfo))
            {
                return;
            }
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

           
            if (_MemCache.CacheType == CacheType.LocalCache && _MemCache.RemainMemoryPercentage < 15)//可用内存低于15%
            {
                return;
            }
            string key = GetKey(action, aopInfo, baseKey);
            int flag;//0 正常；1：未识别；2：不允许缓存
            SetBaseKeys(aopInfo, key, out flag);//存档Key，后续缓存失效 批量删除
            if (flag == 2)
            {
                return;//
            }
            double cacheTime = Math.Abs(12 - DateTime.Now.Hour) * 60 + DateTime.Now.Second;//缓存中午或到夜里1点
            if (flag == 1 || aopInfo.PageIndex > 2) // 后面的页数，缓存时间可以短一些
            {
                cacheTime = 2;//未知道操作何表时，只缓存2分钟（比如存储过程等语句）
            }
            switch (action)
            {
                case AopEnum.ExeMDataTableList:
                    if (IsCanCache(aopInfo.TableList))
                    {
                        JsonHelper js = new JsonHelper(false, false);
                        foreach (MDataTable table in aopInfo.TableList)
                        {
                            js.Add(Guid.NewGuid().ToString(), table.ToJson(true, true, RowOp.IgnoreNull));
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
        static bool IsCanCache(List<MDataTable> dtList)
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
        static bool IsCanCache(MDataTable dt)
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
        private static void SetBaseKeys(AopInfo para, string key, out int flag)
        {
            List<string> items = GetBaseKeys(para, out flag);
            if (items != null && items.Count > 0)
            {
                foreach (string item in items)
                {
                    SetBaseKey(item, key);
                }
                items = null;
            }
        }
        private static void SetBaseKey(string baseKey, string key)
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
        private static List<string> _NoCacheTables = null;
        internal static List<string> NoCacheTables
        {
            get
            {
                if (_NoCacheTables == null)
                {
                    _NoCacheTables = new List<string>();
                    string tables = AppConfig.Cache.NoCacheTables;
                    if (!string.IsNullOrEmpty(tables))
                    {
                        _NoCacheTables.AddRange(tables.Split(','));
                    }
                }
                return _NoCacheTables;
            }
            set
            {
                _NoCacheTables = value;
            }
        }
        private static bool IsCanOperateCache(AopInfo para)
        {
            List<string> tables = GetRelationTables(para);
            if (tables != null && tables.Count > 0)
            {

                foreach (string tableName in tables)
                {
                    if (NoCacheTables.Contains(tableName))
                    {
                        return false;
                    }
                    string baseKey = GetBaseKey(para, tableName);
                    string delKey = "DeleteAutoCache:" + baseKey;
                    if (_MemCache.Contains(delKey))
                    {
                        return false;
                    }
                }
            }
            return true;
            // string delKey = "DeleteAutoCache:" + baseKey;
            // return !_MemCache.Contains(delKey);
            //if (baseKey.Contains(".ActionV") || baseKey.Contains(".ProcS"))
            //{

            //    return true;
            //}
            //TimeSpan ts = DateTime.Now - _MemCache.Get<DateTime>("Del:" + baseKey);
            //return ts.TotalSeconds > 6;//5秒内无缓存。
        }

        private static string GetBaseKey(AopInfo para)
        {
            return GetBaseKey(para, null);
        }
        internal static string GetBaseKey(DalType dalType, string database, string tableName)
        {
            return "AutoCache:" + dalType + "." + database + "." + tableName;
        }
        private static List<string> GetRelationTables(AopInfo para)
        {
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
            return tables;
        }
        private static string GetBaseKey(AopInfo para, string tableName)
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
        private static List<string> GetBaseKeys(AopInfo para, out int flag)
        {
            flag = 0;//0 正常；1：未识别；2：暂不允许缓存 
            List<string> baseKeys = new List<string>();
            List<string> tables = GetRelationTables(para);
            if (tables != null && tables.Count > 0)
            {
                foreach (string tableName in tables)
                {
                    string baseKey = GetBaseKey(para, tableName);
                    string delKey = "DeleteAutoCache:" + baseKey;
                    if (_MemCache.Contains(delKey))
                    {
                        //说明此项不可缓存
                        flag = 2;
                        baseKeys.Clear();
                        baseKeys = null;
                        return null;
                    }
                    baseKeys.Add(baseKey);
                }
                //tables.Clear();//自己给自己造坑，花了2小时才找到这坑
                //tables = null;
            }
            if (baseKeys.Count == 0)
            {
                flag = 1;
            }
            return baseKeys;
        }
        private static string GetKey(AopEnum action, AopInfo aopInfo)
        {
            return GetKey(action, aopInfo, GetBaseKey(aopInfo));
        }
        private static string GetKey(AopEnum action, AopInfo aopInfo, string baseKey)
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
            string delKey = "DeleteAutoCache:" + baseKey;
            if (!_MemCache.Contains(delKey))
            {
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
            _MemCache.Set(delKey, 0, 0.1);//设置6秒时间
        }

        public static void ClearCache(object threadID)
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(5);
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
            catch
            {
                ;
            }
        }
        private static readonly object lockObj = new object();
        private static DateTime errTime = DateTime.MinValue;
        internal static void RemoveCache(string baseKey)
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
            catch (ThreadAbortException e)
            {
            }
            catch (OutOfMemoryException)
            { }
            catch (Exception err)
            {
                if (errTime == DateTime.MinValue || errTime.AddMinutes(10) < DateTime.Now) // 10分钟记录一次
                {
                    errTime = DateTime.Now;
                    Log.WriteLogToTxt(err);
                }
            }
        }
        #endregion

        /*
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
            
        }

        #endregion
         */
    }
}
