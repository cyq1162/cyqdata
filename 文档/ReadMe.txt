###--------------------------------------------------------###

   Txt::  Txt Path=E:\
   Xml::  Xml Path=E:\
Access::  Provider=Microsoft.Jet.OLEDB.4.0; Data Source=E:\cyqdata.mdb
Sqlite::  Data Source=E:\cyqdata.db;failifmissing=false;
 MySql::  host=localhost;port=3306;database=cyqdata;uid=root;pwd=123456;Convert Zero Datetime=True;
 Mssql::  server=.;database=cyqdata;uid=sa;pwd=123456; 
Sybase::  data source=127.0.0.1;port=5000;database=cyqdata;uid=sa;pwd=123456;
Postgre:  server=localhost;uid=sa;pwd=123456;database=cyqdata; 
    DB2:  Database=SAMPLE;User ID=administrator;Server=127.0.0.1;password=1234560;provider=db2; 

Oracle OracleClient:: 
Provider=MSDAORA;Data Source=orcl;User ID=sa;Password=123456
Provider=MSDAORA;Data Source=ip\orcl;User ID=sa;Password=123456

Oracle ODP.NET::
Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT = 1521)))(CONNECT_DATA =(SID = orcl)));User ID=sa;password=123456

由于各种数据库链接语句基本一致，除了特定写法外，可以通过链接补充：provider=mssql、provider=mysql、provider=db2、provider=postgre等来区分。
###--------------------------------------------------------###

中文说明：
1：{0} 代表根目录
2：sqlite、MySql、Sybase 都需要将对应的dll放到和cyq.data.dll同一目录。
相关的dll下载：http://www.cyqdata.com/download/article-detail-426
3：oracle 时：
A：默认OracleClient引的是64位的，如果是32位的，需要自己从源码移除重新引用32位的。
B：用odp.net 的Oracle.DataAccess 需要自己下载安装，将把Oracle.DataAccess.dll放到和同一目录下。
C：用Oracle.ManagedDataAccess放到同一目录下即可以使用。

Explanation:
1: {0} represents the root directory
2: sqlite, MySql, Sybase needs to put the corresponding dll and cyq.data.dll same directory.
Related dll download: http: //www.cyqdata.com/download/article-detail-426
3: oracle when:
A: The default OracleClient lead is 64, if it is 32, you need to remove yourself from the re-introduction of 32-bit source.
B: with odp.net of Oracle.DataAccess need to download to install, and will Oracle.DataAccess.dll into the same directory.
C: with Oracle.ManagedDataAccess into the same directory that is available.
