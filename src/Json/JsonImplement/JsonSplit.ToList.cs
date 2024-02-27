using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using CYQ.Data.Table;
using CYQ.Data.Tool;
using CYQ.Data.Emit;
using System.Threading;

namespace CYQ.Data.Json
{
    /// <summary>
    /// �ָ�Json�ַ���Ϊ�ֵ伯�ϡ�
    /// </summary>
    internal partial class JsonSplit
    {
        #region ��չתʵ��T

        #region �߳�ִ��
        private static void ToInThread<T>(object entityObj)
        {

            Entity<T> entity = entityObj as Entity<T>;
            var dicFunc = DictionaryToEntity.Delegate(typeof(T));
            //Console.WriteLine("ThreadID��" + System.Threading.Thread.CurrentThread.ManagedThreadId);
            //Console.WriteLine("StartIndex��" + entity.StartIndex);


            int start = entity.StartIndex;
            int end = entity.EndIndex;
            for (int i = end; i >= start; i--)
            {
                if (entity.Items[i] == null)
                {
                    var dic = entity.Dics[i];
                    var et = (T)dicFunc(dic);
                    entity.Items[i] = et;
                }
                else
                {
                    break;
                }
            }
            //�����������Ҫ������
            start = end + 1;
            end = entity.Dics.Count;
            if (start < end)
            {
                for (int i = start; i < end; i++)
                {
                    if (entity.Items[i] == null)
                    {
                        var dic = entity.Dics[i];
                        var et = (T)dicFunc(dic);
                        entity.Items[i] = et;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        #endregion

        class Entity<T>
        {
            public T[] Items { get; set; }
            public List<Dictionary<string, string>> Dics;
            public int StartIndex;
            public int EndIndex;
        }

        internal static List<T> ToList<T>(string json, int topN, EscapeOp op)
        {
            Type t = typeof(T);
            if (t.IsValueType || t.Name == "String")
            {
                List<string> stringItems = JsonSplit.SplitEscapeArray(json);
                if (stringItems == null || stringItems.Count == 0) { return null; }
                List<T> list = new List<T>();
                foreach (string stringItem in stringItems)
                {
                    list.Add(ConvertTool.ChangeType<T>(stringItem));
                }
                return list;
            }
            List<T> result = new List<T>();
            var dicFunc = DictionaryToEntity.Delegate(t);
            //if (dicFunc != null)
            //{
            #region Emit ����


            var dics = Split(json, topN, op);
            if (dics == null) { return result; }
            int dicCount = dics.Count;
            if (dicCount == 0) { return result; }
            if (dicCount <= 50)
            {
                #region �������ֱ࣬�Ӵ���

                foreach (var dic in dics)
                {
                    var value = dicFunc(dic);
                    result.Add((T)value);
                }

                #endregion

                return result;
            }


            #region �����϶࣬���̷߳�������

            int batchSize = Math.Max(20, dicCount / (Environment.ProcessorCount * 2));
            int batchCount = (dicCount % batchSize) == 0 ? dicCount / batchSize : dicCount / batchSize + 1;//ҳ��
            T[] items = new T[dicCount];
            int i = -1, batchID = 1;
            var firstList = new List<Dictionary<string, string>>(batchSize);
            Entity<T> entity = null;
            int batchNum = 0;
            foreach (var dic in dics)
            {
                i++;

                //�Լ����������
                if (i < batchSize) { firstList.Add(dic); continue; }//

                if (i % batchSize == 0)
                {
                    entity = new Entity<T>();
                    entity.StartIndex = i;
                    entity.Items = items;
                    entity.Dics = dics;
                    batchNum = 1;
                }
                else
                {
                    batchNum++;
                }
                if (batchNum == batchSize || i == dicCount - 1)
                {
                    entity.EndIndex = i;
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ToInThread<T>), entity);
                    batchID++;
                }
            }

            foreach (var dic in firstList)
            {
                //�Լ�����һ��
                result.Add((T)dicFunc(dic));
            }
            i = batchSize;

            while (true)
            {
                if (i > items.Length - 1)
                {
                    break;
                }
                if (items[i] != null)
                {
                    result.Add(items[i]);
                }
                else
                {
                    result.Add((T)dicFunc(dics[i]));
                }
                i++;
            }
            items = null;
            firstList = null;
            return result;
            #endregion

            #endregion
        }
        /* 
        /// <summary>
        /// ֧��ת����ʵ���ʵ���б����һ
        /// </summary>
        internal static object ToEntityOrList(Type t, string json, EscapeOp op)
        {
            if (t == null || string.IsNullOrEmpty(json)) { return null; }
            var sysType = ReflectTool.GetSystemType(ref t);

            if (sysType == SysType.Custom)
            {
                return ToEntity(t, json, op);
            }
            else
            {
                Type[] argsTypes;
                int len = ReflectTool.GetArgumentLength(ref t, out argsTypes);
                if (len == 1)
                {
                    var toType = argsTypes[0];
                    if (toType.IsValueType || toType.Name == "String")
                    {
                        List<string> stringItems = JsonSplit.SplitEscapeArray(json);//�ڲ�ȥ��ת�����
                        if (stringItems == null || stringItems.Count == 0) { return null; }
                        var func = ListStringToList.Delegate(t, toType);
                        return func(stringItems);
                    }
                    return ToList(t, toType, json, op);
                }
                if (len == 2)
                {
                    return ToKeyValue(t, json, op);
                }
                return null;
            }

            //Type toType = t;
            //object listObj = null;
            //MethodInfo method = null;
            //if (t.IsGenericType && (t.Name.StartsWith("List") || t.Name.StartsWith("IList") || t.Name.StartsWith("MList")))
            //{
            //    Type[] paraTypeList = null;
            //    ReflectTool.GetArgumentLength(ref t, out paraTypeList);
            //    toType = paraTypeList[0];
            //    if (toType.IsValueType || toType.Name == "String")
            //    {
            //        List<string> stringItems = JsonSplit.SplitEscapeArray(json);//�ڲ�ȥ��ת�����
            //        if (stringItems == null || stringItems.Count == 0) { return null; }
            //        var func = ListStringToList.Delegate(t, toType);
            //        return func(stringItems);
            //    }
            //    listObj = Activator.CreateInstance(t);//����ʵ��

            //    method = t.GetMethod("Add");
            //}
            //var dics = Split(json, 0, op);
            //if (dics != null && dics.Count > 0)
            //{
            //    var dicFunc = DictionaryToEntity.Delegate(t);
            //    if (method == null)
            //    {
            //        return dicFunc(dics[0]);
            //    }

            //    foreach (var item in dics)
            //    {
            //        var entity = dicFunc(item);
            //        method.Invoke(listObj, new object[] { entity });
            //    }
            //}
            //return listObj;
        }
        */

        /// <summary>
        /// ֻ����תʵ��
        /// </summary>
        internal static object ToKeyValue(Type t, string json, EscapeOp op)
        {
            var dic = Split(json, 1, op);
            if (dic != null && dic.Count > 0)
            {
                var func = DictionaryToKeyValue.Delegate(t);
                return func(dic[0]);
            }
            return null;
        }

        /// <summary>
        /// ֻ����תʵ��
        /// </summary>
        internal static object ToEntity(Type t, string json, EscapeOp op)
        {
            var dic = Split(json, 1, op);
            if (dic != null && dic.Count > 0)
            {
                var func = DictionaryToEntity.Delegate(t);
                return func(dic[0]);
            }
            return null;
        }
        /// <summary>
        /// ֻ����תListʵ��
        /// </summary>
        internal static object ToList(Type listType, Type argType, string json, EscapeOp op)
        {
            if (argType.IsValueType || argType.Name == "String")
            {
                List<string> stringItems = JsonSplit.SplitEscapeArray(json);//�ڲ�ȥ��ת�����
                if (stringItems == null || stringItems.Count == 0) { return null; }
                var func = ListStringToList.Delegate(listType, argType);
                return func(stringItems);
            }

            var list = Split(json, 0, op);
            if (list != null && list.Count > 0)
            {
                var func = ListDictionaryToList.Delegate(listType, argType);
                return func(list);
            }
            return null;
        }
        #endregion
    }

}
