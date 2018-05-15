using System;
using System.Collections.Generic;
using System.Text;

namespace System.Configuration
{
    public class ConnectionStringSettings
    {
        public string ProviderName { get; internal set; }
        public string ConnectionString { get; internal set; }
        public string Name { get; internal set; }
    }
}
