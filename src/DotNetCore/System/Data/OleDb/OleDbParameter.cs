namespace System.Data.OleDb
{
    internal class OleDbParameter
    {
        public string ParameterName { get; internal set; }
        public object Value { get; internal set; }
        public DbType DbType { get; internal set; }
        public object OleDbType { get; internal set; }
    }
}