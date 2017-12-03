using CYQ.Data.Tool;
using System.Collections;
using System.Collections.Generic;

namespace System.Configuration
{
    internal class ConnectionStringSettingsCollection: List<ConnectionStringSettings>
    {
        //public ConnectionStringSettings this[int index] { get { return null; } }
        public ConnectionStringSettings this[string name]
        {
            get
            {
                for (int i = 0; i < base.Count; i++)
                {
                    if (base[i].Name.ToLower() == name.ToLower())
                    {
                        return base[i];
                    }
                }
                return null;
            }
        }

    }
}