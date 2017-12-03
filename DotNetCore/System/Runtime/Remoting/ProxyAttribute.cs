using System;

namespace System.Runtime.Remoting.Proxies
{
    internal class ProxyAttribute:Attribute
    {
        public virtual MarshalByRefObject CreateInstance(Type serverType)
        {
            throw new NotImplementedException();
        }
    }
}