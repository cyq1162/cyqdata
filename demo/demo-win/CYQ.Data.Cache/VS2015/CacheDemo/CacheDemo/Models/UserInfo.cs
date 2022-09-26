using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheDemo.Models
{
    /// <summary>
    /// 用户信息表
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// 主键
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// 用户状态：0正常，1停用
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public String Account { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public String Password { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public String Name { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        public String Phone { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public String Email { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public String Remark { get; set; }
    }
}
