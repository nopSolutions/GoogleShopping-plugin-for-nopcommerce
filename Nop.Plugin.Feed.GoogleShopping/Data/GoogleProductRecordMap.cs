using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Data.Mapping;
using Nop.Plugin.Feed.GoogleShopping.Domain;

namespace Nop.Plugin.Feed.GoogleShopping.Data
{
    public partial class GoogleProductRecordMap : NopEntityTypeConfiguration<GoogleProductRecord>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<GoogleProductRecord> builder)
        {
            builder.ToTable(nameof(GoogleProductRecord));
            builder.HasKey(x => x.Id);            

            base.Configure(builder);
        }
    }
}