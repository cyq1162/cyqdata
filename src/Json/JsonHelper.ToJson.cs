using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using CYQ.Data.Table;
using System.Data;
using System.Text.RegularExpressions;
using CYQ.Data.SQL;
using System.IO;
using System.Reflection;
using CYQ.Data.Xml;
using System.ComponentModel;
using CYQ.Data.Tool;
using System.Threading;
namespace CYQ.Data.Json
{

    // 扩展交互部分
    public partial class JsonHelper
    {

        /// <summary>
        /// Convert object to json
        /// <para>将一个对象（实体，泛型List，字典Dictionary）转成Json</para>
        /// </summary>
        public static string ToJson(object obj)
        {
            return ToJson(obj, null);
        }

        /// <param name="jsonOp">Json Config</param>

        public static string ToJson(object obj, JsonOp jsonOp)
        {
            if (obj == null) { return null; }
            if (obj is string)
            {
                string text = Convert.ToString(obj);
                if (text == "")
                {
                    return "{}";
                }
                else if (text[0] == '{' || text[0] == '[')
                {
                    if (IsJson(text))
                    {
                        return text;
                    }
                }
                else if (text[0] == '<' && text[text.Length - 1] == '>')
                {
                    return XmlToJson(text, true);
                }
            }
            if (obj is IEnumerable && obj is ICollection)
            {

                int count = ((ICollection)obj).Count;
                if (count > 50)
                {
                    int batchSize = Math.Max(20, count / Environment.ProcessorCount);//按CPU核数量进行分批。
                    var objIEnumerable = obj as IEnumerable;
                    StringBuilder sb = new StringBuilder();
                    sb.Append("[");
                    int batchCount = (count % batchSize) == 0 ? count / batchSize : count / batchSize + 1;//页数
                    string[] items = new string[batchCount];
                    int i = -1, batchID = 1;
                    List<object> firstList = new List<object>(batchSize);
                    ThreadEntity entity = null;
                    foreach (var o in objIEnumerable)
                    {
                        i++;

                        //自己处理的条数
                        if (i < batchSize) { firstList.Add(o); continue; }

                        if (i % batchSize == 0)
                        {
                            entity = new ThreadEntity();
                            entity.Items = items;
                            entity.BatchID = batchID;
                            entity.Objs = new List<object>(batchSize);
                            entity.Objs.Add(o);
                            entity.JsonOp = jsonOp;
                        }
                        else
                        {
                            entity.Objs.Add(o);
                        }
                        if (entity.Objs.Count == batchSize || i == count - 1)
                        {
                            // new Thread(new ParameterizedThreadStart(ToInThread)).Start(entity);
                            ThreadPool.QueueUserWorkItem(new WaitCallback(ToInThread), entity);
                            batchID++;
                        }

                    }
                    //自己处理前100条数据。
                    string json = ListToJson(firstList, jsonOp);
                    sb.Append(json);
                    i = 1;
                    while (true)
                    {
                        if (items[i] != null)
                        {
                            json = items[i];
                            sb.Append(',');
                            sb.Append(json);
                            i++;
                        }

                        if (i > items.Length - 1)
                        {
                            break;
                        }
                    }
                    sb.Append(']');
                    return sb.ToString();
                }
            }

            JsonHelper js = new JsonHelper(jsonOp);
            js.Fill(obj);
            return js.ToString(obj is IList || obj is DataTable || obj is MDataTable);

        }

        private static string ListToJson(List<object> obj, JsonOp jsonOp)
        {
            JsonHelper js = new JsonHelper(jsonOp);
            js.isArrayEnd = false;
            js.Fill(obj);
            return js.ToString();
        }
        private static void ToInThread(object entityObj)
        {
            ThreadEntity entity = entityObj as ThreadEntity;
            string json = ListToJson(entity.Objs, entity.JsonOp);
            entity.Items[entity.BatchID] = json;
        }

    }

    internal class ThreadEntity
    {
        public int BatchID { get; set; }
        public List<object> Objs { get; set; }

        public string[] Items { get; set; }
        public JsonOp JsonOp { get; set; }
    }
}
