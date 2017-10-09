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
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using Rock.Attribute;

namespace RockWeb.Plugins.com_rocklabs.Forums
{
    /// <summary>
    /// Generic category list formatted by Lava.
    /// </summary>
    [DisplayName( "Category List Lava" )]
    [Category( "Rock Labs > Forums" )]
    [Description( "Lists categories with formatting via Lava." )]
    [EntityTypeField( "Entity Type", "Display categories for the selected entity type.", true, "CustomSetting", order: 0 )]
    [CategoryField( "Default Category", "The default category to use as a root if nothing is provided in the query string.", category: "CustomSetting", required: false, order: 1 )]
    [LinkedPage( "Detail Page", "Page reference to use for the detail page.", false, "", "CustomSetting", order: 2 )]
    [CodeEditorField( "Lava Template", "Lava template to use to display the categories.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, category: "CustomSetting", order: 3, defaultValue:
 @"{% for category in Categories %}
    {% if category.ChildCategories != empty %}
        {% capture linkUrl %}?categoryId={{ category.Id }}{% endcapture %}
    {% else %}
        {% capture linkUrl %}{{ LinkedPages.DetailPage }}?categoryId={{ category.Id }}{% endcapture %}
    {% endif %}

    <div class=""well"">
        <div style=""font-size: 1.75em;"">
            {% if category.IconCssClass != empty %}
                <span class=""pull-left""><i class=""{{ category.IconCssClass }}""></i>&nbsp;</span> 
            {% endif %}
            <a href=""{{ linkUrl }}"">
                <span>{{ category.Name }}</span>
            </a>
        </div>
        {% if category.Description != empty %}
            <div style=""font-size: 0.75em;"">
                {{ category.Description }}</span>
            </div>
        {% endif %}
    </div>
{% endfor %}
" )]
    public partial class CategoryListLava : RockBlockCustomSettings
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

            if ( !Page.IsPostBack )
            {
                pEntityType.EntityTypes = new EntityTypeService( new RockContext() ).GetEntities().ToList();
            }
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
                ShowCategories();
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
            ShowCategories();
        }

        /// <summary>
        /// Handles the SelectedIndexChanges event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void pEntityType_SelectedIndexChanged( object sender, EventArgs e )
        {
            var entityType = EntityTypeCache.Read( pEntityType.SelectedValue.AsInteger() );

            if ( entityType != null )
            {
                pDefaultCategory.EntityTypeId = entityType.Id;
                pDefaultCategory.SetValue( null );
                pDefaultCategory.Visible = true;
            }
            else
            {
                pDefaultCategory.EntityTypeName = string.Empty;
                pDefaultCategory.SetValue( null );
                pDefaultCategory.Visible = false;
            }
        }

        /// <summary>
        /// Handles the SaveClick event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdlSettings_SaveClick( object sender, EventArgs e )
        {
            var entityType = EntityTypeCache.Read( pEntityType.SelectedValue.AsInteger() );
            var category = CategoryCache.Read( pDefaultCategory.SelectedValue.AsInteger() );
            var page = PageCache.Read( pDetailPage.SelectedValueAsId().Value );

            SetAttributeValue( "EntityType", entityType.Guid.ToString() );
            SetAttributeValue( "DefaultCategory", category != null ? category.Guid.ToString() : string.Empty );
            SetAttributeValue( "DetailPage", page != null ? page.Guid.ToString() : string.Empty );
            SetAttributeValue( "LavaTemplate", ceLavaTemplate.Text );

            mdlSettings.Hide();

            ShowCategories();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Load and show all the categories with the configured parameters.
        /// </summary>
        private void ShowCategories()
        {
            Guid? entityTypeGuid = GetAttributeValue( "EntityType" ).AsGuidOrNull();

            if ( entityTypeGuid.HasValue )
            {
                int entityTypeId = EntityTypeCache.Read( entityTypeGuid.Value ).Id;
                var categoryService = new CategoryService( new RockContext() );
                List<Category> categories;
                int? parentCategoryId = PageParameter( "categoryId" ).AsIntegerOrNull();

                //
                // Process the default category setting if we have not been provided one in the
                // query string.
                //
                if ( !parentCategoryId.HasValue && !string.IsNullOrEmpty( GetAttributeValue( "DefaultCategory" ) ) )
                {
                    var category = CategoryCache.Read( GetAttributeValue( "DefaultCategory" ).AsGuid() );
                    parentCategoryId = category != null ? ( int? ) category.Id : null;
                }

                //
                // Get all child categories of the parent category or all root categories.
                //
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

                //
                // Setup lava merge fields.
                //
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeFields.Add( "Categories", categories );

                //
                // Add link to detail page
                //
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

        /// <summary>
        /// Show the custom settings modal dialog.
        /// </summary>
        protected override void ShowSettings()
        {
            var entityType = EntityTypeCache.Read( GetAttributeValue( "EntityType" ).AsGuid() );
            var category = CategoryCache.Read( GetAttributeValue( "DefaultCategory" ).AsGuid() );
            var page = PageCache.Read( GetAttributeValue( "DetailPage" ).AsGuid() );

            pEntityType.SelectedEntityTypeId = entityType != null ? ( int? ) entityType.Id : null;
            pDetailPage.SetValue( page != null ? ( int? ) page.Id : null );
            ceLavaTemplate.Text = GetAttributeValue( "LavaTemplate" );

            if ( entityType != null )
            {
                pDefaultCategory.EntityTypeId = entityType.Id;
                pDefaultCategory.SetValue( category != null ? ( int? ) category.Id : null );
                pDefaultCategory.Visible = true;
            }
            else
            {
                pDefaultCategory.EntityTypeName = string.Empty;
                pDefaultCategory.SetValue( null );
                pDefaultCategory.Visible = false;
            }

            mdlSettings.Show();
        }

        #endregion
    }
}