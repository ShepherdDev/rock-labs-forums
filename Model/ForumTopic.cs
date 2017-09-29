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
using Rock.Web.Cache;

namespace com.rocklabs.Forums.Model
{
    [Table( "_com_rocklabs_Forums_ForumTopic" )]
    [DataContract]
    public class ForumTopic : Model<ForumTopic>, IRockEntity
    {
        #region Entity Properties

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the category identifier that this topic belongs to.
        /// </summary>
        [DataMember]
        public int CategoryId { get; set; }

        #endregion

        #region Virtual Properties

        /// <summary>
        /// Gets or sets the <see cref="Model.Category"/>.
        /// </summary>
        [LavaInclude]
        public virtual Category Category { get; set; }

        /// <summary>
        /// Gets the parent security authority for this Attachment instance.
        /// </summary>
        public override ISecured ParentAuthority
        {
            get
            {
                return Category ?? base.ParentAuthority;
            }
        }

        #endregion
    }

    #region Entity Configuration

    /// <summary>
    /// Defines the Entity Framework configuration for the <see cref="ForumTopic"/>  model.
    /// </summary>
    public partial class ForumTopicConfiguration : EntityTypeConfiguration<ForumTopic>
    {
        public ForumTopicConfiguration()
        {
            this.HasRequired( c => c.Category )
                .WithMany()
                .HasForeignKey( c => c.CategoryId )
                .WillCascadeOnDelete( false );

            this.HasEntitySetName( "ForumTopic" );
        }
    }

    #endregion
}
