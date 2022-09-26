using System;

namespace Web.Entity
{
    public class UsersBean :CYQ.Data.Orm.SimpleOrmBase
    {
        public UsersBean()
        {
            base.SetInit(this);
        }
        public int? UserID { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? EditTime { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
