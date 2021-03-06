﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CommentList.ascx.cs" Inherits="RockWeb.Plugins.com_rocklabs.Forums.CommentList" %>
<%@ Register Namespace="com.rocklabs.Forums.UI" Assembly="com.rocklabs.Forums" TagPrefix="RLF" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlCommentList" runat="server">
            <asp:Literal ID="lComments" runat="server" />

            <asp:Panel ID="pnlBottomActions" runat="server">
                <asp:LinkButton ID="btnReply" runat="server" CssClass="btn btn-primary" OnClick="btnReply_Click" Visible="false"><i class="fa fa-reply"></i> Reply</asp:LinkButton>
                <asp:LinkButton ID="btnToggleSubscribe" runat="server" OnClick="btnToggleSubscribe_Click" CssClass="btn btn-default" />
            </asp:Panel>

            <asp:Panel ID="pnlReply" runat="server" Visible="false">
                <RLF:MarkdownEditor ID="meNewComment" runat="server" CssClass="margin-b-md" />
                <asp:LinkButton ID="btnComment" runat="server" CssClass="btn btn-primary" Text="Comment" OnClick="btnComment_Click" />
                <asp:LinkButton ID="btnCancel" runat="server" CssClass="btn btn-link" Text="Cancel" OnClick="btnCancel_Click" />
            </asp:Panel>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
