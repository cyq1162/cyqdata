using System.Data;
using System.Data.Common;
using CYQ.Data.Cache;
using System.Reflection;
using System;
using System.IO;
using System.Collections.Generic;
namespace CYQ.Data
{
    internal partial class FireBirdDal : DalBase
    {
        private DistributedCache _Cache = DistributedCache.Local;//Cache操作
        public FireBirdDal(ConnObject co)
            : base(co)
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
            object ass = _Cache.Get("Firebird_Assembly");
            if (ass == null)
            {
                string name = string.Empty;
                if (File.Exists(AppConst.AssemblyPath + "FirebirdSql.Data.FirebirdClient.dll"))
                {
                    name = "FirebirdSql.Data.FirebirdClient";
                }
                else
                {
                    name = "Can't find the FirebirdSql.Data.FirebirdClient.dll";
                    Error.Throw(name);
                }
                ass = Assembly.Load(name);
                DistributedCache.Local.Set("Firebird_Assembly", ass, 10080);
            }
            return ass as Assembly;
        }
        protected override DbProviderFactory GetFactory()
        {
            object factory = _Cache.Get("Firebird_Factory");
            if (factory == null)
            {
                Assembly ass = GetAssembly();
                Type t = ass.GetType("FirebirdSql.Data.FirebirdClient.FirebirdClientFactory");
                if (t != null)
                {
                    FieldInfo fi = t.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                    if (fi != null)
                    {
                        factory = fi.GetValue(null);
                    }
                }
                //factory = ass.CreateInstance(DllName + ".DB2Factory.Instance");
                if (factory == null)
                {
                    throw new System.Exception("Can't Create FirebirdClientFactory in FirebirdSql.Data.FirebirdClient.dll");
                }
                else
                {
                    _Cache.Set("Firebird_Factory", factory, 10080);
                }
            }
            return factory as DbProviderFactory;

        }
        public override string DataBaseName
        {
            get
            {
                return Path.GetFileNameWithoutExtension(DbFilePath);
            }
        }
        public string DbFilePath
        {
            get
            {
                return _con.Database;
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

    internal partial class FireBirdDal
    {
        protected override string GetUVPSql(string type)
        {
            switch (type)
            {
                case "U":
                    return "SELECT RDB$RELATION_NAME AS TableName, RDB$DESCRIPTION AS Description FROM RDB$RELATIONS WHERE RDB$SYSTEM_FLAG = 0 AND RDB$VIEW_BLR IS NULL ORDER BY TableName";
                case "V":
                    return "SELECT RDB$RELATION_NAME AS TableName, RDB$DESCRIPTION AS Description FROM RDB$RELATIONS WHERE RDB$SYSTEM_FLAG = 0 AND RDB$VIEW_BLR IS NOT NULL ORDER BY TableName";
            }
            return "";
        }
    }
}
