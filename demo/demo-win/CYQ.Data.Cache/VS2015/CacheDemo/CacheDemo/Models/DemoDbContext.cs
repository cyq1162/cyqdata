using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.ModelConfiguration.Conventions;
using SQLite.CodeFirst;

namespace CacheDemo.Models
{
    /// <summary>
    /// Demo 数据库实例类
    /// </summary>
    public class DemoDbContext : DbContext
    {
        ////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Type: 变量定义
        //
        ////////////////////////////////////////////////////////////////////////////////////////////

        public System.Data.Entity.DbSet<UserInfo> UserInfos { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Type: 函数
        //
        ////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// DefaultConnection对应config的节点connectionStrings的属性
        /// </summary>
        public DemoDbContext() : base("Conn")
        {

        }

        /// <summary>
        ///  在完成对派生上下文的模型的初始化后，并在该模型已锁定并用于初始化上下文之前，将调用此方法。虽然此方法的默认实现不执行任何操作，
        ///  但可在派生类中重写此方法，这样便能在锁定模型之前对其进行进一步的配置。
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //移除表名复数的契约
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Configurations.AddFromAssembly(typeof(DemoDbContext).Assembly);
#if DEBUG
            Database.SetInitializer(new MyDbInitializer(modelBuilder));
#endif
        }

        /// <summary>
        /// 自定义初始化类
        /// </summary>
        public class MyDbInitializer : SqliteDropCreateDatabaseAlways<DemoDbContext>
        {
            public MyDbInitializer(DbModelBuilder modelBuilder)
                : base(modelBuilder)
            {

            }

            protected override void Seed(DemoDbContext context)
            {

                //初始管理员表
                context.UserInfos.AddOrUpdate(x => x.ID,
                    new UserInfo()
                    {
                        ID = 1,
                        Account = "admin",
                        Password = "123456",
                        Name = "超级管理员",
                    },
                    new UserInfo()
                    {
                        ID = 2,
                        Account = "test",
                        Password = "123456",
                        Name = "测试用户",
                    }
                );
            }
        }

    }
}
