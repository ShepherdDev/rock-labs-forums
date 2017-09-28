using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

using com.rocklabs.Forums.Model;

namespace com.rocklabs.Forums.UI
{
    /// <summary>
    /// Handles displaying and deleting comments on a project.
    /// </summary>
    public class CommentContainer : CompositeControl, INamingContainer
    {
        /// <summary>
        /// The identifier of the project whose comments will be displayed.
        /// </summary>
        public int? TopicId
        {
            get { return ViewState["TopicId"] as int?; }
            set { ViewState["TopicId"] = value; }
        }

        /// <summary>
        /// Called when a note is deleted. NoteId will be null.
        /// </summary>
        public event EventHandler<NoteEventArgs> NoteUpdated;

        /// <summary>
        /// Called when the page is still in a loading state.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            //
            // Rebuild the notes so any postback events on the controls will fire.
            //
            RebuildNotes();
        }

        /// <summary>
        /// Render this control onto the page.
        /// </summary>
        /// <param name="writer">The HtmlTextWriter that will be used when generating content.</param>
        public override void RenderControl( HtmlTextWriter writer )
        {
            if ( this.Visible )
            {
                writer.AddAttribute( HtmlTextWriterAttribute.Class, CssClass );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                {
                    foreach ( Control control in Controls )
                    {
                        control.RenderControl( writer );
                    }
                }
                writer.RenderEndTag();
            }
        }

        /// <summary>
        /// Rebuild all the notes that will be displayed in this container.
        /// </summary>
        public void RebuildNotes()
        {
            var rockPage = this.Page as RockPage;
            int? topicEntityTypeId = EntityTypeCache.GetId( typeof( ForumTopic ) );
            int? topicId = TopicId;

            //
            // Clear any old notes out.
            //
            Controls.Clear();

            if ( rockPage != null && topicEntityTypeId.HasValue && topicId.HasValue )
            {
                var currentPerson = rockPage.CurrentPerson;

                using ( var rockContext = new RockContext() )
                {
                    var notes = new NoteService( rockContext ).Queryable()
                        .Where( n => n.NoteType.EntityTypeId == topicEntityTypeId.Value && n.EntityId == topicId.Value )
                        .OrderBy( n => n.CreatedDateTime ).ToList();

                    foreach ( var note in notes )
                    {
                        //
                        // Only display notes the person is authorized to view.
                        //
                        if ( note.IsAuthorized( Authorization.VIEW, currentPerson ) )
                        {
                            var commentControl = new CommentControl();
                            commentControl.ID = string.Format( "note_{0}", note.Guid.ToString().Replace( "-", "_" ) );
                            commentControl.Note = note;
                            commentControl.CanDelete = note.IsAuthorized( Authorization.EDIT, currentPerson );
                            commentControl.CommentDeleted += CommentControl_CommentDeleted;

                            //
                            // A user can always delete their own comments.
                            //
                            if ( note.CreatedByPersonAlias != null && note.CreatedByPersonAlias.PersonId == currentPerson.Id )
                            {
                                commentControl.CanDelete = true;
                            }

                            Controls.Add( commentControl );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the CommentDeleted event for the control.
        /// </summary>
        /// <param name="sender">The CommentControl that sent this event.</param>
        /// <param name="e">The NoteEventArgs related to this event.</param>
        private void CommentControl_CommentDeleted( object sender, NoteEventArgs e )
        {
            RebuildNotes();

            if ( NoteUpdated != null )
            {
                NoteUpdated.Invoke( this, new NoteEventArgs( null ) );
            }
        }
    }
}
