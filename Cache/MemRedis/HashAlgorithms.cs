
using System;
using System.Security.Cryptography;

namespace CYQ.Data.Cache
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
    internal class HashCreator
    {
        public static uint Create(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Error.Throw("HashCreator.Create key can't be null");
            }
            return (uint)key.GetHashCode();
        }
    }
}