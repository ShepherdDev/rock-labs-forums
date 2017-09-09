using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

using com.blueboxmoon.ProjectManagement;
using com.blueboxmoon.ProjectManagement.Model;

namespace RockWeb.Plugins.com_blueboxmoon.ProjectManagement
{
    [DisplayName( "Comment List" )]
    [Category( "Blue Box Moon > Project Management" )]
    [Description( "Displays existing allows entry of new comments for a project." )]

    [ContextAware( typeof( Project ) )]
    public partial class CommentList : RockBlock, ISecondaryBlock
    {
        #region Private Fields

        private bool _canAddEditDelete;

        #endregion

        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddCSSLink( "~/Plugins/com_blueboxmoon/ProjectManagement/Styles/project-management.css" );
            RockPage.AddCSSLink( "~/Plugins/com_blueboxmoon/ProjectManagement/Styles/bootstrap-markdown-editor.css" );
            RockPage.AddScriptLink( "~/Scripts/ace/ace.js" );
            RockPage.AddScriptLink( "~/Plugins/com_blueboxmoon/ProjectManagement/Scripts/bootstrap-markdown-editor.js" );

            //
            // Verify that the user is allowed to make edits to the project in question.
            //
            _canAddEditDelete = IsUserAuthorized( Authorization.EDIT );
            if ( _canAddEditDelete && ContextEntity<Project>() != null )
            {
                var project = ContextEntity<Project>();

                if ( project != null )
                {
                    _canAddEditDelete = project.IsAuthorized( Authorization.EDIT, CurrentPerson );
                }
            }

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            EventHelper.GetHelper().ProjectCommentsChanged += ProjectCommentsChanged;
        }

        /// <summary>
        /// Initialize basic information about the page structure and setup the default content.
        /// </summary>
        /// <param name="sender">Object that is generating this event.</param>
        /// <param name="e">Arguments that describe this event.</param>
        protected void Page_Load( object sender, EventArgs e )
        {
            if ( !IsPostBack )
            {
                var project = ContextEntity<Project>();

                if ( project != null && project.IsAuthorized( Authorization.VIEW, CurrentPerson ) )
                {
                    var globalAttributesCache = GlobalAttributesCache.Read();

                    lbComment.Visible = _canAddEditDelete;
                    meNewComment.Visible = _canAddEditDelete;
                    meNewComment.PublicApplicationRoot = globalAttributesCache.GetValue( "PublicApplicationRoot" );
                    if ( project.ProjectType.BinaryFileType != null )
                    {
                        meNewComment.BinaryFileTypeGuid = project.ProjectType.BinaryFileType.Guid;
                    }

                    pnlCommentList.Visible = true;
                    ShowComments();
                }
            }
            else
            {
                var project = ContextEntity<Project>();

                if ( project != null )
                {
                    ShowComments();
                }
            }
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Bind the repeater to the list of projects in the project.
        /// </summary>
        private void ShowComments()
        {
            var project = ContextEntity() as Project;

            ccComments.ProjectId = project.Id;
            ccComments.RebuildNotes();
        }

        /// <summary>
        /// Sets the visibility of this block when requested by other blocks.
        /// </summary>
        /// <param name="visible">true if this block should be visible.</param>
        void ISecondaryBlock.SetVisible( bool visible )
        {
            pnlCommentList.Visible = visible;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowComments();
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAdd_Click( object sender, EventArgs e )
        {
            Project project = ContextEntity() as Project;

            if ( project != null )
            {
            }
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbComment_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var noteService = new NoteService( rockContext );
            var followingService = new FollowingService( rockContext );
            var binaryFileService = new BinaryFileService( rockContext );
            var attachmentService = new AttachmentService( rockContext );
            var project = new ProjectService( rockContext ).Get( ( ContextEntity() as Project ).Id );
            var note = new Note { Id = 0 };

            //
            // Add the note to the database.
            //
            noteService.Add( note );
            note.NoteTypeId = NoteTypeCache.Read( com.blueboxmoon.ProjectManagement.SystemGuid.NoteType.PROJECT_USER_COMMENT.AsGuid() ).Id;
            note.EntityId = project.Id;
            note.Text = meNewComment.Text;
            note.CreatedByPersonAliasId = CurrentPersonAliasId;

            //
            // Check each file they uploaded while writing this note. If it is referenced in the
            // note text then attach it to the project and mark the binary file as permanent.
            //
            foreach ( var hfId in meNewComment.UploadedFileIds )
            {
                var binaryFile = binaryFileService.Get( hfId );

                if ( binaryFile != null && note.Text.Contains( string.Format( "/GetFile.ashx?Id={0})", hfId ) ) )
                {
                    binaryFile.IsTemporary = false;

                    var attachment = new Attachment { Id = 0 };
                    project.Attachments.Add( attachment );
                    attachment.CreatedByPersonAliasId = CurrentPersonAliasId;
                    attachment.CreatedDateTime = RockDateTime.Now;
                    attachment.BinaryFileId = binaryFile.Id;
                }
            }

            //
            // Save the comment, and if they are not already following then start following.
            //
            rockContext.WrapTransaction( () =>
            {
                rockContext.SaveChanges();
                Project.WatchProject( project.Id, CurrentPersonAliasId.Value, false, rockContext, true );
            } );

            Project.SendCommentNotification( project.Id, CurrentPersonAliasId, note.Id );
            EventHelper.GetHelper().PostProjectCommentsChanged( this, project.Id );

            ShowComments();

            meNewComment.UploadedFileIds = null;
            meNewComment.Text = string.Empty;
        }

        /// <summary>
        /// Handles the ProjectCommentsChanged event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ProjectEventArgs"/> instance containing the event data.</param>
        private void ProjectCommentsChanged( object sender, ProjectEventArgs e )
        {
            var project = ContextEntity<Project>();

            if ( sender != this && e.ProjectId.HasValue && project != null && e.ProjectId.Value == project.Id )
            {
                ShowComments();
            }
        }

        #endregion
    }
}