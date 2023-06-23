using CYQ.Data;
using System.IO;
using CYQ.Data.Tool;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
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
                Log.Write(err, LogType.Error);
            }
        }
        static ConfigurationManager()
        {
            RegGB2312(null);
            //ThreadBreak.AddGlobalThread(new ParameterizedThreadStart(RegGB2312));
            string filePath = AppConfig.RunPath + "appsettings.json";
            ReInitConfig(filePath);
            IOWatch.On(filePath, delegate (FileSystemEventArgs e)
            {
                ReInitConfig(e.FullPath);
            });
        }
        static void ReInitConfig(string filePath)
        {

            appSettingJson = JsonHelper.ReadJson(filePath);
            if (settings != null)
            {
                settings.Clear();
            }
            settings = JsonHelper.Split(appSettingJson);
            _AppSettings.Clear();
            _ConnectionStrings.Clear();
            //  AppConfig.Clear();通过代码设置的数据，不随配置文件修改而改变。
            ConnBean.Clear();
            ConnObject.Clear();
            InitAddtionalConfigFiles();//加载额外的附加配置。
        }
        private static readonly object o = new object();
        private static NameValueCollection _AppSettings = new NameValueCollection();
        public static NameValueCollection AppSettings
        {
            get
            {
                if (_AppSettings.Count == 0)
                {
                    lock (o)
                    {
                        if (_AppSettings.Count == 0)
                        {
                            if (settings.ContainsKey("appsettings"))
                            {
                                //EscapeOp.Default 参数若不设置，会造成死循环
                                string settingValue = settings["appsettings"];
                                if (!string.IsNullOrEmpty(settingValue))
                                {
                                    NameValueCollection nvc = JsonHelper.ToEntity<NameValueCollection>(settingValue, EscapeOp.Default);
                                    if (nvc != null && nvc.Count > 0)
                                    {
                                        _AppSettings = nvc;
                                    }
                                }
                            }
                        }
                    }
                }
                return _AppSettings;
            }
        }
        private static readonly object oo = new object();
        private static ConnectionStringSettingsCollection _ConnectionStrings = new ConnectionStringSettingsCollection();
        public static ConnectionStringSettingsCollection ConnectionStrings
        {
            get
            {
                if (_ConnectionStrings.Count == 0)
                {
                    lock (oo)
                    {
                        if (_ConnectionStrings.Count == 0)
                        {
                            if (settings.ContainsKey("connectionStrings"))
                            {
                                string settingValue = settings["connectionStrings"];
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
                        string path = AppConfig.RunPath + item;
                        string json = JsonHelper.ReadJson(path);
                        if (string.IsNullOrEmpty(json))
                        {
                            continue;
                        }
                        IOWatch.On(path, delegate (FileSystemEventArgs e)
                        {
                            SetKeyValue(JsonHelper.ReadJson(e.FullPath));
                        });
                        SetKeyValue(json);
                    }
                }
            }
        }
        private static void SetKeyValue(string json)
        {
            Dictionary<string, string> dic = JsonHelper.Split(json);
            if (dic != null && dic.Count > 0)
            {
                foreach (KeyValuePair<string, string> keyValue in dic)
                {
                    AppConfig.SetApp(keyValue.Key, keyValue.Value);
                }
            }
        }

        /// <summary>
        /// appsetting.json
        /// </summary>
        private static Dictionary<string, string> settings = null;

        /// <summary>
        /// 获得其它节点的值(字符串)。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static object GetSection(string key)
        {
            if (settings.ContainsKey(key))
            {
                return settings[key];
            }
            if (key.IndexOf('.') > -1)
            {
                string[] items = key.Split('.');
                string firstKey = items[0];
                for (int i = 0; i < items.Length - 1; i++)
                {
                    if (i > 0)
                    {
                        firstKey += "." + items[i];
                    }
                    if (settings.ContainsKey(firstKey))
                    {
                        //性能优化，仅找到首个key的，才进行后续取值操作。
                        string json = settings[firstKey];
                        string leftKey = key.Substring(firstKey.Length + 1);
                        //EscapeOp.Default 参数若不设置，会造成死循环
                        return JsonHelper.GetValue(json, leftKey, EscapeOp.Default);
                    }
                }
            }

            return null;
        }

    }
}