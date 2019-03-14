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
        public RedisCommand(MSocket socket, int commandCount, string command)
        {
            this.socket = socket;
            //Write(string.Format("*{0}\r\n", commandCount));
            //WriteKey(command);

            //听说redis verstion 2.8 不支持参数分开发送，所以打包一起发送。
            StringBuilder sb=new StringBuilder();
            sb.AppendFormat("*{0}\r\n",commandCount);
            sb.AppendFormat("${0}\r\n", Encoding.UTF8.GetBytes(command).Length);
            sb.Append(command);
            sb.Append("\r\n");
            Write(sb.ToString());

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



        public void Reset(int commandCount, string command)
        {
            Write(string.Format("*{0}\r\n", commandCount));
            WriteKey(command);
        }

        public void Dispose()
        {

        }


    }
}
