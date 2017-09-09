using System.Web.Http;

using Rock.Rest;

namespace com.blueboxmoon.ProjectManagement.Rest
{
    public class BBM_ProjectManagement_UtilityController : ApiControllerBase
    {
        #region API Methods

        /// <summary>
        /// Retrieve the HTML for the comment that is being written.
        /// </summary>
        /// <param name="markdown">The markdown text to be converted.</param>
        /// <returns>An HTML formatted string.</returns>
        [HttpPost]
        [System.Web.Http.Route( "api/BBM_ProjectManagement_Utility/PreviewComment" )]
        public string GetPreviewComment( [FromBody]string markdown )
        {
            return Utility.ConvertMarkdownToHtml( markdown );
        }

        #endregion

        #region Internal Methods

        #endregion
    }
}
