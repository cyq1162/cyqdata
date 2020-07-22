using CYQ.Data.Cache;
using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Text;

namespace CYQ.Data
{
    internal partial class DB2Dal : DalBase
    {
        private CacheManage _Cache = CacheManage.LocalInstance;//Cache操作
        public DB2Dal(ConnObject co)
              : base(co)
        { }
        private static string DllName
        {
            get
            {
                if (AppConfig.IsAspNetCore)
                {
                    return "IBM.Data.DB2.Core";
                }
                return "IBM.Data.DB2";
            }
        }
        private static string NameSpace
        {
            get
            {
                return DllName;
            }
        }
        internal static Assembly GetAssembly()
        {
            string key = "DB2Client_Assembly";
            object ass = CacheManage.LocalInstance.Get(key);
            if (ass == null)
            {
                try
                {
                    ass = Assembly.Load(DllName);
                    CacheManage.LocalInstance.Set(key, ass, 10080);
                }
                catch (Exception err)
                {
                    string errMsg = err.Message;
                    if (!File.Exists(AppConst.AssemblyPath + DllName + ".dll"))
                    {
                        errMsg = "Can't find the " + DllName + ".dll more info : " + errMsg;
                    }
                    Error.Throw(errMsg);
                }
            }
            return ass as Assembly;
        }
        protected override DbProviderFactory GetFactory()
        {
            //return DbProviderFactories.GetFactory(DllName);
            string key = "DB2Client_Factory";
            object factory = _Cache.Get(key);
            if (factory == null)
            {
                Assembly ass = GetAssembly();
                Type t = ass.GetType(DllName + ".DB2Factory");
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
                    throw new System.Exception("Can't Create DB2Factory in " + DllName + ".dll");
                }
                else
                {
                    _Cache.Set(key, factory, 10080);
                }

            }
            return factory as DbProviderFactory;
        }
        protected override bool IsExistsDbName(string dbName)
        {
            return DBTool.TestConn(GetConnString(dbName));
        }
        public override char Pre
        {
            get
            {
                return '@';
            }
        }
    }
    internal partial class DB2Dal
    {
        protected override string GetSchemaSql(string type)
        {
            if (type == "U")
            {
                return "select name as TableName,remarks as Description from sysibm.systables where type = 'T' and creator<>'SYSIBM'";
            }
            else if (type == "V")
            {
                return "select name as TableName,remarks as Description from sysibm.systables where type = 'V' and creator<>'SYSCAT'";
            }
            return "";
        }
    }
}
