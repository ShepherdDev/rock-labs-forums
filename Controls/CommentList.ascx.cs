using System;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_rocklabs.Forums
{
    [DisplayName( "Comment List" )]
    [Category( "Rock Labs > Forums" )]
    [Description( "Displays existing comments and allows entry of new comments for an entity." )]

    [ContextAware]
    [NoteTypeField( "Note Type", "The note type to use when a person leaves a new comment.", order: 0 )]
    [BinaryFileTypeField( "Binary File Type", "The storage type to use for uploaded files and images in comments.", false, "", order: 1 )]
    [BooleanField( "Enforce Entity Security", "If set to true and the user does not have Edit permissions to the entity then they will not be allowed to post replies.", false, order: 2 )]
    [BooleanField( "Follow On First Post", "If set to true then the user will automatically Follow the entity on their first post.", false, order: 3 )]
    [BooleanField( "Show Subscribe Button", "If set to true then a subscribe/unsubscribe button will be shown allowing the user to toggle their subscription status.", false, order: 4 )]
    [SystemEmailField( "Notification Email", "If set, then all users that are following the entity will receive an e-mail when a new comment is left.", false, order: 5 )]
    [CodeEditorField( "Comment Template", "The lava template to use when displaying comments.", CodeEditorMode.Lava, height: 400, order: 6 )]
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
            // Verify that the user is allowed to make edits to the entity in question.
            //
            _canAddEditDelete = IsUserAuthorized( Authorization.EDIT );
            var entity = ContextEntity();
            if ( _canAddEditDelete && entity != null )
            {
                if ( GetAttributeValue( "EnforceEntitySecurity" ).AsBoolean( false ) && entity is ISecured )
                {
                    _canAddEditDelete = ( ( ISecured ) entity ).IsAuthorized( Authorization.EDIT, CurrentPerson );
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
                var entity = ContextEntity();
                var canView = true;

                if ( entity is ISecured )
                {
                    canView = ( ( ISecured ) entity ).IsAuthorized( Authorization.VIEW, CurrentPerson );
                }

                if ( entity != null && canView )
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
                ShowComments();
            }
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Show all the comments on this entity.
        /// </summary>
        private void ShowComments()
        {
            var entity = ContextEntity();
            var noteType = NoteTypeCache.Read( GetAttributeValue( "NoteType" ).AsGuid() );

            if ( entity != null && noteType != null )
            {
                int? entityTypeId = EntityTypeCache.GetId( ContextEntity().TypeName );

                using ( var rockContext = new RockContext() )
                {
                    var notes = new NoteService( rockContext ).Queryable()
                        .Where( n => n.NoteTypeId == noteType.Id && n.EntityId == entity.Id )
                        .OrderBy( n => n.CreatedDateTime ).ToList();

                    var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( RockPage, CurrentPerson );
                    mergeFields.Add( "Notes", notes );

                    lComments.Text = GetAttributeValue( "CommentTemplate" ).ResolveMergeFields( mergeFields );
                }

                btnReply.Visible = noteType != null && noteType.EntityTypeId == entityTypeId && UserCanEdit;

                SetupSubscribeButton();
            }
            else
            {
                lComments.Text = string.Empty;
                btnReply.Visible = false;
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
        /// Adds a following record on the entity for the specified person. Checks if
        /// one is needed first.
        /// </summary>
        /// <param name="entityId">The entity identifier to be followed.</param>
        /// <param name="personAliasId">The person who is to follow the project.</param>
        /// <param name="rockContext">The RockContext to work in. Changes are not saved by this method.</param>
        public void FollowEntity( int entityId, int personAliasId, RockContext rockContext )
        {
            int entityTypeId = EntityTypeCache.Read( ContextEntity().TypeName ).Id;

            var followingService = new FollowingService( rockContext );
            var personId = new PersonAliasService( rockContext ).Get( personAliasId ).PersonId;

            bool isFollowing = IsFollowingEntity( entityId, personAliasId, rockContext );

            if ( !isFollowing )
            {
                var person = new PersonService( rockContext ).Get( personId );

                if ( person != null )
                {
                    var following = new Following { Id = 0 };
                    followingService.Add( following );

                    following.EntityTypeId = entityTypeId;
                    following.EntityId = entityId;
                    following.PersonAliasId = person.PrimaryAliasId.Value;
                    following.CreatedDateTime = RockDateTime.Now;
                }
            }
        }

        /// <summary>
        /// Deletes a following record on the entity for the specified person.
        /// </summary>
        /// <param name="entityId">The entity identifier to be unfollowed.</param>
        /// <param name="personAliasId">The person who is to unfollow the project.</param>
        /// <param name="rockContext">The RockContext to work in. Changes are not saved by this method.</param>
        public void UnfollowEntity( int entityId, int personAliasId, RockContext rockContext )
        {
            int entityTypeId = EntityTypeCache.Read( ContextEntity().TypeName ).Id;

            var followingService = new FollowingService( rockContext );
            var personId = new PersonAliasService( rockContext ).Get( personAliasId ).PersonId;

            var followings = followingService.Queryable()
                .Where( f => f.EntityTypeId == entityTypeId && f.EntityId == entityId && f.PersonAlias.PersonId == personId );

            followingService.DeleteRange( followings );
        }

        /// <summary>
        /// Determines if the person alias is currently following the entity.
        /// </summary>
        /// <param name="entityId">The entity identifier to be followed.</param>
        /// <param name="personAliasId">The person who is to follow the project.</param>
        /// <param name="rockContext">The RockContext to work in.</param>
        /// <returns>True if the person is currently following the entity.</returns>
        protected bool IsFollowingEntity( int entityId, int personAliasId, RockContext rockContext )
        {
            int entityTypeId = EntityTypeCache.Read( ContextEntity().TypeName ).Id;

            var followingService = new FollowingService( rockContext );
            var personId = new PersonAliasService( rockContext ).Get( personAliasId ).PersonId;

            return followingService.Queryable()
                .Where( f => f.EntityTypeId == entityTypeId && f.EntityId == entityId && f.PersonAlias.Person.Id == personId )
                .Any();
        }

        /// <summary>
        /// Setup the subscribe toggle button to show the correct icon and text.
        /// </summary>
        protected void SetupSubscribeButton()
        {
            var entity = ContextEntity();

            if ( entity != null && CurrentPersonAliasId.HasValue && GetAttributeValue( "ShowSubscribeButton" ).AsBoolean( false ) )
            {
                if ( IsFollowingEntity( entity.Id, CurrentPersonAliasId.Value, new RockContext() ) )
                {
                    btnToggleSubscribe.Text = "<i class='fa fa-bell-o'></i> Unsubscribe";
                }
                else
                {
                    btnToggleSubscribe.Text = "<i class='fa fa-bell-slash-o'></i> Subscribe";
                }

                btnToggleSubscribe.Visible = true;
            }
            else
            {
                btnToggleSubscribe.Visible = false;
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
            var entityId = ContextEntity().Id;
            var note = new Note { Id = 0 };
            int noteTypeId = NoteTypeCache.Read( GetAttributeValue( "NoteType" ).AsGuid() ).Id;

            //
            // Add the note to the database.
            //
            noteService.Add( note );
            note.NoteTypeId = noteTypeId;
            note.EntityId = entityId;
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

                if ( GetAttributeValue( "FollowOnFirstPost" ).AsBoolean( false ) )
                {
                    int postCount = noteService.Queryable().Count( n => n.NoteTypeId == noteTypeId && n.EntityId == entityId );
                    if ( postCount == 1 )
                    {
                        FollowEntity( entityId, CurrentPersonAliasId.Value, rockContext );

                        rockContext.SaveChanges();
                    }
                }
            } );

            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "NotificationEmail" ) ) )
            {
                //SendCommentNotification( entityId, CurrentPersonAliasId, note.Id );
            }

            ShowComments();

            meNewComment.UploadedFileIds = null;
            meNewComment.Text = string.Empty;

            pnlReply.Visible = false;
            btnReply.Visible = true;

            SetupSubscribeButton();
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnReply_Click( object sender, EventArgs e )
        {
            btnReply.Visible = false;
            btnToggleSubscribe.Visible = false;
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

            SetupSubscribeButton();
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnToggleSubscribe_Click( object sender, EventArgs e )
        {
            var entity = ContextEntity();

            if ( entity != null && CurrentPersonAliasId.HasValue )
            {
                var rockContext = new RockContext();

                if ( IsFollowingEntity( entity.Id, CurrentPersonAliasId.Value, rockContext ) )
                {
                    UnfollowEntity( entity.Id, CurrentPersonAliasId.Value, rockContext );
                }
                else
                {
                    FollowEntity( entity.Id, CurrentPersonAliasId.Value, rockContext );
                }

                rockContext.SaveChanges();

                SetupSubscribeButton();
            }
        }

        #endregion
    }
}