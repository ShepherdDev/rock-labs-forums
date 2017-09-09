using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Security;

namespace com.blueboxmoon.ProjectManagement.Model
{
    [Table( "_com_blueboxmoon_ProjectManagement_Attachment" )]
    [DataContract]
    public class Attachment : Model<Attachment>, IRockEntity
    {
        #region Entity Properties

        /// <summary>
        /// Gets or sets the project identifier that this task belongs to.
        /// </summary>
        [DataMember]
        public int? ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the binary file identifier. 
        /// </summary>
        [DataMember]
        public int BinaryFileId { get; set; }

        #endregion

        #region Virtual Properties

        /// <summary>
        /// Gets or sets the <see cref="Model.Project"/>.
        /// </summary>
        [LavaInclude]
        public virtual Project Project { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.BinaryFile"/>.
        /// </summary>
        [LavaInclude]
        public virtual BinaryFile BinaryFile { get; set; }

        /// <summary>
        /// Gets the parent security authority for this Attachment instance.
        /// </summary>
        public override ISecured ParentAuthority
        {
            get
            {
                return Project ?? base.ParentAuthority;
            }
        }

        #endregion
    }

    #region Entity Configuration

    /// <summary>
    /// Defines the Entity Framework configuration for the <see cref="Attachment"/>  model.
    /// </summary>
    public partial class AttachmentConfiguration : EntityTypeConfiguration<Attachment>
    {
        public AttachmentConfiguration()
        {
            this.HasOptional( c => c.Project )
                .WithMany( p => p.Attachments )
                .HasForeignKey( c => c.ProjectId )
                .WillCascadeOnDelete( false );

            this.HasRequired( c => c.BinaryFile )
                .WithMany()
                .HasForeignKey( c => c.BinaryFileId )
                .WillCascadeOnDelete( true );

            this.HasEntitySetName( "Attachment" );
        }
    }

    #endregion
}
