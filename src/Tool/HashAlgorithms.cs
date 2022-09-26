
using System;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Text;

namespace CYQ.Data.Tool
{
    ///// <summary>
    ///// Fowler-Noll-Vo hash, variant 1, 32-bit version.
    ///// http://www.isthe.com/chongo/tech/comp/fnv/
    ///// </summary>
    //internal class FNV1_32 : HashAlgorithm
    //{
    //    private static readonly uint FNV_prime = 16777619;
    //    private static readonly uint offset_basis = 2166136261;

    //    protected uint hash;

    //    public FNV1_32()
    //    {
    //        HashSizeValue = 32;
    //    }

    //    public override void Initialize()
    //    {
    //        hash = offset_basis;
    //    }

    //    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    //    {
    //        int length = ibStart + cbSize;
    //        for (int i = ibStart; i < length; i++)
    //        {
    //            hash = (hash * FNV_prime) ^ array[i];
    //        }
    //    }

    //    protected override byte[] HashFinal()
    //    {
    //        return BitConverter.GetBytes(hash);
    //    }
    //}

    ///// <summary>
    ///// Fowler-Noll-Vo hash, variant 1a, 32-bit version.
    ///// http://www.isthe.com/chongo/tech/comp/fnv/
    ///// </summary>
    //internal class FNV1a_32 : HashAlgorithm
    //{
    //    private static readonly uint FNV_prime = 16777619;
    //    private static readonly uint offset_basis = 2166136261;

    //    protected uint hash;

    //    public FNV1a_32()
    //    {
    //        HashSizeValue = 32;
    //    }

    //    public override void Initialize()
    //    {
    //        hash = offset_basis;
    //    }

    //    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    //    {
    //        int length = ibStart + cbSize;
    //        for (int i = ibStart; i < length; i++)
    //        {
    //            hash = (hash ^ array[i]) * FNV_prime;
    //        }
    //    }

    //    protected override byte[] HashFinal()
    //    {
    //        return BitConverter.GetBytes(hash);
    //    }
    //}

    ///// <summary>
    ///// Modified Fowler-Noll-Vo hash, 32-bit version.
    ///// http://home.comcast.net/~bretm/hash/6.html
    ////  这个坑货的算法，同一个key在不同的线程下，竟然偶尔会出来不同的hash。
    ///// </summary>
    //internal class ModifiedFNV1_32 : FNV1_32
    //{
    //    protected override byte[] HashFinal()
    //    {
    //        hash += hash << 13;
    //        hash ^= hash >> 7;
    //        hash += hash << 3;
    //        hash ^= hash >> 17;
    //        hash += hash << 5;
    //        return BitConverter.GetBytes(hash);
    //    }

    //}
    /// <summary>
    /// 创建唯一Hash。
    /// </summary>
    internal class HashCreator
    {
        /// <summary>
        /// 创建HashKey，重复概率低一些。
        /// </summary>
        /// <returns></returns>
        public static string CreateKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "K" + CreateCode(key);
            }
            return "K" + (int)key[0] + CreateCode(key) + key.Length;
        }
        //private static readonly FNV1a_32 fnv1 = new FNV1a_32();//不受进程影响，但受cpu影响，所以不能用。
        /// <summary>
        /// 创建HashCode，有一定重复概率。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static uint CreateCode(string key)
        {
            if (key == null)
            {
                return uint.MinValue;
            }
            else if (key.Trim() == "")
            {
                return uint.MaxValue;
            }
            return GetHashCode(key);
            //return BitConverter.ToUInt32(fnv1.ComputeHash(Encoding.Unicode.GetBytes(key)), 0);
        }
        /// <summary>
        /// 取自java的字符串hash算法(有一定重复的概率)。
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static uint GetHashCode(string value)
        {
            uint h = 0;
            foreach (char c in value)
            {
                h = 31 * h + c;
            }
            return h;

        }
        //[System.Security.SecuritySafeCritical]  // auto-generated
        //[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        //private static uint GetHashCode(string key)
        //{
        //    unsafe
        //    {
        //        fixed (char* src = key)
        //        {
        //            int hash1 = 5381;

        //            int hash2 = hash1;

        //            int c;
        //            char* s = src;
        //            while ((c = s[0]) != 0)
        //            {
        //                hash1 = ((hash1 << 5) + hash1) ^ c;
        //                c = s[1];
        //                if (c == 0)
        //                    break;
        //                hash2 = ((hash2 << 5) + hash2) ^ c;
        //                s += 2;
        //            }

        //            return (uint)((long)(hash1 + hash2 * 1566083941) + int.MaxValue);
        //        }
        //    }
        //}
    }
}