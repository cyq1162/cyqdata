
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

        //用于验证权限的委托事件。
        internal delegate bool OnAfterSocketCreateDelegate(MSocket socket);
        internal event OnAfterSocketCreateDelegate OnAfterSocketCreateEvent;

        private static LogAdapter logger = LogAdapter.GetLogger(typeof(HostNode));
        #region 可对外的属性
        //Public variables and properties
        public readonly string Host;
        /// <summary>
        /// 扩展属性：如果链接后需要验证密码（如Redis可设置密码）
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
        /// 最后的错误信息。
        /// </summary>
        public string Error;
        /// <summary>
        /// 当前 Socket 池的可用数量。
        /// </summary>
        public int Poolsize { get { return socketQueue.Count; } }

        /// <summary>
        /// 主机节点是不是挂了。
        /// </summary>
        public bool IsEndPointDead = false;
        public DateTime DeadEndPointRetryTime;
        #endregion

        /// <summary>
        /// 备份的Socket池，如果某主机挂了，在配置了备份的情况下，会由备份Socket池提供服务。
        /// </summary>
        // public HostNode HostNodeBak;
        /// <summary>
        /// Socket的挂科时间。
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
        /// 超出最大链接后的等待时间。
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
        /// 主机服务。
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
        /// 从队列获取数据，并可能进行等待。
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
        /// 这个方法扩展（备份链接池）
        /// </summary>
        internal MSocket Acquire()
        {
            if (IsEndPointDead) { return null; }
            //If we know the endpoint is dead, check if it is time for a retry, otherwise return null.

            //检测当前是否挂科，如果是(15分钟内)，由备份服务器提供服务
            //if (socketDeadTime.AddMinutes(15) >= DateTime.Now && HostNodeBak != null)
            //{
            //    return HostNodeBak.Acquire();
            //}
            //else
            //{
            Interlocked.Increment(ref Acquired);

            //}
            //已经生产超过最大队列值
            MSocket socket = GetFromQueue(0);
            if (socket != null) { return socket; }

            //提前检测，少进锁
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

            //加锁，避免并发瞬时产生太多链接，浪费资源
            lock (this)
            {
                if (NewSockets - CloseSockets < MaxQueue)
                {
                    //If we know the endpoint is dead, check if it is time for a retry, otherwise return null.
                    if (IsEndPointDead) { return null; }
                    socket = new MSocket(this, Host);
                    if (socket.IsAlive)
                    {
                        Interlocked.Increment(ref NewSockets);//先处理计数器
                    }
                    else
                    {
                        Interlocked.Increment(ref FailedNewSockets);//先处理计数器
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
                //不抛异常，则正常链接。
                if (OnAfterSocketCreateEvent != null)
                {
                    OnAfterSocketCreateEvent(socket);
                }
                return socket;
            }
            else
            {
                logger.Error("Error connecting to: " + Host);
                //返回备份的池
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
                    //socket 服务超过半小时的，也可以休息了，只保留最底个数。
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
        /// 线程池里重试链接。
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
                        OnAfterSocketCreateEvent(socket);//身份验证
                    }
                    Return(socket);//不浪费，丢到池里重用。
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
        /// 添加故障节点
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


        #region IDisposable 成员
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
        /// 已故障节点
        /// </summary>
        static MDictionary<string, HostNode> deadNode = new MDictionary<string, HostNode>();

        /// <summary>
        /// 线程检测已故障节点。
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
