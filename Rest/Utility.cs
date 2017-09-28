using System.Web.Http;

using Rock.Rest;

namespace com.rocklabs.Forums.Rest
{
    public class RockLabs_Forums_UtilityController : ApiControllerBase
    {
        #region API Methods

        /// <summary>
        /// Retrieve the HTML for the comment that is being written.
        /// </summary>
        /// <param name="markdown">The markdown text to be converted.</param>
        /// <returns>An HTML formatted string.</returns>
        [HttpPost]
        [System.Web.Http.Route( "api/RockLabs_Forums_Utility/MarkdownToHtml" )]
        public string MarkdownToHtml( [FromBody]string markdown )
        {
            return Utility.ConvertMarkdownToHtml( markdown );
        }

        #endregion

        #region Internal Methods

        #endregion
    }
}
