
using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Tool;
using System.Threading;
using System.Security.Policy;

namespace CYQ.Data.Cache
{
    internal delegate T UseSocket<T>(MSocket socket, out bool isNoResponse);
    internal delegate void UseSocketVoid(MSocket socket);

    /// <summary>
    /// The ServerPool encapsulates a collection of memcached servers and the associated SocketPool objects.
    /// This class contains the server-selection logic, and contains methods for executing a block of code on 
    /// a socket from the server corresponding to a given key.
    /// 管理多个主机实例的服务
    /// </summary>
    internal partial class HostServer
    {
        //用于验证权限的委托事件。
        internal delegate bool AuthDelegate(MSocket socket);
        internal event AuthDelegate OnAuthEvent;

        private CacheType serverType = CacheType.MemCache;
        /// <summary>
        /// 缓存类型
        /// </summary>
        public CacheType ServerType
        {
            get
            {
                return serverType;
            }
        }
        /// <summary>
        /// 备份的主机池，如果某主机挂了，在配置了备份的情况下，会由备份提供服务。
        /// </summary>
        //private HostServer hostServerBak;
        //public HostServer HostServerBak { get { return hostServerBak; } set { hostServerBak = value; } }

        //Expose the socket pools.
        //private HostInstance[] hostList;
        //internal HostInstance[] HostList { get { return hostList; } }


        // private uint[] hostKeys;

        //Internal configuration properties
        //private int sendReceiveTimeout = 5000;
        //private int connectTimeout = 3000;
        //private uint maxPoolSize = 10;
        //private uint minPoolSize = 1;
        //private TimeSpan socketRecycleAge = TimeSpan.FromMinutes(30);
        //internal int SendReceiveTimeout { get { return sendReceiveTimeout; } set { sendReceiveTimeout = value; } }
        //internal int ConnectTimeout { get { return connectTimeout; } set { connectTimeout = value; } }
        //internal uint MaxPoolSize { get { return maxPoolSize; } set { maxPoolSize = value; } }
        //internal uint MinPoolSize { get { return minPoolSize; } set { minPoolSize = value; } }
        //internal TimeSpan SocketRecycleAge { get { return socketRecycleAge; } set { socketRecycleAge = value; } }

        //private string clientName;
        /// <summary>
        /// Internal constructor. This method takes the array of hosts and sets up an internal list of socketpools.
        /// </summary>
        //internal HostServer(string[] hosts, CacheType serverType)
        //{
        //    this.serverType = serverType;//服务的缓存类型（为了支持Redis的密码功能，在这里增加点依赖扩展。）
        //    hashHostDic = new Dictionary<uint, HostInstance>();
        //    List<HostInstance> pools = new List<HostInstance>();
        //    List<uint> keys = new List<uint>();
        //    foreach (string hostItem in hosts)//遍历每一台主机。
        //    {
        //        if (string.IsNullOrEmpty(hostItem)) { continue; }
        //        string[] items = hostItem.Split('-');
        //        string pwd = "";
        //        string host = items[0].Trim(); ;
        //        if (items.Length > 1)
        //        {
        //            pwd = items[1].Trim();
        //        }
        //        //Create pool
        //        HostInstance pool = new HostInstance(this, host);
        //        pool.OnAfterSocketCreateEvent += new HostInstance.OnAfterSocketCreateDelegate(pool_OnAfterSocketCreateEvent);
        //        if (!string.IsNullOrEmpty(pwd))
        //        {
        //            pool.password = pwd;

        //        }
        //        //Create 250 keys for this pool, store each key in the hostDictionary, as well as in the list of keys.
        //        for (int i = 0; i < 250; i++)
        //        {
        //            uint key = BitConverter.ToUInt32(new ModifiedFNV1_32().ComputeHash(Encoding.UTF8.GetBytes(host + "-" + i)), 0);
        //            if (!hashHostDic.ContainsKey(key))
        //            {
        //                hashHostDic[key] = pool;
        //                keys.Add(key);
        //            }
        //        }

        //        pools.Add(pool);
        //    }

        //    //Hostlist should contain the list of all pools that has been created.
        //    hostList = pools.ToArray();

        //    //Hostkeys should contain the list of all key for all pools that have been created.
        //    //This array forms the server key continuum that we use to lookup which server a
        //    //given item key hash should be assigned to.
        //    keys.Sort();
        //    hostKeys = keys.ToArray();
        //}

        HostConfigWatch watch;
        internal HostServer(CacheType cacheType, string configValue)
        {
            this.serverType = cacheType;
            watch = new HostConfigWatch(configValue);
            watch.OnConfigChangedEvent += new HostConfigWatch.OnConfigChangedDelegate(watch_OnConfigChangedEvent);
            RefleshHostServer(configValue);
        }

        /// <summary>
        /// 缓存配置文件修改时
        /// </summary>
        void watch_OnConfigChangedEvent(string configValue)
        {
            RefleshHostServer(configValue);
        }
        /// <summary>
        /// 刷新主机配置
        /// </summary>
        public void RefleshHostServer(string configValue)
        {
            if (string.IsNullOrEmpty(configValue))
            {
                hostList.Clear();
                hashHostDic.Clear();
                hashKeys = null;
                return;
            }
            lock (this)
            {
                if (CreateHost(configValue))
                {
                    CreateHashHost();
                    CreateHashKeys();
                }
            }
        }

        #region 主机管理
        /// <summary>
        /// 主机【仅有N个】
        /// </summary>
        MDictionary<string, HostNode> hostList = new MDictionary<string, HostNode>();
        /// <summary>
        /// 主机列表。
        /// </summary>
        public MDictionary<string, HostNode> HostList
        {
            get
            {
                return hostList;
            }
        }
        private bool CreateHost(string configValue)
        {
            watch.CompareHostListByConfig(configValue);
            if (hostList.Count == 0)
            {
                AddHost(watch.HostList);
            }
            else //处理变化的情况
            {
                //移除主机。
                foreach (string host in watch.HostRemoveList.List)
                {
                    if (hostList.ContainsKey(host))
                    {
                        hostList[host].Dispose();//释放资源。
                        hostList.Remove(host);//移除主机
                    }
                }
                //添加主机
                AddHost(watch.HostAddList);
            }
            return hostList.Count > 0;
        }
        private void AddHost(MList<string> hosts)
        {
            foreach (string host in hosts.List)
            {
                HostNode instance = new HostNode(this, host);
                instance.OnAfterSocketCreateEvent += new HostNode.OnAfterSocketCreateDelegate(instance_OnAfterSocketCreateEvent);
                hostList.Add(host, instance);
            }
        }
        bool instance_OnAfterSocketCreateEvent(MSocket socket)
        {
            if (OnAuthEvent != null)
            {
                return OnAuthEvent(socket);
            }
            return true;
        }

        #endregion



        #region Execute Host Command


        //internal HostInstance GetSocketPool(string host)
        //{
        //    return Array.Find(HostList, delegate(HostInstance socketPool) { return socketPool.Host == host; });
        //}

        /// <summary>
        /// This method executes the given delegate on a socket from the server that corresponds to the given hash.
        /// If anything causes an error, the given defaultValue will be returned instead.
        /// This method takes care of disposing the socket properly once the delegate has executed.
        /// </summary>
        internal T Execute<T>(uint hash, T defaultValue, UseSocket<T> useSocket)
        {
            HostNode host = GetHost(hash);
            if (host == null)
            {
                return defaultValue;
            }
            return Execute<T>(host, hash, defaultValue, useSocket, true);
        }
        internal T Execute<T>(uint hash, T defaultValue, UseSocket<T> useSocket, bool tryAgain)
        {
            HostNode host = GetHost(hash);
            if (host == null)
            {
                return defaultValue;
            }
            return Execute<T>(host, hash, defaultValue, useSocket, tryAgain);
        }
        internal T Execute<T>(HostNode host, uint hash, T defaultValue, UseSocket<T> useSocket, bool tryAgain)
        {

            MSocket sock = null;
            try
            {
                //Acquire a socket
                sock = host.Acquire();
                if (sock == null && tryAgain)
                {
                    host.AddToDeadPool();
                    return Execute<T>(hash, defaultValue, useSocket, false);
                }

                //Use the socket as a parameter to the delegate and return its result.
                if (sock != null)
                {
                    bool isNoResponse = false;
                    var result = useSocket(sock, out isNoResponse);
                    if (isNoResponse)
                    {
                        host.AddToDeadPool();
                        //Console.WriteLine("1---" + host.IsEndPointDead + " host :" + host.Host);
                        return Execute<T>(hash, defaultValue, useSocket, false);
                    }
                    return result;


                }
            }
            catch (Exception e)
            {

                //Socket is probably broken
                if (sock != null)
                {
                    Interlocked.Increment(ref host.CloseSockets);
                    sock.Close();
                }
                if (tryAgain)
                {
                    host.AddToDeadPool();
                    return Execute<T>(hash, defaultValue, useSocket, false);
                }
                else
                {
                    Log.Write(e, LogType.Cache);
                }
            }
            finally
            {
                if (sock != null)
                {
                    sock.ReturnPool();
                }
            }
            return defaultValue;
        }

        internal void Execute(HostNode host, UseSocketVoid use)
        {
            MSocket sock = null;
            try
            {
                //Acquire a socket
                sock = host.Acquire();
                //Use the socket as a parameter to the delegate and return its result.
                if (sock != null)
                {
                    use(sock);
                }
            }
            catch (Exception e)
            {
                //Socket is probably broken
                if (sock != null)
                {
                    Interlocked.Increment(ref host.CloseSockets);
                    sock.Close();
                }
                Log.Write(e, LogType.Cache);
            }
            finally
            {
                if (sock != null)
                {
                    sock.ReturnPool();
                }
            }
        }

        /// <summary>
        /// This method executes the given delegate on all servers.
        /// </summary>
        internal int ExecuteAll(UseSocket<bool> useSocket, uint hash)
        {
            int okCount = 0;
            foreach (KeyValuePair<string, HostNode> item in hostList)
            {
                if (Execute<bool>(item.Value, hash, false, useSocket, false))
                {
                    okCount++;
                }
            }
            return okCount;
        }

        #endregion
    }

    /// <summary>
    /// 处理一致性hash
    /// </summary>
    internal partial class HostServer
    {
        /// <summary>
        /// 一致性Hash产生的结点数，数越大，分布越平均
        /// </summary>
        private int HashNodeCount
        {
            get
            {
                //服务节点>1，一致性hash才有意义
                if (watch.HostList.Count > 1)
                {
                    return 160;//nginx 节点数 160
                }
                return 1;
            }
        }
        /// <summary>
        /// 一致性hash的主机分布列表
        /// </summary>
        private MDictionary<uint, HostNode> hashHostDic = new MDictionary<uint, HostNode>();
        /// <summary>
        /// 创建用于查询（已排序）的hashkey。
        /// </summary>
        private uint[] hashKeys;
        private void CreateHashHost()
        {
            if (hashHostDic.Count == 0)
            {
                AddHashHost(watch.HostList);
            }
            else
            {
                //移除hashHost
                foreach (string host in watch.HostRemoveList.List)
                {
                    for (int i = 0; i < HashNodeCount; i++)
                    {
                        uint hostHashKey = HashCreator.CreateCode(host + "-" + i);
                        if (hashHostDic.ContainsKey(hostHashKey))
                        {
                            hashHostDic.Remove(hostHashKey);
                        }
                    }
                }
                //添加hashHost
                AddHashHost(watch.HostAddList);
            }
        }
        private void AddHashHost(MList<string> hosts)
        {
            foreach (string host in hosts.List)
            {
                //Create keys for this pool, store each key in the hostDictionary, as well as in the list of keys.
                for (int i = 0; i < HashNodeCount; i++)
                {
                    uint hostHashKey = HashCreator.CreateCode(host + "-" + i);
                    if (!hashHostDic.ContainsKey(hostHashKey) && hostList.ContainsKey(host))
                    {
                        hashHostDic.Add(hostHashKey, hostList[host]);
                    }
                }
            }
        }

        private void CreateHashKeys()
        {
            List<uint> list = new List<uint>(hashHostDic.Count);
            foreach (KeyValuePair<uint, HostNode> item in hashHostDic)
            {
                list.Add(item.Key);
            }
            list.Sort();
            hashKeys = list.ToArray();
        }

        /// <summary>
        /// Given an item key hash, this method returns the socketpool which is closest on the server key continuum.
        /// 获取一台用于服务的主机实例。
        /// </summary>
        //internal HostNode GetHost(uint hash)
        //{
        //    HostNode node = GetHost(hash, null);
        //    if (node != null && node.IsEndPointDead)
        //    {
        //        return GetHost(hash, node.Host);
        //    }
        //    return node;
        //}
        internal HostNode GetHost(uint hash)
        {
            //Quick return if we only have one host.
            if (hostList.Count == 1)
            {
                return hostList[0];
            }
            uint index = 0;
            int i = 0;
            lock (this)
            {
                //New "ketama" host selection.
                i = Array.BinarySearch(hashKeys, hash);

                //If not exact match...
                if (i < 0)
                {
                    //Get the index of the first item bigger than the one searched for.
                    i = ~i;

                    //If i is bigger than the last index, it was bigger than the last item = use the first item.
                    if (i >= hashKeys.Length)
                    {
                        i = 0;
                    }
                }
                index = hashKeys[i];
            }
            HostNode node = hashHostDic[index];
            if (node.IsEndPointDead)
            {
                //往后循环
                for (int j = i + 1; j < hashKeys.Length; j++)
                {
                    index = hashKeys[j];
                    if (!hashHostDic[index].IsEndPointDead)
                    {
                        return hashHostDic[index];
                    }
                }
                //从0开始循环
                for (uint j = 0; j < i; j++)
                {
                    index = hashKeys[j];
                    if (!hashHostDic[index].IsEndPointDead)
                    {
                        return hashHostDic[index];
                    }
                }
            }
            return node;
        }
    }
}