# cyq.data is a high-performance and the most powerful orm. ormit's very special and different from others,who use who love it.
<br/>Support：Txt、Xml、Access、Sqlite、Mssql、Mysql、Oracle、Sybase、Postgres、Redis、MemCache。
<hr />

Demo(入门)：https://github.com/cyq1162/CYQ.Data.Demo <br/>
开篇介绍：http://www.cnblogs.com/cyq1162/p/5634414.html <br />
更多文章：http://www.cnblogs.com/cyq1162/category/852300.html<br />

<br /><br />QQ群：6033006<br />
VIP培训课程，精通系列视频，300元/套，(共18集，每集1小时左右)，可群里联系作者购买！
<br /><br />

CYQ.Data 最近汇总了一下教程，放个人微信公众号里了，有需要的在公众号里输入cyq.data就可以看到了<br />
<img src="https://images2018.cnblogs.com/blog/17408/201805/17408-20180523041027505-1002652922.jpg" width="200" height="200" /><br />

注意事项：
<hr />
1：MySQL 5.7.9版本需要把用命令行设置：
执行SET GLOBAL sql_mode = ''; 把sql_mode 改成非only_full_group_by模式。验证是否生效 SELECT @@GLOBAL.sql_mode 或 SELECT @@sql_mode
<hr />
<h1><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Foreword:</span></span></h1>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">As CYQ.Data began to return to free use, it was found that users' emotions were getting more and more excited. In order to maintain this continuous excitement, I had the idea of ​​open source.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">At the same time, because the framework has evolved over the past 5-6 years, the early tutorials that were previously published are too backward, including the way they are used, and related introductions, which are easily misleading.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">To this end, I intend to re-write a series to introduce the latest version, let everyone transition from traditional ORM programming to </span></span><strong><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">automated framework-based thinking programming</span></span></strong><span style="vertical-align: inherit;"><span style="vertical-align: inherit;"> (self-created words).</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">So: the name of this new series is called: </span></span><strong><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">CYQ.Data from entry to give up ORM series</span></span></strong></p>
<h1><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">What is: CYQ.Data</span></span></h1>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">1: It is an ORM framework.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">2: It is a data layer component.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">3: It is a tool set class library.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Look at a picture below:</span></span></p>
<p><img src="http://images2015.cnblogs.com/blog/17408/201607/17408-20160701223405921-97929732.jpg" alt="" /></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">As can be seen from the above figure, it is more than just an ORM, but also comes with some functions.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">therefore:</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Write log: You no longer need: Log4net.dll</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Manipulating Json: You no longer need newtonjson.dll</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Distributed Cache: You no longer need Memcached.ClientLibrary.dll</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">At present, the framework is only 340K, and subsequent versions will not be confused and the volume will be smaller.</span></span></p>
<h1><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">The development process of traditional ORM:</span></span></h1>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Look at a one-size-fits-all development trend chart:</span></span></p>
<p><img src="http://images2015.cnblogs.com/blog/17408/201607/17408-20160701225858124-845201463.jpg" alt="" /></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">In the open source China search for .NET department: ORM, the number is about 110, in the CodeProject search for .NET department: ORM, the number is about 530.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">After a lot of review, it's easy to see that the ORMs on the market are almost the same, the only difference:</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">It is in the custom query grammar, each family is playing their own tricks, and must play differently, otherwise everyone is the same, showing no sense of superiority.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">At the same time, this variety of nonsense query syntax sugar also wastes a lot of developer time, because the cost of learning is to look at a book or a series from entry to mastery.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">In general, it is possible to jump out of this trend! </span><span style="vertical-align: inherit;">Explain that ORM is a routine, innovative, and requires art cells.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Once, I also had a very simple and traditional ORM called XQData:</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">I created it in 2009, and found that I am still lying on the hard disk, and I will openly share it with open source to the small partners who have not made ORM.</span></span></p>
<p><strong><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">XQData source code (SVN download) address: http://code.taobao.org/svn/cyqopen/trunk/XQData</span></span></strong></p>
<h1><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">The automated framework thinking of CYQ.Data:</span></span></h1>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">In the early version of CYQ.Data (not too early to say), compared with the traditional entity ORM, in addition to eclectic, it seems a bit tide, value encouragement and attention, it does not feel cool where it is used .</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">With the formation of the automation framework thinking, after years of improvement, today, the gap with the physical ORM is not at the same level.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">First look at the way the entity ORM code is written: the entity inherits from CYQ.Data.Orm.OrmBase</span></span></p>
<div class="cnblogs_code">
<pre> <span style="color: #0000ff;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Using</span></span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;"> (Users u = </span></span><span style="color: #0000ff;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">new </span></span></span><span style="color: #000000;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Users())</span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">
{</span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">
            u.Name</span></span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;"> = </span></span><span style="color: #800000;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">" </span></span></span><span style="color: #800000;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">passing the fall </span></span></span><span style="color: #800000;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">" </span></span></span><span style="color: #000000;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">;</span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">
            u.TypeID</span></span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;"> = Request["typeid"] </span></span><span style="color: #000000;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">;
             </span></span></span><span style="color: #008000;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">// </span></span></span><span style="color: #008000;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">.... </span></span></span>
<span style="color: #000000;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">            u.Insert();</span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">
 }</span></span></span></pre>
</div>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">It looks very simple, isn't it? </span><span style="vertical-align: inherit;">It is indeed, but it is too fixed, not smart enough, once written, it is a pair of heavenly connections.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Why do I recommend MAction? </span><span style="vertical-align: inherit;">Because it has an automated framework thinking:</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Look at the following code:</span></span></p>
<div class="cnblogs_code">
<pre><span style="color: #0000ff;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Using</span></span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;"> (MAction action = </span></span><span style="color: #0000ff;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">new </span></span></span><span style="color: #000000;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">MAction(TableNames.Users))</span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">
{</span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">
    action.Insert( </span></span></span><span style="color: #0000ff;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">true </span></span></span><span style="color: #000000;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">);//There is no single assignment process in the middle</span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">
}</span></span></span></pre>
</div>
<h3><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Comparing the code, you can see the advantages:</span></span></h3>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">1: The code is less, there is no intermediate assignment process;</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">2: No dependency on attributes and database fields: no matter whether you modify the interface or modify the database, the background code is not adjusted;</span></span></p>
<h3><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">If you increase the switch table operation and transaction, then there are two more advantages:</span></span></h3>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">1: Entity ORM: Code segments can only be included with distributed transactions, and links cannot be reused.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">2: MAction: You can use local transactions, you can reuse links.</span></span></p>
<p><strong><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">The above MAction code, there is a TableNames.Users table name dependency, if you turn it into a parameter, you will find a different sky:</span></span></strong></p>
<div class="cnblogs_code">
<pre><span style="color: #0000ff;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Using</span></span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;"> (MAction action = </span></span><span style="color: #0000ff;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">new </span></span></span><span style="color: #000000;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">MAction)</span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">
{</span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">
     action.Insert( </span></span></span><span style="color: #0000ff;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">true </span></span></span><span style="color: #000000;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">);</span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">
}</span></span></span></pre>
</div>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">With just two lines of code, you find that it is completely decoupled from the database and interface.</span></span></p>
<h3><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Here you find that this is where the framework and the solid ORM are not at a level:</span></span></h3>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">1: Because it implements the true decoupling of the data layer and the UI layer.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">2: Because it is based on the thinking of automated framework programming, there is no longer a process of attribute assignment.</span></span></p>
<h3><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Seeing this, and then looking back at the AjaxBase in the ASP.NET Aries open source framework, you can understand that the total code in the background is able to handle the automatic processing of arbitrary tables and data:</span></span></h3>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">The following method only needs to pass a table name (+ corresponding data) to the front page:</span></span></p>
<p><img src="http://images2015.cnblogs.com/blog/17408/201607/17408-20160702010930781-1983971376.jpg" alt="" /></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">If you further configure the table name in the Url menu field in the database, then an automated page is formed:</span></span></p>
<p><img src="http://images2015.cnblogs.com/blog/17408/201607/17408-20160702013410921-777449644.jpg" alt="" /></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">And these automatic automation framework programming thinking, are not possessed by the physical ORM, the entity ORM can only play a small bunch of code for a certain interface of a bunch of code.</span></span></p>
<h1><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Look at an API interface design:</span></span></h1>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Assume that there is an App project, there are Android version and IOS, they all need to call the background API. At this time, how do you design?</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Don't move, wait for the App product manager to finalize the interface prototype, and then what elements are needed for the App interface, discuss with the development app development engineer, and then write the method for the request?</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">After all, you have to know which table to read and which data to check, so you can only passively? </span><span style="vertical-align: inherit;">Every time you add a page or feature, you have to go to the background to write a bunch of business logic code, and then joint debugging?</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Is it particularly tired?</span></span></p>
<h3><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Looking at the direct use of this framework, the process of your design will become simple, elegant and abstract:</span></span></h3>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Interface core code:</span></span></p>
<div class="cnblogs_code">
<pre> <span style="color: #0000ff;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Using</span></span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;"> (MAction action = </span></span><span style="color: #0000ff;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">new </span></span></span><span style="color: #000000;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">MAction(tableName))</span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">
{</span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">
     action.Select(pageIndex, pageSize, </span></span></span><span style="color: #0000ff;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">where </span></span></span><span style="color: #000000;"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">).ToJson();</span></span><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">
}</span></span></span></pre>
</div>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">The next thing you want to design is:</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">1: Format the client request parameters for the app: {key:'xx',pageindex:1,pagesize:10,wherekey:'xxxx'}</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">2: Put the table name mapping into the database (Key, Value), the App only passes the Key when requesting the name</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">3: According to the actual business, construct the where condition.</span></span></p>
<h3><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Design a few of these common interfaces, and give them to the app developer to see what advantages they have:</span></span></h3>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">1: Can reduce a lot of communication costs.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">2: The design of the API is universal, reducing a lot of code, and subsequent maintenance is simple and configurable.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">3: You can start work from the beginning, you don't have to wait until the App prototype starts.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">4: Whether the continuous table exists or not, can be used in advance, and can be configured in the later stage.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">5: After implementing a set, you can use the company for business change, because your design is decoupled from the specific business.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Imagine changing to an entity ORM. Do you have to have a database in advance to generate a bunch of entities, and then the specific business continues to be New instance, the limitations of thinking can only be limited to specific business.</span></span></p>
<h1><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">The abstract thinking of the framework and the intelligent derivation of the where condition</span></span></h1>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Look at a picture first:</span></span></p>
<p><img src="http://images2015.cnblogs.com/blog/17408/201607/17408-20160702021644374-99284450.jpg" alt="" /></p>
<h2><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">For the common data addition, deletion and change operations of the table, as can be seen from the above figure, the framework finally abstracts two core parameters:</span></span></h2>
<h3><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Table name +where condition:</span></span></h3>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">I once thought about syntactic sugar, whether to design the piece of Where as: .Select(...).Where(...).Having(...).GroupBy(...).OrderBy(.. .)...</span></span></p>
<h3><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Later, I still insisted on keeping my heart:</span></span></h3>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">1: Developers have no learning costs.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">2: Maintain the youthful creativity of the frame.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">3: Have an automated framework thinking.</span></span></p>
<h3><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">The downside of syntactic sugar:</span></span></h3>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">1: The complexity of the frame's own complex design increases.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">2: The user has high learning costs and increased usage complexity.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">3: Not suitable for automated extension: design has been an expression, can not dynamically construct query conditions dynamically based on a key and table! </span><span style="vertical-align: inherit;">Only suitable for specific examples and business, not suitable for automated programming.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Of course, in most of the Where conditions, many are based on the conditions of the primary key or the unique key. In order to further abstract and adapt to the automation programming, I have designed a self-powered derivation mechanism.</span></span></p>
<h2><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Intelligent derivation for where:</span></span></h2>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Look at the following two codes: the left is where the construct is relatively complete, and the one on the right can automatically derive where. </span><span style="vertical-align: inherit;">(There is anti-SQL injection inside, so don't worry about where condition injection problem).</span></span></p>
<p><img src="http://images2015.cnblogs.com/blog/17408/201607/17408-20160702025039406-750125499.jpg" alt="" /></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Through intelligent derivation, the primary key name parameter is removed (because the primary key table of different tables is different), intelligent derivation is generated, which allows the programmer to mainly care about the value passed, without paying attention to the specific primary key name.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">If the value is a comma-separated multivalue of "1, 2, 3", the framework automatically derives the condition into the primary key in (1, 2, 3).</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Look at the two sets of code: the left is still relatively complete where conditions, the right is intelligent derivation programming.</span></span></p>
<p><img src="http://images2015.cnblogs.com/blog/17408/201607/17408-20160702025059984-1561140189.jpg" alt="" /></p>
<p><strong><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Note: The same is the value, but we want UserName, not the primary key, the system can also be derived?</span></span></strong></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">At this time, the system will comprehensively analyze the type according to the type of the value, the primary key, and the unique key. It is found that the value should be constructed with the primary key or the unique key.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">(PS: Unique key derivation is a feature that was completed yesterday, so only the latest version is available.)</span></span></p>
<p><strong><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Because the framework has intelligent derivation function, the difference between the fields is shielded, so that the user only needs to pay attention to the value. </span><span style="vertical-align: inherit;">It is also an important feature that allows you to implement automated framework programming thinking.</span></span></strong></p>
<h1><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Automated batch programming:&nbsp;</span></span></h1>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Look at a picture: MDataTable: It can directly generate batch conversions with various data types:</span></span></p>
<p><img src="http://images2015.cnblogs.com/blog/17408/201607/17408-20160702113130234-1502127877.jpg" alt="" /></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">MDataTable is one of the core of the framework, and the previous article has an exclusive introduction to it.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Of course, the construction of Table is often based on rows, so look at a picture: MDataRow (it is the core of single-line data)</span></span></p>
<p><img src="http://images2015.cnblogs.com/blog/17408/201607/17408-20160702112326359-181995847.jpg" alt="" /></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">In fact, because MDataRow opened up a batch of data in a single line, it created a batch processing of multi-line data of MDataTable.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">In fact, MDataRow is the core implementation layer, but it is relatively low-key.</span></span></p>
<h1><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">Supplement important address:</span></span></h1>
<p><strong><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">1: Source SVN address: </span></span><a href="https://github.com/cyq1162/cyqdata" target="_blank"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">https://github.com/cyq1162/cyqdata.git</span></span></a></strong></p>
<p><strong><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">2: Project Demo example SVN address: </span></span><a href="http://code.taobao.org/p/cyqopen/src/trunk/CYQ.Data.GettingStarted/" target="_blank"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">https://github.com/cyq1162/CYQ.Data.Demo</span></span></a></strong></p>
<p><strong><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">3: Framework download address:</span></span></strong></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">1: VS high version: search for cyqdata on Nuget</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">2: VS low version: </span></span><a href="http://www.cyqdata.com/download/article-detail-426" target="_blank"><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">http://www.cyqdata.com/download/article-detail-426</span></span></a></p>
<h1><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">to sum up:</span></span></h1>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">&nbsp;When using framework programming, you will find more concern about the flow of data and how to build a configuration system for abstract parameters.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">&nbsp;In most of the programming time, in addition to the specific field meaning requires specific attention, most of them are based on automated programming thinking, data flow to thinking.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">&nbsp;Early series: Without such programming thinking, it is inevitable that after reading the introduction, there will be a sense of violation.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">&nbsp;Today's system: automation framework programming thinking, is also the reason for the high user loyalty, especially after free.</span></span></p>
<p><span style="vertical-align: inherit;"><span style="vertical-align: inherit;">&nbsp;Of course, the follow-up will also rewrite the tutorial for this series, and the tutorial source code will be updated to SVN, so stay tuned.</span></span></p>
