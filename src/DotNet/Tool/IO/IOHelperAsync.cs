using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CYQ.Data.Tool
{
    public static partial class IOHelper
    {
        private static byte[] ReadAllBytesSync(string fileName)
        {
            return File.ReadAllBytes(fileName);
        }

        private static bool StreamWriterSync(string fileName, string text, bool isAppend, bool writeLogOnError, Encoding encode)
        {
            var mutex = GetMutex(fileName);

            try
            {
                using (StreamWriter writer = new StreamWriter(fileName, isAppend, encode))
                {
                    //if (!isAppend && fileName.EndsWith(".txt"))
                    //{
                    //    //写入bom头

                    //}
                    writer.Write(text);

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
