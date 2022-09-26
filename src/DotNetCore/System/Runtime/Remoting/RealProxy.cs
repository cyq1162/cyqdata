using System;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Messaging;

namespace System.Runtime.Remoting.Proxies
{
    internal class RealProxy
    {
        private Type serverType;

        public RealProxy(Type serverType)
        {
            this.serverType = serverType;
        }
        internal MarshalByRefObject GetTransparentProxy()
        {
            throw new NotImplementedException();
        }
        internal void InitializeServerObject(IConstructionCallMessage constructCallMsg)
        {
            throw new NotImplementedException();
        }
        public virtual IMessage Invoke(IMessage msg)
        {
            throw new Exception("Invoke");
        }
    }
}