using CYQ.Data;
using System.IO;
using CYQ.Data.Tool;
using System.Collections.Specialized;

namespace System.Configuration
{
    internal class ConfigurationManager
    {
        static string appSettingJson = string.Empty;
        static ConfigurationManager()
        {
            AppConfig.IsAspNetCore = true;
            string filePath = AppConfig.RunPath + "appsettings.json";
            if (System.IO.File.Exists(filePath))
            {
                appSettingJson = File.ReadAllText(filePath, Text.Encoding.UTF8);
                if (!string.IsNullOrEmpty(appSettingJson))
                {
                    appSettingJson = appSettingJson.Replace("\\\\", "\\");
                }
            }
        }
        private static NameValueCollection _AppSettings;
        public static NameValueCollection AppSettings
        {
            get
            {
                if (_AppSettings == null && !string.IsNullOrEmpty(appSettingJson))
                {
                    //EscapeOp.Default 参数若不设置，会造成死循环
                    string settingValue = JsonHelper.GetValue(appSettingJson, "appsettings",EscapeOp.Default);
                    if (!string.IsNullOrEmpty(settingValue))
                    {
                        _AppSettings = JsonHelper.ToEntity<NameValueCollection>(settingValue,EscapeOp.Default);
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
                                foreach (string key  in nv.Keys)
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
    }
}