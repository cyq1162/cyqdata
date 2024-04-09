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
            var mutex = GetMutex(fileName);

            bool result = StreamWriterSync2(fileName, text, isAppend, writeLogOnError, encode).GetAwaiter().GetResult();

            mutex.ReleaseMutex();

            return result;
        }

        private static async Task<bool> StreamWriterSync2(string fileName, string text, bool isAppend, bool writeLogOnError, Encoding encode)
        {
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

            return true;
        }

    }
}
