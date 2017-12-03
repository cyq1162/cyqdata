using System;
using System.Collections.Generic;
using System.Text;

namespace System.Configuration
{
    class ConnectionStringSettings
    {
        public string ProviderName { get; internal set; }
        public string ConnectionString { get; internal set; }
        public string Name { get; internal set; }
    }
}
