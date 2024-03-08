using CYQ.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;

namespace System.Web
{
    /// <summary>
    /// HttpPostedFile 文件创建类
    /// </summary>
    public class HttpPostedFileCreator
    {
        /// <summary>
        /// 创建 HttpPostedFile 实例
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public static HttpPostedFile Create(string filePath)
        {
            string rootPath = AppConst.WebRootPath;
            if (!filePath.StartsWith(rootPath))
            {
                filePath = rootPath + filePath;
            }
            return new HttpPostedFile(filePath);
        }
    
        
        //private static HttpPostedFile NetCoreCreate(string path)
        //{
        //    Type[] parameters = { typeof(string) };
        //    HttpPostedFile postedFile = (HttpPostedFile)typeof(HttpPostedFile)
        //      .GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, parameters, null)
        //      .Invoke(new object[] { path });
        //    return postedFile;
        //}
    }
}
