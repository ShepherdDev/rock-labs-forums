<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ForumTopicListLava.ascx.cs" Inherits="RockWeb.Plugins.com_rocklabs.Forums.ForumTopicListLava" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbNotConfigured" runat="server" NotificationBoxType="Warning" Visible="false">
            <i class="fa fa-2x fa-meh-o"></i> Oops! Looks like you haven't configured this block yet.
        </Rock:NotificationBox>

        <asp:Panel ID="pnlCategories" runat="server">
            <asp:Literal ID="lOutput" runat="server"></asp:Literal>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
