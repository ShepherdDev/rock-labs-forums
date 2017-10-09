<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CategoryListLava.ascx.cs" Inherits="RockWeb.Plugins.com_rocklabs.Forums.CategoryListLava" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbNotConfigured" runat="server" NotificationBoxType="Warning">
            <i class="fa fa-2x fa-meh-o"></i> Oops! Looks like you haven't configured this block yet.
        </Rock:NotificationBox>

        <asp:Panel ID="pnlCategories" runat="server">
            <asp:Literal ID="lOutput" runat="server"></asp:Literal>
        </asp:Panel>

        <Rock:ModalDialog ID="mdlSettings" runat="server" Title="Settings" OnSaveClick="mdlSettings_SaveClick" ValidationGroup="Settings">
            <Content>
                <Rock:EntityTypePicker ID="pEntityType" runat="server" Label="Entity Type" Help="Display categories for the selected entity type." Required="true" OnSelectedIndexChanged="pEntityType_SelectedIndexChanged" AutoPostBack="true" />

                <Rock:CategoryPicker ID="pDefaultCategory" runat="server" Label="Default Category" Help="The default category to use as a root if nothing is provided in the query string. If not provided all root-level categories will be used." Required="false" Visible="false" />

                <Rock:PagePicker ID="pDetailPage" runat="server" Label="Detail Page" Help="Page reference to use for the detail page." Required="false" />

                <Rock:CodeEditor ID="ceLavaTemplate" runat="server" Label="Template" Help="Lava template to use to display the categories." Required="true" EditorMode="Lava" EditorTheme="Rock" EditorHeight="400" />
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>
