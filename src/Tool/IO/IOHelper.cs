using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CYQ.Data.Cache;
using System.Threading;
namespace CYQ.Data.Tool
{
    /// <summary>
    /// 文件读取类（能自动识别文件编码）
    /// </summary>
    public static partial class IOHelper
    {
        private static DistributedCache _cache = null;
        private static DistributedCache cache
        {
            get
            {
                if (_cache == null)
                {
                    _cache = DistributedCache.Local;
                }
                return _cache;
            }
        }
        internal static Encoding DefaultEncoding = Encoding.Default;

        #region ReadAllText

        /// <summary>
        /// 读取文件内容，并自动识别编码
        /// </summary>
        /// <param name="fileName">完整路径</param>
        /// <returns></returns>
        public static string ReadAllText(string fileName)
        {
            return ReadAllText(fileName, 0, DefaultEncoding);
        }
        /// <param name="cacheMinutes">缓存分钟数（为0则不缓存）</param>
        /// <returns></returns>
        public static string ReadAllText(string fileName, int cacheMinutes)
        {
            return ReadAllText(fileName, cacheMinutes, DefaultEncoding);
        }
        /// <summary>
        /// 读取文件内容
        /// </summary>
        /// <param name="encoding">指定编码时（会跳过编码自动检测）</param>
        /// <returns></returns>
        public static string ReadAllText(string fileName, int cacheMinutes, Encoding encoding)
        {
            return ReadAllText(fileName, cacheMinutes, encoding, 3);
        }
        private static string ReadAllText(string fileName, int cacheMinutes, Encoding encoding, int tryCount)
        {
            string key = "IO_" + fileName.GetHashCode();
            if (cache.Contains(key)) { return cache.Get<string>(key); }
            Byte[] buff = ReadAllBytes(fileName);
            string result = BytesToText(buff, encoding);
            if (cacheMinutes > 0)
            {
                cache.Set(key, result, cacheMinutes);
            }
            return result;
        }
        #endregion

        #region ReadLines
        /// <summary>
        /// 读取文件内容，并自动识别编码
        /// </summary>
        public static string[] ReadAllLines(string fileName)
        {
            return ReadAllLines(fileName, 0, DefaultEncoding);
        }
        public static string[] ReadAllLines(string fileName, int cacheMinutes)
        {
            return ReadAllLines(fileName, cacheMinutes, DefaultEncoding);
        }
        public static string[] ReadAllLines(string fileName, int cacheMinutes, Encoding encoding)
        {
            string result = ReadAllText(fileName, cacheMinutes, encoding);
            if (!string.IsNullOrEmpty(result))
            {
                return result.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }
            return null;

        }
        #endregion

        #region ReadAllBytes
        /// <summary>
        /// 读取IO数据【带Mutex锁】
        /// </summary>
        /// <returns></returns>
        public static byte[] ReadAllBytes(string fileName)
        {
            byte[] buff = null;
            var mutex = GetMutex(fileName);
            try
            {
                if (File.Exists(fileName))
                {
                    buff = File.ReadAllBytes(fileName);
                }
            }
            catch (Exception e)
            {
                Log.WriteLogToTxt(e);
            }
            finally
            {
                // 释放互斥锁
                mutex.ReleaseMutex();
            }

            return buff;
        }
        #endregion

        /// <summary>
        /// 往文件里写入数据(文件存在则复盖，不存在则创建)
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool Write(string fileName, string text)
        {
            return Save(fileName, text, false, true, DefaultEncoding);
        }
        public static bool Write(string fileName, string text, Encoding encode)
        {
            return Save(fileName, text, false, true, encode);
        }
        /// <summary>
        /// 往文件里追加数据(文件存在则追加，不存在则更新)，并自动识别文件编码
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool Append(string fileName, string text)
        {
            return Save(fileName, text, true, true, DefaultEncoding);
        }
        public static bool Append(string fileName, string text, Encoding encode)
        {
            return Save(fileName, text, true, true, encode);
        }

        private static Encoding GetEncoding(string fileName, Encoding defaultEncoding)
        {
            string key = "IOEncoding" + fileName.GetHashCode();
            if (cache.Contains(key))
            {
                return cache.Get<Encoding>(key);
            }
            string folder = Path.GetDirectoryName(fileName);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            else if (File.Exists(fileName))
            {
                TextEncodingDetect detect = new TextEncodingDetect();
                byte[] bytes = ReadAllBytes(fileName);
                Encoding detectEncode = detect.GetEncoding(bytes, defaultEncoding);
                if (detectEncode != Encoding.ASCII)
                {
                    defaultEncoding = detectEncode;
                }
                cache.Set(key, defaultEncoding, 5);
            }
            return defaultEncoding;
        }

        internal static bool Save(string fileName, string text, bool isAppend, bool writeLogOnError, Encoding encode)
        {
            if (isAppend)
            {
                encode = GetEncoding(fileName, encode);
            }
            try
            {
                if (!isAppend)
                {
                    string folderPath = Path.GetDirectoryName(fileName);
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }
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
        /// <summary>
        /// 检测文件是否存在
        /// </summary>
        public static bool Exists(string fileName)
        {
            return File.Exists(fileName);
        }
        /// <summary>
        /// 删除文件
        /// </summary>
        public static bool Delete(string fileName)
        {
            var mutex = GetMutex(fileName);

            try
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                    return true;
                }
            }
            finally
            {
                // 释放互斥锁
                mutex.ReleaseMutex();
            }

            return false;
        }

        private static Mutex GetMutex(string fileName)
        {
            string key = "IO" + fileName.GetHashCode();
            var mutex = new Mutex(false, key);
            try
            {
                mutex.WaitOne();
            }
            catch (AbandonedMutexException ex)
            {
                //其它进程直接关闭，未释放即退出时【锁未对外开放，因此不存在重入锁问题，释放1次即可】。
                mutex.ReleaseMutex();
                mutex.WaitOne();
            }
            return mutex;
        }

        internal static bool IsLastFileWriteTimeChanged(string fileName, ref DateTime compareTime)
        {
            bool isChanged = false;
            IOInfo info = new IOInfo(fileName);
            if (info.Exists && info.LastWriteTime != compareTime)
            {
                isChanged = true;
                compareTime = info.LastWriteTime;
            }
            return isChanged;
        }
        internal static string BytesToText(byte[] buff, Encoding encoding)
        {
            if (buff == null || buff.Length == 0) { return ""; }
            TextEncodingDetect detect = new TextEncodingDetect();
            encoding = detect.GetEncoding(buff, encoding);
            if (detect.hasBom)
            {
                if (encoding == Encoding.UTF8)
                {
                    return encoding.GetString(buff, 3, buff.Length - 3);
                }
                if (encoding == Encoding.Unicode || encoding == Encoding.BigEndianUnicode)
                {
                    return encoding.GetString(buff, 2, buff.Length - 2);
                }
                if (encoding == Encoding.UTF32)
                {
                    return encoding.GetString(buff, 4, buff.Length - 4);
                }
            }
            return encoding.GetString(buff);


        }

    }

    public static partial class IOHelper
    {
        /// <summary>
        /// 检测并返回文件编码。
        /// </summary>
        /// <param name="fileName">文件路径。</param>
        /// <returns></returns>
        public static Encoding DetectEncode(string fileName)
        {
            TextEncodingDetect detect = new TextEncodingDetect();
            return detect.GetEncoding(File.ReadAllBytes(fileName));
        }
        /// <summary>
        /// 检测并返回文件编码。
        /// </summary>
        /// <param name="bytes">检测字节</param>
        /// <returns></returns>
        public static Encoding DetectEncode(byte[] bytes)
        {
            TextEncodingDetect detect = new TextEncodingDetect();
            return detect.GetEncoding(bytes);
        }
    }
    /// <summary>
    /// 文件夹操作
    /// </summary>
    public static partial class IOHelper
    {
        /// <summary>
        /// 检测文件夹是否存在
        /// </summary>
        public static bool ExistsDirectory(string path)
        {
            var mutex = GetMutex(path);

            try
            {
                return Directory.Exists(path);
            }
            finally
            {
                // 释放互斥锁
                mutex.ReleaseMutex();
            }
        }
        /// <summary>
        /// 删除文件夹
        /// </summary>
        public static bool DeleteDirectory(string path, bool recursive)
        {
            var mutex = GetMutex(path);

            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive);
                    return true;
                }
            }
            finally
            {
                // 释放互斥锁
                mutex.ReleaseMutex();
            }

            return false;
        }
        /// <summary>
        /// 获取文件夹文件
        /// </summary>
        public static string[] GetFiles(string path)
        {
            return GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
        }
        /// <summary>
        /// 获取文件夹文件
        /// </summary>
        public static string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            var mutex = GetMutex(path);

            try
            {
                if (Directory.Exists(path))
                {
                    return Directory.GetFiles(path, searchPattern, searchOption);
                }
            }
            finally
            {
                // 释放互斥锁
                mutex.ReleaseMutex();
            }

            return null;
        }
    }
}
