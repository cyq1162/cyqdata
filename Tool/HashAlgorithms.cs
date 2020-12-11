
using System;
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

    /// <summary>
    /// Fowler-Noll-Vo hash, variant 1a, 32-bit version.
    /// http://www.isthe.com/chongo/tech/comp/fnv/
    /// </summary>
    internal class FNV1a_32 : HashAlgorithm
    {
        private static readonly uint FNV_prime = 16777619;
        private static readonly uint offset_basis = 2166136261;

        protected uint hash;

        public FNV1a_32()
        {
            HashSizeValue = 32;
        }

        public override void Initialize()
        {
            hash = offset_basis;
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            int length = ibStart + cbSize;
            for (int i = ibStart; i < length; i++)
            {
                hash = (hash ^ array[i]) * FNV_prime;
            }
        }

        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes(hash);
        }
    }

    ///// <summary>
    ///// Modified Fowler-Noll-Vo hash, 32-bit version.
    ///// http://home.comcast.net/~bretm/hash/6.html
    ////  ����ӻ����㷨��ͬһ��key�ڲ�ͬ���߳��£���Ȼż���������ͬ��hash��
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
    /// ����ΨһHash��
    /// </summary>
    internal class HashCreator
    {
        public static uint Create(string key)
        {
            if (key == null)
            {
                return uint.MinValue;
            }
            else if (key.Trim() == "")
            {
                return uint.MaxValue;
            }
            return BitConverter.ToUInt32(new FNV1a_32().ComputeHash(Encoding.Unicode.GetBytes(key)), 0);
        }
    }
}