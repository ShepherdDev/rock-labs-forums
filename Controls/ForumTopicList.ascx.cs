// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using Rock.Store;
using System.Text;
using Rock.Security;

using com.rocklabs.Forums.Model;

namespace RockWeb.Plugins.com_rocklabs.Forums
{
    /// <summary>
    /// Forum Topic list.
    /// </summary>
    [DisplayName( "Forum Topic List" )]
    [Category( "Rock Labs > Forums" )]
    [Description( "Lists forum topics in a standard Rock grid." )]
    [LinkedPage( "Detail Page", "Page reference to use for the detail page.", false, "", "", order: 0 )]
    public partial class ForumTopicList : Rock.Web.UI.RockBlock
    {
        #region Fields

        #endregion

        #region Properties

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            gTopics.GridRebind += gTopics_GridRebind;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                BindGrid();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Handles the GridRebind event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridRebindEventArgs"/> instance containing the event data.</param>
        private void gTopics_GridRebind( object sender, GridRebindEventArgs e )
        {
            BindGrid();
        }

        /// <summary>
        /// Handles the RowSelected event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs"/> instance containing the event data.</param>
        protected void gTopics_RowSelected( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "topicId", e.RowKeyId );
        }

        #endregion

        #region Methods

        private void BindGrid()
        {
            int? categoryId = PageParameter( "categoryId" ).AsIntegerOrNull();

            if ( categoryId.HasValue )
            {
                var rockContext = new RockContext();
                var forumTopicService = new ForumTopicService( rockContext );
                var noteService = new NoteService( rockContext );
                var entityTypeId = EntityTypeCache.Read( typeof( ForumTopic ) ).Id;

                var noteTypeIds = new NoteTypeService( rockContext ).Queryable()
                    .Where( t => t.EntityTypeId == entityTypeId )
                    .Select( t => t.Id )
                    .ToList();

                var topics = forumTopicService.Queryable()
                    .Where( t => t.CategoryId == categoryId.Value )
                    .OrderByDescending( t => t.CreatedDateTime )
                    .Select( t => new TopicDrop
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Author = t.CreatedByPersonAlias != null ? t.CreatedByPersonAlias.Person : null,
                        PostedDate = t.CreatedDateTime,
                    } )
                    .ToList();

                foreach ( var topic in topics )
                {
                    var notes = noteService.Queryable()
                        .Where( n => noteTypeIds.Contains( n.NoteTypeId ) && n.EntityId == topic.Id );

                    topic.LastPost = notes
                        .OrderByDescending( n => n.CreatedDateTime )
                        .FirstOrDefault();

                    topic.ReplyCount = notes.Count();
                }

                gTopics.DataSource = topics;
                gTopics.DataBind();
            }
        }

        #endregion

        #region Support Classes

        protected class TopicDrop
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public Person Author { get; set; }

            public DateTime? PostedDate { get; set; }

            public Note LastPost { get; set; }

            public int ReplyCount { get; set; }
        }

        #endregion
    }
}