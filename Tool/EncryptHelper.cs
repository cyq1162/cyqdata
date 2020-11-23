using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using CYQ.Data;

namespace CYQ.Data.Tool
{
    /// <summary>
    /// 密码加密类
    /// </summary>
    public static class EncryptHelper
    {
        internal static byte[] GetHash(string key)
        {
            using (MD5CryptoServiceProvider hashMD5 = new MD5CryptoServiceProvider())
            {
                return hashMD5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(key));
            }
        }
        private static byte[] _DefaultHashKey;
        internal static byte[] DefaultHashKey
        {
            get
            {
                if (_DefaultHashKey == null)
                {
                    _DefaultHashKey = GetHash("!1@2#3$4%5^6");
                }
                return _DefaultHashKey;
            }
        }
        ///// <summary>
        ///// 预留的二次加密
        ///// </summary>
        //internal static string EncryptKey
        //{
        //    get
        //    {
        //        return AppConfig.GetApp("EncryptKey", "");
        //    }
        //}
        #region
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="text">加密的内容</param>
        /// <returns></returns>
        public static string Encrypt(string text)
        {
            return Encrypt(text, "");
        }
        /// <param name="key">指定加密的key</param>
        public static string Encrypt(string text, string key)
        {
            string result = Encrypt(text, DefaultHashKey);
            if (string.IsNullOrEmpty(key))
            {
                key = AppConfig.GetApp("EncryptKey", "");
            }
            if (!string.IsNullOrEmpty(key))
            {
                result = Encrypt(result, GetHash(key)) + "=2";//设置二级加密标识
            }
            return result;
        }
        /// <summary>
        /// 3des加密字符串
        /// </summary>
        /// <param name="text">要加密的字符串</param>
        /// <param name="hashKey">密钥</param>
        /// <returns>加密后并经base64编码的字符串</returns>
        /// <remarks>静态方法，采用默认ascii编码</remarks>
        private static string Encrypt(string text, byte[] hashKey)
        {
            string result = string.Empty;
            using (TripleDESCryptoServiceProvider DES = new TripleDESCryptoServiceProvider())
            {
                DES.Key = hashKey;
                DES.Mode = CipherMode.ECB;
                ICryptoTransform DESEncrypt = DES.CreateEncryptor();

                byte[] Buffer = ASCIIEncoding.UTF8.GetBytes(text);
                string pass = Convert.ToBase64String(DESEncrypt.TransformFinalBlock(Buffer, 0, Buffer.Length));
                result = pass.Replace('=', '#').Replace("+", "-").Replace("/", "_");
            }

            return result;
        }//end method


        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="text">要解密的字符串</param>
        /// <returns>解密后的字符串</returns>
        public static string Decrypt(string text)
        {
            return Decrypt(text, "");
        }
        /// <param name="key">指定加密的key</param>
        public static string Decrypt(string text, string key)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            else
            {
                text = text.Trim().Replace(' ', '+');//处理Request的+号变空格问题。
                if (string.IsNullOrEmpty(key))
                {
                    key = AppConfig.GetApp("EncryptKey", "");
                }
                if (!string.IsNullOrEmpty(key) && text.EndsWith("=2"))
                {
                    text = Decrypt(text.Substring(0, text.Length - 2), GetHash(key));//先解一次Key
                }
                return Decrypt(text, DefaultHashKey);
            }
        }
        /// <summary>
        /// 3des解密字符串
        /// </summary>
        /// <param name="text">要解密的字符串</param>
        /// <param name="key">密钥</param>
        /// <returns>解密后的字符串</returns>
        /// <exception cref="">密钥错误</exception>
        /// <remarks>静态方法，采用默认ascii编码</remarks>
        private static string Decrypt(string text, byte[] hashKey)
        {
            string result = "";
            text = text.Replace('#', '=').Replace("-", "+").Replace("_", "/");
            using (TripleDESCryptoServiceProvider DES = new TripleDESCryptoServiceProvider())
            {
                DES.Key = hashKey;
                DES.Mode = CipherMode.ECB;

                ICryptoTransform DESDecrypt = DES.CreateDecryptor();
                try
                {
                    byte[] Buffer = Convert.FromBase64String(text);
                    result = ASCIIEncoding.UTF8.GetString(DESDecrypt.TransformFinalBlock(Buffer, 0, Buffer.Length));
                }
                catch
                {
                    return text;
                }
            }
            return result;
        }
        internal static bool HashKeyIsValid()
        {
            string acKey = Decrypt(AppConst.ACKey, AppConst.Host);
            acKey = AppConfig.GetConn(acKey);
            if (!string.IsNullOrEmpty(acKey))
            {
                string alKey = EncryptHelper.Decrypt(AppConst.ALKey, AppConst.Host);
                if (!string.IsNullOrEmpty(alKey))
                {
                    string code = AppConfig.GetApp(alKey);
                    if (!string.IsNullOrEmpty(code))
                    {
                        string[] items = EncryptHelper.Decrypt(code.Substring(4), code.Substring(0, 4)).Split(',');
                        DateTime d;
                        if ((DateTime.TryParse(items[0], out d) && d > DateTime.Now) || (items.Length > 1 && items[1] == AppConst.HNKey))
                        {
                            AppConfig.SetApp(alKey + AppConst.Result, "1");
                            return true;
                        }
                    }
                }
                AppConfig.SetApp(alKey + AppConst.Result, "0");
                return false;
            }
            return true;
        }
        #endregion
    }
}
