using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CYQ.Data;
namespace IField_Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            //这个功能当年想的，目前已不推荐使用用了，所以演示Demo就到这里了，仅供观赏。。。
            AppConfig.DB.DefaultConn = "";
            using (MAction action = new MAction(DB.User.LeftJoin(DB.Article).On(UsersBean.ID == ArticleBean.ID)))
            {
                action.Select(UsersBean.ID > 10);
            }
        }
    }
}
