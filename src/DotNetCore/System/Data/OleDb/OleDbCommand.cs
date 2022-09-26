namespace System.Data.OleDb
{
    internal class OleDbCommand
    {
        private string sqlText;
        private OleDbConnection con;

        public OleDbCommand(string sqlText, OleDbConnection con)
        {
            this.sqlText = sqlText;
            this.con = con;
        }

        public OleDbDataReader ExecuteReader() { return null; }

        public OleDbDataReader ExecuteReader(CommandBehavior behavior) { return null; }
    }
}