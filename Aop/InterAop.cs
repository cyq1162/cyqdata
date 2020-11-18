using CYQ.Data.Cache;
using System;
using CYQ.Data.Table;

namespace CYQ.Data.Aop
{
    /// <summary>
    /// 内部预先实现CacheAop
    /// </summary>
    internal class InterAop
    {
        private CacheManage _Cache = CacheManage.LocalInstance;//Cache操作
        // private AutoCache cacheAop = new AutoCache();
        private static readonly object lockObj = new object();
        internal bool isHasCache = false;
        public AopOp aopOp = AopOp.OpenAll;
        internal bool IsLoadAop
        {
            get
            {
                return aopOp != AopOp.CloseAll;
            }
        }
        internal bool IsTxtDataBase
        {
            get
            {
                return Para.DalType == DataBaseType.Txt || Para.DalType == DataBaseType.Xml;
            }
        }
        private AopInfo _AopInfo;
        /// <summary>
        /// Aop参数
        /// </summary>
        public AopInfo Para
        {
            get
            {
                if (_AopInfo == null)
                {
                    _AopInfo = new AopInfo();
                }
                return _AopInfo;
            }
        }

        private IAop outerAop;
        public InterAop()
        {
            outerAop = GetFromConfig();
            if (outerAop == null)
            {
                aopOp = AppConfig.Cache.IsAutoCache ? AopOp.OnlyInner : AopOp.CloseAll;
            }
            else
            {
                aopOp = AppConfig.Cache.IsAutoCache ? AopOp.OpenAll : AopOp.OnlyOuter;
            }
        }
        #region IAop 成员

        public AopResult Begin(AopEnum action)
        {
            AopResult ar = AopResult.Continue;
            if (outerAop != null && (aopOp == AopOp.OpenAll || aopOp == AopOp.OnlyOuter))
            {
                ar = outerAop.Begin(action, Para);
                if (ar == AopResult.Return)
                {
                    return ar;
                }
            }
            if (aopOp == AopOp.OpenAll || aopOp == AopOp.OnlyInner)
            {
                if (!IsTxtDataBase) // 只要不是直接返回，------调整机制，由Aop参数控制。
                {
                    isHasCache = AutoCache.GetCache(action, Para); //找看有没有Cache
                }
                if (isHasCache)  //找到Cache
                {

                    if (outerAop == null || ar == AopResult.Default)//不执行End
                    {
                        return AopResult.Return;
                    }
                    return AopResult.Break;//外部Aop说：还需要执行End
                }
            }
            return ar;// 没有Cache，默认返回
        }

        public void End(AopEnum action)
        {
            if (outerAop != null && (aopOp == AopOp.OpenAll || aopOp == AopOp.OnlyOuter))
            {
                outerAop.End(action, Para);
            }
            if (aopOp == AopOp.OpenAll || aopOp == AopOp.OnlyInner)
            {
                if (!isHasCache  && Para.IsSuccess)//Select内部调用了GetCount，GetCount的内部isHasCache为true影响了
                {
                    AutoCache.SetCache(action, Para); //找看有没有Cache
                }
            }
        }

        public void OnError(string msg)
        {
            if (outerAop != null)
            {
                outerAop.OnError(msg);
            }
        }

        #endregion
        static bool _IsLoadCompleted = false;
        private IAop GetFromConfig()
        {

            IAop aop = null;
            string aopApp = AppConfig.Aop;
            if (!string.IsNullOrEmpty(aopApp))
            {
                string key = "OuterAop_Instance";
                if (_Cache.Contains(key))
                {
                    aop = _Cache.Get(key) as IAop;
                }
                else
                {
                    #region AOP加载

                    string[] aopItem = aopApp.Split(',');
                    if (aopItem.Length == 2)//完整类名,程序集(dll)名称
                    {
                        if (!_IsLoadCompleted)
                        {
                            try
                            {
                                lock (lockObj)
                                {
                                    if (_IsLoadCompleted)
                                    {
                                        return GetFromConfig();//重新去缓存里拿。
                                    }
                                    _IsLoadCompleted = true;
                                    System.Reflection.Assembly ass = System.Reflection.Assembly.Load(aopItem[1]);
                                    if (ass != null)
                                    {
                                        object instance = ass.CreateInstance(aopItem[0]);
                                        if (instance != null)
                                        {
                                            _Cache.Set(key, instance, 1440, AppConst.AssemblyPath + aopItem[1].Replace(".dll", "") + ".dll");
                                            aop = instance as IAop;
                                            aop.OnLoad();
                                        }
                                    }
                                }

                            }
                            catch (Exception err)
                            {
                                string errMsg = err.Message + "--Web.config need add a config item,for example:<add key=\"Aop\" value=\"Web.Aop.AopAction,Aop\" />(value format : ClassFullName,AssemblyName) ";
                                Error.Throw(errMsg);
                            }
                        }
                    }
                    #endregion
                }
            }
            if (aop != null)
            {
                IAop cloneAop= aop.Clone();
                return cloneAop == null ? aop : cloneAop;
            }
            return null;
        }
    }
}
