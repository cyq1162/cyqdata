using System.Text;
namespace CYQ.Data.SQL
{
    /// <summary>
    /// 分页存储过程语句类
    /// </summary>
    internal partial class SqlCreateForPager
   {
       #region 分页存储过程
       /// <summary>
       /// 获取sql2005分页存储过程[输出带"br换行标签"]
       /// </summary>
       public static string GetSelectBaseForSql2005()
       {
           return @"Create procedure [SelectBase] 
@PageIndex         int, 
@PageSize      int, 
@TableName    nvarchar(4000), 
@Where     nvarchar(max)='' 
as 
Declare @rowcount    int 
Declare @intStart    int 
Declare @intEnd         int 
Declare @SQl nvarchar(max), @WhereR nvarchar(max), @OrderBy nvarchar(max) 
set @rowcount=0 
set nocount on 
if @Where<>'' 
begin 
set @Where=' and '+@Where 
end 
if CHARINDEX('order by', @Where)>0 
begin 
set @WhereR=substring(@Where, 1, CHARINDEX('order by',@Where)-1) --取得条件 
set @OrderBy=substring(@Where, CHARINDEX('order by',@Where), Len(@Where)) --取得排序方式(order by 字段 方式) 
end 
else 
begin 
set @WhereR=@Where 
set @OrderBy=' order by id asc' 
end 
set @SQl='SELECT @rowcount=count(*) from '+cast(@TableName as varchar(4000))+' where 1=1 '+@WhereR 
exec sp_executeSql @SQl,N'@rowcount int output',@rowcount output 
if @PageIndex=0 and @PageSize=0 --不进行分页,查询所有数据列表 
begin 
set @SQl='SELECT * from '+cast(@TableName as varchar(4000))+' where 1=1 '+@Where 
end 
else --进行分页查询数据列表 
begin 
set @intStart=(@PageIndex-1)*@PageSize+1; 
set @intEnd=@intStart+@PageSize-1 
set @SQl='select * from(select *,ROW_NUMBER() OVER('+cast(@OrderBy as nvarchar(400))+') as cyqrownum from ' 
set @SQl=@SQL+@TableName+' where 1=1 '+@WhereR+') as a where cyqrownum between '+cast(@intStart as varchar)+' and '+cast(@intEnd as varchar) 
end 
exec sp_executesql @SQl 
return @rowcount 
set nocount off";
       }
       /*
        /// <summary>
       /// 获取sql2000分页存储过程[输出带"br换行标签"]
       /// </summary>
       public static string GetSelectBaseForSql2000()
       {
           bool isForSybase = false;
           string executeCount= isForSybase ? "execute(@Sql)" : "exec sp_executeSql @Sql,N'@rowcount int output',@rowcount output";
           string executeSql = isForSybase ? "execute(@Sql)" : "exec sp_executeSql @Sql ";
           return @"Create procedure [dbo].[SelectBase] 
@PageIndex     int, 
@PageSize      int, 
@TableName     varchar(4000), 
@Where         varchar(2000)='' 
as 
Declare @rowcount    int
Declare @Sql nvarchar(4000), @WhereOnly varchar(1000),@OrderbyColumn varchar(64)
set @rowcount=0 
set nocount on
if @Where<>'' 
   begin 
  set @Where=' where '+@Where
  if CHARINDEX('order by', @Where)>0 
     begin 
      set @rowcount=CHARINDEX('order by',@Where)
      set @WhereOnly=substring(@Where, 1, @rowcount-1) 
      set @OrderbyColumn=RTRIM(LTRIM(substring(@Where,@rowcount+9,64)))
      set @rowcount=CHARINDEX(' ',@OrderbyColumn)
     if @rowcount>0
    begin
         if CHARINDEX('+',@OrderbyColumn)=0
         begin
         set @OrderbyColumn=substring(@OrderbyColumn,1,@rowcount-1)
         end
         else set @OrderbyColumn=''    end
     end 
     else 
     begin 
      set @WhereOnly=@Where 
     end 
     end 
 else
 begin
  set @WhereOnly=''
 end

set @Sql='SELECT @rowcount=count(*) from '+cast(@TableName as varchar(4000))+@WhereOnly
exec sp_executeSql @Sql,N'@rowcount int output',@rowcount output
if @PageIndex=0 and @PageSize=0
   begin 
  set @Sql='SELECT * from '+cast(@TableName as varchar(4000))+@Where 
   end 
   else
   begin 
  Declare @intStart int 
  Declare @intEnd   int 
  declare @Column1 varchar(64)
   
  if CHARINDEX('(', @TableName)>0 
  begin 
   
   set @TableName=reverse(@TableName)
   set @TableName=reverse(substring(@TableName,charindex(')',@TableName),4000))+' t '
   if @OrderbyColumn<>''  begin set @Column1=@OrderbyColumn end
   else begin set @Column1='ID' end
  end
  else
  begin
   set @Column1=col_name(object_id(@TableName),1)
   set @TableName=@TableName+' t '
  end
    set @intStart=(@PageIndex-1)*@PageSize+1
    set @intEnd=@intStart+@PageSize-1
    set @Sql='Create table #tem(tempID int identity(1,1) not null,cyqrownum varchar(64)) '  
    set @Sql=@Sql+'insert #tem(cyqrownum) select cast('+@Column1+' as varchar(64)) from '+@TableName+@Where  
    set @Sql=@Sql+' select t.* from #tem left join '+@TableName+' on cast('+@Column1+' as varchar(64))=#tem.cyqrownum '  
    set @Sql=@Sql+' where  #tem.tempID between '+cast(@intStart as varchar)+' and '+cast(@intEnd as varchar)+' order by #tem.tempID asc' 
    set @Sql=@Sql+' drop table #tem' 
   end 
exec sp_executeSql @Sql 
return @rowcount 
set nocount off";
       }
      
       //public static string GetSelectBaseOutPutToHtmlForOracle()
       //{
       //    return GetPackageHeadForOracle() + GetPackageBodyForOracle();
       //}
       
       public static string GetPackageHeadForOracle()
       {
           return  @"
           create or replace package MyPackage as 
type MyCursor is ref cursor;
procedure SelectBase(pageIndex int,pageSize int,tableName varchar2,whereStr varchar2,
  resultCount out int, resultCursor out MyCursor);
end MyPackage;";
       }
       public static string GetPackageBodyForOracle()
       {
           return @"
create or replace package Body MyPackage is 
 procedure SelectBase(pageIndex int,pageSize int,tableName varchar2,whereStr varchar2,
  resultCount out int, resultCursor out MyCursor)
  is
  rowStart  int;
  rowEnd    int;
  mySql varchar2(8000);
  whereOnly varchar2(8000);
  OrderOnly varchar2(400);
  begin
    mySql:='select count(*) from '||tableName;
    whereOnly:=whereStr;
    rowStart:=instr(whereStr,'order by');
    if whereStr is not null and  rowStart>0 
      then
        whereOnly:=substr(whereStr, 1,rowStart-1);
        OrderOnly:=substr(whereStr,rowStart, length(whereStr)-rowStart+1);
        end if;  
    if length(whereOnly)>1
      then
       whereOnly:=' where '|| whereOnly;
       mySql:=mySql||whereOnly;
        end if;
        execute immediate mySql into resultCount;

        if pageIndex=0 and pageSize=0	
        then 
        mySql:='select * from '||tableName||whereOnly||OrderOnly;
       else

        rowStart:=(pageIndex-1)*pageSize+1; 
        rowEnd:=rowStart+pageSize-1;
        mySql:='select * from (select t.*,RowNum as cyqrownum from ('||tableName||') t'||whereOnly||OrderOnly||') where cyqrownum between '||rowStart||' and '||rowEnd; 
        end if;
    open ResultCursor for mySql;
    end SelectBase;
  end MyPackage;";
       }
         * */
       #endregion
   }
}
