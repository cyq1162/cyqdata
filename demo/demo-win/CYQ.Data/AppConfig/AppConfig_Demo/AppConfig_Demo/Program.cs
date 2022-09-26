using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CYQ.Data;
namespace AppConfig_Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            //总体说明：
            //1 : AppConfig操作的是Web项目的web.config 或win项目的app.config或netcore里的appsettings.json
            //2 : AppConfig操作的都是Connections节点和AppSettings节点，其它节点与此无关
            //3 ：AppConfig里的属性名称，都和 AppSettings 里的配置name一致。

            string connectionString = AppConfig.GetConn("Conn");//读取链接，框架从链接字符串里自动识别类型，所以不用配ProviderName

            AppConfig.SetApp("MyKey", "hello world");//这个只是在内存中，并没有写到相应的web/app.config文件。
            string hello = AppConfig.GetApp("MyKey");
            string isWriteLog = AppConfig.GetApp("IsWriteLog");
            int defaultCacheTime = AppConfig.GetAppInt("defaultCacheTime", 0);//内部自动转成int
            Console.WriteLine(hello + ":" + isWriteLog + ":" + defaultCacheTime);


            /*
属性介绍：
IsEnumToInt：是否使用表字段枚举转Int方式（默认为false）。 设置为true时，可以加快一点性能，但生成的表字段枚举必须和数据库一致。（通常数据库稳定不改时，枚举也对应时可启用）  
Aop Aop 插件配置项 示例配置：[ 完整类名,程序集(dll)名称]<add key="Aop" value="Web.Aop.AopAction,Aop"/>  
ThreadBreakPath Tool.ThreadBreak 使用时，外置的文件配置相对路径（默认在环境变量Temp对应文件中）  （这个可以暂时不用管）
EntitySuffix 生成的实体类的后缀（默认是Bean，即一个实体名可以叫：Users 或 UsersBean ）后续的东西会被替换成空，以此找到表名。  
Version 获取当前Dll的版本号  

        */
            //其它，关键看API文档的说明。
           // AppConfig.Debug.XXX;                     --这个见AppDebug项目
           // AppConfig.Log.XXX                        --这个见Log&SysLogs项目 
           // AppConfig.Cache.DefaultCacheTime         -- 这个见 Cache_Demo项目
           // AppConfig.XHtml                          -- 这个见 XHtmlAction 
           // AppConfig.DB                             -- 这个见 MAction 项目
              
        }
       
    }
}
