using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using System.Text;

namespace CYQ.Data.Cache
{
    internal class RedisCommand : IDisposable
    {
        MSocket socket;
        public RedisCommand(MSocket socket, int commandCount, string command)
        {
            this.socket = socket;
            Write(string.Format("*{0}\r\n", commandCount));
            WriteKey(command);
        }
        private void Write(string cmd)
        {
            WriteData(Encoding.UTF8.GetBytes(cmd));
        }
        private void WriteHeader(int bodyLen)
        {
            Write("$" + bodyLen + "\r\n");
        }
        public void WriteKey(string cmd)
        {
            byte[] data = Encoding.UTF8.GetBytes(cmd);

            WriteHeader(data.Length);

            WriteData(data);
            Write("\r\n");
        }
        public void WriteValue(string value)
        {
            WriteKey(value);
        }
        public void WriteValue(byte[] data1)
        {
            WriteValue(data1, null);
        }
        public void WriteValue(byte[] data1, byte[] data2)
        {
            int len = data1.Length;
            if (data2 != null)
            {
                len += data2.Length;
            }
            WriteHeader(len);
            WriteData(data1);
            if (data2 != null)
            {
                WriteData(data2);
            }
            Write("\r\n");
        }
        private void WriteData(byte[] cmd)
        {
            socket.Write(cmd);
        }


        //public void Add(byte[] data, SerializedType st)
        //{
        //    string header = "$" + (data.Length);// + (st != SerializedType.None ? 1 : 0));
        //    byte[] headerdata = Encoding.ASCII.GetBytes(header);
        //    mStream.Write(headerdata, 0, headerdata.Length);
        //    mStream.Write(Eof, 0, 2);
        //    //if (st != SerializedType.None)
        //    //{
        //    //    //追加序列化的类型
        //    //    byte[] b = new byte[1] { (byte)st };
        //    //    mStream.Write(b, 0, 1);
        //    //}
        //    mStream.Write(data, 0, data.Length);
        //    mStream.Write(Eof, 0, 2);
        //    Count++;
        //}

        //public byte[] ToArray()
        //{
        //    try
        //    {
        //        string header = string.Format("*{0}\r\n", Count);
        //        byte[] headerdata = Encoding.ASCII.GetBytes(header);
        //        byte[] result = new byte[mStream.Length + headerdata.Length];
        //        Buffer.BlockCopy(headerdata, 0, result, 0, headerdata.Length);
        //        int length = (int)mStream.Position;
        //        mStream.Position = 0;
        //        mStream.Read(result, headerdata.Length, length);
        //        return result;
        //    }
        //    catch (Exception e)
        //    {
        //        Log.WriteLogToTxt(e);
        //        return null;
        //    }
        //}

        // public static byte[] Eof = Encoding.ASCII.GetBytes("\r\n");

        public void Reset(int commandCount, string command)
        {
            Write(string.Format("*{0}\r\n", commandCount));
            WriteKey(command);
        }

        public void Dispose()
        {
            //mStream.Close();
            //mStream.Dispose();
            //mStream = null;
        }


    }
}
