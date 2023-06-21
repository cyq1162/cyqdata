
using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Tool;
using System.Threading;
using Newtonsoft.Json;

namespace CYQ.Data.Cache
{
    internal delegate T UseSocket<T>(MSocket socket);
    internal delegate void UseSocket(MSocket socket);

    /// <summary>
    /// The ServerPool encapsulates a collection of memcached servers and the associated SocketPool objects.
    /// This class contains the server-selection logic, and contains methods for executing a block of code on 
    /// a socket from the server corresponding to a given key.
    /// ����������ʵ���ķ���
    /// </summary>
    internal partial class HostServer
    {
        //������֤Ȩ�޵�ί���¼���
        internal delegate bool AuthDelegate(MSocket socket);
        internal event AuthDelegate OnAuthEvent;

        private static LogAdapter logger = LogAdapter.GetLogger(typeof(HostServer));

        internal CacheType serverType = CacheType.MemCache;

        /// <summary>
        /// ���ݵ������أ����ĳ�������ˣ��������˱��ݵ�����£����ɱ����ṩ����
        /// </summary>
        internal HostServer hostServerBak;

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
        //    this.serverType = serverType;//����Ļ������ͣ�Ϊ��֧��Redis�����빦�ܣ����������ӵ�������չ����
        //    hashHostDic = new Dictionary<uint, HostInstance>();
        //    List<HostInstance> pools = new List<HostInstance>();
        //    List<uint> keys = new List<uint>();
        //    foreach (string hostItem in hosts)//����ÿһ̨������
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
            watch = new HostConfigWatch(cacheType, configValue);
            watch.OnConfigChangedEvent += new HostConfigWatch.OnConfigChangedDelegate(watch_OnConfigChangedEvent);
            ResetHostServer();
        }

        /// <summary>
        /// ���������ļ��޸�ʱ
        /// </summary>
        void watch_OnConfigChangedEvent()
        {
            ResetHostServer();
        }
        void ResetHostServer()
        {
            lock (o)
            {
                if (CreateHost())
                {
                    CreateHashHost();
                    CreateHashKeys();
                }
            }
        }

        #region ��������
        MDictionary<string, HostNode> hostList = new MDictionary<string, HostNode>();
        /// <summary>
        /// �����б�
        /// </summary>
        public MDictionary<string, HostNode> HostList
        {
            get
            {
                return hostList;
            }
        }
        private bool CreateHost()
        {
            if (hostList.Count == 0)
            {
                AddHost(watch.HostList);
            }
            else //����仯�����
            {
                //�Ƴ�������
                foreach (string host in watch.HostRemoveList.List)
                {
                    if (hostList.ContainsKey(host))
                    {
                        hostList[host].Dispose();//�ͷ���Դ��
                        hostList.Remove(host);//�Ƴ�����
                    }
                }
                //�������
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
        internal T Execute<T>(uint hash, T defaultValue, UseSocket<T> use)
        {
            HostNode node = GetHost(hash);
            if (node.HostNodeBak == null && hostServerBak != null)
            {
                //Ϊ��Socket�عҽӱ��ݵ�Socket��
                node.HostNodeBak = hostServerBak.GetHost(hash, node.Host);
            }
            return Execute(node, defaultValue, use);
        }

        internal T Execute<T>(HostNode host, T defaultValue, UseSocket<T> use)
        {
            MSocket sock = null;
            try
            {
                //Acquire a socket
                sock = host.Acquire();

                //Use the socket as a parameter to the delegate and return its result.
                if (sock != null)
                {
                    return use(sock);
                }
            }
            catch (Exception e)
            {
                logger.Error("Error in Execute<T>: " + host.Host, e);

                //Socket is probably broken
                if (sock != null)
                {
                    Interlocked.Increment(ref host.CloseSockets);
                    sock.Close();
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

        internal void Execute(HostNode host, UseSocket use)
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
                logger.Error("Error in Execute: " + host.Host, e);

                //Socket is probably broken
                if (sock != null)
                {
                    Interlocked.Increment(ref host.CloseSockets);
                    sock.Close();
                }
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
        internal void ExecuteAll(UseSocket use)
        {
            foreach (KeyValuePair<string, HostNode> item in hostList)
            {
                Execute(item.Value, use);
            }
        }

        #endregion
    }

    /// <summary>
    /// ����һ����hash
    /// </summary>
    internal partial class HostServer
    {
        /// <summary>
        /// һ����Hash�����Ľ��������Խ�󣬷ֲ�Խƽ��
        /// </summary>
        private int HashNodeCount
        {
            get
            {
                //����ڵ�>1��һ����hash��������
                if (watch.HostList.Count > 1)
                {
                    return 50;
                }
                return 1;
            }
        }
        /// <summary>
        /// һ����hash�������ֲ��б�
        /// </summary>
        private MDictionary<uint, HostNode> hashHostDic = new MDictionary<uint, HostNode>();
        /// <summary>
        /// �������ڲ�ѯ�������򣩵�hashkey��
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
                //�Ƴ�hashHost
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
                //���hashHost
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
        private static object o = new object();
        /// <summary>
        /// Given an item key hash, this method returns the socketpool which is closest on the server key continuum.
        /// ��ȡһ̨���ڷ��������ʵ����
        /// </summary>
        internal HostNode GetHost(uint hash)
        {
            return GetHost(hash, null);
        }
        internal HostNode GetHost(uint hash, string ignoreHost)
        {
            lock (o)
            {
                //Quick return if we only have one host.
                if (hostList.Count == 1)
                {
                    return hostList[0];
                }

                //New "ketama" host selection.
                int i = Array.BinarySearch(hashKeys, hash);

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
                HostNode node = hashHostDic[hashKeys[i]];
                if (!string.IsNullOrEmpty(ignoreHost) && node.Host == ignoreHost)
                {
                    for (int j = 0; j < hostList.Count; j++)
                    {
                        if (hostList[j].Host == ignoreHost)
                        {
                            //ȡ���һ������ǰһ����
                            return j < hostList.Count - 1 ? hostList[j + 1] : hostList[j - 1];
                        }
                    }
                }
                return node;
            }
        }
    }
}