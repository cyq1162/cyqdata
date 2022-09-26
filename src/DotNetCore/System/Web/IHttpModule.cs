using System;
using System.Collections.Generic;
using System.Text;

namespace System.Web
{
    /// <summary>
    /// 兼容MVC（Taurus、Aries）而存在，以下都是。
    /// </summary>
    public interface IHttpModule
    {
        void Dispose();
        void Init(HttpApplication context);
    }

}
