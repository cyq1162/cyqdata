using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Web;
using CYQ.Data.Tool;

namespace CYQ.Data
{
    /// <summary>
    /// 一个数据库对像实例：集合了主从备三种
    /// </summary>
    internal partial class ConnObject
    {
        public ConnBean Master;
        public ConnBean BackUp;
        public MList<ConnBean> Slave = new MList<ConnBean>();
        /// <summary>
        /// 主备互换位置。
        /// </summary>
        internal void InterChange()
        {
            if (BackUp != null)
            {
                ConnBean middle = Master;
                Master = BackUp;
                BackUp = middle;
            }
        }
        static int index = -1;
        static readonly object o = new object();
        public ConnBean GetSlave()
        {
            if (Slave.Count > 0)
            {
                lock (o)
                {
                    if (index == -1)
                    {
                        index = new Random().Next(Slave.Count);
                    }
                    if (index >= Slave.Count)//2
                    {
                        index = 0;
                    }
                    ConnBean slaveBean = Slave[index];
                    index++;
                    if (slaveBean.IsOK)//链接正常，则返回。
                    {
                        return slaveBean;
                    }
                    else if (Slave.Count > 1)
                    {
                        //int i = index + 1;//尝试一下个，。
                        for (int i = index + 1; i < Slave.Count + 1; i++)
                        {
                            if (i == Slave.Count)
                            {
                                i = 0;
                            }
                            if (i == index) { break; }
                            if (Slave[i].IsOK)
                            {
                                return Slave[i];
                            }
                        }
                    }
                    //从全部挂了，返回主
                    if (Master != null && Master.IsOK)
                    {
                        return Master;
                    }
                    else if (BackUp != null && BackUp.IsOK)
                    {
                        return BackUp;
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// 设置临时仅允许访问主库（默认10秒）
        /// </summary>
        public void SetFocusOnMaster()
        {
            if (Slave.Count > 0)
            {
                string id = GetIdentity();//获取当前的标识
                Cache.CacheManage.LocalInstance.Set(id, 1, AppConfig.DB.MasterSlaveTime / 60.0);
            }
        }
        public bool IsAllowSlave()
        {
            if (Slave.Count == 0) { return false; }
            string id = GetIdentity();//获取当前的标识
            return !Cache.CacheManage.LocalInstance.Contains(id);
        }
        private string GetIdentity()
        {
            string id = string.Empty;
            if (HttpContext.Current != null)
            {
                if (HttpContext.Current.Session != null)
                {
                    id = HttpContext.Current.Session.SessionID;
                }
                else if (HttpContext.Current.Request["Token"] != null)
                {
                    id = HttpContext.Current.Request["Token"];
                }
                else if (HttpContext.Current.Request.Headers["Token"] != null)
                {
                    id = HttpContext.Current.Request.Headers["Token"];
                }
                else if (HttpContext.Current.Request["MasterSlaveID"] != null)
                {
                    id = HttpContext.Current.Request["MasterSlaveID"];
                }
                if (string.IsNullOrEmpty(id))
                {
                    HttpCookie cookie = HttpContext.Current.Request.Cookies["MasterSlaveID"];
                    if (cookie != null)
                    {
                        id = cookie.Value;
                    }
                    else
                    {
                        id = Guid.NewGuid().ToString().Replace("-", "");
                        cookie = new HttpCookie("MasterSlaveID", id);
                        cookie.Expires = DateTime.Now.AddMonths(1);
                        HttpContext.Current.Response.Cookies.Add(cookie);
                    }
                }
            }
            if (string.IsNullOrEmpty(id))
            {
                id = DateTime.Now.Minute + Thread.CurrentThread.ManagedThreadId.ToString();
            }
            return "MasterSlave_" + id;
        }
    }

    internal partial class ConnObject
    {
        /// <summary>
        /// 所有链接的对象集合
        /// </summary>
        private static MDictionary<string, ConnObject> connDicCache = new MDictionary<string, ConnObject>(StringComparer.OrdinalIgnoreCase);

        internal static ConnObject Create(string connNameOrString)
        {
            connNameOrString = string.IsNullOrEmpty(connNameOrString) ? AppConfig.DB.DefaultConn : connNameOrString;
            if (connNameOrString.EndsWith("_Bak"))
            {
                connNameOrString = connNameOrString.Replace("_Bak", "");
            }
            if (connDicCache.ContainsKey(connNameOrString))
            {
                return connDicCache[connNameOrString];
            }
            ConnBean cbMaster = ConnBean.Create(connNameOrString);
            if (cbMaster == null)
            {
                #region 重试读取默认Conn配置
                string errMsg = string.Format("Can't find the connection key '{0}' from connectionStrings config section !", connNameOrString);
                if (connNameOrString == AppConfig.DB.DefaultConn)
                {
                    Error.Throw(errMsg);
                }
                else
                {
                    ConnBean cb = ConnBean.Create(AppConfig.DB.DefaultConn); // 这里切到了默认链接，导致ConnName变成了默认
                    if (cb != null)
                    {
                        cbMaster = cb.Clone();//获取默认的值。
                    }
                    else
                    {
                        Error.Throw(errMsg);
                    }
                }
                #endregion
            }
            ConnObject co = new ConnObject();
            co.Master = cbMaster;
            #region 加载主从备节点
            if (connNameOrString != null && connNameOrString.Length < 32 && !connNameOrString.Trim().Contains(" ")) // 为configKey
            {
                ConnBean coBak = ConnBean.Create(connNameOrString + "_Bak");
                if (coBak != null && coBak.ConnDalType == cbMaster.ConnDalType)
                {
                    co.BackUp = coBak;
                    co.BackUp.IsBackup = true;
                }
                for (int i = 1; i < 10000; i++)
                {
                    ConnBean cbSlave = ConnBean.Create(connNameOrString + "_Slave" + i);
                    if (cbSlave == null)
                    {
                        break;
                    }
                    cbSlave.IsSlave = true;
                    co.Slave.Add(cbSlave);
                }
            }
            #endregion

            if (!connDicCache.ContainsKey(connNameOrString) && co.Master.ConnName == connNameOrString) // 非一致的，由外面切换后再缓存
            {
                connDicCache.Set(connNameOrString, co);
            }
            return co;
        }
        public void SaveToCache(string key)
        {
            if (!connDicCache.ContainsKey(key))
            {
                connDicCache.Add(key, this);
            }
        }
        public static void ClearCache(string key)
        {
            connDicCache.Remove(key);
        }
        /// <summary>
        /// 定时检测异常的链接是否恢复。
        /// </summary>
        /// <param name="threadID"></param>
        public static void CheckConnIsOk(object threadID)
        {
            while (true)
            {
                Thread.Sleep(3000);
                if (connDicCache.Count > 0)
                {
                    try
                    {
                        string[] items = new string[connDicCache.Count];
                        connDicCache.Keys.CopyTo(items, 0);
                        foreach (string key in items)
                        {
                            ConnObject obj = connDicCache[key];
                            if (obj != null)
                            {
                                if (!obj.Master.IsOK) 
                                {
                                    if (obj.Master.ConnName == obj.Master.ConnString)
                                    {
                                        connDicCache.Remove(key);//移除错误的链接。
                                        continue;
                                    }
                                    obj.Master.TryTestConn(); 
                                }
                                if (obj.BackUp != null && !obj.BackUp.IsOK) { obj.BackUp.TryTestConn(); }
                                if (obj.Slave != null && obj.Slave.Count > 0)
                                {
                                    for (int i = 0; i < obj.Slave.Count; i++)
                                    {
                                        if (!obj.Slave[i].IsOK)
                                        {
                                            obj.Slave[i].TryTestConn();
                                        }
                                    }
                                }
                            }
                        }
                        items = null;
                    }
                    catch
                    {

                    }

                }
            }
        }
    }
}
