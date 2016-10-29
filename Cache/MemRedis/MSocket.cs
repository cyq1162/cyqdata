//Copyright (c) 2007-2008 Henrik Schröder, Oliver Kofoed Pedersen

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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// The PooledSocket class encapsulates a socket connection to a specified memcached server.
    /// It contains a buffered stream for communication, and methods for sending and retrieving
    /// data from the memcached server, as well as general memcached error checking.
    /// </summary>
    internal class MSocket : IDisposable
    {
        private static LogAdapter logger = LogAdapter.GetLogger(typeof(MSocket));

        private SocketPool socketPool;
        private Socket socket;
        private Stream stream;
        public readonly DateTime Created;

        public MSocket(SocketPool socketPool, IPEndPoint endPoint, int sendReceiveTimeout, int connectTimeout)
        {
            this.socketPool = socketPool;
            Created = DateTime.Now;

            //Set up the socket.
            socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, sendReceiveTimeout);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, sendReceiveTimeout);
            socket.ReceiveTimeout = sendReceiveTimeout;
            socket.SendTimeout = sendReceiveTimeout;

            //socket.SendBufferSize = 1024 * 1024;

            //Do not use Nagle's Algorithm
            socket.NoDelay = true;

            //Establish connection asynchronously to enable connect timeout.
            IAsyncResult result = socket.BeginConnect(endPoint, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(connectTimeout, false);
            if (!success)
            {
                try { socket.Close(); }
                catch { }
                throw new SocketException();
            }
            socket.EndConnect(result);

            //Wraps two layers of streams around the socket for communication.
            stream = new BufferedStream(new NetworkStream(socket, false));
        }

        /// <summary>
        /// Disposing of a PooledSocket object in any way causes it to be returned to its SocketPool.
        /// </summary>
        public void Dispose()
        {
            socketPool.Return(this);
        }

        /// <summary>
        /// This method closes the underlying stream and socket.
        /// </summary>
        public void Close()
        {
            if (stream != null)
            {
                try { stream.Close(); }
                catch (Exception e) { logger.Error("Error closing stream: " + socketPool.Host, e); }
                stream = null;
            }
            if (socket != null)
            {
                try { socket.Shutdown(SocketShutdown.Both); }
                catch (Exception e) { logger.Error("Error shutting down socket: " + socketPool.Host, e); }
                try { socket.Close(); }
                catch (Exception e) { logger.Error("Error closing socket: " + socketPool.Host, e); }
                socket = null;
            }
        }

        /// <summary>
        /// Checks if the underlying socket and stream is connected and available.
        /// </summary>
        public bool IsAlive
        {
            get { return socket != null && socket.Connected && stream.CanRead; }
        }

        /// <summary>
        /// Writes a string to the socket encoded in UTF8 format.
        /// </summary>
        public void Write(string str)
        {
            Write(Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// Writes an array of bytes to the socket and flushes the stream.
        /// </summary>
        public void Write(byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }
        /// <summary>
        /// Reads from the socket until the sequence '\r\n' is encountered, 
        /// and returns everything up to but not including that sequence as a UTF8-encoded string
        /// </summary>
        public string ReadLine()
        {
            return Encoding.UTF8.GetString(ReadBytes());
        }

        public byte[] ReadBytes()
        {
            MemoryStream buffer = new MemoryStream();
            int b;
            bool gotReturn = false;
            while ((b = stream.ReadByte()) != -1)
            {
                if (gotReturn)
                {
                    if (b == 10)
                    {
                        break;
                    }
                    else
                    {
                        buffer.WriteByte(13);
                        gotReturn = false;
                    }
                }
                if (b == 13)
                {
                    gotReturn = true;
                }
                else
                {
                    buffer.WriteByte((byte)b);
                }
            }
            return buffer.ToArray();

        }

        /// <summary>
        /// Reads a response line from the socket, checks for general memcached errors, and returns the line.
        /// If an error is encountered, this method will throw an exception.
        /// </summary>
        public string ReadResponse()
        {
            string response = ReadLine();

            if (String.IsNullOrEmpty(response))
            {
                Error.Throw("Received empty response.");
            }

            if (response.StartsWith("ERROR")
                || response.StartsWith("CLIENT_ERROR")
                || response.StartsWith("SERVER_ERROR"))
            {
                Error.Throw("Server returned " + response);
            }

            return response;
        }

        /// <summary>
        /// Fills the given byte array with data from the socket.
        /// </summary>
        public void Read(byte[] bytes)
        {
            if (bytes == null)
            {
                return;
            }

            int readBytes = 0;
            while (readBytes < bytes.Length)
            {
                readBytes += stream.Read(bytes, readBytes, (bytes.Length - readBytes));
            }
        }

        /// <summary>
        /// Reads from the socket until the sequence '\r\n' is encountered.
        /// </summary>
        public void SkipUntilEndOfLine()
        {
            int b;
            bool gotReturn = false;
            while ((b = stream.ReadByte()) != -1)
            {
                if (gotReturn)
                {
                    if (b == 10)
                    {
                        break;
                    }
                    else
                    {
                        gotReturn = false;
                    }
                }
                if (b == 13)
                {
                    gotReturn = true;
                }
            }
        }

        /// <summary>
        /// Resets this PooledSocket by making sure the incoming buffer of the socket is empty.
        /// If there was any leftover data, this method return true.
        /// </summary>
        public bool Reset()
        {
            if (socket.Available > 0)
            {
                byte[] b = new byte[socket.Available];
                Read(b);
                return true;
            }
            return false;
        }
    }
}