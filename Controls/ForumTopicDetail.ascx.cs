using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

using com.rocklabs.Forums;
using com.rocklabs.Forums.Model;
using com.rocklabs.Forums.UI;

namespace RockWeb.Plugins.com_rocklabs.Forums
{
    [DisplayName( "Forum Topic Detail" )]
    [Category( "Rock Labs > Forums" )]
    [Description( "Displays the details for a forum topic." )]

    [BinaryFileTypeField( "Binary File Type", "The storage type to use for uploaded files and images.", false, "", order: 1 )]
    public partial class ForumTopicDetail : RockBlock
    {
        #region Private Fields

        private bool _canAddEditDelete = false;

        #endregion

        #region Properties

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
            // Verify that the user is allowed to make edits to the topic/category in question.
            //
            _canAddEditDelete = IsUserAuthorized( Authorization.EDIT );
            if ( _canAddEditDelete  )
            {
                var rockContext = new RockContext();
                var topic = new ForumTopicService( rockContext ).Get( PageParameter( "Id" ).AsInteger() );

                if ( topic != null )
                {
                    _canAddEditDelete = topic.IsAuthorized( Authorization.EDIT, CurrentPerson );
                }
                else
                {
                    var category = CategoryCache.Read( PageParameter( "CategoryId" ).AsInteger() );
                    if ( category != null )
                    {
                        _canAddEditDelete = category.IsAuthorized( Authorization.EDIT, CurrentPerson );
                    }
                    else
                    {
                        _canAddEditDelete = false;
                    }
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
                meDescription.PublicApplicationRoot = GlobalAttributesCache.Value( "PublicApplicationRoot" );

                if ( PageParameter( "Id" ).AsInteger() == 0 )
                {
                    ShowEdit( 0 );
                }
                else
                {
                    ShowDetail( PageParameter( "Id" ).AsInteger() );
                }
            }
            else
            {
                if ( pnlEdit.Visible )
                {
                    //
                    // Add the attribute controls.
                    //
                    var topic = new ForumTopicService( new RockContext() ).Get( PageParameter( "Id" ).AsInteger() );
                    if ( topic == null )
                    {
                        topic = new ForumTopic { Id = 0 };
                        topic.CategoryId = PageParameter( "Id" ).AsInteger();
                    }

                    topic.LoadAttributes();
                    phEditAttributes.Controls.Clear();
                    Rock.Attribute.Helper.AddEditControls( topic, phEditAttributes, false, BlockValidationGroup );
                }
                else if ( pnlDetails.Visible )
                {
                    var topic = new ForumTopicService( new RockContext() ).Get( PageParameter( "Id" ).AsInteger() );

                    topic.LoadAttributes();
                    phAttributes.Controls.Clear();
                    Rock.Attribute.Helper.AddDisplayControls( topic, phAttributes, null, false, false );
                }
            }
        }

        /// <summary>
        /// All postback events have been processed. We are about to render the page
        /// but allowed to make final changes.
        /// </summary>
        /// <param name="e">Arguments that describe this event.</param>
        protected override void OnPreRender( EventArgs e )
        {
            if ( pnlDetails.Visible )
            {
                var topic = new ForumTopicService( new RockContext() ).Get( PageParameter( "Id" ).AsInteger() );

                if ( topic != null )
                {
                    Utility.SetFollowing( topic, pnlFollowing, CurrentPerson );
                }
            }

            base.OnPreRender( e );
        }

        /// <summary>
        /// Gets the bread crumbs.
        /// </summary>
        /// <param name="pageReference">The page reference.</param>
        /// <returns></returns>
        public override List<BreadCrumb> GetBreadCrumbs( PageReference pageReference )
        {
            var breadCrumbs = new List<BreadCrumb>();

            int topicId = PageParameter( pageReference, "Id" ).AsInteger();
            var topic = new ForumTopicService( new RockContext() ).Get( topicId );
            if ( topic != null )
            {
                breadCrumbs.Add( new BreadCrumb( topic.Name, pageReference ) );
            }
            else
            {
                breadCrumbs.Add( new BreadCrumb( "New Topic", pageReference ) );
            }

            return breadCrumbs;
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="topicId">The topic identifier.</param>
        public void ShowDetail( int topicId )
        {
            var rockContext = new RockContext();
            ForumTopic topic = null;

            pnlDetails.Visible = true;
            pnlEdit.Visible = false;
            HideSecondaryBlocks( false );

            if ( topicId != 0 )
            {
                topic = new ForumTopicService( rockContext ).Get( topicId );
            }

            //
            // Ensure the user is allowed to view this project.
            //
            if ( topic == null || !topic.IsAuthorized( Authorization.VIEW, CurrentPerson ) )
            {
                HideSecondaryBlocks( true );
                nbUnauthorized.Text = EditModeMessage.NotAuthorizedToView( ForumTopic.FriendlyTypeName );
                pnlDetails.Visible = false;
                return;
            }

            //
            // Set all the simple field values.
            //
            lTitle.Text = topic.Name;
            lDetails.Text = Utility.ConvertMarkdownToHtml( topic.Description );
            lAuthorName.Text = topic.CreatedByPersonAlias.Person.FullName;
            lDatePosted.Text = topic.CreatedDateTime.HasValue ? topic.CreatedDateTime.ToRelativeDateString() : string.Empty;

            //
            // Add the attribute controls.
            //
            topic.LoadAttributes();
            phAttributes.Controls.Clear();
            Rock.Attribute.Helper.AddDisplayControls( topic, phAttributes, null, false, false );

            //
            // Set button states.
            //
            lbEdit.Visible = UserCanAdministrate;
            lbDelete.Visible = UserCanAdministrate;
        }

        /// <summary>
        /// Shows the edit panel.
        /// </summary>
        /// <param name="topicId">The topic identifier.</param>
        public void ShowEdit( int topicId )
        {
            var rockContext = new RockContext();
            ForumTopic topic = null;

            pnlDetails.Visible = false;
            pnlEdit.Visible = true;
            HideSecondaryBlocks( true );

            if ( topicId != 0 )
            {
                topic = new ForumTopicService( rockContext ).Get( topicId );
                pdAuditDetails.SetEntity( topic, ResolveRockUrl( "~" ) );
            }

            if ( !_canAddEditDelete )
            {
                HideSecondaryBlocks( true );
                nbUnauthorized.Text = EditModeMessage.NotAuthorizedToEdit( ForumTopic.FriendlyTypeName );
                pnlEdit.Visible = false;
                return;
            }

            if ( topic == null )
            {
                topic = new ForumTopic { Id = 0 };
                topic.CategoryId = PageParameter( "CategoryId" ).AsInteger();

                // hide the panel drawer that show created and last modified dates
                pdAuditDetails.Visible = false;
            }

            string title = topic.Id > 0 ?
                ActionTitle.Edit( ForumTopic.FriendlyTypeName ) :
                ActionTitle.Add( ForumTopic.FriendlyTypeName );
            lEditTitle.Text = title.FormatAsHtmlTitle();

            meDescription.BinaryFileTypeGuid = GetAttributeValue( "BinaryFileType" ).AsGuidOrNull();

            hfId.Value = topic.Id.ToString();

            tbName.Text = topic.Name;
            meDescription.Text = topic.Description;

            //
            // Add the attribute controls.
            //
            topic.LoadAttributes();
            phEditAttributes.Controls.Clear();
            Rock.Attribute.Helper.AddEditControls( topic, phEditAttributes, true, BlockValidationGroup );
        }

        /// <summary>
        /// Adds a following record on the entity for the specified person. Checks if
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
        /// Handles the Click event of the lbSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void lbSave_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var topicService = new ForumTopicService( rockContext );
            ForumTopic topic;
            bool isNew = false;

            int topicId = int.Parse( hfId.Value );

            if ( topicId == 0 )
            {
                isNew = true;
                topic = new ForumTopic();
                topicService.Add( topic );
                topic.CreatedByPersonAliasId = CurrentPersonAliasId;
                topic.CreatedDateTime = RockDateTime.Now;
                topic.CategoryId = PageParameter( "CategoryId" ).AsInteger();
            }
            else
            {
                topic = topicService.Get( topicId );
            }

            if ( topic != null )
            {
                var completedProjectIds = new List<int>();

                topic.Name = tbName.Text;
                topic.Description = meDescription.Text;
                topic.ModifiedByPersonAliasId = CurrentPersonAliasId;
                topic.ModifiedDateTime = RockDateTime.Now;

                //
                // Store the attribute values.
                //
                topic.LoadAttributes( rockContext );
                Rock.Attribute.Helper.GetEditValues( phEditAttributes, topic );

                if ( !Page.IsValid || !topic.IsValid )
                {
                    // Controls will render the error messages                    
                    return;
                }

                rockContext.WrapTransaction( () =>
                {
                    rockContext.SaveChanges();
                    topic.SaveAttributeValues( rockContext );

                    if ( isNew )
                    {
                        FollowTopic( topic.Id, CurrentPersonAliasId.Value, rockContext );

                        rockContext.SaveChanges();
                    }
                } );

                if ( topicId == 0 )
                {
                    NavigateToCurrentPage( new Dictionary<string, string>() { { "Id", topic.Id.ToString() } } );
                }
                else
                {
                    ShowDetail( topicId );
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the lbCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void lbCancel_Click( object sender, EventArgs e )
        {
            if ( PageParameter( "Id" ).AsInteger() == 0 )
            {
                NavigateToParentPage();
            }
            else
            {
                ShowDetail( PageParameter( "Id" ).AsInteger() );
            }
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            int topicId = hfId.ValueAsInt();

            if ( topicId != 0 )
            {
                ShowDetail( topicId );
            }
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbEdit_Click( object sender, EventArgs e )
        {
            ShowEdit( PageParameter( "Id" ).AsInteger() );
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbDelete_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var topicService = new ForumTopicService( rockContext );
            var topic = topicService.Get( PageParameter( "Id" ).AsInteger() );

            if ( topic != null )
            {
                topicService.Delete( topic );

                rockContext.SaveChanges();

                NavigateToParentPage();
            }
            else
            {
                ShowDetail( PageParameter( "Id" ).AsInteger() );
            }
        }

        #endregion
    }
}