using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace CYQ.Data.Tool
{
    /// <summary>
    /// 线程安全字典类
    /// </summary>
    /// <typeparam name="K">key</typeparam>
    /// <typeparam name="V">value</typeparam>
    public partial class MDictionary<K, V> : Dictionary<K, V>
    {
        private static readonly object lockObj = new object();
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

        public new void Add(K key, V value)
        {
            Add(key, value, 1);
        }
        private void Add(K key, V value, int times)
        {
            try
            {
                lock (lockObj)
                {
                    if (!ContainsKey(key))
                    {
                        base.Add(key, value);
                    }
                }
            }
            catch (Exception err)
            {
                if (!ContainsKey(key))
                {

                    if (times > 3)
                    {
                        Log.Write(err, LogType.Error);
                        return;
                    }
                    else if (times > 2)
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                    times++;
                    Add(key, value, times);
                }
            }
        }
        public new bool Remove(K key)
        {
            return Remove(key, 1);
        }
        private bool Remove(K key, int times)
        {
            try
            {
                lock (lockObj)
                {
                    if (ContainsKey(key))
                    {
                        return base.Remove(key);
                    }
                }
                return true;
            }
            catch (Exception err)
            {
                if (ContainsKey(key))
                {
                    if (times > 3)
                    {
                        Log.Write(err, LogType.Error);
                        return false;
                    }
                    else if (times > 2)
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                    times++;
                    return Remove(key, times);
                }
                return false;
            }
        }
        /// <summary>
        /// 索引取值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public new V this[K key]
        {
            get
            {
                lock (lockObj)
                {
                    if (base.ContainsKey(key))
                    {
                        return base[key];
                    }
                }
                return default(V);
            }
            set
            {
                base[key] = value;
            }
        }
        /// <summary>
        /// 通过index索引取值
        /// </summary>
        /// <returns></returns>
        public V this[int index]
        {
            get
            {
                if (index >= 0 && index < this.Count)
                {
                    lock (lockObj)
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
                }
                else // 兼容存档hash int
                {
                    K key = ConvertTool.ChangeType<K>(index);
                    if (base.ContainsKey(key))
                    {
                        return base[key];
                    }
                }
                return default(V);
            }
            set
            {
                if (index >= 0 && index < this.Count)
                {
                    lock (lockObj)
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
                }
                else
                {
                    K key = ConvertTool.ChangeType<K>(index);
                    base[key] = value;
                }
            }
        }

        /// <summary>
        /// 当Key为int时，通过此方法取值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public V Get(K key)
        {
            lock (lockObj)
            {
                if (base.ContainsKey(key))
                {
                    return base[key];
                }
            }
            return default(V);
        }
        /// <summary>
        /// 当Key为int时，通过此方法取值
        /// </summary>
        public void Set(K key, V value)
        {
            lock (lockObj)
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
        }
        public new void Clear()
        {
            if (Count > 0)
            {
                lock (lockObj)
                {
                    if (Count > 0)
                    {
                        base.Clear();
                    }
                }
            }
        }

        public new bool ContainsKey(K key)
        {
            if (key == null) { return false; }
            return base.ContainsKey(key);
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
