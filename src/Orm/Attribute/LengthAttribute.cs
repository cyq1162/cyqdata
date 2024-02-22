using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Orm
{
    /// <summary>
    /// �г���
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class LengthAttribute : Attribute
    {
        private int _MaxSize = -1;
        /// <summary>
        /// ����
        /// </summary>
        public int MaxSize
        {
            get { return _MaxSize; }
            set { _MaxSize = value; }
        }
        private short _Scale = -1;
        /// <summary>
        /// ����
        /// </summary>
        public short Scale
        {
            get { return _Scale; }
            set { _Scale = value; }
        }

        public LengthAttribute(int maxSize)
        {
            _MaxSize = maxSize;
        }
        public LengthAttribute(int maxSize, short scale)
        {
            _MaxSize = maxSize;
            _Scale = scale;
        }
    }
}
