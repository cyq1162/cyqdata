using System.Data;
using System.Data.Common;
using CYQ.Data.Cache;
using System.Reflection;
using System;
using System.IO;
using System.Collections.Generic;
namespace CYQ.Data
{
    internal partial class SQLiteDal : DalBase
    {
        private CacheManage _Cache = CacheManage.LocalInstance;//Cache操作
        public SQLiteDal(ConnObject co)
            : base(co)
        {
            base.isUseUnsafeModeOnSqlite = co.Master.ConnString.ToLower().Contains("syncpragma=off");
        }
        protected override bool IsExistsDbName(string dbName)
        {
            string name = Path.GetFileNameWithoutExtension(DbFilePath);
            string newDbPath = DbFilePath.Replace(name, dbName);
            return File.Exists(newDbPath);
        }
        //protected override string ChangedDbConn(string newDbName)
        //{
        //    string dbName = Path.GetFileNameWithoutExtension(DbFilePath);
        //    string newDbPath = DbFilePath.Replace(dbName, newDbName);
        //    if (File.Exists(newDbPath))
        //    {
        //        filePath = string.Empty;
        //        return base.conn.Replace(dbName, newDbName);
        //    }
        //    return conn;
        //}
        private Assembly GetAssembly()
        {
            object ass = _Cache.Get("SQLite_Assembly");
            if (ass == null)
            {
                try
                {
                    ass = Assembly.Load("System.Data.SQLite");
                    _Cache.Set("SQLite_Assembly", ass, 10080);
                }
                catch (Exception err)
                {
                    string errMsg = err.Message;
                    if (errMsg.Contains("v2.0"))
                    {
                        //混合模式程序集是针对“v2.0.50727”版的运行时生成的，在没有配置其他信息的情况下，无法在 4.0 运行时中加载该程序集。
                        //提示用户要增加配置文件
                        errMsg = "You need to add web.config or app.config : <startup useLegacyV2RuntimeActivationPolicy=\"true\"></startup> more info : " + AppConst.NewLine + errMsg;

                    }
                    else if (!System.IO.File.Exists(AppConst.AssemblyPath + "System.Data.SQLite.DLL"))
                    {
                        errMsg = "Can't find the System.Data.SQLite.dll more info : " + AppConst.NewLine + errMsg;
                    }
                    else
                    {
                        errMsg = "You need to choose the right version : x86 or x64. more info : " + AppConst.NewLine + errMsg;
                    }
                    Error.Throw(errMsg);
                }
            }
            return ass as Assembly;
        }
        protected override DbProviderFactory GetFactory()
        {
            object factory = _Cache.Get("SQLite_Factory");
            if (factory == null)
            {
                Assembly ass = GetAssembly();
                factory = ass.CreateInstance("System.Data.SQLite.SQLiteFactory");
                if (factory == null)
                {
                    throw new System.Exception("Can't Create  SQLiteFactory in System.Data.SQLite.dll");
                }
                else
                {
                    _Cache.Set("SQLite_Factory", factory, 10080);
                }

            }
            return factory as DbProviderFactory;

        }
        public override string DataBase
        {
            get
            {
                return Path.GetFileNameWithoutExtension(DbFilePath) + "." + _con.Database;
            }
        }
        string filePath = string.Empty;
        public string DbFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    string conn = _con.ConnectionString;
                    int start = conn.IndexOf('=') + 1;
                    int end = conn.IndexOf(';');
                    int length = end > start ? end - start : conn.Length - start;
                    filePath = conn.Substring(start, length);
                }
                return filePath;
            }
        }
        //public override DbParameter GetNewParameter()
        //{
        //    Assembly ass = GetAssembly();
        //    object para = ass.CreateInstance("System.Data.SQLite.SQLiteParameter");
        //    if (para == null)
        //    {
        //        throw new System.Exception("Can't Create  SQLiteParameter in System.Data.SQLite.dll");
        //    }
        //    else
        //    {
        //        return para as DbParameter;
        //    }
        //    //object para = _Cache.Get("SQLite_Parameter");
        //    //if (para == null)
        //    //{
        //    //    Assembly ass = GetAssembly();
        //    //    para = ass.CreateInstance("System.Data.SQLite.SQLiteParameter");
        //    //    if (para == null)
        //    //    {
        //    //        throw new System.Exception("Can't Create  SQLiteParameter in System.Data.SQLite.dll");
        //    //    }
        //    //    else
        //    //    {
        //    //        _Cache.Set("SQLite_Parameter", para, null, 10080);
        //    //    }
        //    //}
        //    //return para as DbParameter;

        //}
    }

    internal partial class SQLiteDal
    {
        protected override string GetSchemaSql(string type)
        {
            switch (type)
            {
                case "U":
                    return "SELECT name as TableName,'' as Description FROM sqlite_master where type='table'";
                case "V":
                    return "SELECT name as TableName,'' as Description FROM sqlite_master where type='view'";
            }
            return "";
        }
    }
}
