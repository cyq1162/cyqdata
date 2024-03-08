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
    public static class HttpPostedFileCreator
    {
        private static Assembly systemWebAssembly;
        private static Type typeHttpRawUploadedContent;
        private static ConstructorInfo typeHttpRawUploadedContentConInfo;
        private static MethodInfo typeHttpRawUploadedContentAddBytesMethod;
        private static MethodInfo typeHttpRawUploadedContentDoneAddingBytesMethod;

        private static Type typeHttpInputStream;
        private static ConstructorInfo typeHttpInputStreamtConInfo;

        private static ConstructorInfo httpPostedFileConInfo;

        static HttpPostedFileCreator()
        {
            systemWebAssembly = typeof(HttpPostedFile).Assembly;
            typeHttpRawUploadedContent = systemWebAssembly.GetType("System.Web.HttpRawUploadedContent");
            // Prepare the signatures of the constructors we want.
            Type[] uploadedParams = { typeof(int), typeof(int) };
            typeHttpRawUploadedContentConInfo = typeHttpRawUploadedContent
              .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, uploadedParams, null);
            typeHttpRawUploadedContentAddBytesMethod = typeHttpRawUploadedContent
              .GetMethod("AddBytes", BindingFlags.NonPublic | BindingFlags.Instance);
            typeHttpRawUploadedContentDoneAddingBytesMethod = typeHttpRawUploadedContent
              .GetMethod("DoneAddingBytes", BindingFlags.NonPublic | BindingFlags.Instance);

            typeHttpInputStream = systemWebAssembly.GetType("System.Web.HttpInputStream");
            Type[] streamParams = { typeHttpRawUploadedContent, typeof(int), typeof(int) };
            typeHttpInputStreamtConInfo = typeHttpInputStream
              .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, streamParams, null);


            Type[] parameters = { typeof(string), typeof(string), typeHttpInputStream };
            httpPostedFileConInfo = typeof(HttpPostedFile)
              .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, parameters, null);
        }
        /// <summary>
        /// 根据文件路径，返回 HttpPostedFile 实例。
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public static HttpPostedFile Create(string filePath)
        {
            try
            {
                string rootPath = AppConst.WebRootPath;
                if (!filePath.StartsWith(rootPath))
                {
                    filePath = rootPath + filePath;
                }

                byte[] data = File.ReadAllBytes(filePath);
                string contentType = "image/" + Path.GetExtension(filePath).ToLower().Substring(1);
                return DotNetCreate(data, Path.GetFileName(filePath), contentType);
            }
            catch (Exception err)
            {
                Log.Write(err);
            }
            return null;
        }
        private static HttpPostedFile DotNetCreate(byte[] data, string filename, string contentType)
        {
            // Create an HttpRawUploadedContent instance
            object uploadedContent = typeHttpRawUploadedContentConInfo.Invoke(new object[] { data.Length, data.Length });

            // Call the AddBytes method
            typeHttpRawUploadedContentAddBytesMethod.Invoke(uploadedContent, new object[] { data, 0, data.Length });

            // This is necessary if you will be using the returned content (ie to Save)
            typeHttpRawUploadedContentDoneAddingBytesMethod.Invoke(uploadedContent, null);

            // Create an HttpInputStream instance
            object stream = (Stream)typeHttpInputStreamtConInfo.Invoke(new object[] { uploadedContent, 0, data.Length });

            // Create an HttpPostedFile instance
            HttpPostedFile postedFile = (HttpPostedFile)httpPostedFileConInfo.Invoke(new object[] { filename, contentType, stream });

            return postedFile;
        }
    }
}
