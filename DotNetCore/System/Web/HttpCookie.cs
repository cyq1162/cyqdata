using System;
using System.Collections.Generic;
using System.Text;

namespace System.Web
{
    class HttpCookie
    {
        private string v;
        private string lanKey;

        public HttpCookie(string v, string lanKey)
        {
            this.v = v;
            this.lanKey = lanKey;
        }

        public string Name { get; internal set; }
        public DateTime Expires { get; internal set; }
        public string Path { get; internal set; }
        public bool HttpOnly { get; internal set; }
        public string Value { get; internal set; }
        public string Domain { get; internal set; }
    }
}
