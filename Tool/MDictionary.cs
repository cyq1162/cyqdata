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
    }
}
