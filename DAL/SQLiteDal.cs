using System.Data;
using System.Data.Common;
using CYQ.Data.Cache;
using System.Reflection;
using System;
using System.IO;
namespace CYQ.Data
{
    internal class SQLiteDal : DbBase
    {
        private CacheManage _Cache = CacheManage.LocalInstance;//Cache操作
        public SQLiteDal(ConnObject co)
            : base(co)
        {
            base.isUseUnsafeModeOnSqlite = co.Master.Conn.ToLower().Contains("syncpragma=off");
        }
        public override void AddReturnPara()
        {

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
                    ass = Assembly.Load(providerName);
                    _Cache.Add("SQLite_Assembly", ass, null, 10080, System.Web.Caching.CacheItemPriority.High);
                }
                catch (Exception err)
                {
                    string errMsg = err.Message;
                    if (!System.IO.File.Exists(AppConst.RunFolderPath + "System.Data.SQLite.DLL"))
                    {
                        errMsg = "Can't find the System.Data.SQLite.dll more info : " + errMsg;
                    }
                    else
                    {
                        errMsg = "You need to choose the right version : x86 or x64. more info : \r\n" + errMsg;
                    }
                    Error.Throw(errMsg);
                }
            }
            return ass as Assembly;
        }
        protected override DbProviderFactory GetFactory(string providerName)
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
                    _Cache.Add("SQLite_Factory", factory, null, 10080, System.Web.Caching.CacheItemPriority.High);
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
        //    //        _Cache.Add("SQLite_Parameter", para, null, 10080);
        //    //    }
        //    //}
        //    //return para as DbParameter;

        //}
    }
}
