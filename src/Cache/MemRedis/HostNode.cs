
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
        public HostNode HostNodeBak;
        /// <summary>
        /// Socket�Ĺҿ�ʱ�䡣
        /// </summary>
        private DateTime socketDeadTime = DateTime.MinValue;

        private int maxQueue = 128;
        private int minQueue = 2;

        /// <summary>
        /// If the host stops responding, we mark it as dead for this amount of seconds, 
        /// and we double this for each consecutive failed retry. If the host comes alive
        /// again, we reset this to 1 again.
        /// </summary>
        private int deadEndPointSecondsUntilRetry = 1;
        private const int maxDeadEndPointSecondsUntilRetry = 60 * 10; //10 minutes
        private HostServer hostServer;
        private Queue<MSocket> socketQueue = new Queue<MSocket>(16);

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
        /// Gets a socket from the pool.
        /// If there are no free sockets, a new one will be created. If something goes
        /// wrong while creating the new socket, this pool's endpoint will be marked as dead
        /// and all subsequent calls to this method will return null until the retry interval
        /// has passed.
        /// ���������չ���������ӳأ�
        /// </summary>
        internal MSocket Acquire()
        {
            //��⵱ǰ�Ƿ�ҿƣ������(15������)���ɱ��ݷ������ṩ����
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

                MSocket socket = new MSocket(this, Host);
                Interlocked.Increment(ref NewSockets);
                //Reset retry timer on success.
                //�����쳣�����������ӡ�
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
                logger.Error("Error connecting to: " + Host, e);
                //Mark endpoint as dead
                IsEndPointDead = true;
                //Retry in 2 minutes
                DeadEndPointRetryTime = DateTime.Now.AddSeconds(deadEndPointSecondsUntilRetry);
                if (deadEndPointSecondsUntilRetry < maxDeadEndPointSecondsUntilRetry)
                {
                    deadEndPointSecondsUntilRetry = deadEndPointSecondsUntilRetry * 2; //Double retry interval until next time
                }

                socketDeadTime = DateTime.Now;
                //���ر��ݵĳ�
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
                    //socket ���񳬹���Сʱ�ģ�Ҳ������Ϣ�ˣ�ֻ������׸�����
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
}
