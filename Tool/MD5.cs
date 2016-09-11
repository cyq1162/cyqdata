using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Tool
{
    /// <summary>
    /// 返回一个加密过的值
    /// </summary>
    internal class MD5
    {
        static Dictionary<string, string> md5Cache = new Dictionary<string, string>(32);
        //取消MD5，避开win2008的异常：此实现不是 Windows 平台 FIPS 验证的加密算法的一部
        //static System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        
    }
}
