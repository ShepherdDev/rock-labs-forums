using System;
using System.Linq;

using Rock.Data;

namespace com.rocklabs.Forums.Model
{
    /// <summary>
    /// ForumTopic Service class.
    /// </summary>
    public class ForumTopicService : Service<ForumTopic>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ForumTopicService"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public ForumTopicService( RockContext context ) : base( context ) { }
    }
}