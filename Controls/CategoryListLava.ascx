<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CategoryListLava.ascx.cs" Inherits="RockWeb.Plugins.com_rocklabs.Forums.CategoryListLava" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbNotConfigured" runat="server" NotificationBoxType="Warning">
            <i class="fa fa-2x fa-meh-o"></i> Oops! Looks like you haven't configured this block yet.
        </Rock:NotificationBox>

        <asp:Panel ID="pnlCategories" runat="server">
            <asp:Literal ID="lOutput" runat="server"></asp:Literal>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
