//Copyright (c) 2007-2008 Henrik Schr鰀er, Oliver Kofoed Pedersen

//Permission is hereby granted, free of charge, to any person
//obtaining a copy of this software and associated documentation
//files (the "Software"), to deal in the Software without
//restriction, including without limitation the rights to use,
//copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the
//Software is furnished to do so, subject to the following
//conditions:

//The above copyright notice and this permission notice shall be
//included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
//OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
//WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
//FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
//OTHER DEALINGS IN THE SOFTWARE.

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
    [DebuggerDisplay("[ Host: {Host} ]")]
    internal class SocketPool
    {
        private static LogAdapter logger = LogAdapter.GetLogger(typeof(SocketPool));

        /// <summary>
        /// 备份的Socket池，如果某主机挂了，在配置了备份的情况下，会由备份Socket池提供服务。
        /// （问题：主SocketPool怎么找到对应的备份？）
        /// </summary>
        internal SocketPool socketPoolBak;
        /// <summary>
        /// Socket的挂科时间。
        /// </summary>
        private DateTime socketDeadTime = DateTime.MinValue;

        /// <summary>
        /// If the host stops responding, we mark it as dead for this amount of seconds, 
        /// and we double this for each consecutive failed retry. If the host comes alive
        /// again, we reset this to 1 again.
        /// </summary>
        private int deadEndPointSecondsUntilRetry = 1;
        private const int maxDeadEndPointSecondsUntilRetry = 60 * 10; //10 minutes
        private ServerPool owner;
        private IPEndPoint endPoint;
        private Queue<MSocket> queue;

        //Debug variables and properties
        private int newsockets = 0;
        private int failednewsockets = 0;
        private int reusedsockets = 0;
        private int deadsocketsinpool = 0;
        private int deadsocketsonreturn = 0;
        private int dirtysocketsonreturn = 0;
        private int acquired = 0;
        public int NewSockets { get { return newsockets; } }
        public int FailedNewSockets { get { return failednewsockets; } }
        public int ReusedSockets { get { return reusedsockets; } }
        public int DeadSocketsInPool { get { return deadsocketsinpool; } }
        public int DeadSocketsOnReturn { get { return deadsocketsonreturn; } }
        public int DirtySocketsOnReturn { get { return dirtysocketsonreturn; } }
        public int Acquired { get { return acquired; } }
        public int Poolsize { get { return queue.Count; } }

        //Public variables and properties
        public readonly string Host;

        private bool isEndPointDead = false;
        public bool IsEndPointDead { get { return isEndPointDead; } }

        private DateTime deadEndPointRetryTime;
        public DateTime DeadEndPointRetryTime { get { return deadEndPointRetryTime; } }

        internal SocketPool(ServerPool owner, string host)
        {
            Host = host;
            this.owner = owner;
            endPoint = getEndPoint(host);
            queue = new Queue<MSocket>();
        }

        /// <summary>
        /// This method parses the given string into an IPEndPoint.
        /// If the string is malformed in some way, or if the host cannot be resolved, this method will throw an exception.
        /// </summary>
        private static IPEndPoint getEndPoint(string host)
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
            if (IPAddress.TryParse(host, out address))
            {
                //host string successfully resolved as an IP address.
            }
            else
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
            if (socketDeadTime.AddMinutes(15) >= DateTime.Now && socketPoolBak != null)
            {
                return socketPoolBak.Acquire();
            }

            //Do we have free sockets in the pool?
            //if so - return the first working one.
            //if not - create a new one.
            Interlocked.Increment(ref acquired);
            lock (queue)
            {
                while (queue.Count > 0)
                {
                    MSocket socket = queue.Dequeue();
                    if (socket != null && socket.IsAlive)
                    {
                        Interlocked.Increment(ref reusedsockets);
                        return socket;
                    }
                    Interlocked.Increment(ref deadsocketsinpool);
                }
            }

            Interlocked.Increment(ref newsockets);
            //If we know the endpoint is dead, check if it is time for a retry, otherwise return null.
            if (isEndPointDead)
            {
                if (DateTime.Now > deadEndPointRetryTime)
                {
                    //Retry
                    isEndPointDead = false;
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
                MSocket socket = new MSocket(this, endPoint, owner.SendReceiveTimeout, owner.ConnectTimeout);
                //Reset retry timer on success.
                deadEndPointSecondsUntilRetry = 1;
                return socket;
            }
            catch (Exception e)
            {
                Interlocked.Increment(ref failednewsockets);
                logger.Error("Error connecting to: " + endPoint.Address, e);
                //Mark endpoint as dead
                isEndPointDead = true;
                //Retry in 2 minutes
                deadEndPointRetryTime = DateTime.Now.AddSeconds(deadEndPointSecondsUntilRetry);
                if (deadEndPointSecondsUntilRetry < maxDeadEndPointSecondsUntilRetry)
                {
                    deadEndPointSecondsUntilRetry = deadEndPointSecondsUntilRetry * 2; //Double retry interval until next time
                }

                socketDeadTime = DateTime.Now;
                //返回备份的池
                if (socketPoolBak != null)
                {
                    return socketPoolBak.Acquire();
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
            if (!socket.IsAlive)
            {
                Interlocked.Increment(ref deadsocketsonreturn);
                socket.Close();
            }
            else
            {
                //Clean up socket
                if (socket.Reset())
                {
                    Interlocked.Increment(ref dirtysocketsonreturn);
                }

                //Check pool size.
                if (queue.Count >= owner.MaxPoolSize)
                {
                    //If the pool is full, destroy the socket.
                    socket.Close();
                }
                else if (queue.Count > owner.MinPoolSize && DateTime.Now - socket.Created > owner.SocketRecycleAge)
                {
                    //If we have more than the minimum amount of sockets, but less than the max, and the socket is older than the recycle age, we destroy it.
                    socket.Close();
                }
                else
                {
                    //Put the socket back in the pool.
                    lock (queue)
                    {
                        queue.Enqueue(socket);
                    }
                }
            }
        }
    }
}
