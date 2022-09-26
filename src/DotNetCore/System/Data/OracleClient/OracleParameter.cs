namespace System.Data.OracleClient
{
    internal class OracleParameter
    {
        public string ParameterName { get; internal set; }
        public object OracleType { get; internal set; }
        public int Size { get; internal set; }
        public object Value { get; internal set; }
        public ParameterDirection Direction { get; internal set; }
    }
}