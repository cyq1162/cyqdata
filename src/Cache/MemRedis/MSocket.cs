using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// �ײ�Cache Socket
    /// </summary>
    internal class MSocket
    {
        private static LogAdapter logger = LogAdapter.GetLogger(typeof(MSocket));

        private HostNode hostNode;
        /// <summary>
        /// ���ص�Socket�ء�
        /// </summary>
        public HostNode HostNode
        {
            get
            {
                return hostNode;
            }
        }
        private Socket socket;
        private Stream stream;
        public readonly DateTime CreateTime;
        public MSocket(HostNode socketPool, string host)
        {
            this.hostNode = socketPool;
            CreateTime = DateTime.Now;
            socket = SocketCreate.New(host);

            //Wraps two layers of streams around the socket for communication.
            stream = new BufferedStream(new NetworkStream(socket, false));
        }

        /// <summary>
        /// �ع� Socket��
        /// </summary>
        public void ReturnPool()
        {
            if (hostNode != null)
            {
                hostNode.Return(this);
            }
        }

        /// <summary>
        /// This method closes the underlying stream and socket.
        /// �ر�Socket���ͷ������Դ��
        /// </summary>
        public void Close()
        {
            if (stream != null)
            {
                try
                {
                    stream.Close();
                }
                catch (Exception e)
                {
                    logger.Error("Error closing stream: " + hostNode.Host, e);
                }
                stream = null;
            }
            if (socket != null)
            {
                //try 
                //{
                //    socket.Shutdown(SocketShutdown.SocketShutdown.Both); 
                //}
                //catch (Exception e)
                //{
                //    logger.Error("Error shutting down socket: " + socketPool.Host, e);
                //}
                try
                {
                    socket.Close();
                }
                catch (Exception e)
                {
                    logger.Error("Error closing socket: " + hostNode.Host, e);
                }
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
            try
            {
                if (stream != null && stream.CanWrite)
                {
                    stream.Flush();//�������ǰ�棬�����кܴ�������ͬʱ����Redis�ط������ݣ���
                    stream.Write(bytes, 0, bytes.Length);
                    //IAsyncResult result = stream.BeginWrite(bytes, 0, bytes.Length, null, null);
                    //stream.EndWrite(result);
                    //if (result.AsyncWaitHandle.WaitOne(3000))
                    //{
                    //    stream.EndWrite(result);
                    //}
                }

            }
            catch (Exception e)
            {
                logger.Error("Error socket Write : bytes length " + bytes.Length, e);
            }
        }
        /// <summary>
        /// Reads from the socket until the sequence '\r\n' is encountered, 
        /// and returns everything up to but not including that sequence as a UTF8-encoded string
        /// ����Null��û�������ˣ�
        /// </summary>
        public string ReadLine()
        {
            byte[] data = ReadLineBytes();
            if (data != null && data.Length > 0)
            {
                return Encoding.UTF8.GetString(data);
            }
            return null;
        }
        /// <summary>
        /// ��һ�е�����
        /// </summary>
        /// <returns></returns>
        public byte[] ReadLineBytes()
        {
            MemoryStream buffer = new MemoryStream();
            int b;
            bool gotReturn = false;
            while ((b = stream.ReadByte()) != -1)
            {
                if (gotReturn)
                {
                    if (b == 10)//\n
                    {
                        break;
                    }
                    else
                    {
                        buffer.WriteByte(13);
                        gotReturn = false;
                    }
                }
                if (b == 13)//\r
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
        /// ��һ�е�����
        /// </summary>
        /// <returns></returns>
        public byte[] ReadBytes(int maxLen)
        {
            MemoryStream buffer = new MemoryStream();
            int b;
            int i = 0;
            while ((b = stream.ReadByte()) != -1)
            {
                buffer.WriteByte((byte)b);
                i++;
                if (i >= maxLen)
                {
                    try
                    {
                        stream.ReadByte();//13
                        stream.ReadByte();//10
                    }
                    catch { }
                    break;
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

            if (response.StartsWith("-ERR")
                || response.StartsWith("ERROR")
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
        public void SkipToEndOfLine()
        {
            int b;
            bool gotReturn = false;
            while ((b = stream.ReadByte()) != -1)
            {
                if (gotReturn)
                {
                    if (b == 10)//\n
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
        public void SkipToEndOfLine(int cmdCount)
        {
            for (int i = 0; i < cmdCount; i++)
            {
                SkipToEndOfLine();
            }
        }

        /// <summary>
        /// Resets this PooledSocket by making sure the incoming buffer of the socket is empty.
        /// If there was any leftover data, this method return true.
        /// </summary>
        public bool Reset()
        {
            try
            {
                if (socket.Available > 0)
                {
                    byte[] b = new byte[socket.Available];
                    Read(b);

                }
                stream.Flush();//��������������
                return true;
            }
            catch
            {
                return false;
            }

        }
    }
}