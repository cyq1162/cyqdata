using CYQ.Data.Tool;
using System.Collections.Generic;

namespace System.Web
{
    public class HttpFileCollection:List<HttpPostedFile>
    {
        private List<string> _Keys = new List<string>();
        public List<string> Keys 
        {
            get
            {
                return _Keys;
            }
        }
        public string[] AllKeys 
        {
            get
            {
                return _Keys.ToArray();
            }
        }
        public int Count
        {
            get
            {
                return base.Count;
            }
        }
        public HttpPostedFile this[int index] 
        {
            get
            {
                if (index >= 0 && index < base.Count)
                {
                    return base[index];
                }
                return null;
            }
        }
        public HttpPostedFile this[string name]
        {
            get
            {
                for (int i = 0; i < Keys.Count; i++)
                {
                    if (Keys[i].ToLower() == name.ToLower())
                    {
                        return this[i];
                    }
                }
                return null;
            }
        }
        internal void Add(HttpPostedFile file,string name)
        {
            _Keys.Add(name);
            base.Add(file);
        }
    }
}
