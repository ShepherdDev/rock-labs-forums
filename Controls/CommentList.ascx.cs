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

using com.rocklabs.Forums;
using com.rocklabs.Forums.Model;

namespace RockWeb.Plugins.com_rocklabs.Forums
{
    [DisplayName( "Comment List" )]
    [Category( "Rock Labs > Forums" )]
    [Description( "Displays existing comments and allows entry of new comments for a forum topic." )]

    [ContextAware( typeof( ForumTopic ) )]
    [CodeEditorField( "Comment Template", "The lava template to use when displaying comments.", CodeEditorMode.Lava, height: 400, order: 0 )]
    [BinaryFileTypeField( "Binary File Type", "The storage type to use for uploaded files and images in comments.", false, "", order: 1 )]
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

            RockPage.AddCSSLink( "~/Plugins/com_rocklabs/Forums/Styles/bootstrap-markdown-editor.css" );
            RockPage.AddScriptLink( "~/Scripts/ace/ace.js" );
            RockPage.AddScriptLink( "~/Plugins/com_rocklabs/Forums/Scripts/bootstrap-markdown-editor.js" );

            //
            // Verify that the user is allowed to make edits to the topic in question.
            //
            _canAddEditDelete = IsUserAuthorized( Authorization.EDIT );
            if ( _canAddEditDelete && ContextEntity<ForumTopic>() != null )
            {
                var project = ContextEntity<ForumTopic>();

                if ( project != null )
                {
                    _canAddEditDelete = project.IsAuthorized( Authorization.EDIT, CurrentPerson );
                }
            }

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
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
                var topic = ContextEntity<ForumTopic>();

                if ( topic != null && topic.IsAuthorized( Authorization.VIEW, CurrentPerson ) )
                {
                    var globalAttributesCache = GlobalAttributesCache.Read();

                    btnReply.Visible = _canAddEditDelete;
                    meNewComment.PublicApplicationRoot = globalAttributesCache.GetValue( "PublicApplicationRoot" );
                    meNewComment.BinaryFileTypeGuid = GetAttributeValue( "BinaryFileType" ).AsGuidOrNull();

                    pnlCommentList.Visible = true;
                    ShowComments();
                }
            }
            else
            {
                var topic = ContextEntity<ForumTopic>();

                if ( topic != null )
                {
                    ShowComments();
                }
            }
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Show all the comments on this topic.
        /// </summary>
        private void ShowComments()
        {
            int? topicEntityTypeId = EntityTypeCache.GetId( typeof( ForumTopic ) );
            var topic = ContextEntity<ForumTopic>();

            if ( topic != null )
            {
                //using ( var rockContext = new RockContext() )
                var rockContext = new RockContext();
                {
                    var notes = new NoteService( rockContext ).Queryable()
                        .Where( n => n.NoteType.EntityTypeId == topicEntityTypeId.Value && n.EntityId == topic.Id )
                        .OrderBy( n => n.CreatedDateTime ).ToList();

                    var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( RockPage, CurrentPerson );
                    mergeFields.Add( "Notes", notes );

                    lComments.Text = GetAttributeValue( "CommentTemplate" ).ResolveMergeFields( mergeFields );
                }
            }
            else
            {
                lComments.Text = string.Empty;
            }
        }


        /// <summary>
        /// Sets the visibility of this block when requested by other blocks.
        /// </summary>
        /// <param name="visible">true if this block should be visible.</param>
        void ISecondaryBlock.SetVisible( bool visible )
        {
            pnlCommentList.Visible = visible;
        }

        /// <summary>
        /// Adds a following record on the topic for the specified person. Checks if
        /// one is needed first.
        /// </summary>
        /// <param name="topicId">The topic identifier to be followed.</param>
        /// <param name="personAliasId">The person who is to follow the project.</param>
        /// <param name="rockContext">The RockContext to work in. Changes are not saved by this method.</param>
        public void FollowTopic( int topicId, int personAliasId, RockContext rockContext )
        {
            int entityTypeId = EntityTypeCache.Read( typeof( ForumTopic ) ).Id;

            rockContext = rockContext ?? new RockContext();
            var followingService = new FollowingService( rockContext );
            var personId = new PersonAliasService( rockContext ).Get( personAliasId ).PersonId;

            bool isFollowing = followingService.Queryable()
                .Where( f => f.EntityTypeId == entityTypeId && f.EntityId == topicId && f.PersonAlias.Person.Id == personId )
                .Any();

            if ( !isFollowing )
            {
                var person = new PersonService( rockContext ).Get( personId );

                if ( person != null )
                {
                    var following = new Following { Id = 0 };
                    followingService.Add( following );

                    following.EntityTypeId = entityTypeId;
                    following.EntityId = topicId;
                    following.PersonAliasId = person.PrimaryAliasId.Value;
                    following.CreatedDateTime = RockDateTime.Now;
                }
            }
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
        protected void btnComment_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var noteService = new NoteService( rockContext );
            var followingService = new FollowingService( rockContext );
            var binaryFileService = new BinaryFileService( rockContext );
            var topic = new ForumTopicService( rockContext ).Get( ( ContextEntity<ForumTopic>() ).Id );
            var note = new Note { Id = 0 };
            int noteTypeId = NoteTypeCache.Read( com.rocklabs.Forums.SystemGuid.NoteType.FORUM_TOPIC_COMMENT.AsGuid() ).Id;

            //
            // Add the note to the database.
            //
            noteService.Add( note );
            note.NoteTypeId = noteTypeId;
            note.EntityId = topic.Id;
            note.Text = meNewComment.Text;
            note.CreatedByPersonAliasId = CurrentPersonAliasId;

            //
            // Check each file they uploaded while writing this note. If it is referenced in the
            // note text then mark the binary file as permanent.
            //
            foreach ( var hfId in meNewComment.UploadedFileIds )
            {
                var binaryFile = binaryFileService.Get( hfId );

                if ( binaryFile != null && note.Text.Contains( string.Format( "/GetFile.ashx?Id={0})", hfId ) ) )
                {
                    binaryFile.IsTemporary = false;
                }
            }

            //
            // Save the comment, and if they are not already following then start following.
            //
            rockContext.WrapTransaction( () =>
            {
                rockContext.SaveChanges();

                if ( noteService.Queryable().Count( n => n.NoteTypeId == noteTypeId && n.EntityId == topic.Id ) == 1 )
                {
                    FollowTopic( topic.Id, CurrentPersonAliasId.Value, rockContext );

                    rockContext.SaveChanges();
                }
            } );

            //SendCommentNotification( topic.Id, CurrentPersonAliasId, note.Id );

            ShowComments();

            meNewComment.UploadedFileIds = null;
            meNewComment.Text = string.Empty;

            pnlReply.Visible = false;
            btnReply.Visible = true;
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnReply_Click( object sender, EventArgs e )
        {
            btnReply.Visible = false;
            pnlReply.Visible = true;
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            meNewComment.UploadedFileIds = null;
            meNewComment.Text = string.Empty;

            btnReply.Visible = true;
            pnlReply.Visible = false;
        }

        #endregion
    }
}