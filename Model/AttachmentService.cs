using System;
using System.Linq;

using Rock.Data;

namespace com.blueboxmoon.ProjectManagement.Model
{
    /// <summary>
    /// Attachment Service class.
    /// </summary>
    public class AttachmentService : Service<Attachment>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentService"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public AttachmentService( RockContext context ) : base( context ) { }
    }
}