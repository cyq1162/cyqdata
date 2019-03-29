
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// The SocketPool encapsulates the list of PooledSockets against one specific host, and contains methods for 
    /// acquiring or returning PooledSockets.
    /// </summary>
    internal class HostNode : IDisposable
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
        public HostNode HostNodeBak;
        /// <summary>
        /// Socket的挂科时间。
        /// </summary>
        private DateTime socketDeadTime = DateTime.MinValue;

        private int maxQueue = 64;
        private int minQueue = 2;

        /// <summary>
        /// If the host stops responding, we mark it as dead for this amount of seconds, 
        /// and we double this for each consecutive failed retry. If the host comes alive
        /// again, we reset this to 1 again.
        /// </summary>
        private int deadEndPointSecondsUntilRetry = 1;
        private const int maxDeadEndPointSecondsUntilRetry = 60 * 10; //10 minutes
        private HostServer hostServer;
        private IPEndPoint endPoint;
        private Queue<MSocket> socketQueue = new Queue<MSocket>();

        internal HostNode(HostServer hostServer, string host)
        {
            this.hostServer = hostServer;
            string[] items = host.Split('-');
            Host = items[0].Trim();
            if (items.Length > 1)
            {
                Password = items[1].Trim();
            }
            endPoint = GetEndPoint(Host);
        }

        /// <summary>
        /// This method parses the given string into an IPEndPoint.
        /// If the string is malformed in some way, or if the host cannot be resolved, this method will throw an exception.
        /// </summary>
        private static IPEndPoint GetEndPoint(string host)
        {
            //Parse port, default to 11211.
            int port = 11211;
            if (host.Contains(":"))
            {
                string[] split = host.Split(new char[] { ':' });
                if (!Int32.TryParse(split[1], out port))
                {
                    throw new ArgumentException("Unable to parse host: " + host);
                }
                host = split[0];
            }

            //Parse host string.
            IPAddress address;
            if (!IPAddress.TryParse(host, out address))
            {
                //See if we can resolve it as a hostname
                try
                {
                    address = Dns.GetHostEntry(host).AddressList[0];
                }
                catch (Exception e)
                {
                    throw new ArgumentException("Unable to resolve host: " + host, e);
                }
            }

            return new IPEndPoint(address, port);
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
            //检测当前是否挂科，如果是(15分钟内)，由备份服务器提供服务
            if (socketDeadTime.AddMinutes(15) >= DateTime.Now && HostNodeBak != null)
            {
                return HostNodeBak.Acquire();
            }

            //Do we have free sockets in the pool?
            //if so - return the first working one.
            //if not - create a new one.
            Interlocked.Increment(ref Acquired);
            lock (socketQueue)
            {
                while (socketQueue.Count > 0)
                {
                    MSocket socket = socketQueue.Dequeue();
                    if (socket != null && socket.IsAlive)
                    {
                        Interlocked.Increment(ref ReusedSockets);
                        return socket;
                    }
                    Interlocked.Increment(ref DeadSocketsInPool);
                }
            }


            //If we know the endpoint is dead, check if it is time for a retry, otherwise return null.
            if (IsEndPointDead)
            {
                if (DateTime.Now > DeadEndPointRetryTime)
                {
                    //Retry
                    IsEndPointDead = false;
                }
                else
                {
                    //Still dead
                    return null;
                }
            }

            //Try to create a new socket. On failure, mark endpoint as dead and return null.
            try
            {
                MSocket socket = new MSocket(this, endPoint);
                Interlocked.Increment(ref NewSockets);
                //Reset retry timer on success.
                //不抛异常，则正常链接。
                if (OnAfterSocketCreateEvent != null)
                {
                    OnAfterSocketCreateEvent(socket);
                }
                deadEndPointSecondsUntilRetry = 1;
                return socket;
            }
            catch (Exception e)
            {
                Interlocked.Increment(ref FailedNewSockets);
                logger.Error("Error connecting to: " + endPoint.Address, e);
                //Mark endpoint as dead
                IsEndPointDead = true;
                //Retry in 2 minutes
                DeadEndPointRetryTime = DateTime.Now.AddSeconds(deadEndPointSecondsUntilRetry);
                if (deadEndPointSecondsUntilRetry < maxDeadEndPointSecondsUntilRetry)
                {
                    deadEndPointSecondsUntilRetry = deadEndPointSecondsUntilRetry * 2; //Double retry interval until next time
                }

                socketDeadTime = DateTime.Now;
                //返回备份的池
                if (HostNodeBak != null)
                {
                    return HostNodeBak.Acquire();
                }
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
                socket.Close();
            }
            else
            {
                //Clean up socket
                socket.Reset();
                //Check pool size.
                if (socketQueue.Count >= maxQueue)
                {
                    //If the pool is full, destroy the socket.
                    socket.Close();
                }
                else if (socketQueue.Count > minQueue && socket.CreateTime.AddMinutes(30) < DateTime.Now)
                {
                    //socket 服务超过半小时的，也可以休息了，只保留最底个数。
                    //If we have more than the minimum amount of sockets, but less than the max, and the socket is older than the recycle age, we destroy it.
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
}
