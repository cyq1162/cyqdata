using System;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

namespace System.Runtime.Remoting
{
    internal class RemotingServices
    {
        internal static RealProxy GetRealProxy(MarshalByRefObject target)
        {
            throw new NotImplementedException();
        }

        internal static IMessage ExecuteMessage(MarshalByRefObject target, IMethodCallMessage callMsg)
        {
            throw new NotImplementedException();
        }
    }
}