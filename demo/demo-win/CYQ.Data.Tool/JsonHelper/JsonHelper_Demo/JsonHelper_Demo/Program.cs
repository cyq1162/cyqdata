﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CYQ.Data;
using CYQ.Data.Tool;
using System.IO;

namespace JsonHelper_Demo
{
    class Program
    {
        static void OutMsg(string msg)
        {
            Console.WriteLine(msg);
        }
        static void OutDic(Dictionary<string, string> dic)
        {
            if (dic != null)
            {
                foreach (KeyValuePair<string, string> item in dic)
                {
                    OutMsg(item.Key + ":" + item.Value);
                }
            }
        }
        static string path = AppDomain.CurrentDomain.BaseDirectory;
        static string json1 = File.ReadAllText(path + "json1.txt");
        static string json2 = File.ReadAllText(path + "json2.txt");
        static string json3 = File.ReadAllText(path + "json3.txt");
        static string errJson = File.ReadAllText(path + "errJson.txt");


        static void Main(string[] args)
        {
            TestListIntString();
            //TestEntityArray();
            // JsonHelperStatic();
            // JsonHelperInstance();
            Console.Read();
        }
        //JsonHeper的静态方法
        static void JsonHelperStatic()
        {
            OutMsg("SplitArray：--------------------------------------------");
            List<Dictionary<string, string>> dicList = JsonHelper.SplitArray(json3);
            foreach (var item in dicList)
            {
                OutDic(item);
                OutMsg("-----------------");
            }

            OutMsg("Split：--------------------------------------------");
            Dictionary<string, string> dic = JsonHelper.Split(json1);
            OutDic(dic);

            OutMsg("IsJson：--------------------------------------------");
            bool result = JsonHelper.IsJson(errJson);
            OutMsg("    IsJson:" + result);

            OutMsg("GetJosnValue：--------------------------------------------");
            string value = JsonHelper.GetValue(json2, "SysConfig");
            OutMsg("    GetJosnValue:" + value);

            OutMsg("OutResult：--------------------------------------------");
            value = JsonHelper.OutResult(true, "i'm right");
            OutMsg("    OutResult:" + value);

            OutMsg("ToEntity：--------------------------------------------");
            Json1Class jc = JsonHelper.ToEntity<Json1Class>(json1);
            OutMsg("    ToEntity:" + jc.MID);

            OutMsg("ToXml：--------------------------------------------");
            value = JsonHelper.ToXml(json2);
            OutMsg("    ToXml:" + value);

            OutMsg("ToList<T>：--------------------------------------------");
            List<Json3Class> j3List = JsonHelper.ToList<Json3Class>(json3);
            OutMsg("    ToList<T>:" + j3List.Count);

            OutMsg("ToJson：--------------------------------------------");
            value = JsonHelper.ToJson(j3List);//什么对象都可以进来的..这里只是一个演示List<T>进来
            OutMsg("    ToJson:" + value);

            OutMsg("-------------------------JsonHelper静态方法已介绍完毕-------------------");

        }

        //JsonHelper的实例方法
        static void JsonHelperInstance()
        {
            JsonHelper js = new JsonHelper(true, true);
            js.Add("key", "value...");
            js.Add("boolKey", "true", true);
            js.AddBr();//需要构造多个Json时，要换一下行

            js.Add("key", "value2...");
            js.Add("child", GetJson());//可以构造复杂Json。
            js.AddBr();
            string value = js.ToString();
            OutMsg("JsonHelper：--------------------------------------------");
            OutMsg(value);
        }
        static string GetJson()
        {
            JsonHelper js = new JsonHelper();
            js.Add("v1", "v1...");
            js.Add("v2", "222", true);
            return js.ToString();
        }

        static void TestEntityArray()
        {
            EntityArray ta = new EntityArray();
            ta.Name = "dfdf";
            AA a1 = new AA();
            a1.AName = "a1";
            AA a2 = new AA();
            a2.AName = "a2";
            List<int> numList = new List<int>() { 1, 3, 5 };
            ta.NumList = numList;
            ta.AADTO = new AA[] { a1, a2 };
            string json = JsonHelper.ToJson(ta);

            EntityArray ta2 = JsonHelper.ToEntity<EntityArray>(json);
            string bbb = "dd";
        }
        static void TestListIntString()
        {
            List<string> aa = new List<string>();
            aa.Add("a1");
            aa.Add("a2");

            string json = JsonHelper.ToJson(aa);

            List<string> aResult = JsonHelper.ToList<string>(json);

            List<int> bb = new List<int>();
            bb.Add(111);
            bb.Add(222);

            string jsonBB = JsonHelper.ToJson(bb);

            List<int> bbccc = JsonHelper.ToList<int>(jsonBB);
            object dd = bbccc;
        }
    }
}
