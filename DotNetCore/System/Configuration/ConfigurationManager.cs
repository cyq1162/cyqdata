using CYQ.Data;
using System.IO;
using CYQ.Data.Tool;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;

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
                Log.WriteLogToTxt(err);
            }
        }
        static ConfigurationManager()
        {
            AppConfig.IsAspNetCore = true;
            ThreadBreak.AddGlobalThread(new ParameterizedThreadStart(RegGB2312));
            string filePath = AppConfig.RunPath + "appsettings.json";
            if (System.IO.File.Exists(filePath))
            {
                appSettingJson = IOHelper.ReadAllText(filePath);
                if (!string.IsNullOrEmpty(appSettingJson))
                {
                    int index = appSettingJson.LastIndexOf("/*");
                    if (index > -1)//去掉注释
                    {
                        appSettingJson = Regex.Replace(appSettingJson, @"/\*[.\s\S]*?\*/", string.Empty, RegexOptions.IgnoreCase);
                    }
                    char splitChar = '\n';
                    if (appSettingJson.IndexOf(splitChar) > -1)
                    {
                        string[] items = appSettingJson.Split(splitChar);
                        StringBuilder sb = new StringBuilder();
                        foreach (string item in items)
                        {
                            if (!item.TrimStart(' ', '\r').StartsWith("//"))
                            {
                                sb.Append(item.Trim(' ', '\r'));
                            }
                        }
                        appSettingJson = sb.ToString();
                    }
                    if (appSettingJson.IndexOf("\\\\") > -1)
                    {
                        appSettingJson = appSettingJson.Replace("\\\\", "\\");
                    }
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

        public static object GetSection(string key) { return null; }
    }
}