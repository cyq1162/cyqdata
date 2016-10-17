using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Tool
{
    /// <summary>
    /// 半线程安全字典类
    /// </summary>
    /// <typeparam name="K">key</typeparam>
    /// <typeparam name="V">value</typeparam>
    public class MDictionary<K, V> : Dictionary<K, V>
    {
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
                base.Add(key, value);
            }
            catch (Exception err)
            {
                if (!ContainsKey(key))
                {

                    if (times > 3)
                    {
                        Log.WriteLogToTxt(err);
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
        public new void Remove(K key)
        {
            Remove(key, 1);
        }
        private void Remove(K key, int times)
        {
            try
            {
                base.Remove(key);
            }
            catch (Exception err)
            {
                if (ContainsKey(key))
                {
                    if (times > 3)
                    {
                        Log.WriteLogToTxt(err);
                        return;
                    }
                    else if (times > 2)
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                    times++;
                    Remove(key, times);
                }
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
                if (base.ContainsKey(key))
                {
                    return base[key];
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
                return default(V);
            }
            set
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
            }
        }

        /// <summary>
        /// 当Key为int时，通过此方法取值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public V Get(K key)
        {
            if (base.ContainsKey(key))
            {
                return base[key];
            }
            return default(V);
        }
        /// <summary>
        /// 当Key为int时，通过此方法取值
        /// </summary>
        public void Set(K key, V value)
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
}
