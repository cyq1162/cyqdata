using CYQ.Data;
using System.IO;
using CYQ.Data.Tool;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace System.Configuration
{
    public class ConfigurationManager
    {
        static string appSettingJson = string.Empty;
        static void RegGB2312(object threadID)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//注册编码
                IOHelper.DefaultEncoding = Encoding.GetEncoding("gb2312");
            }
            catch (Exception err)
            {
                Log.Write(err,LogType.Error);
            }
        }
        static ConfigurationManager()
        {
            AppConfig.IsAspNetCore = true;
            ThreadBreak.AddGlobalThread(new ParameterizedThreadStart(RegGB2312));
            string filePath = AppConfig.RunPath + "appsettings.json";
            appSettingJson = JsonHelper.ReadJson(filePath);
            InitAddtionalConfigFiles();//加载额外的附加配置。
        }
        private static NameValueCollection _AppSettings;
        public static NameValueCollection AppSettings
        {
            get
            {
                if (_AppSettings == null && !string.IsNullOrEmpty(appSettingJson))
                {
                    //EscapeOp.Default 参数若不设置，会造成死循环
                    string settingValue = JsonHelper.GetValue(appSettingJson, "appsettings", EscapeOp.Default);
                    if (!string.IsNullOrEmpty(settingValue))
                    {
                        _AppSettings = JsonHelper.ToEntity<NameValueCollection>(settingValue, EscapeOp.Default);
                    }
                }
                if (_AppSettings == null)
                {
                    return new NameValueCollection();
                }
                return _AppSettings;
            }
        }
        private static ConnectionStringSettingsCollection _ConnectionStrings;
        public static ConnectionStringSettingsCollection ConnectionStrings
        {
            get
            {
                if (_ConnectionStrings == null)
                {
                    _ConnectionStrings = new ConnectionStringSettingsCollection();
                    if (!string.IsNullOrEmpty(appSettingJson))
                    {
                        string settingValue = JsonHelper.GetValue(appSettingJson, "connectionStrings");
                        if (!string.IsNullOrEmpty(settingValue))
                        {
                            NameValueCollection nv = JsonHelper.ToEntity<NameValueCollection>(settingValue);
                            if (nv != null && nv.Count > 0)
                            {
                                foreach (string key in nv.Keys)
                                {
                                    ConnectionStringSettings cs = new ConnectionStringSettings();
                                    cs.Name = key;
                                    cs.ConnectionString = nv[key];
                                    _ConnectionStrings.Add(cs);
                                }
                            }

                        }
                    }
                }

                return _ConnectionStrings;
            }
        }
        private static void InitAddtionalConfigFiles()
        {
            string config = Convert.ToString(GetSection("AddtionalConfigFiles"));//里面可以是一个数组，指向多个配置文件
            if (!string.IsNullOrEmpty(config))
            {
                string[] items = JsonHelper.ToEntity<string[]>(config);
                if (items != null && items.Length > 0)
                {
                    foreach (string item in items)
                    {
                        string json = JsonHelper.ReadJson(AppConfig.RunPath + item);
                        if (string.IsNullOrEmpty(json))
                        {
                            continue;
                        }
                        Dictionary<string, string> dic = JsonHelper.Split(json);
                        if (dic != null && dic.Count > 0)
                        {
                            foreach (KeyValuePair<string, string> keyValue in dic)
                            {
                                AppConfig.SetApp(keyValue.Key, keyValue.Value);
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 获得其它节点的值(字符串)。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static object GetSection(string key)
        {
            ////EscapeOp.Default 参数若不设置，会造成死循环
            return JsonHelper.GetValue(appSettingJson, key, EscapeOp.Default);
        }

    }
}