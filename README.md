# cyq.data is not only a orm,but also a data access layer,it's very special and diffirent from others,who use who love it.
<hr />
Demo：<br />
http Url：http://code.taobao.org/p/cyqopen/src/trunk/CYQ.Data.GettingStarted/ <br />
svn checkout: http://code.taobao.org/svn/cyqopen/trunk/CYQ.Data.GettingStarted/
<br />
注意事项：
<hr />
1：MySQL 5.7.9版本需要把用命令行设置：
执行SET GLOBAL sql_mode = ''; 把sql_mode 改成非only_full_group_by模式。验证是否生效 SELECT @@GLOBAL.sql_mode 或 SELECT @@sql_mode
