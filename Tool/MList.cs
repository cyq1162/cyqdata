using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Tool
{
    /*
    internal class MList2<T>
    {
        private List<T>[] listArray;
        public MList2()
        {
            listArray = new List<T>[10];
            for (int i = 0; i < listArray.Length; i++)
            {
                listArray[i] = new List<T>(10000);
            }
        }
        private List<T> Get(T key)
        {
            try
            {
                int num = key.GetHashCode() % 10;
                if (num < 0) { num = 10 + num; }
                return listArray[num];
            }
            catch (Exception)
            {

                throw;
            }

        }
        public void Add(T key)
        {
            Get(key).Add(key);
        }
        public bool Contains(T key)
        {
            return Get(key).Contains(key);
        }
        public void Remove(T key)
        {
            Get(key).Remove(key);
        }
        public void Clear()
        {
            for (int i = 0; i < listArray.Length; i++)
            {
                listArray[i].Clear();
            }
        }
        public int Count
        {
            get
            {
                int count = 0;
                for (int i = 0; i < listArray.Length; i++)
                {
                    count += listArray[i].Count;
                }
                return count;
            }
        }
        public List<T> GetItems()
        {
            List<T> list = new List<T>();
            for (int i = 0; i < listArray.Length; i++)
            {
                list.AddRange(listArray[i]);
            }
            return list;
        }
    }
    */
    internal class MList<T>
    {
        List<T> list;
        Dictionary<T, int> dic;
        public MList()
        {
            list = new List<T>();
            dic = new Dictionary<T, int>();
        }
        public MList(int num)
        {
            list = new List<T>(num);
            dic = new Dictionary<T, int>(num);
        }
        public void Add(T key)
        {
            dic.Add(key, 0);
            list.Add(key);
        }
        public bool Contains(T key)
        {
            return dic.ContainsKey(key);
        }
        public void Remove(T key)
        {
            dic.Remove(key);
            list.Remove(key);
        }
        public void Clear()
        {
            dic.Clear();
            list.Clear();

        }
        public int Count
        {
            get
            {
                return list.Count;
            }
        }
        public List<T> GetList()
        {
            return list;
        }
    }
}
