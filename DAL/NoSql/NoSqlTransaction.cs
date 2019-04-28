using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace CYQ.Data
{
    internal class NoSqlTransaction : DbTransaction
    {
        DbConnection con;
        IsolationLevel level;
        public NoSqlTransaction(DbConnection con, IsolationLevel level)
        {
            this.con = con;
            this.level = level;
        }

        public override void Commit()
        {

        }

        protected override DbConnection DbConnection
        {
            get { return con; }
        }

        public override IsolationLevel IsolationLevel
        {
            get
            {
                return level;
            }
        }

        public override void Rollback()
        {

        }
    }
}
