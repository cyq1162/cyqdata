using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data.SqlClient;

namespace CYQ.Data
{
    /// <summary>
    /// 数据库对象基础实例
    /// </summary>
    internal partial class ConnBean
    {
        private ConnBean()
        {

        }
        /// <summary>
        /// 对应的ConnectionString的Name
        /// 或者最原始传进来的链接
        /// </summary>
        public string ConnName = string.Empty;
        /// <summary>
        /// 链接的状态是否正常。
        /// </summary>
        public bool IsOK = true;
        /// <summary>
        /// 是否从库
        /// </summary>
        public bool IsSlave = false;
        /// <summary>
        /// 是否备用库
        /// </summary>
        public bool IsBackup = false;
        /// <summary>
        /// 链接错误时的异常消息。
        /// </summary>
        internal string ErrorMsg = string.Empty;
        /// <summary>
        /// 经过格式化后的数据库链接字符串
        /// </summary>
        public string ConnString = string.Empty;
        /// <summary>
        /// 数据库类型
        /// </summary>
        public DataBaseType ConnDalType;
        /// <summary>
        /// 数据库版本信息
        /// </summary>
        public string Version;
        public ConnBean Clone()
        {
            ConnBean cb = new ConnBean();
            cb.ConnName = this.ConnName;
            cb.ConnString = this.ConnString;
            cb.ConnDalType = this.ConnDalType;
            cb.IsOK = this.IsOK;
            return cb;
        }
        public bool TryTestConn()
        {
            //err = string.Empty;
            if (!string.IsNullOrEmpty(ConnName))
            {
                DalBase helper = DalCreate.CreateDal(ConnName);
                try
                {

                    helper.Con.Open();
                    Version = helper.Con.ServerVersion;
                    if (string.IsNullOrEmpty(Version)) { Version = helper.DataBaseType.ToString(); }
                    helper.Con.Close();
                    IsOK = true;
                    ErrorMsg = string.Empty;
                }
                catch (Exception er)
                {
                    ErrorMsg = er.Message;
                    IsOK = false;
                }
                finally
                {
                    helper.Dispose();
                }
            }
            else
            {
                IsOK = false;
            }
            return IsOK;
        }
        public override int GetHashCode()
        {
            return Math.Abs(ConnString.Replace(" ", "").ToLower().GetHashCode());
        }
    }
    internal partial class ConnBean
    {
        public static int GetHashCode(string conn)
        {
            return Create(conn).GetHashCode();
        }
        /// <summary>
        /// 创建一个实例。
        /// </summary>
        /// <returns></returns>
        public static ConnBean Create(string connNameOrString)
        {
            string connString = string.Format(AppConfig.GetConn(connNameOrString), AppConfig.WebRootPath);
            if (string.IsNullOrEmpty(connString))
            {
                return null;
            }
            ConnBean cb = new ConnBean();
            cb.ConnName = connNameOrString;
            cb.ConnDalType = GetDalTypeByConnString(connString);
            cb.ConnString = RemoveConnProvider(cb.ConnDalType, connString);

            return cb;
        }
        /// <summary>
        /// 去掉 链接中的 provider=xxxx;
        /// </summary>
        public static string RemoveConnProvider(DataBaseType dal, string connString)
        {
            if (dal != DataBaseType.Access)
            {
                string conn = connString.ToLower();
                int index = conn.IndexOf("provider");
                if (index > -1 && index < connString.Length - 5 && (connString[index + 8] == '=' || connString[index + 9] == '='))
                {
                    int end = conn.IndexOf(';', index);
                    if (end > index)
                    {
                        connString = connString.Remove(index, end - index + 1);
                    }
                }
            }
            return connString;
        }
        public static DataBaseType GetDalTypeByConnString(string connString)
        {
            connString = connString.ToLower().Replace(" ", "");//去掉空格

            #region 先处理容易判断规则的
            if (connString.Contains("server=") && !connString.Contains("port="))
            {
                //server=.;database=xx;uid=xx;pwd=xx;
                return DataBaseType.MsSql;
            }
            if (connString.Contains("txtpath="))
            {
                // txt path={0}
                return DataBaseType.Txt;
            }
            if (connString.Contains("xmlpath="))
            {
                // xml path={0}
                return DataBaseType.Xml;
            }
            if (connString.Contains(".mdb") || connString.Contains(".accdb"))
            {
                //Provider=Microsoft.Jet.OLEDB.4.0; Data Source={0}App_Data/demo.mdb
                //Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0}App_Data/demo.accdb
                return DataBaseType.Access;
            }
            if (connString.Contains(".db;") || connString.Contains(".db3;"))
            {
                //Data Source={0}App_Data/demo.db;failifmissing=false
                return DataBaseType.SQLite;
            }
            if (connString.Contains("provider=msdaora") || connString.Contains("provider=oraoledb.oracle")
               || connString.Contains("description=") || connString.Contains("fororacle"))
            {
                //Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT = 1521)))(CONNECT_DATA =(Sid = orcl)));User id=sa;password=123456
                return DataBaseType.Oracle;
            }

            #endregion

            #region 其它简单或端口识别
            if (connString.Contains("provider=ase") || connString.Contains("port=5000"))
            {
                //data source=127.0.0.1;port=5000;database=cyqdata;uid=sa;pwd=123456
                return DataBaseType.Sybase;
            }
            if (connString.Contains("port=5432"))
            {
                ////server=.;port=5432;database=xx;uid=xx;pwd=xx;
                return DataBaseType.PostgreSQL;
            }
            if (connString.Contains("port=3306"))
            {
                //host=localhost;port=3306;database=mysql;uid=root;pwd=123456;Convert Zero Datetime=True;
                return DataBaseType.MySql;
            }
            #endregion

            if (connString.Contains("host=") && File.Exists(AppConfig.AssemblyPath + "MySql.Data.dll"))
            {
                return DataBaseType.MySql;
            }

            if (connString.Contains("datasource") &&
                (File.Exists(AppConfig.AssemblyPath + "Sybase.AdoNet2.AseClient.dll") || File.Exists(AppConfig.AssemblyPath + "Sybase.AdoNet4.AseClient.dll")))
            {
                return DataBaseType.Sybase;
            }
            //postgre和mssql的链接语句一样，为postgre
            if (File.Exists(AppConfig.AssemblyPath + "Npgsql.dll"))
            {
                return DataBaseType.PostgreSQL;
            }

            return DataBaseType.MsSql;

        }

    }
}
