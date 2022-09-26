using System;
using CYQ.Data.Xml;
using System.Xml;
using System.Web;
namespace CYQ.Data.Xml
{
    /// <summary>
    /// 多语言操作类
    /// </summary>
    public class MutilLanguage : IDisposable
    {

        XHtmlAction helper;
        /// <summary>
        /// 获取加载的Xml语言文件的完整（路径）名称
        /// </summary>
        public string FilePath
        {
            get
            {
                if (helper != null)
                {
                    return helper.FileName;
                }
                return string.Empty;
            }
        }
        private LanguageKey _LanKey = LanguageKey.None;
        /// <summary>
        /// 获取或设置当前语言类型（如果有设置语言Cookie，则初始化时从Cookie恢复）
        /// </summary>
        public LanguageKey LanKey
        {
            get
            {
                if (_LanKey == LanguageKey.None)
                {
                    _LanKey = DefaultLanKey;
                }
                return _LanKey;
            }
            set
            {
                _LanKey = value;
            }
        }
        private LanguageKey _DefaultLanKey = LanguageKey.None;
        /// <summary>
        /// 获取系统默认的语言（取值顺序：AppConfig.XHtml.SysLangKey - > 浏览器语言 -》 默认中文）
        /// </summary>
        public LanguageKey DefaultLanKey
        {
            get
            {
                if (_DefaultLanKey == LanguageKey.None)
                {
                    _DefaultLanKey = GetDefaultLangKey();
                }
                return _DefaultLanKey;
            }
        }
        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="filePath">xml文件路径</param>
        /// <param name="isForHtml">是否Html文件</param>
        /// <param name="isInitValueFromCookie">是获从Cookie初始化默认语言</param>
        public MutilLanguage(string filePath, bool isForHtml, bool isInitValueFromCookie)
        {
            Init(filePath, isForHtml, isInitValueFromCookie);
        }
        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="filePath">xml文件路径</param>
        /// <param name="isForHtml">是否Html文件</param>
        public MutilLanguage(string filePath, bool isForHtml)
        {
            Init(filePath, isForHtml, true);
        }
        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="filePath">xml文件路径</param>
        public MutilLanguage(string filePath)
        {
            Init(filePath, true, true);
        }
        private void Init(string filePath, bool isForHtml, bool isInitValueFromCookie)
        {
            helper = new XHtmlAction(isForHtml);
            helper.IsNoClone = true;//只读，节省Clone，节省CPU
            if (!helper.Load(filePath, XmlCacheLevel.Day))
            {
                Error.Throw("Load xml failed : " + filePath);
            }
            if (isInitValueFromCookie)
            {
                SetLanKeyByCookie();
            }
        }
        /// <summary>
        /// 获取多语言节点值
        /// </summary>
        /// <param name="lanID">Xml节点id</param>
        public string Get(object lanID)
        {
            return Get(lanID, LanKey);
        }
        /// <summary>
        /// 获取多语言节点值
        /// </summary>
        /// <param name="lanID">Xml节点id</param>
        /// <param name="lanKeyID">LanguageKey对应的数字</param>
        public string Get(object lanID, int lanKeyID)
        {
            if (lanKeyID > 0 && lanKeyID < 10)
            {
                return Get(lanID, (LanguageKey)lanKeyID);
            }
            return Get(lanID);
        }
        /// <summary>
        /// 获取多语言节点值
        /// </summary>
        /// <param name="lanID">Xml节点id</param>
        /// <param name="lanEnum">获取的语言</param>
        /// <returns></returns>
        public string Get(object lanID, LanguageKey lanEnum)
        {
            XmlNode node = helper.GetByID(Convert.ToString(lanID));
            if (node != null)
            {
                switch (lanEnum)
                {
                    case LanguageKey.Chinese:
                        return node.InnerXml.Trim('\r', '\n').Trim();
                    default:
                        string key = lanEnum.ToString().ToLower().Substring(0, 3);
                        if (node.Attributes[key] != null)
                        {
                            return node.Attributes[key].Value.Trim('\r', '\n').Trim();
                        }
                        else
                        {
                            return node.InnerXml.Trim('\r', '\n').Trim();
                        }
                }
            }
            return Convert.ToString(lanID);
        }
        private void SetLanKeyByCookie()
        {
            if (HttpContext.Current != null && HttpContext.Current.Handler != null)
            {
                HttpCookie myCookie = HttpContext.Current.Request.Cookies[AppConfig.XHtml.Domain + "_LanKey"];
                if (null != myCookie)
                {
                    try
                    {
                        _LanKey = (LanguageKey)Enum.Parse(typeof(LanguageKey), myCookie.Value);
                    }
                    catch
                    {
                        _LanKey = LanguageKey.None;
                    }
                }
            }
        }
        /// <summary>
        /// 设置语言类型到Cookie中
        /// </summary>
        public void SetToCookie(LanguageKey lanKey)
        {
            SetToCookie(lanKey.ToString());
        }
        /// <summary>
        /// 设置语言类型到Cookie中
        /// </summary>
        public void SetToCookie(string lanKey)
        {
            try
            {
                _LanKey = (LanguageKey)Enum.Parse(typeof(LanguageKey), lanKey);
            }
            catch
            {
                _LanKey = LanguageKey.None;
            }
            SetLanguageCookie(lanKey);
        }
        public static void SetLanguageCookie(string lanKey)
        {
            if (HttpContext.Current != null)
            {
                HttpCookie myCookie = new HttpCookie(AppConfig.XHtml.Domain + "_LanKey", lanKey);
                if (!string.IsNullOrEmpty(AppConfig.XHtml.Domain) && AppConfig.XHtml.Domain.IndexOf(':') == -1)//端口处理
                {
                    myCookie.Domain = AppConfig.XHtml.Domain;
                }
                //myCookie.Expires = System.DateTime.Now.AddHours(1);
                HttpContext.Current.Response.Cookies.Add(myCookie);
            }
        }
        /// <summary>
        /// 获取LanguageKey数字对应的枚举名称
        /// </summary>
        /// <returns></returns>
        public static string GetKey(int value)
        {
            if (value > 0 && value < 10)
            {
                return Convert.ToString((LanguageKey)value);
            }
            return LanguageKey.None.ToString();
        }
        /// <summary>
        /// 获取LanguageKey枚举对应的数字
        /// </summary>
        /// <param name="langKey">LanguageKey枚举名称</param>
        /// <returns></returns>
        public static int GetValue(string langKey)
        {
            switch (langKey)
            {
                case "Chinese":
                    return 1;
                case "English":
                    return 2;
                case "French":
                    return 3;
                case "German":
                    return 4;
                case "Hindi":
                    return 5;
                case "Italian":
                    return 6;
                case "Japanese":
                    return 7;
                case "Korean":
                    return 8;
                case "Russian":
                    return 9;
                case "Custom":
                    return 10;

            }
            return 0;
        }
        private LanguageKey GetDefaultLangKey()
        {
            string key = AppConfig.XHtml.SysLangKey;
            if (!string.IsNullOrEmpty(key) && key != "None")
            {
                try
                {
                    return (LanguageKey)Enum.Parse(typeof(LanguageKey), key);
                }
                catch
                {
                    Error.Throw(string.Format("Error:LanguageKey not contain {0}", key));
                }
            }
            if (HttpContext.Current != null && HttpContext.Current.Handler != null && HttpContext.Current.Request.UserLanguages != null && HttpContext.Current.Request.UserLanguages.Length > 0)
            {
                switch (HttpContext.Current.Request.UserLanguages[0].Substring(0, 2))
                {
                    case "zh":
                        return LanguageKey.Chinese;

                    case "en":
                        return LanguageKey.English;

                    case "fr":
                        return LanguageKey.French;

                    case "de":
                        return LanguageKey.German;

                    case "hi":
                        return LanguageKey.Hindi;

                    case "it":
                        return LanguageKey.Italian;

                    case "ja":
                        return LanguageKey.Japanese;

                    case "ko":
                        return LanguageKey.Korean;

                    case "ru":
                        return LanguageKey.Russian;

                }
            }
            return LanguageKey.Chinese;
        }
        #region IDisposable 成员

        public void Dispose()
        {
            helper.Dispose();
        }

        #endregion
    }
  
}
