using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using CYQ.Data.Tool;

namespace CYQ.Data.Cache
{
    internal static class SocketCreate
    {
        //static Dictionary<string, Queue<Socket>> SocketQueue = new Dictionary<string, Queue<Socket>>();

        //public static Socket Get(string host)
        //{
        //    if (SocketQueue.ContainsKey(host) && SocketQueue[host].Count > 0)
        //    {
        //        return SocketQueue[host].Dequeue();
        //    }
        //    return New(host);
        //}

        public static Socket New(string host)
        {
            IPEndPoint endPoint = GetEndPoint(host);
            //Set up the socket.
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 5000);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 5000);
            //socket.ReceiveTimeout = sendReceiveTimeout;
            //socket.SendTimeout = sendReceiveTimeout;

            //socket.SendBufferSize = 1024 * 1024;
            //Do not use Nagle's Algorithm
            //socket.NoDelay = true;

            //Establish connection asynchronously to enable connect timeout.
            IAsyncResult result = socket.BeginConnect(endPoint, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(3000, false);
            if (!success)
            {
                try { socket.Close(); }
                catch { }
                throw new SocketException();
            }
            socket.EndConnect(result);
            return socket;
        }
        //private static void OnEndConnect(IAsyncResult iar)
        //{
        //    Socket socket = (Socket)iar.AsyncState;
        //    socket.EndConnect(iar);

        //}

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
        ///// <summary>
        ///// 提前准备Socket
        ///// </summary>
        //public static void ReadyForSocket(object list)
        //{
        //    MList<string> hostList = list as MList<string>;
        //    int count =15;
        //    foreach (string host in hostList.List)
        //    {
        //        if (!SocketQueue.ContainsKey(host))
        //        {
        //            SocketQueue.Add(host, new Queue<Socket>());
        //        }
        //        for (int i = 0; i < count; i++)
        //        {
        //            SocketQueue[host].Enqueue(New(host));
        //        }
        //    }
        //}

        //public static void ClearSocketPool()
        //{
        //    foreach (KeyValuePair<string,Queue<Socket>> item in SocketQueue)
        //    {
        //        while (SocketQueue[item.Key].Count > 0)
        //        {
        //            SocketQueue[item.Key].Dequeue().Close();
        //        }
        //    }
        //}
    }
}
