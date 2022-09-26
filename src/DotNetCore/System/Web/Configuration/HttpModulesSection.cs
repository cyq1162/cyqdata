using System;
using System.Collections.Generic;
using System.Text;

namespace System.Web.Configuration
{
    public class HttpModulesSection
    {
        public List<HttpModuleAction> Modules { get; set; }
    }


    public class HttpModuleAction
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
