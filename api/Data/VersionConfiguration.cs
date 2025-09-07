using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Version = RevloDB.Entities.Version;

namespace RevloDB.Data.Configurations
{
    public class VersionConfiguration : IEntityTypeConfiguration<Version>
    {
        public void Configure(EntityTypeBuilder<Version> builder)
        {
            builder.ToTable("versions");

            builder.Property(v => v.Id).HasColumnName("id");

            // Properties
            builder.Property(v => v.Value)
                .HasColumnName("value")
                .IsRequired();

            builder.Property(v => v.Timestamp)
                .HasColumnName("timestamp")
                .HasDefaultValueSql("NOW()");

            builder.Property(v => v.VersionNumber)
                .HasColumnName("version_number");

            builder.Property(v => v.KeyId)
                .HasColumnName("key_id");

            // Indexes
            builder.HasIndex(v => new { v.KeyId, v.VersionNumber })
                .IsUnique()
                .HasDatabaseName("ix_versions_key_id_version_number");

            builder.HasIndex(v => v.KeyId)
                .HasDatabaseName("ix_versions_key_id");
        }
    }
}