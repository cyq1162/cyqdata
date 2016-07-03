using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Tool
{
    internal class MD5
    {
        static Dictionary<string, string> md5Cache = new Dictionary<string, string>(32);
        static System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        internal static string Get(string sourceString)
        {
            try
            {
                if (md5Cache.ContainsKey(sourceString))
                {
                    return md5Cache[sourceString];
                }
                else
                {
                    if (md5Cache.Count > 512)
                    {
                        md5Cache.Clear();
                        md5 = null;
                        md5Cache = new Dictionary<string, string>(64);
                    }
                    string value = BitConverter.ToString(md5.ComputeHash(UTF8Encoding.Default.GetBytes(sourceString)), 4, 8).Replace("-", "");
                    md5Cache.Add(sourceString, value);
                    return value;
                }
            }
            catch
            {
                return sourceString;
            }
        }
    }
}
