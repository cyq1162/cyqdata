using CYQ.Data.SyntaxExtended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IField_Demo
{
    static class DB
    {
        public static TField User { get { return UsersBean.Instance; } }
        public static TField Article { get { return ArticleBean.Instance; } }
    }
    class UsersBean : TField
    {
        public UsersBean(string tableName)
            : base(tableName)
        {

        }
        public static TField Instance
        {
            get
            {
                return Shell.Users;
            }
        }
        class Shell
        {
            public static readonly UsersBean Users = new UsersBean("Users");
        }

        public static MField ID { get { return ID_C.Instance; } }
        public static MField Name { get { return Name_C.Instance; } }
    }
    class ArticleBean : TField
    {
        public ArticleBean(string tableName)
            : base(tableName)
        {

        }
        public static TField Instance
        {
            get
            {
                return Shell.Article;
            }
        }
        class Shell
        {
            public static readonly ArticleBean Article = new ArticleBean("Article");
        }
        public static MField ID { get { return ID_C.Instance; } }
        public static MField Name { get { return Name_C.Instance; } }
    }



    class ID_C : MField
    {
        public ID_C(string columnName, bool isInt)
            : base(columnName, isInt)
        {

        }
        public static readonly ID_C ID = new ID_C("ID", true);
        public static MField Instance
        {
            get
            {
                return ID;
            }
        }
    }
    class Name_C : MField
    {
        public Name_C(string columnName, bool isInt)
            : base(columnName, isInt)
        {

        }
        public static readonly Name_C Name = new Name_C("Name", true);
        public static MField Instance
        {
            get
            {
                return Name;
            }
        }
    }
}
