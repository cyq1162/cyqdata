<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="MutilLanguage_Demo.Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head id="Head1" runat="server">
    <title>无标题页</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:Button ID="btnChina" runat="server" Text="中文输出" OnClick="btnChina_Click" />
        <asp:Button ID="btnEnglish" runat="server" Text="英文输出" OnClick="btnEnglish_Click" />
        <asp:Button ID="btnCustom" runat="server" Text="自定义输出" OnClick="btnCustom_Click" />
       <p>html：<%=lang.Get("autumn") %></p>
       <p>cs ：<asp:Label ID="labUrl" runat="server" Text=""></asp:Label></p>
    </div>
    </form>
</body>
</html>
