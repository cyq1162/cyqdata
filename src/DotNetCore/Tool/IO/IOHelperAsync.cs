using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CYQ.Data.Tool
{
    public static partial class IOHelper
    {
        private static byte[] ReadAllBytesSync(string fileName)
        {
            return ReadAllBytesSync2(fileName).GetAwaiter().GetResult();
        }
        private static async Task<byte[]> ReadAllBytesSync2(string fileName)
        {
            return await File.ReadAllBytesAsync(fileName);
        }

        private static bool StreamWriterSync(string fileName, string text, bool isAppend, bool writeLogOnError, Encoding encode)
        {
            return StreamWriterSync2(fileName, text, isAppend, writeLogOnError, encode).GetAwaiter().GetResult();
        }

        private static async Task<bool> StreamWriterSync2(string fileName, string text, bool isAppend, bool writeLogOnError, Encoding encode)
        {
            var mutex = GetMutex(fileName);

            try
            {
                using (StreamWriter writer = new StreamWriter(fileName, isAppend, encode))
                {
                    await writer.WriteAsync(text);
                }
            }
            catch (Exception err)
            {
                if (writeLogOnError)
                {
                    Log.Write(err, LogType.Error);
                }
                return false;
            }
            finally
            {
                // 释放互斥锁
                mutex.ReleaseMutex();
            }
            return true;
        }

    }
}
