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
using CYQ.Data.Emit;
namespace CYQ.Data.Json
{

    // 扩展交互部分
    public partial class JsonHelper
    {
        //private static object ToIEnumerator(Type t, string json, EscapeOp op)
        //{
        //    if (t.FullName.StartsWith("System.Collections.") || t.FullName.Contains("MDictionary") || t.FullName.Contains("MList"))
        //    {
        //        Type[] ts;
        //        int argLength = ReflectTool.GetArgumentLength(ref t, out ts);
        //        if (argLength == 1)
        //        {
        //            return JsonSplit.ToEntityOrList(t, json, op);
        //        }
        //        else
        //        {
        //            #region Dictionary
        //            if (t.Name.StartsWith("Dictionary") && ts[0].Name == "String" && ts[1].Name == "String")
        //            {
        //                //忽略MDictionary
        //                return Split(json);
        //            }

        //            object objT = t.Name.Contains("Dictionary") ? Activator.CreateInstance(t, StringComparer.OrdinalIgnoreCase) : Activator.CreateInstance(t);
        //            Type oT = objT.GetType();
        //            MethodInfo mi = null;
        //            try
        //            {
        //                if (t.Name == "NameValueCollection")
        //                {
        //                    mi = oT.GetMethod("Add", new Type[] { typeof(string), typeof(string) });
        //                }
        //                else
        //                {
        //                    mi = oT.GetMethod("Add");
        //                }
        //            }
        //            catch
        //            {

        //            }
        //            if (mi == null)
        //            {
        //                mi = oT.GetMethod("Add", new Type[] { typeof(string), typeof(string) });
        //            }
        //            if (mi != null)
        //            {
        //                Dictionary<string, string> dic = Split(json);
        //                if (dic != null && dic.Count > 0)
        //                {
        //                    foreach (KeyValuePair<string, string> kv in dic)
        //                    {
        //                        mi.Invoke(objT, new object[] { ConvertTool.ChangeType(kv.Key, ts[0]), ConvertTool.ChangeType(UnEscape(kv.Value, op), ts[1]) });
        //                    }
        //                }
        //                return objT;
        //            }
        //            #endregion
        //        }


        //    }
        //    else if (t.FullName.EndsWith("[]"))
        //    {
        //        return ConvertTool.GetObj(t, json);
        //    }
        //    return null;
        //}

        /// <summary>
        /// Convert json to Entity
        /// </summary>
        public static T ToEntity<T>(string json) where T : class
        {
            return ToEntity<T>(json, EscapeOp.No);
        }

        /// <summary>
        /// Convert json to Entity
        /// </summary>
        public static T ToEntity<T>(string json, EscapeOp op) where T : class
        {
            return ToEntity(typeof(T), json, op) as T;
        }

        internal static object ToEntity(Type t, string json, EscapeOp op)
        {
            switch (t.Name)
            {
                case "MDataTable": return MDataTable.CreateFrom(json, null, op);
                case "DataTable": return MDataTable.CreateFrom(json, null, op).ToDataTable();
                case "MDataRow": return MDataRow.CreateFrom(json, null, BreakOp.None, op);
            }
            if (t.IsValueType || t.Name == "String")
            {
                return ConvertTool.ChangeType(json, t);
            }
            if (ReflectTool.GetSystemType(ref t) == SysType.Custom)
            {
                return JsonSplit.ToEntity(t, json, op);
            }
            Type[] args;
            int len = ReflectTool.GetArgumentLength(ref t, out args);
            if (len == 1)
            {
                return JsonSplit.ToList(t, args[0], json, op);
            }
            if (len == 2)
            {
                return JsonSplit.ToKeyValue(t, json, op);
            }
            return null;


            //if (t.FullName.StartsWith("System.Collections.") || t.FullName.EndsWith("[]") || t.FullName.Contains("MDictionary") || t.FullName.Contains("MList"))
            //{
            //    return ToIEnumerator(t, json, op);
            //}
            //else
            //{
            //    return JsonSplit.ToEntityOrList(t, json, op);
            //}
        }

        public static List<T> ToList<T>(string json)// where T : class
        {
            return ToList<T>(json, EscapeOp.No);
        }
        /// <summary>
        ///  Convert json to Entity List
        ///  <para>将Json转换为实体列表</para>
        /// </summary>
        /// <typeparam name="T">Type<para>类型</para></typeparam>
        public static List<T> ToList<T>(string json, EscapeOp op)// where T : class
        {
            return JsonSplit.ToList<T>(json, 0, op);//减少中间转换环节。
        }
    }

}
