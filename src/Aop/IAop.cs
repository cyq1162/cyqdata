using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Aop
{
    /// <summary>
    /// Aop接口，需要实现时继承
    /// </summary>
    public interface IAop
    {
        /// <summary>
        /// 方法调用之前被调用
        /// </summary>
        /// <param name="action">方法名称</param>
        /// <param name="aopInfo">附带分支参数</param>
        AopResult Begin(AopEnum action, AopInfo aopInfo);
        /// <summary>
        /// 方法调用之后被调用
        /// </summary>
        /// <param name="action">方法名称</param>
        /// <param name="aopInfo">附带分支参数</param>
        void End(AopEnum action, AopInfo aopInfo);
        /// <summary>
        /// 数据库操作产生异常时,引发此方法
        /// </summary>
        /// <param name="msg"></param>
        void OnError(string msg);

        /// <summary>
        /// 内部获取配置Aop，外部使用返回null即可。
        /// </summary>
        /// <returns></returns>
        //IAop GetFromConfig();
        /// <summary>
        /// 克隆返回一个新的对象
        /// </summary>
        /// <returns></returns>
        IAop Clone();
        /// <summary>
        /// Aop 首次加载时被触发的事件
        /// </summary>
        void OnLoad();
    }
}
