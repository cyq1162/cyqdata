using CYQ.Data.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CYQ.Data
{
    internal partial class NoSqlDal : DalBase
    {
        public NoSqlDal(ConnObject co)
            : base(co)
        {

        }
        protected override System.Data.Common.DbProviderFactory GetFactory()
        {
            return new NoSqlFactory(this);
        }
        protected override bool IsExistsDbName(string dbName)
        {
            string folder = Con.DataSource.TrimEnd('\\', '/');
            folder = folder.Substring(0, folder.Length - DataBaseName.Length) + dbName;

            return System.IO.Directory.Exists(folder);
        }
    }
    internal partial class NoSqlDal
    {
        public override Dictionary<string, string> GetTables(bool isIgnoreCache)
        {
            return GetTables();
        }
        public override Dictionary<string, string> GetTables()
        {
            Dictionary<string, string> tables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] files = Directory.GetFiles(Con.DataSource, "*.ts");
            foreach (string file in files)
            {
                MDataColumn mdc = MDataColumn.CreateFrom(file);
                if (mdc != null)
                {
                    tables.Add(Path.GetFileNameWithoutExtension(file), mdc.Description);
                }
            }
            files = null;
            return tables;
        }
    }
}
