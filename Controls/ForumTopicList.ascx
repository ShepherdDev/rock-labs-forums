<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ForumTopicList.ascx.cs" Inherits="RockWeb.Plugins.com_rocklabs.Forums.ForumTopicList" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbNotConfigured" runat="server" NotificationBoxType="Warning" Visible="false">
            <i class="fa fa-2x fa-meh-o"></i> Oops! Looks like you haven't configured this block yet.
        </Rock:NotificationBox>

        <Rock:Grid ID="gTopics" runat="server" AllowPaging="true" AllowSorting="true" OnRowSelected="gTopics_RowSelected" RowItemText="Topic" ShowActionRow="false" DataKeyNames="Id">
            <Columns>
                <Rock:RockBoundField HeaderText="Topic" DataField="Name" SortExpression="Name" ColumnPriority="AlwaysVisible"></Rock:RockBoundField>
                <Rock:RockBoundField HeaderText="Author" DataField="Author.FullName" SortExpression="Author.FullName" ColumnPriority="TabletSmall"></Rock:RockBoundField>
                <Rock:DateTimeField HeaderText="Posted" DataField="PostedDate" SortExpression="PostedDate" ColumnPriority="DesktopSmall"></Rock:DateTimeField>
                <Rock:DateTimeField HeaderText="Last Post" DataField="LastPost.CreatedDateTime" SortExpression="LastPost.CreatedDateTime" ColumnPriority="Desktop"></Rock:DateTimeField>
                <Rock:RockBoundField HeaderText="Replies" DataField="ReplyCount" SortExpression="ReplyCount" ColumnPriority="Desktop"></Rock:RockBoundField>
            </Columns>
        </Rock:Grid>
        <asp:Panel ID="pnlCategories" runat="server">
            <asp:Literal ID="lOutput" runat="server"></asp:Literal>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
