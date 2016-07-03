using System;
using System.Collections.Generic;
using System.Text;

namespace CYQ.Data.Extension
{
    /// <summary>
    /// 实现此接口，来扩展自定义IField语法
    /// </summary>
    public interface IField
    {
        /// <summary>
        /// 内部的SQL语句
        /// </summary>
        string Sql
        {
            get;
            set;
        }
        /// <summary>
        /// 列序号[列的顺序]
        /// </summary>
        int ColID
        {
            get;
        }
        /// <summary>
        /// 列名或表名
        /// </summary>
        string Name
        {
            get;
        }
    }
}
