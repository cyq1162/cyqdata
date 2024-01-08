
using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// The SocketPool encapsulates the list of PooledSockets against one specific host, and contains methods for 
    /// acquiring or returning PooledSockets.
    /// </summary>
    internal partial class HostNode : IDisposable
    {

        //������֤Ȩ�޵�ί���¼���
        internal delegate bool OnAfterSocketCreateDelegate(MSocket socket);
        internal event OnAfterSocketCreateDelegate OnAfterSocketCreateEvent;

        private static LogAdapter logger = LogAdapter.GetLogger(typeof(HostNode));
        #region �ɶ��������
        //Public variables and properties
        public readonly string Host;
        /// <summary>
        /// ��չ���ԣ�������Ӻ���Ҫ��֤���루��Redis���������룩
        /// </summary>
        public string Password;
        //Debug variables and properties
        public int NewSockets = 0;
        public int CloseSockets = 0;
        public int TimeoutFromSocketPool = 0;
        public int FailedNewSockets = 0;
        public int ReusedSockets = 0;
        public int DeadSocketsInPool = 0;
        public int DeadSocketsOnReturn = 0;
        public int Acquired = 0;
        /// <summary>
        /// ���Ĵ�����Ϣ��
        /// </summary>
        public string Error;
        /// <summary>
        /// ��ǰ Socket �صĿ���������
        /// </summary>
        public int Poolsize { get { return socketQueue.Count; } }

        /// <summary>
        /// �����ڵ��ǲ��ǹ��ˡ�
        /// </summary>
        public bool IsEndPointDead = false;
        public DateTime DeadEndPointRetryTime;
        #endregion

        /// <summary>
        /// ���ݵ�Socket�أ����ĳ�������ˣ��������˱��ݵ�����£����ɱ���Socket���ṩ����
        /// </summary>
        // public HostNode HostNodeBak;
        /// <summary>
        /// Socket�Ĺҿ�ʱ�䡣
        /// </summary>
        private DateTime socketDeadTime = DateTime.MinValue;

        public int MaxQueue
        {
            get
            {
                return hostServer.ServerType == CacheType.Redis ? AppConfig.Redis.MaxSocket : AppConfig.MemCache.MaxSocket;
            }
        }
        /// <summary>
        /// ����������Ӻ�ĵȴ�ʱ�䡣
        /// </summary>
        public int MaxWait
        {
            get
            {
                return hostServer.ServerType == CacheType.Redis ? AppConfig.Redis.MaxWait : AppConfig.MemCache.MaxWait;
            }
        }
        public int minQueue = 1;

        /// <summary>
        /// If the host stops responding, we mark it as dead for this amount of seconds, 
        /// and we double this for each consecutive failed retry. If the host comes alive
        /// again, we reset this to 1 again.
        /// </summary>
        private int deadEndPointSecondsUntilRetry = 1;
        private const int maxDeadEndPointSecondsUntilRetry = 60;
        private HostServer hostServer;
        /// <summary>
        /// ��������
        /// </summary>
        public HostServer HostServer
        {
            get
            {
                return hostServer;
            }
        }
        private Queue<MSocket> socketQueue = new Queue<MSocket>(32);

        internal HostNode(HostServer hostServer, string host)
        {
            this.hostServer = hostServer;
            string[] items = host.Split('-');
            Host = items[0].Trim();
            if (items.Length > 1)
            {
                Password = items[1].Trim();
            }
        }

        /// <summary>
        /// �Ӷ��л�ȡ���ݣ������ܽ��еȴ���
        /// </summary>
        /// <param name="wait"></param>
        /// <returns></returns>
        private MSocket GetFromQueue(int wait)
        {
            //Do we have free sockets in the pool?
            //if so - return the first working one.
            //if not - create a new one.
            int count = 0;
            while (true)
            {
                count++;
                if (socketQueue.Count > 0)
                {
                    MSocket mSocket = null;
                    lock (socketQueue)
                    {
                        if (socketQueue.Count > 0)
                        {
                            mSocket = socketQueue.Dequeue();
                        }
                    }
                    if (mSocket != null)
                    {
                        if (mSocket.IsAlive)
                        {
                            Interlocked.Increment(ref ReusedSockets);
                            return mSocket;
                        }
                        else
                        {
                            Interlocked.Increment(ref DeadSocketsInPool);
                            Interlocked.Increment(ref CloseSockets);
                            return null;
                        }
                    }
                }
                if (count > wait)
                {
                    return null;
                }
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Gets a socket from the pool.
        /// If there are no free sockets, a new one will be created. If something goes
        /// wrong while creating the new socket, this pool's endpoint will be marked as dead
        /// and all subsequent calls to this method will return null until the retry interval
        /// has passed.
        /// ���������չ���������ӳأ�
        /// </summary>
        internal MSocket Acquire()
        {
            if (IsEndPointDead) { return null; }
            //If we know the endpoint is dead, check if it is time for a retry, otherwise return null.

            //��⵱ǰ�Ƿ�ҿƣ������(15������)���ɱ��ݷ������ṩ����
            //if (socketDeadTime.AddMinutes(15) >= DateTime.Now && HostNodeBak != null)
            //{
            //    return HostNodeBak.Acquire();
            //}
            //else
            //{
            Interlocked.Increment(ref Acquired);

            //}
            //�Ѿ���������������ֵ
            MSocket socket = GetFromQueue(0);
            if (socket != null) { return socket; }

            //��ǰ��⣬�ٽ���
            if (NewSockets - CloseSockets >= MaxQueue)
            {
                //Try to create a new socket. On failure, mark endpoint as dead and return null.
                socket = GetFromQueue(MaxWait);
                if (socket != null) { return socket; }
                string timeout = "The timeout period elapsed prior to obtaining a connection from the socket pool.";
                logger.Error(timeout);
                Interlocked.Increment(ref TimeoutFromSocketPool);
                return null;
            }

            //���������Ⲣ��˲ʱ����̫�����ӣ��˷���Դ
            lock (this)
            {
                if (NewSockets - CloseSockets < MaxQueue)
                {
                    //If we know the endpoint is dead, check if it is time for a retry, otherwise return null.
                    if (IsEndPointDead) { return null; }
                    socket = new MSocket(this, Host);
                    if (socket.IsAlive)
                    {
                        Interlocked.Increment(ref NewSockets);//�ȴ��������
                    }
                    else
                    {
                        Interlocked.Increment(ref FailedNewSockets);//�ȴ��������
                        AddToDeadPool();
                    }
                }
            }
            if (socket == null)
            {
                if (NewSockets - CloseSockets >= MaxQueue)
                {
                    //Try to create a new socket. On failure, mark endpoint as dead and return null.
                    socket = GetFromQueue(MaxWait);
                    if (socket != null) { return socket; }
                    string timeout = "The timeout period elapsed prior to obtaining a connection from the socket pool.";
                    logger.Error(timeout);
                    Interlocked.Increment(ref TimeoutFromSocketPool);
                }
                return null;
            }
            if (socket.IsAlive)
            {
                //�����쳣�����������ӡ�
                if (OnAfterSocketCreateEvent != null)
                {
                    OnAfterSocketCreateEvent(socket);
                }
                return socket;
            }
            else
            {
                logger.Error("Error connecting to: " + Host);
                //���ر��ݵĳ�
                //if (HostNodeBak != null)
                //{
                //    return HostNodeBak.Acquire();
                //}
                return null;
            }
        }

        /// <summary>
        /// Returns a socket to the pool.
        /// If the socket is dead, it will be destroyed.
        /// If there are more than MaxPoolSize sockets in the pool, it will be destroyed.
        /// If there are less than MinPoolSize sockets in the pool, it will always be put back.
        /// If there are something inbetween those values, the age of the socket is checked. 
        /// If it is older than the SocketRecycleAge, it is destroyed, otherwise it will be 
        /// put back in the pool.
        /// </summary>
        internal void Return(MSocket socket)
        {
            //If the socket is dead, destroy it.
            if (!socket.IsAlive || hasDisponse)
            {
                Interlocked.Increment(ref DeadSocketsOnReturn);
                Interlocked.Increment(ref CloseSockets);
                socket.Close();
            }
            else
            {
                //Clean up socket
                socket.Reset();
                //Check pool size.
                if (socketQueue.Count >= MaxQueue)
                {
                    //If the pool is full, destroy the socket.
                    Interlocked.Increment(ref CloseSockets);
                    socket.Close();
                }
                else if (socketQueue.Count > minQueue && socket.CreateTime.AddMinutes(10) < DateTime.Now)
                {
                    //socket ���񳬹���Сʱ�ģ�Ҳ������Ϣ�ˣ�ֻ������׸�����
                    //If we have more than the minimum amount of sockets, but less than the max, and the socket is older than the recycle age, we destroy it.
                    Interlocked.Increment(ref CloseSockets);
                    socket.Close();
                }
                else
                {
                    //Put the socket back in the pool.
                    lock (socketQueue)
                    {
                        socketQueue.Enqueue(socket);
                    }
                }
            }
        }

        /// <summary>
        /// �̳߳����������ӡ�
        /// </summary>
        /// <returns></returns>
        internal bool TryConnection()
        {
            if (DateTime.Now > DeadEndPointRetryTime)
            {
                MSocket socket = new MSocket(this, Host);
                if (socket.IsAlive)
                {
                    IsEndPointDead = false;
                    deadEndPointSecondsUntilRetry = 1; //Reset retry timer on success.
                    if (OnAfterSocketCreateEvent != null)
                    {
                        OnAfterSocketCreateEvent(socket);//�����֤
                    }
                    Return(socket);//���˷ѣ������������á�
                    return true;
                }
                else
                {
                    //Retry in 1 minutes
                    DeadEndPointRetryTime = DateTime.Now.AddSeconds(deadEndPointSecondsUntilRetry);
                    if (deadEndPointSecondsUntilRetry < maxDeadEndPointSecondsUntilRetry)
                    {
                        deadEndPointSecondsUntilRetry += 1; //Double retry interval until next time
                    }
                }
            }
            return false;
        }


        static bool isTaskDoing = false;
        /// <summary>
        /// ��ӹ��Ͻڵ�
        /// </summary>
        /// <param name="hostNode"></param>
        public void AddToDeadPool()
        {
            if (!IsEndPointDead)
            {
                IsEndPointDead = true;
                socketDeadTime = DateTime.Now;
                if (!deadNode.ContainsKey(Host))
                {
                    deadNode.Add(Host, this);
                }
                if (!isTaskDoing)
                {
                    lock (deadNode)
                    {
                        isTaskDoing = true;
                        ThreadBreak.AddGlobalThread(new ParameterizedThreadStart(DoHostNodeTask));
                    }
                }
            }
        }


        #region IDisposable ��Ա
        bool hasDisponse = false;
        public void Dispose()
        {
            hasDisponse = true;
            while (socketQueue.Count > 0)
            {
                socketQueue.Dequeue().Close();
            }
        }

        #endregion
    }

    internal partial class HostNode
    {
        /// <summary>
        /// �ѹ��Ͻڵ�
        /// </summary>
        static MDictionary<string, HostNode> deadNode = new MDictionary<string, HostNode>();

        /// <summary>
        /// �̼߳���ѹ��Ͻڵ㡣
        /// </summary>
        /// <param name="threadID"></param>
        static void DoHostNodeTask(object threadID)
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (deadNode.Count > 0)
                {
                    List<string> keys = deadNode.GetKeys();
                    foreach (string key in keys)
                    {
                        if (deadNode[key].TryConnection())
                        {
                            deadNode.Remove(key);
                        }
                    }
                }
            }
        }


    }
}
