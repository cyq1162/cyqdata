using System;
using System.Configuration;
using CYQ.Data.SQL;
using CYQ.Data.Tool;
using System.Web;

namespace CYQ.Data
{
       
    public static partial class AppConst
    {
       
        /// <summary>
        /// 当前是否.NET Core环境。
        /// </summary>
        public static bool IsNetCore
        {
            get
            {
                return true;
            }
        }
       
    }

}
