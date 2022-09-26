using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CYQ.Data.Aop;
using CYQ.Data;
namespace MyAopForDemo
{
    public class AopForRecordState:IAop
    {
        //注意，我把这类库的生成目录指向了Aop_Demo的目录。
        //如果路径有误，可以改（项目属性=》生成=》输出路径）
        //这样你们就不用手工把生成的MyAopForDemo.dll Copy到 Aop_Demo项目的Debug目录下了

        //想让dll自动生成到另一个项目下，最方便是通过项目引用，不过Aop通常都是动态加载，项目之前没有引用关系，这里怕引用误导了大伙。

        public AopResult Begin(AopEnum action, AopInfo aopInfo)
        {
            OutMsg("----------------现在开始操作:" + action);
            OutMsg("表名:" + aopInfo.TableName);
            OutMsg("Where:" + aopInfo.Where);

            return AopResult.Continue;//让执行继续了，返回有四种状态
        }


        public void End(AopEnum action, AopInfo aopInfo)
        {
             OutMsg("操作结果:" + aopInfo.IsSuccess);
             OutMsg("操作表名:" + aopInfo.TableName);
             OutMsg("操作参数:" + aopInfo.Where);
             OutMsg("----------------操作结束:" + action);
        }

        public IAop Clone()
        {
            return new AopForRecordState();//返回自身新实例即可
        }

        public void OnError(string msg)
        {
            Log.WriteLogToTxt("如果执行操作发生了异常，我得干点什么呢:" + msg);
        }

        public void OnLoad()
        {
            //第一次被加载，我也不知道该干什么
            OutMsg("Hello world...哈，我被加载了");
        }
        //以下是我自己写的方法，不是接口的
        private void OutMsg(string msg)
        {
            //我知道你是控制台，所以...，如果是Web，我就Response.Write了
            System.Console.WriteLine(msg);
        }
    }
}
