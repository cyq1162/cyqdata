using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting;
using System;
using System.Reflection;
using System.Runtime.Remoting.Services;

namespace CYQ.Data.Orm
{
    class AopAttribute : ProxyAttribute
    {

        public override MarshalByRefObject CreateInstance(Type serverType)
        {
            AopProxy realProxy = new AopProxy(serverType, base.CreateInstance(serverType));
            return realProxy.GetTransparentProxy() as MarshalByRefObject;

        }
    }
    class AopProxy : RealProxy
    {
        MethodInfo method;
        MarshalByRefObject _target = null;
        public AopProxy(Type serverType, MarshalByRefObject target)
            : base(serverType)
        {
            _target = target;
            method = serverType.GetMethod("Set", BindingFlags.NonPublic | BindingFlags.Instance);
        }
        public override IMessage Invoke(IMessage msg)
        {
            if (msg != null)
            {
                if (msg is IConstructionCallMessage)
                {
                    IConstructionCallMessage constructCallMsg = msg as IConstructionCallMessage;
                    //IConstructionReturnMessage constructionReturnMessage = this.InitializeServerObject((IConstructionCallMessage)msg);
                    //RealProxy.SetStubData(this, constructionReturnMessage.ReturnValue);
                    //return constructionReturnMessage;

                    RealProxy defaultProxy = RemotingServices.GetRealProxy(_target);

                    //�������������һ����_target����һ��û��ֱ��ʵ��������������͸������
                    //�����Ļ����ᵼ��û��ֱ����������
                    defaultProxy.InitializeServerObject(constructCallMsg);

                    //������һ��RealProxy������ͨ��GetTransparentProxy�����õ�͸������
                    return EnterpriseServicesHelper.CreateConstructionReturnMessage(constructCallMsg, (MarshalByRefObject)GetTransparentProxy());

                }
                else if (msg is IMethodCallMessage)
                {
                    IMethodCallMessage callMsg = msg as IMethodCallMessage;
                    object[] args = callMsg.Args;

                    //System.Windows.Forms.MessageBox.Show(callMsg.MethodBase.ToString());

                    if (callMsg.MethodName.StartsWith("set_") && args.Length == 1)
                    {
                        method.Invoke(_target, new object[] { callMsg.MethodName.Substring(4), args[0] });//�����Խ��е���
                    }
                    return RemotingServices.ExecuteMessage(_target, callMsg);

                    //IMessage message = null;
                    //try
                    //{

                    //    MarshalByRefObject obj = GetUnwrappedServer();
                    //    if (callMsg.MethodName.StartsWith("set_") && args.Length == 1)
                    //    {
                    //        method.Invoke(obj, new object[] { callMsg.MethodName.Substring(4), args[0] });//�����Խ��е���
                    //    } 

                    //    object o = callMsg.MethodBase.Invoke(obj, args);
                    //    message = new ReturnMessage(o, args, args.Length, callMsg.LogicalCallContext, callMsg);
                    //}
                    //catch (Exception e)
                    //{
                    //    message = new ReturnMessage(e, callMsg);
                    //}
                    //return message;
                }
            }
            return msg;
        }
    }
}
