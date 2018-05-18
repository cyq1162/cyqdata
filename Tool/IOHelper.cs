using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CYQ.Data.Tool
{
    internal static class IOHelper
    {
        internal static Encoding DefaultEncoding = Encoding.Default;

        private static List<object> tenObj = new List<object>(10);
        private static List<object> TenObj
        {
            get
            {
                if (tenObj.Count == 0)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        tenObj.Add(new object());
                    }
                }
                return tenObj;
            }
        }
        private static object GetLockObj(int length)
        {
            int i = length % 9;
            return TenObj[i];
        }
        /// <summary>
        /// 先自动识别UTF8，否则归到Default编码读取
        /// </summary>
        /// <returns></returns>
        public static string ReadAllText(string fileName)
        {
            return ReadAllText(fileName, DefaultEncoding);
        }
        public static string ReadAllText(string fileName, Encoding encoding)
        {
            try
            {
                if (!File.Exists(fileName))
                {
                    return string.Empty;
                }
                Byte[] buff = null;
                lock (GetLockObj(fileName.Length))
                {
                    if (!File.Exists(fileName))//多线程情况处理
                    {
                        return string.Empty;
                    }
                    buff = File.ReadAllBytes(fileName);
                }
                if (buff.Length == 0) { return ""; }
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
                return encoding.GetString(buff);
            }
            catch (Exception err)
            {
                Log.WriteLogToTxt(err);
            }
            return string.Empty;
        }
        public static bool Write(string fileName, string text)
        {
            return Save(fileName, text, false, DefaultEncoding, true);
        }
        public static bool Write(string fileName, string text, Encoding encode)
        {
            return Save(fileName, text, false, encode, true);
        }
        public static bool Append(string fileName, string text)
        {
            return Save(fileName, text, true, true);
        }

        internal static bool Save(string fileName, string text, bool isAppend, bool writeLogOnError)
        {
            return Save(fileName, text, true, DefaultEncoding, writeLogOnError);
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

                lock (GetLockObj(fileName.Length))
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
            return false;
        }

        internal static bool Delete(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    lock (GetLockObj(fileName.Length))
                    {
                        if (File.Exists(fileName))
                        {
                            File.Delete(fileName);
                            return true;
                        }
                    }
                }
            }
            catch
            {

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
