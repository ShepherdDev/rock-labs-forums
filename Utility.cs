using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace com.rocklabs.Forums
{
    public static class Utility
    {
        /// <summary>
        /// Convert the markdown into Html. This should be used anytime a project comment
        /// is to be displayed as it automatically converst #1234 references into links
        /// as well as fixes some standard formatting issues with images.
        /// </summary>
        /// <param name="markdown">The markdown text to be converted.</param>
        /// <returns>A string of HTML text that may be displayed.</returns>
        public static string ConvertMarkdownToHtml( string markdown )
        {
            var settings = CommonMark.CommonMarkSettings.Default.Clone();
            settings.OutputDelegate = ( doc, output, dSettings ) =>
            {
                new Support.MarkdownHtmlFormatter( output, dSettings ).WriteDocument( doc );
            };

            return CommonMark.CommonMarkConverter.Convert( markdown, settings );
        }

        /// <summary>
        /// Configures a control to display and toggle following for the specified entity
        /// This is a duplication of core functionality until #2401 is fixed.
        /// </summary>
        /// <param name="followEntity">The follow entity. NOTE: Make sure to use PersonAlias instead of Person when following a Person</param>
        /// <param name="followControl">The follow control.</param>
        /// <param name="follower">The follower.</param>
        public static void SetFollowing( IEntity followEntity, WebControl followControl, Person follower )
        {
            var followingEntityType = EntityTypeCache.Read( followEntity.GetType() );
            if ( follower != null && follower.PrimaryAliasId.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    var personAliasService = new PersonAliasService( rockContext );
                    var followingService = new FollowingService( rockContext );

                    var followingQry = followingService.Queryable()
                        .Where( f =>
                            f.EntityTypeId == followingEntityType.Id &&
                            f.PersonAlias.PersonId == follower.Id );

                    followingQry = followingQry.Where( f => f.EntityId == followEntity.Id );

                    if ( followingQry.Any() )
                    {
                        followControl.AddCssClass( "following" );
                    }
                    else
                    {
                        followControl.RemoveCssClass( "following" );
                    }
                }

                int entityId = followEntity.Id;

                // only show the following control if the entity has been saved to the database
                followControl.Visible = entityId > 0;

                string script = string.Format(
                    @"Rock.controls.followingsToggler.initialize($('#{0}'), {1}, {2}, {3}, {4});",
                        followControl.ClientID,
                        followingEntityType.Id,
                        entityId,
                        follower.Id,
                        follower.PrimaryAliasId );

                ScriptManager.RegisterStartupScript( followControl, followControl.GetType(), string.Format( "{0}_following", followControl.ClientID ), script, true );
            }
        }
    }
}
