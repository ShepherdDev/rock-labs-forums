<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ForumTopicDetail.ascx.cs" Inherits="RockWeb.Plugins.com_rocklabs.Forums.ForumTopicDetail" %>
<%@ Register Namespace="com.rocklabs.Forums.UI" Assembly="com.rocklabs.Forums" TagPrefix="RL" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbUnauthorized" runat="server" NotificationBoxType="Warning"></Rock:NotificationBox>

        <asp:Panel ID="pnlDetails" CssClass="panel panel-block" runat="server" Visible="false">
            <div class="panel-heading panel-follow clearfix">
                <h1 class="panel-title pull-left">
                    <asp:Literal ID="lTitle" runat="server" />
                </h1>

                <asp:Panel ID="pnlFollowing" runat="server" CssClass="panel-follow-status" data-toggle="tooltip" data-placement="top" title="Click to Follow"></asp:Panel>
            </div>
            <div class="panel-heading clearfix" style="padding-top: 4px; padding-bottom: 4px;">
                <span class="pull-right">
                    <asp:Literal ID="lDatePosted" runat="server" />
                </span>

                <span><asp:Literal ID="lAuthorName" runat="server" /></span>
            </div>

            <div class="panel-body">
                <asp:Literal ID="lDetails" runat="server" />
            </div>
 
            <asp:PlaceHolder ID="phAttributes" runat="server" />

            <div class="actions">
                <asp:LinkButton ID="lbEdit" runat="server" CssClass="btn btn-link" Text="Edit" OnClick="lbEdit_Click" />
                <asp:LinkButton ID="lbDelete" runat="server" CssClass="btn btn-link" Text="Delete" OnClick="lbDelete_Click" OnClientClick="return Rock.dialogs.confirmDelete(event, 'project');" />
            </div>
        </asp:Panel>

        <asp:Panel ID="pnlEdit" CssClass="panel panel-block" runat="server" Visible="false">
            <asp:HiddenField ID="hfId" runat="server" />

            <div class="panel-heading">
                <h1 class="panel-title">
                    <asp:Literal ID="lEditTitle" runat="server" />
                </h1>
            </div>
            <Rock:PanelDrawer ID="pdAuditDetails" runat="server"></Rock:PanelDrawer>
            <div class="panel-body">

                <Rock:NotificationBox ID="nbWarningMessage" runat="server" NotificationBoxType="Warning" />
                <asp:ValidationSummary ID="vSummary" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />

                <div class="row">
                    <div class="col-md-6">
                        <Rock:DataTextBox ID="tbName" runat="server" SourceTypeName="com.rocklabs.Forums.Model.ForumTopic, com.rocklabs.Forums" PropertyName="Name" />
                    </div>
                    <div class="col-md-6">
                    </div>
                </div>

                <div class="row">
                    <div class="col-md-12">
                        <div class="form-group">
                            <label class="control-label" for="<%= meDescription.ClientID %>">Details</label>
                            <RL:MarkdownEditor ID="meDescription" runat="server" CssClass="margin-b-md" />
                        </div>
                    </div>
                </div>

                <div class="row">
                    <div class="col-md-6">
                        <asp:PlaceHolder ID="phEditAttributes" runat="server"></asp:PlaceHolder>
                    </div>
                </div>

                <div class="actions">
                    <asp:LinkButton ID="lbSave" runat="server" Text="Save" CssClass="btn btn-primary" OnClick="lbSave_Click" />
                    <asp:LinkButton ID="lbCancel" runat="server" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="lbCancel_Click" />
                </div>

            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>