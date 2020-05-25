using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace CYQ.Data
{
    internal class NoSqlParameterCollection : DbParameterCollection
    {
        MDictionary<string, NoSqlParameter> dic = new MDictionary<string, NoSqlParameter>();
        public override int Add(object value)
        {
            NoSqlParameter p = value as NoSqlParameter;
            if (p != null && !dic.ContainsKey(p.ParameterName))
            {
                dic.Add(p.ParameterName, p);
                return 1;
            }
            return 0;
        }

        public override void AddRange(Array values)
        {
            // throw new NotImplementedException();
        }

        public override void Clear()
        {
            dic.Clear();
        }

        public override bool Contains(string value)
        {
            return dic.ContainsKey(value);
        }

        public override bool Contains(object value)
        {
            NoSqlParameter p = value as NoSqlParameter;
            return p != null && dic.ContainsKey(p.ParameterName);

        }

        public override void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public override int Count
        {
            get { return dic.Count; }
        }

        public override System.Collections.IEnumerator GetEnumerator()
        {
            return dic.Values.GetEnumerator();
        }

        protected override DbParameter GetParameter(string parameterName)
        {
            if (dic.ContainsKey(parameterName))
            {
                return dic[parameterName];
            }
            return null;
        }

        protected override DbParameter GetParameter(int index)
        {
            if (index < dic.Count)
            {
                return dic[index];
            }
            return null;
        }

        public override int IndexOf(string parameterName)
        {
            throw new NotImplementedException();
        }

        public override int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        public override void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public override bool IsFixedSize
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }

        public override void Remove(object value)
        {
            NoSqlParameter p = value as NoSqlParameter;
            if (p != null && dic.ContainsKey(p.ParameterName))
            {
                dic.Remove(p.ParameterName);
            }
        }

        public override void RemoveAt(string parameterName)
        {
            dic.Remove(parameterName);
        }

        public override void RemoveAt(int index)
        {
            NoSqlParameter p = GetParameter(index) as NoSqlParameter;
            if (p != null)
            {
                dic.Remove(p.ParameterName);
            }
        }

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            throw new NotImplementedException();
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            throw new NotImplementedException();
        }

        public override object SyncRoot
        {
            get { throw new NotImplementedException(); }
        }
    }
}
