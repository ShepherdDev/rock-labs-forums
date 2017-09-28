using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
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
        /// Get the base route prefix for all installed routes.
        /// </summary>
        public static string BaseRoute
        {
            get
            {
                return Rock.Web.SystemSettings.GetValue( "com.blueboxmoon.ProjectManagement.BaseRoute" );
            }
        }

        /// <summary>
        /// Convert the date into a relative text string. Expects to be prefixed with
        /// a verb such as "due" or "remind me". If the time difference is less than
        /// 1 day then a relative time string is returned, otherwise a relative date
        /// string is returned.
        /// </summary>
        /// <param name="dateTime">The DateTime to convert.</param>
        /// <returns>A string that represents the relative time or date.</returns>
        public static string RelativeTimeOrDateText( DateTime dateTime, bool includePreposition = true )
        {
            if ( dateTime == null )
            {
                return string.Empty;
            }

            DateTime now = DateTime.Now;
            DateTime date = dateTime;

            if ( date < now )
            {
                var span = now.Subtract( date );

                if ( span.Days >= 1 )
                {
                    return RelativeDateText( dateTime, includePreposition );
                }
                else if ( span.Hours >= 1 )
                {
                    return string.Format( "{0} hour{1} ago", span.Hours, span.Hours > 1 ? "s" : "" );
                }
                else if ( span.Minutes >= 1 )
                {
                    return string.Format( "{0} minute{1} ago", span.Minutes, span.Minutes > 1 ? "s" : "" );
                }
                else if ( span.Seconds >= 1 )
                {
                    return string.Format( "{0} second{1} ago", span.Seconds, span.Seconds > 1 ? "s" : "" );
                }
                else
                {
                    return "Just now";
                }
            }
            else
            {
                var span = date.Subtract( now );

                if ( span.Days >= 1 )
                {
                    return RelativeDateText( dateTime, includePreposition );
                }
                else if ( span.Hours >= 1 )
                {
                    return string.Format( "{2}{0} hour{1}", span.Hours, span.Hours > 1 ? "s" : string.Empty, includePreposition ? "in " : string.Empty );
                }
                else if ( span.Minutes >= 1 )
                {
                    return string.Format( "{2}{0} minute{1}", span.Minutes, span.Minutes > 1 ? "s" : string.Empty, includePreposition ? "in " : string.Empty );
                }
                else if ( span.Seconds >= 1 )
                {
                    return string.Format( "{2}{0} second{1}", span.Seconds, span.Seconds > 1 ? "s" : string.Empty, includePreposition ? "in " : string.Empty );
                }
                else
                {
                    return "Just now";
                }
            }
        }

        /// <summary>
        /// Convert the date into a relative text string. Expects to be prefixed with
        /// a verb such as "due" or "remind me".
        /// </summary>
        /// <param name="dateTime">The DateTime to convert.</param>
        /// <param name="includePreposition">If true then prepositions "in" and "on" will be included.</param>
        /// <returns>A string that represents the relative date.</returns>
        public static string RelativeDateText( DateTime dateTime, bool includePreposition = false )
        {
            if ( dateTime == null )
            {
                return string.Empty;
            }

            DateTime now = DateTime.Now.Date;
            DateTime date = dateTime.Date;

            if ( now == date )
            {
                return "Today";
            }
            else if ( date < now )
            {
                var span = now.Subtract( date );

                if ( span.Days == 1 )
                {
                    return "Yesterday";
                }
                else if ( span.Days <= 4 )
                {
                    return string.Format( "{0} days ago", span.Days );
                }
                else
                {
                    return string.Format( "{1}{0}", date.ToShortDateString(), includePreposition ? "on " : string.Empty );
                }
            }
            else
            {
                var span = date.Subtract( now );

                if ( span.Days == 1 )
                {
                    return "Tomorrow";
                }
                else if ( span.Days <= 4 )
                {
                    return string.Format( "{1}{0} days", span.Days, includePreposition ? "in " : string.Empty );
                }
                else
                {
                    return string.Format( "{1}{0}", date.ToShortDateString(), includePreposition ? "on " : string.Empty );
                }
            }
        }

        /// <summary>
        /// Convert the markdown into Html. This should be used anytime a project comment
        /// is to be displayed as it automatically converst #1234 references into links
        /// as well as fixes some standard formatting issues with images.
        /// </summary>
        /// <param name="markdown">The markdown text to be converted.</param>
        /// <returns>A string of HTML text that may be displayed.</returns>
        public static string ConvertMarkdownToHtml( string markdown )
        {
            Regex projectReference = new Regex( "\\B#(\\d+)\\b" );
            MatchEvaluator projectReferenceDelegate = ( Match m ) =>
            {
                return string.Format( "[#{0}]({1})", m.Groups[1].Value, GetProjectRoute( m.Groups[1].Value.AsInteger() ) );
            };

            var settings = CommonMark.CommonMarkSettings.Default.Clone();
            settings.OutputDelegate = ( doc, output, dSettings ) =>
            {
                new Support.MarkdownHtmlFormatter( output, dSettings ).WriteDocument( doc );
            };

            //
            // Convert all project references to links.
            //
            markdown = projectReference.Replace( markdown, projectReferenceDelegate );

            return CommonMark.CommonMarkConverter.Convert( markdown, settings );
        }

        /// <summary>
        /// Gets the full URL route for the given project Id.
        /// </summary>
        /// <param name="projectId">The identifier of the project whose route we need.</param>
        /// <returns>A string that represents the full absolute URL of the project.</returns>
        public static string GetProjectRoute( int projectId )
        {
            return string.Format( "{0}{1}/{2}",
                Rock.Web.Cache.GlobalAttributesCache.Value( "InternalApplicationRoot" ),
                BaseRoute, projectId );
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

        /// <summary>
        /// Perform an OR operation on the expressions. Useful when building expressions dynamically.
        /// </summary>
        /// <typeparam name="T">The entity type to operate on.</typeparam>
        /// <param name="source">The source of this extension method.</param>
        /// <param name="predicates">The predicates to be combined in an OR clause.</param>
        /// <returns>A queryable that has had this WhereAny() applied.</returns>
        public static IQueryable<T> WhereAny<T>( this IQueryable<T> source, IList<Expression<Func<T, bool>>> predicates )
        {
            if ( predicates.Count == 0 )
            {
                return source.Where( t => false );
            }

            if ( predicates.Count == 1 )
            {
                return source.Where( predicates[0] );
            }

            Expression body = predicates[0].Body;
            for ( int i = 1; i < predicates.Count; i++ )
            {
                body = Expression.Or( body, predicates[i].Body );
            }

            var param = Expression.Parameter( typeof( T ) );
            body = new ParameterReplacer( param ).Visit( body );

            return source.Where( Expression.Lambda<Func<T, bool>>( body, param ) );
        }

        /// <summary>
        /// Helper class for the WhereAny extension.
        /// </summary>
        private class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _parameter;

            protected override Expression VisitParameter( ParameterExpression node )
            {
                return base.VisitParameter( _parameter );
            }

            internal ParameterReplacer( ParameterExpression parameter )
            {
                _parameter = parameter;
            }
        }

    }

}
