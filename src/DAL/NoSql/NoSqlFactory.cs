using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data;
using System.IO;
using CYQ.Data.Table;
namespace CYQ.Data
{
    internal sealed class NoSqlFactory : DbProviderFactory
    {

        NoSqlDal _NoSqlDal;

        // Methods
        public NoSqlFactory(NoSqlDal noSqlDal)
        {
            _NoSqlDal = noSqlDal;
        }
        public override DbConnection CreateConnection()
        {
            return new NoSqlConnection(base.ToString(), _NoSqlDal);
        }
    }
   

}
