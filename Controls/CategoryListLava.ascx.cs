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

namespace RockWeb.Plugins.com_rocklabs.Forums
{
    /// <summary>
    /// Generic category list formatted by Lava.
    /// </summary>
    [DisplayName( "Category List Lava" )]
    [Category( "Rock Labs > Forums" )]
    [Description( "Lists categories with formatting via Lava." )]
    [CodeEditorField( "Lava Template", "Lava template to use to display the categories", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, @"", "", order: 0 )]
    [EntityTypeField( "Entity Type", "Display categories for the selected entity type.", true, "", order: 1 )]
    [LinkedPage( "Detail Page", "Page reference to use for the detail page.", false, "", "", order: 2 )]
    public partial class CategoryListLava : Rock.Web.UI.RockBlock
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
                LoadCategories();
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
            LoadCategories();
        }

        #endregion

        #region Methods

        private void LoadCategories()
        {
            Guid? entityTypeGuid = GetAttributeValue( "EntityType" ).AsGuidOrNull();

            if ( entityTypeGuid.HasValue )
            {
                int entityTypeId = EntityTypeCache.Read( entityTypeGuid.Value ).Id;
                var categoryService = new CategoryService( new RockContext() );
                List<Category> categories;
                int? parentCategoryId = PageParameter( "categoryId" ).AsIntegerOrNull();

                if ( parentCategoryId.HasValue )
                {
                    categories = categoryService.Queryable()
                        .Where( c => c.ParentCategoryId == parentCategoryId.Value && c.EntityTypeId == entityTypeId )
                        .OrderBy( c => c.Order )
                        .ToList();
                }
                else
                {
                    categories = categoryService.Queryable()
                        .Where( c => !c.ParentCategoryId.HasValue && c.EntityTypeId == entityTypeId )
                        .OrderBy( c => c.Order )
                        .ToList();
                }

                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeFields.Add( "Categories", categories );

                // add link to detail page
                Dictionary<string, object> linkedPages = new Dictionary<string, object>();
                linkedPages.Add( "DetailPage", LinkedPageRoute( "DetailPage" ) );
                mergeFields.Add( "LinkedPages", linkedPages );

                lOutput.Text = GetAttributeValue( "LavaTemplate" ).ResolveMergeFields( mergeFields );
                nbNotConfigured.Visible = false;
            }
            else
            {
                lOutput.Text = string.Empty;
                nbNotConfigured.Visible = true;
            }
        }

        #endregion
    }
}