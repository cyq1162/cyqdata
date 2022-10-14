using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Web;
using CYQ.Data.Tool;

namespace CYQ.Data
{
    /// <summary>
    /// һ�����ݿ����ʵ�������������ӱ�����
    /// </summary>
    internal partial class ConnObject
    {
        public ConnBean Master;
        public ConnBean BackUp;
        public MList<ConnBean> Slave = new MList<ConnBean>();
        /// <summary>
        /// ��������λ�á�
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
                    if (slaveBean.IsOK)//�����������򷵻ء�
                    {
                        return slaveBean;
                    }
                    else if (Slave.Count > 1)
                    {
                        //int i = index + 1;//����һ�¸�����
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
                    //��ȫ�����ˣ�������
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
        /// ������ʱ������������⣨Ĭ��10�룩
        /// </summary>
        public void SetFocusOnMaster()
        {
            if (Slave.Count > 0)
            {
                string id = StaticTool.GetMasterSlaveKey();//��ȡ��ǰ�ı�ʶ
                Cache.CacheManage.LocalInstance.Set(id, 1, AppConfig.DB.MasterSlaveTime / 60.0);
            }
        }
        public bool IsAllowSlave()
        {
            if (Slave.Count == 0) { return false; }
            string id = StaticTool.GetMasterSlaveKey();//��ȡ��ǰ�ı�ʶ
            return !Cache.CacheManage.LocalInstance.Contains(id);
        }

    }

    internal partial class ConnObject
    {
        /// <summary>
        /// �������ӵĶ��󼯺�
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
                #region ���Զ�ȡĬ��Conn����
                string errMsg = string.Format("Can't find the connection key '{0}' from connectionStrings config section !", connNameOrString);
                if (connNameOrString == AppConfig.DB.DefaultConn)
                {
                    Error.Throw(errMsg);
                }
                else
                {
                    ConnBean cb = ConnBean.Create(AppConfig.DB.DefaultConn); // �����е���Ĭ�����ӣ�����ConnName�����Ĭ��
                    if (cb != null)
                    {
                        cbMaster = cb.Clone();//��ȡĬ�ϵ�ֵ��
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
            #region �������ӱ��ڵ�
            if (connNameOrString != null && connNameOrString.Length < 32 && !connNameOrString.Trim().Contains(" ")) // ΪconfigKey
            {
                ConnBean coBak = ConnBean.Create(connNameOrString + "_Bak");
                if (coBak != null && coBak.ConnDataBaseType == cbMaster.ConnDataBaseType)
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

            if (!connDicCache.ContainsKey(connNameOrString) && co.Master.ConnName == connNameOrString) // ��һ�µģ��������л����ٻ���
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
        public static void Clear()
        {
            connDicCache.Clear();
        }
        public static void Remove(string key)
        {
            connDicCache.Remove(key);
        }
        /// <summary>
        /// ��ʱ����쳣�������Ƿ�ָ���
        /// </summary>
        /// <param name="threadID"></param>
        public static void CheckConnIsOk(object threadID)
        {
            System.Diagnostics.Debug.WriteLine("ConnObject.CheckConnIsOk on Thread :" + threadID);
            while (true)
            {
                Thread.Sleep(1000);
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
                                        connDicCache.Remove(key);//�Ƴ���������ӡ�
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
