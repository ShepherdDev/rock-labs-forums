using System;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace com.blueboxmoon.ProjectManagement.UI
{
    /// <summary>
    /// Handles the display of a single project comment.
    /// </summary>
    public class CommentControl : CompositeControl
    {
        /// <summary>
        /// If true then the user will be allowed to delete this comment.
        /// </summary>
        public bool CanDelete
        {
            get { return ViewState["CanDelete"] as bool? ?? false; }
            set { ViewState["CanDelete"] = value; }
        }

        /// <summary>
        /// The note that will be displayed on the page.
        /// </summary>
        public Rock.Model.Note Note
        {
            set
            {
                this.NoteId = value.Id;
                this.NoteText = value.Text;
                this.IsUserNote = value.NoteType.UserSelectable;
                this.NoteCssClass = value.NoteType.CssClass;
                this.IconCssClass = value.NoteType.IconCssClass;
                if ( value.CreatedByPersonAlias != null )
                {
                    this.CreatedByPersonName = value.CreatedByPersonName;
                    this.CreatedByPersonPhotoUrl = value.CreatedByPersonAlias.Person.PhotoUrl;
                }
                this.CreatedDateTime = value.CreatedDateTime;
            }
        }

        /// <summary>
        /// The identifier of the note that will be displayed.
        /// </summary>
        public int? NoteId
        {
            get { return ViewState["NoteId"] as int?; }
            protected set { ViewState["NoteId"] = value; }
        }

        /// <summary>
        /// The text of the comment.
        /// </summary>
        protected string NoteText
        {
            get { return ViewState["NoteText"] as string; }
            set { ViewState["NoteText"] = value; }
        }

        /// <summary>
        /// Wether or not to display this note as if it were a user comment.
        /// </summary>
        protected bool IsUserNote
        {
            get { return ViewState["IsUserNote"] as bool? ?? false; }
            set { ViewState["IsUserNote"] = value; }
        }

        /// <summary>
        /// The css class to apply to the entire note.
        /// </summary>
        protected string NoteCssClass
        {
            get { return ViewState["NoteCssClass"] as string; }
            set { ViewState["NoteCssClass"] = value; }
        }

        /// <summary>
        /// If this is not a user note, which icon css class to use.
        /// </summary>
        protected string IconCssClass
        {
            get { return ViewState["IconCssClass"] as string; }
            set { ViewState["IconCssClass"] = value; }
        }

        /// <summary>
        /// Name of the person that will be displayed for this note.
        /// </summary>
        protected string CreatedByPersonName
        {
            get { return ViewState["CreatedByPersonName"] as string; }
            set { ViewState["CreatedByPersonName"] = value; }
        }

        /// <summary>
        /// Photo URL to be used when displaying a user note.
        /// </summary>
        protected string CreatedByPersonPhotoUrl
        {
            get { return ViewState["CreatedByPersonPhotoUrl"] as string; }
            set { ViewState["CreatedByPersonPhotoUrl"] = value; }
        }

        /// <summary>
        /// Date and time the note was written.
        /// </summary>
        protected DateTime? CreatedDateTime
        {
            get { return ViewState["CreatedDateTime"] as DateTime?; }
            set { ViewState["CreatedDateTime"] = value; }
        }

        /// <summary>
        /// Called after a note had been deleted from the system.
        /// </summary>
        public event EventHandler<NoteEventArgs> CommentDeleted;

        /// <summary>
        /// The button to include on the note to enable deletion callback events.
        /// </summary>
        protected LinkButton _lbDelete;

        /// <summary>
        /// Creates all the child controls needed for this control.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            //
            // Create the delete button that will be used.
            //
            _lbDelete = new LinkButton();
            _lbDelete.ID = this.ID + "_lbDelete";
            _lbDelete.AddCssClass( "btn btn-sm btn-link pm-delete-button" );
            _lbDelete.CausesValidation = false;
            _lbDelete.Click += lbDelete_Click;
            _lbDelete.OnClientClick = "Rock.dialogs.confirmDelete(event, 'comment');";
            Controls.Add( _lbDelete );

            var iDeleteIcon = new System.Web.UI.HtmlControls.HtmlGenericControl( "i" );
            iDeleteIcon.AddCssClass( "fa fa-times" );

            _lbDelete.Controls.Add( iDeleteIcon );
        }

        /// <summary>
        /// Handles the Click event for the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void lbDelete_Click( object sender, EventArgs e )
        {
            var rockPage = this.Page as RockPage;

            if ( rockPage != null && CanDelete )
            {
                var currentPerson = rockPage.CurrentPerson;

                using ( var rockContext = new RockContext() )
                {
                    var noteService = new NoteService( rockContext );
                    var note = noteService.Get( NoteId.Value );

                    if ( note != null )
                    {
                        noteService.Delete( note );
                        rockContext.SaveChanges();
                    }
                }
            }

            if ( CommentDeleted != null )
            {
                CommentDeleted.Invoke( this, new NoteEventArgs( NoteId ) );
            }
        }

        /// <summary>
        /// Render the comment into the web page.
        /// </summary>
        /// <param name="writer">The HtmlTextWriter to use when generating content.</param>
        public override void RenderControl( HtmlTextWriter writer )
        {
            writer.AddAttribute( HtmlTextWriterAttribute.Class, "clearfix pm-comment-container " + NoteCssClass );
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            {
                //
                // User notes are displayed different than system notes.
                //
                if ( IsUserNote )
                {
                    //
                    // Render the person photo.
                    //
                    if ( !string.IsNullOrWhiteSpace( CreatedByPersonPhotoUrl ) )
                    {
                        writer.AddAttribute( HtmlTextWriterAttribute.Class, "photo-icon photo-round pm-comment-photo pull-left" );
                        writer.AddAttribute( HtmlTextWriterAttribute.Style, string.Format( "background-image: url('{0}');", CreatedByPersonPhotoUrl ) );
                        writer.RenderBeginTag( HtmlTextWriterTag.Div );
                        writer.RenderEndTag();
                    }

                    //
                    // Render any action buttons.
                    //
                    writer.AddAttribute( HtmlTextWriterAttribute.Class, "pull-right" );
                    writer.RenderBeginTag( HtmlTextWriterTag.Div );
                    {
                        if ( CanDelete )
                        {
                            _lbDelete.RenderControl( writer );
                        }
                    }
                    writer.RenderEndTag();

                    //
                    // Render the comment text block.
                    //
                    writer.AddAttribute( HtmlTextWriterAttribute.Class, "pm-comment" );
                    writer.RenderBeginTag( HtmlTextWriterTag.Div );
                    {
                        //
                        // Render the author and date/time of the comment.
                        //
                        writer.AddAttribute( HtmlTextWriterAttribute.Class, "pm-comment-author" );
                        writer.RenderBeginTag( HtmlTextWriterTag.Div );
                        {
                            if ( !string.IsNullOrWhiteSpace( CreatedByPersonName ) )
                            {
                                writer.AddAttribute( HtmlTextWriterAttribute.Class, "pm-comment-name" );
                                writer.RenderBeginTag( HtmlTextWriterTag.Span );
                                {
                                    writer.Write( "From {0} ", CreatedByPersonName );
                                }
                                writer.RenderEndTag();
                            }
                            if ( CreatedDateTime.HasValue )
                            {
                                writer.AddAttribute( HtmlTextWriterAttribute.Class, "pm-comment-date" );
                                writer.RenderBeginTag( HtmlTextWriterTag.Span );
                                {
                                    writer.Write( Utility.RelativeTimeOrDateText( CreatedDateTime.Value ).ToLower() );
                                }
                                writer.RenderEndTag();
                            }
                        }
                        writer.RenderEndTag();

                        //
                        // Render the comment text itself.
                        //
                        writer.AddAttribute( HtmlTextWriterAttribute.Class, "pm-comment-text" );
                        writer.RenderBeginTag( HtmlTextWriterTag.Div );
                        {
                            writer.Write( Utility.ConvertMarkdownToHtml( NoteText ) );
                        }
                        writer.RenderEndTag();
                    }
                    writer.RenderEndTag();
                }
                else
                {
                    if ( !string.IsNullOrWhiteSpace( IconCssClass ) )
                    {
                        writer.AddAttribute( HtmlTextWriterAttribute.Class, "pull-left margin-r-sm" );
                        writer.RenderBeginTag( HtmlTextWriterTag.Div );
                        {
                            var icon = IconCssClass;
                            if ( icon.Contains( "fa-" ) )
                            {
                                icon = icon + " fa-border pm-fa-rounded-border";
                            }
                            writer.AddAttribute( HtmlTextWriterAttribute.Class, icon );
                            writer.RenderBeginTag( HtmlTextWriterTag.I );
                            writer.RenderEndTag();
                        }
                        writer.RenderEndTag();
                    }

                    var text = Utility.ConvertMarkdownToHtml( NoteText );
                    var dateTimeText = string.Empty;

                    if ( text.StartsWith( "<p>" ) && text.IndexOf( "<p>", 3 ) < 0 )
                    {
                        text = text.Replace( "<p>", string.Empty ).Replace( "</p>", string.Empty ).Trim();
                    }

                    if ( !string.IsNullOrWhiteSpace( CreatedByPersonName ) )
                    {
                        writer.AddAttribute( HtmlTextWriterAttribute.Class, "pm-comment-name" );
                        writer.RenderBeginTag( HtmlTextWriterTag.Span );
                        {
                            writer.Write( CreatedByPersonName );
                        }
                        writer.RenderEndTag();
                        writer.Write( " " );
                    }

                    writer.Write( text );

                    if ( CreatedDateTime.HasValue )
                    {
                        dateTimeText = Utility.RelativeTimeOrDateText( CreatedDateTime.Value ).ToLower();

                        writer.Write( " " );
                        writer.AddAttribute( HtmlTextWriterAttribute.Class, "pm-comment-date" );
                        writer.RenderBeginTag( HtmlTextWriterTag.Span );
                        {
                            writer.Write( " " );
                            writer.Write( dateTimeText );
                        }
                        writer.RenderEndTag();
                    }
                }
            }
            writer.RenderEndTag();
        }
    }
}
