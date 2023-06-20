using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CYQ.Data.Cache
{
    /*
     协议规范

redis允许客户端以TCP方式连接，默认6379端口。传输数据都以\r\n结尾。

请求格式

*<number of arguments>\r\n$<number of bytes of argument 1>\r\n<argument data>\r\n

例：*1\r\n$4\r\nINFO\r\n

响应格式

1：简单字符串，非二进制安全字符串，一般是状态回复。  +开头，例：+OK\r\n 

2: 错误信息。　　　　　　　　　　-开头， 例：-ERR unknown command 'mush'\r\n

3: 整型数字。                            :开头， 例：:1\r\n

4：大块回复值，最大512M。           $开头+数据长度。 例：$4\r\mush\r\n

5：多条回复。                           *开头， 例：*2\r\n$3\r\nfoo\r\n$3\r\nbar\r\n 
     */
    /// <summary>
    /// RedisCommand
    /// </summary>
    internal class RedisCommand : IDisposable
    {
        MSocket socket;
        //public RedisCommand(MSocket socket)
        //{

        //}
        List<byte> command = new List<byte>();
        public RedisCommand(MSocket socket)
        {
            this.socket = socket;
            socket.Reset();
        }
        public RedisCommand(MSocket socket, int commandCount, string command)
        {
            this.socket = socket;
            this.socket.Reset();
            //Write(string.Format("*{0}\r\n", commandCount));
            //WriteKey(command);

            //听说redis verstion 2.8 不支持参数分开发送，所以打包一起发送。
            string cmd = string.Format("*{0}\r\n${1}\r\n{2}\r\n", commandCount, Encoding.UTF8.GetBytes(command).Length, command);
            Add(cmd);

        }

        //private void WriteHeader(int bodyLen)
        //{
        //    Send("$" + bodyLen + "\r\n");
        //}
        public void AddKey(string key)
        {
            string cmd = string.Format("${0}\r\n{1}\r\n", Encoding.UTF8.GetBytes(key).Length, key);
            Add(cmd);
            //byte[] data = Encoding.UTF8.GetBytes(cmd);

            //WriteHeader(data.Length);

            //WriteData(data);
            //Write("\r\n");
        }
        //public void WriteValue(string value)
        //{
        //    WriteKey(value);
        //}
        //public void WriteValue(byte[] data1)
        //{
        //    WriteValue(data1, null);
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="dataValue"></param>
        public void AddValue(byte[] dataType, byte[] dataValue)
        {
            int len = dataType.Length;
            if (dataValue != null)
            {
                len += dataValue.Length;
            }

            List<byte> bytes = new List<byte>();

            bytes.AddRange(Encoding.UTF8.GetBytes(string.Format("${0}\r\n", len)));
            bytes.AddRange(dataType);
            if (dataValue != null)
            {
                bytes.AddRange(dataValue);
            }
            bytes.AddRange(Encoding.UTF8.GetBytes("\r\n"));
            Add(bytes.ToArray());

            //WriteHeader(len);
            //Send(data1);
            //if (data2 != null)
            //{
            //    Send(data2);
            //}
            //Send("\r\n");
        }




        public void Reset(int commandCount, string command)
        {
            string cmd = string.Format("*{0}\r\n${1}\r\n{2}\r\n", commandCount, Encoding.UTF8.GetBytes(command).Length, command);
            Add(cmd);
            //Write(string.Format("*{0}\r\n", commandCount));
            //WriteKey(command);
        }

        #region 送到Socket 发送
        private void Add(string cmd)
        {
            Add(Encoding.UTF8.GetBytes(cmd));
        }
        private void Add(byte[] cmd)
        {
            command.AddRange(cmd);
            //socket.Write(cmd);
        }
        #endregion

        public void Send()
        {
            if (command.Count > 0)
            {
                socket.Write(command.ToArray());
                command.Clear();
            }
        }
        public void Dispose()
        {
            Send();
        }


    }
}
