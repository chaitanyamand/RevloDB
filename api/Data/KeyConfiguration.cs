using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RevloDB.Entities;

namespace RevloDB.Data.Configurations
{
    public class KeyConfiguration : IEntityTypeConfiguration<Key>
    {
        public void Configure(EntityTypeBuilder<Key> builder)
        {
            builder.ToTable("keys");

            builder.Property(k => k.Id).HasColumnName("id");

            // Properties
            builder.Property(k => k.KeyName)
                .HasColumnName("key_name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(k => k.CurrentVersionId)
                .HasColumnName("current_version_id");

            builder.Property(k => k.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            builder.Property(k => k.IsDeleted)
                .HasColumnName("is_deleted")
                .HasDefaultValue(false);

            builder.Property(k => k.NamespaceId)
                .HasColumnName("namespace_id");

            // Indexes
            builder.HasIndex(k => k.CurrentVersionId)
                .HasDatabaseName("ix_keys_current_version_id");

            builder.HasIndex(k => new { k.KeyName, k.NamespaceId })
                .IsUnique()
                .HasFilter("is_deleted = FALSE")
                .HasDatabaseName("ix_keys_unique_active_key_name_namespace_id");

            builder.HasIndex(k => k.IsDeleted)
                .HasFilter("is_deleted = TRUE")
                .HasDatabaseName("ix_keys_is_deleted_true");

            builder.HasIndex(k => k.NamespaceId)
                .HasDatabaseName("ix_keys_namespace_id");

            // Relationships
            builder.HasOne(k => k.CurrentVersion)
                .WithMany()
                .HasForeignKey(k => k.CurrentVersionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_keys_versions_current_version_id");

            builder.HasMany(k => k.Versions)
                .WithOne(v => v.Key)
                .HasForeignKey(v => v.KeyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_versions_keys_key_id");

            builder.HasOne(k => k.Namespace)
                .WithMany(n => n.Keys)
                .HasForeignKey(k => k.NamespaceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_keys_namespaces_namespace_id");
        }
    }
}