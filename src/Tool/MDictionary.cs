using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace CYQ.Data.Tool
{
    /// <summary>
    /// �̰߳�ȫ�ֵ���
    /// </summary>
    /// <typeparam name="K">key</typeparam>
    /// <typeparam name="V">value</typeparam>
    public partial class MDictionary<K, V> : Dictionary<K, V>
    {
        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        public MDictionary()
            : base()
        {

        }
        public MDictionary(IEqualityComparer<K> comparer)
            : base(comparer)
        {

        }
        public MDictionary(int capacity)
            : base(capacity)
        {

        }
        public MDictionary(int capacity, IEqualityComparer<K> comparer)
            : base(capacity, comparer)
        {

        }
        /// <summary>
        /// ���ֵ��������������������ӣ����ڣ�����ԡ�
        /// </summary>
        public new void Add(K key, V value)
        {
            _lock.TryEnterWriteLock(Timeout.Infinite);
            try
            {
                if (!ContainsKey(key))
                {
                    base.Add(key, value);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        /// <summary>
        /// �Ƴ���������
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public new bool Remove(K key)
        {
            _lock.TryEnterWriteLock(Timeout.Infinite);
            try
            {
                return base.Remove(key);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

        }
        /// <summary>
        /// ����ȡֵ��������
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public new V this[K key]
        {
            get
            {
                _lock.TryEnterReadLock(Timeout.Infinite);
                try
                {
                    if (base.ContainsKey(key))
                    {
                        return base[key];
                    }
                    return default(V);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            set
            {
                _lock.TryEnterWriteLock(Timeout.Infinite);
                try
                {
                    if (base.ContainsKey(key))
                    {
                        base[key] = value;
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }
        /// <summary>
        /// ͨ��index����ȡֵ��������
        /// </summary>
        /// <returns></returns>
        public V this[int index]
        {
            get
            {
                _lock.TryEnterReadLock(Timeout.Infinite);
                try
                {
                    if (index >= 0 && index < this.Count)
                    {
                        int i = 0;
                        foreach (V value in this.Values)
                        {
                            if (i == index)
                            {
                                return value;
                            }
                            i++;
                        }
                    }
                    else // ���ݴ浵hash int
                    {
                        K key = ConvertTool.ChangeType<K>(index);
                        if (base.ContainsKey(key))
                        {
                            return base[key];
                        }
                    }
                    return default(V);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            set
            {
                _lock.TryEnterWriteLock(Timeout.Infinite);
                try
                {
                    if (index >= 0 && index < this.Count)
                    {
                        int i = 0;
                        foreach (K key in this.Keys)
                        {
                            if (i == index)
                            {
                                this[key] = value;
                                break;
                            }
                            i++;
                        }
                    }
                    else
                    {
                        K key = ConvertTool.ChangeType<K>(index);
                        base[key] = value;
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// ��KeyΪintʱ��ͨ���˷���ȡֵ��������
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public V Get(K key)
        {
            _lock.TryEnterReadLock(Timeout.Infinite);
            try
            {
                if (base.ContainsKey(key))
                {
                    return base[key];
                }
                return default(V);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        /// <summary>
        /// ���ֵ���ڡ�����¡�������������ӣ�������
        /// </summary>
        public void Set(K key, V value)
        {
            _lock.TryEnterWriteLock(Timeout.Infinite);
            try
            {
                if (base.ContainsKey(key))
                {
                    base[key] = value;
                }
                else
                {
                    base.Add(key, value);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        /// <summary>
        /// ������ݣ�������
        /// </summary>
        public new void Clear()
        {
            _lock.TryEnterWriteLock(Timeout.Infinite);
            try
            {
                if (Count > 0)
                {
                    base.Clear();
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

        }

        public new bool ContainsKey(K key)
        {
            if (key == null) { return false; }
            return base.ContainsKey(key);
        }
        /// <summary>
        /// ��ȡ��ֵ�б�������
        /// </summary>
        public List<K> GetKeys()
        {
            _lock.TryEnterReadLock(Timeout.Infinite);
            try
            {
                List<K> keys = new List<K>(base.Keys.Count);
                foreach (K item in base.Keys)
                {
                    keys.Add(item);
                }
                return keys;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    [Serializable]
    public partial class MDictionary<K, V>
    {
        protected MDictionary(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
