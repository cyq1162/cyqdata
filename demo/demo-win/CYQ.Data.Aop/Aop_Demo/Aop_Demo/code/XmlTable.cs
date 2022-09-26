using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CYQ.Data.Orm;
namespace Aop_Demo
{
    /// <summary>
    /// Code First 模式
    /// </summary>
    public class XmlTable:SimpleOrmBase
    {
        public XmlTable()
        {
            base.SetInit(this);
        }
        public int ID { get; set; }
        public string Name { get; set; }
        public DateTime DateTime { get; set; }
    }
}
