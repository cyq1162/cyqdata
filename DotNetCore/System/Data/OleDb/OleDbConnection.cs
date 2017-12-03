namespace System.Data.OleDb
{
    internal class OleDbConnection:IDisposable
    {
        private string connectionString;

        public OleDbConnection(string connectionString)
        {
            this.connectionString = connectionString;
        }

        internal void Open()
        {
            throw new NotImplementedException();
        }

        internal void Close()
        {
            throw new NotImplementedException();
        }

        internal DataTable GetOleDbSchemaTable(object columns, object[] v)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}