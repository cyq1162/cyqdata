using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data
{
    class NoSqlDal : DbBase
    {
        public NoSqlDal(ConnObject co)
            : base(co)
        {

        }
        protected override System.Data.Common.DbProviderFactory GetFactory(string providerName)
        {
            return NoSqlFactory.Instance;
        }
        protected override bool IsExistsDbName(string dbName)
        {
            string folder = Con.DataSource.TrimEnd('\\');
            folder = folder.Substring(0, folder.Length - DataBase.Length) + dbName;

            return System.IO.Directory.Exists(folder);
        }
    }
}
