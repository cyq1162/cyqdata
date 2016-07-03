using System;
using System.IO;
using System.Text;

namespace CYQ.Data.Tool
{
    internal static class IOHelper
    {
        /// <summary>
        /// 先自动识别UTF8，否则归到Default编码读取
        /// </summary>
        /// <returns></returns>
        public static string ReadAllText(string fileName)
        {
            Byte[] buff = File.ReadAllBytes(fileName);
            if (buff[0] == 239 && buff[1] == 187 && buff[2] == 191)
            {
                return Encoding.UTF8.GetString(buff, 3, buff.Length - 3);
            }
            else if (buff[0] == 255 && buff[1] == 254)
            {
                return Encoding.Unicode.GetString(buff, 2, buff.Length - 2);
            }
            else if (buff[0] == 254 && buff[1] == 255)
            {
                if (buff.Length > 3 && buff[2] == 0 && buff[3] == 0)
                {
                    return Encoding.UTF32.GetString(buff, 4, buff.Length - 4);
                }
                return Encoding.BigEndianUnicode.GetString(buff, 2, buff.Length - 2);
            }
            return Encoding.Default.GetString(buff);
        }
        public static string ReadAllText(string fileName, Encoding encoding)
        {
            try
            {
                using (StreamReader sr = new StreamReader(fileName, encoding, true))
                {
                    return sr.ReadToEnd();
                }
            }
            catch (Exception err)
            {
                Log.WriteLogToTxt(err);
            }
            return string.Empty;
        }
        public static bool Write(string fileName, string text)
        {
            return Save(fileName, text, false, Encoding.Default, true);
        }
        public static bool Write(string fileName, string text, Encoding encode)
        {
            return Save(fileName, text, false, encode, true);
        }
        public static bool Append(string fileName, string text)
        {
            return Save(fileName, text, true, true);
        }

        static System.Threading.Semaphore _Mutex;
        static System.Threading.Semaphore Mutex
        {
            get
            {
                if (_Mutex == null)
                {
                    try
                    {
                        _Mutex = new System.Threading.Semaphore(1, 1, "IOHelper.Save");
                    }
                    catch
                    {

                    }

                }
                return _Mutex;
            }
        }
        internal static bool Save(string fileName, string text, bool isAppend, bool writeLogOnError)
        {
            return Save(fileName, text, true, Encoding.Default, writeLogOnError);
        }
        internal static bool Save(string fileName, string text, bool isAppend, Encoding encode, bool writeLogOnError)
        {
            try
            {
                string folder = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                if (Mutex == null || Mutex.WaitOne(2000, false))//进程间同步。
                {
                    using (StreamWriter writer = new StreamWriter(fileName, isAppend, encode))
                    {
                        writer.Write(text);
                    }
                }
                return true;
            }
            catch (Exception err)
            {
                if (writeLogOnError)
                {
                    Log.WriteLogToTxt(err);
                }
                else
                {
                    Error.Throw("IOHelper.Save() : " + err.Message);
                }
            }
            finally
            {
                try
                {
                    if (Mutex != null)
                    {
                        Mutex.Release();
                    }
                }
                catch
                {

                }
            }
            return false;
        }
        public static bool IsLastFileWriteTimeChanged(string fileName, ref DateTime compareTimeUtc)
        {
            bool isChanged = false;
            IOInfo info = new IOInfo(fileName);
            if (info.Exists && info.LastWriteTimeUtc != compareTimeUtc)
            {
                isChanged = true;
                compareTimeUtc = info.LastWriteTimeUtc;
            }
            return isChanged;
        }

    }
    internal class IOInfo : FileSystemInfo
    {
        public IOInfo(string fileName)
        {
            base.FullPath = fileName;
        }
        public override void Delete()
        {
        }

        public override bool Exists
        {
            get
            {
                return File.Exists(base.FullPath);
            }
        }

        public override string Name
        {
            get
            {
                return null;
            }
        }
    }
}
