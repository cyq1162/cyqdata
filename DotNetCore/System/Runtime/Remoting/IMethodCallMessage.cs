namespace System.Runtime.Remoting.Activation
{
    internal interface IMethodCallMessage
    {
        string MethodName { get; set; }
        object[] Args { get; }
    }
}